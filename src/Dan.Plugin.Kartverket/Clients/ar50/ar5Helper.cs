using OSGeo.OGR;
using OSGeo.OSR;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public static class ar5Helper
    {

        public static Geometry GeometryToWGS84 (
            Geometry geometry,
            SpatialReference spatialReference)
        {
            geometry.ExportToWkt(out var wkt);
            var result = Geometry.CreateFromWkt(wkt);

            var destination = new SpatialReference("");
            destination.ImportFromEPSG(4326);

            // GDAL 3 swapped coordinates. See https://github.com/OSGeo/gdal/issues/1546
            destination.SetAxisMappingStrategy(OSGeo.OSR.AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

            var transformer = new OSGeo.OSR.CoordinateTransformation(spatialReference, destination);
            result.Transform(transformer);

            return result;
        }
    }
}
