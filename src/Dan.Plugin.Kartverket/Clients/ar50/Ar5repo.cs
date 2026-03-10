using Dan.Plugin.Kartverket.Config;
using Dapper;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Dan.Plugin.Kartverket.Clients.ar50.Ar5repo;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public class Ar5repo : IAr5Repo
    {
        private readonly NpgsqlDataSource _dataSource;

        public Ar5repo(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<List<Ar5OmradeDbModel>> GetOmrade(string coordinates)
        {
            if (string.IsNullOrWhiteSpace(coordinates))
                throw new ArgumentException("Coordinates required");

            coordinates = ConvertCoordinates(coordinates);
            coordinates = coordinates.Replace(" ", "");
            var cords = coordinates.Split(',');

            if (cords.Length % 2 != 0)
                throw new ArgumentException("Invalid coordinate format");

            //4326 is the Spatial Reference System Identifier (SRID) for WGS 84,
            //common coordinate system used for geographic data.
            //It represents coordinates in terms of latitude and longitude.
            //By using SRID 4326, the code ensures that the input geometry is correctly interpreted as geographic coordinates,
            //allowing for accurate spatial operations and queries against the database that also uses this coordinate system.
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            Geometry inputGeometry;

            // 📍 ONE POINT
            if (cords.Length == 2)
            {
                var longitute = double.Parse(cords[0], CultureInfo.InvariantCulture);
                var latitude = double.Parse(cords[1], CultureInfo.InvariantCulture);

                inputGeometry = geometryFactory.CreatePoint(new Coordinate(longitute, latitude));
            }
            // 🔷 POLYGON
            else if (cords.Length >= 6)
            {
                var coordinatesList = new List<Coordinate>();

                for (int i = 0; i < cords.Length; i += 2)
                {
                    var longitude = double.Parse(cords[i], CultureInfo.InvariantCulture);
                    var latitude = double.Parse(cords[i + 1], CultureInfo.InvariantCulture);
                    coordinatesList.Add(new Coordinate(longitude, latitude));
                }

                if (!coordinatesList.First().Equals2D(coordinatesList.Last()))
                    coordinatesList.Add(coordinatesList.First());

                inputGeometry = geometryFactory.CreatePolygon(coordinatesList.ToArray());
            }
            else
            {
                throw new ArgumentException("At least 3 coordinate pairs required for polygon");
            }
                      
            await using var connection = await _dataSource.OpenConnectionAsync();

            //Checks if the geometry intersects with any of the areas in the fkb_ar5_omrade table,
            //if so, retrieves the relevant information about those areas.
            //25533 is an EPSG-code for a specific coordinate reference system (CRS) used in Norway, known as EUREF89 / UTM zone 33N.
            string sql = @"
                            WITH eiendom AS (
                                SELECT ST_Transform(ST_SetSRID(ST_GeomFromText(@geom), 4326), 25833) AS shape
                            )
                            SELECT
                                o.objectid AS ""Objectid"",
                                o.lokalid AS ""LokalId"",
                                o.arealtype AS ""ArealType"",
                                o.shape_area AS ""ShapeArea"",
                                o.shape_length AS ""ShapeLength"",
                                ST_AsText(o.shape) AS ""Shape""
                            FROM fkb_ar5_omrade o, eiendom e
                            WHERE ST_Intersects(o.shape, e.shape)
                            ORDER BY ST_Area(ST_Intersection(o.shape, e.shape)) DESC;";

            var results = (await connection.QueryAsync<Ar5OmradeDbModel>(
                sql,
                new { geom = inputGeometry.AsText() }
            )).ToList();


            return results;
        }

        // use . instead of , for coordinates for better seperation in sql query
        private string ConvertCoordinates(string coordinates)
        {
            coordinates = coordinates.Replace(" ", "");

            var regex = new System.Text.RegularExpressions.Regex(@"(\d+),(\d+)");
            coordinates = regex.Replace(coordinates, "$1.$2");
            return coordinates;
        }

        public interface IAr5Repo
        {
            Task<List<Ar5OmradeDbModel>> GetOmrade(string coordinates);
        }

    }
}
