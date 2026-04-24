using Dapper;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public class Ar5repo : IAr5Repo
    {
        private readonly NpgsqlDataSource _dataSource;

        public Ar5repo(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<List<Ar5OmradeDbModel>> GetOmrade(List<List<double>> coordinates)
        {
            if (coordinates == null || coordinates.Count < 1)
                throw new ArgumentException("Coordinates required");

            //4326 is the Spatial Reference System Identifier (SRID) for WGS 84,
            //common coordinate system used for geographic data.
            //It represents coordinates in terms of latitude and longitude.
            //By using SRID 4326, the code ensures that the input geometry is correctly interpreted as geographic coordinates,
            //allowing for accurate spatial operations and queries against the database that also uses this coordinate system.
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            Geometry inputGeometry;

            // 📍 ONE POINT
            if (coordinates.Count == 1)
            {
                var point = coordinates[0];
                if (point.Count != 2)
                    throw new ArgumentException("Each coordinate must have exactly 2 values [longitude, latitude]");

                inputGeometry = geometryFactory.CreatePoint(new Coordinate(point[0], point[1]));
            }
            // 🔷 POLYGON
            else if (coordinates.Count >= 3)
            {
                var coordinatesList = new List<Coordinate>();

                foreach (var point in coordinates)
                {
                    if (point.Count != 2)
                        throw new ArgumentException("Each coordinate must have exactly 2 values [longitude, latitude]");

                    coordinatesList.Add(new Coordinate(point[0], point[1]));
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
            //25833 is an EPSG-code for a specific coordinate reference system (CRS) used in Norway, known as EUREF89 / UTM zone 33N.
            //4326 is the Spatial Reference System Identifier (SRID) for WGS 84,
            //4258 is the EPSG coordinate reference system used for the returned GeoJSON coordinates.
            string sql = @"
                        WITH eiendom AS (
                            SELECT ST_Transform(
                                ST_SetSRID(ST_GeomFromText(@geom), 4326),
                                25833
                            ) AS shape
                        )
                        SELECT
                            o.objectid AS ""Objectid"",
                            o.lokalid AS ""LokalId"",
                            o.arealtype AS ""ArealType"",
                            o.shape_area AS ""ShapeArea"",
                            o.shape_length AS ""ShapeLength"",
                            ST_AsGeoJSON(ST_Transform(o.shape, 4258)) AS ""GeoJson""
                        FROM fkb_ar5_omrade o, eiendom e
                        WHERE ST_Intersects(o.shape, e.shape)
                        ORDER BY ST_Area(ST_Intersection(o.shape, e.shape)) DESC;";

            var results = (await connection.QueryAsync<Ar5OmradeDbModel>(
                sql,
                new { geom = inputGeometry.AsText() }
            )).ToList();


            return results;
        }
    }

    public interface IAr5Repo
    {
        Task<List<Ar5OmradeDbModel>> GetOmrade(List<List<double>> coordinates);
    }

}
