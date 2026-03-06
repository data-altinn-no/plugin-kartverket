using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Dan.Plugin.Kartverket.Clients.ar50.ar50repo;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public class ar50repo : Iar50Repo
    {
        private readonly string ConnectionString;
        private readonly ApplicationSettings _settings;

        public ar50repo(IOptions<ApplicationSettings> settings)
        {
            _settings = settings.Value;
            ConnectionString = _settings.ConnectionString;
        }

        public async Task<List<ar5OmradeDbModel>> GetOmrade(string coordinates)
        {
            if (string.IsNullOrWhiteSpace(coordinates))
                throw new ArgumentException("Coordinates required");

            coordinates = ConvertCoordinates(coordinates);
            coordinates = coordinates.Replace(" ", "");
            var cords = coordinates.Split(',');

            if (cords.Length % 2 != 0)
                throw new ArgumentException("Invalid coordinate format");

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            Geometry inputGeometry;

            // 📍 ONE POINT
            if (cords.Length == 2)
            {
                var lon = double.Parse(cords[0], CultureInfo.InvariantCulture);
                var lat = double.Parse(cords[1], CultureInfo.InvariantCulture);

                inputGeometry = geometryFactory.CreatePoint(new Coordinate(lon, lat));
            }
            // 🔷 POLYGON
            else if (cords.Length >= 6)
            {
                var coordinatesList = new List<Coordinate>();

                for (int i = 0; i < cords.Length; i += 2)
                {
                    var lon = double.Parse(cords[i], CultureInfo.InvariantCulture);
                    var lat = double.Parse(cords[i + 1], CultureInfo.InvariantCulture);
                    coordinatesList.Add(new Coordinate(lon, lat));
                }

                if (!coordinatesList.First().Equals2D(coordinatesList.Last()))
                    coordinatesList.Add(coordinatesList.First());

                inputGeometry = geometryFactory.CreatePolygon(coordinatesList.ToArray());
            }
            else
            {
                throw new ArgumentException("At least 3 coordinate pairs required for polygon");
            }

            var results = new List<ar5OmradeDbModel>();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
            dataSourceBuilder.UseNetTopologySuite();
            await using var dataSource = dataSourceBuilder.Build();
            await using var connection = await dataSource.OpenConnectionAsync();

            string sql = @"
                        WITH eiendom AS (
                            SELECT ST_Transform(@geom, 25833) AS shape
                        )
                        SELECT o.*, ST_AsText(o.shape) AS shape_wkt
                        FROM fkb_ar5_omrade o, eiendom e
                        WHERE ST_Intersects(o.shape, e.shape)
                        ORDER BY ST_Area(ST_Intersection(o.shape, e.shape)) DESC";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("geom", inputGeometry);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var result = new ar5OmradeDbModel
                {
                    Objectid = reader.GetInt32(reader.GetOrdinal("objectid")),
                    Objtype = reader["objtype"] as string,
                    Lokalid = reader["lokalid"] as string,
                    Informasjon = reader["informasjon"] as string,
                    ArealType = reader["arealType"] as string,
                    Treslag = reader["treslag"] as string,
                    Skogbonitet = reader["skogbonitet"] as string,
                    Grunnforhold = reader["grunnforhold"] as string,
                    Klassifiseringsmetode = reader["klassifiseringsmetode"] as string,
                    ShapeLength = reader.IsDBNull(reader.GetOrdinal("shape_length")) ? 0 : reader.GetDouble(reader.GetOrdinal("shape_length")),
                    ShapeArea = reader.IsDBNull(reader.GetOrdinal("shape_area")) ? 0 : reader.GetDouble(reader.GetOrdinal("shape_area")),
                    Shape = reader["shape_wkt"] as string
                };

                results.Add(result);
            }

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

        public interface Iar50Repo
        {
            Task<List<ar5OmradeDbModel>> GetOmrade(string coordinates);
        }

    }
}
