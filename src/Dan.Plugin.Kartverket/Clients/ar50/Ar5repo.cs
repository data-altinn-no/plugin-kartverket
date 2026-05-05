using Dapper;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Union;
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

        public async Task<List<Ar5OmradeDbModel>> GetOmrade(List<List<List<double>>> coordinates)
        {
            if (coordinates == null || coordinates.Count < 1)
                throw new ArgumentException("Coordinates required");

            //4326 is the Spatial Reference System Identifier (SRID) for WGS 84,
            //common coordinate system used for geographic data.
            //It represents coordinates in terms of latitude and longitude.
            //By using SRID 4326, the code ensures that the input geometry is correctly interpreted as geographic coordinates,
            //allowing for accurate spatial operations and queries against the database that also uses this coordinate system.
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);


            // Convert all inputs to polygons first
            var polygons = coordinates.Select(poly =>
            {
                var coords = poly.Select(p => new Coordinate(p[0], p[1])).ToArray();
                var ring = geometryFactory.CreateLinearRing(coords);
                return geometryFactory.CreatePolygon(ring);
            }).ToList();
            var unionAll = CascadedPolygonUnion.Union(polygons.Cast<Geometry>().ToList());


            // Finn hull
            var holes = polygons.Where(p =>
                polygons.Any(o =>
                    !ReferenceEquals(p, o) &&
                    o.Covers(p)
                )
            ).ToList();

            // Subtract hull
            Geometry result = unionAll;

            if (holes.Any())
            {
                var holeUnion = CascadedPolygonUnion.Union(holes.Cast<Geometry>().ToList());
                holeUnion = GeometryFixer.Fix(holeUnion);

                result = result.Difference(holeUnion);
            }
            else
            {
                result = unionAll;
            }

            result = GeometryFixer.Fix(result);

            await using var connection = await _dataSource.OpenConnectionAsync();

            //Checks if the geometry intersects with any of the areas in the fkb_ar5_omrade table,
            //if so, retrieves the relevant information about those areas.
            //25833 is an EPSG-code for a specific coordinate reference system (CRS) used in Norway, known as EUREF89 / UTM zone 33N.
            //4326 is the Spatial Reference System Identifier (SRID) for WGS 84,
            //4258 is the EPSG coordinate reference system used for the returned GeoJSON coordinates.
            string sql = @"
                        WITH eiendom AS (
                        SELECT CASE
                            WHEN ST_GeometryType(
                                ST_Transform(ST_SetSRID(ST_GeomFromText(@geom), 4326), 25833)
                            ) = 'ST_Polygon'
                        THEN ST_Transform(ST_SetSRID(ST_GeomFromText(@geom), 4326), 25833)
                        ELSE ST_Buffer(
                            ST_Transform(ST_SetSRID(ST_GeomFromText(@geom), 4326), 25833),
                            0
                        )
                        END AS shape
                    ),
                    intersections AS (
                        SELECT
                            o.objectid,
                            o.arealtype,
                            ST_Intersection(o.shape, e.shape) AS geom
                        FROM fkb_ar5_omrade o
                        JOIN eiendom e
                            ON ST_Intersects(o.shape, e.shape)
                    )
                    SELECT
                        objectid AS ""Objectid"",
                        arealtype AS ""ArealType"",
                        ST_Area(geom) AS ""ClippedArea"",
                        ST_AsGeoJSON(ST_Transform(geom, 4258)) AS ""GeoJson""
                    FROM intersections
                    -- ST_Area(geom) > 10 - Threshold of 10 m² filters out topology slivers from ST_Intersection
                    WHERE
                        NOT ST_IsEmpty(geom)";

            var results = (await connection.QueryAsync<Ar5OmradeDbModel>(
                sql,
                new { geom = result.AsText() }
            )).ToList();


            return results;
        }
    }

    public interface IAr5Repo
    {
        Task<List<Ar5OmradeDbModel>> GetOmrade(List<List<List<double>>> coordinates);
    }

}
