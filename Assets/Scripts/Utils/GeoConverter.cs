using UnityEngine;

namespace CampusNav.Utils
{
    /// <summary>
    /// Converts between real-world GPS (lat/lon) and the shared local XZ
    /// meter coordinate space used everywhere else in the project (waypoints,
    /// building footprints, the mini-map, the player marker).
    ///
    /// Uses an equirectangular flat-plane approximation, which is accurate to
    /// well under a meter of error at campus scale (a few km across) — far
    /// simpler than full geodesic math, and plenty good enough for walking
    /// navigation.
    ///
    /// +X = east, +Z = north, origin (0,0) = the campus's originLat/originLon.
    /// </summary>
    public static class GeoConverter
    {
        private const double EarthRadiusMeters = 6371000.0;

        /// <summary>
        /// Converts a GPS coordinate into local XZ meters relative to the
        /// given origin.
        /// </summary>
        public static Vector2 LatLonToLocal(double lat, double lon, double originLat, double originLon)
        {
            double latRad = originLat * Mathf.Deg2Rad;

            double dLat = (lat - originLat) * Mathf.Deg2Rad;
            double dLon = (lon - originLon) * Mathf.Deg2Rad;

            double z = dLat * EarthRadiusMeters;                       // north-south -> Z
            double x = dLon * EarthRadiusMeters * System.Math.Cos(latRad); // east-west -> X

            return new Vector2((float)x, (float)z);
        }

        /// <summary>
        /// Converts a local XZ meter position back into GPS lat/lon, given
        /// the same origin used for LatLonToLocal. Useful for debugging or
        /// for showing the player's local position on an external map.
        /// </summary>
        public static (double lat, double lon) LocalToLatLon(float x, float z, double originLat, double originLon)
        {
            double latRad = originLat * Mathf.Deg2Rad;

            double dLat = z / EarthRadiusMeters;
            double dLon = x / (EarthRadiusMeters * System.Math.Cos(latRad));

            double lat = originLat + dLat * Mathf.Rad2Deg;
            double lon = originLon + dLon * Mathf.Rad2Deg;

            return (lat, lon);
        }

        /// <summary>
        /// Straight-line distance in meters between two GPS points, via the
        /// haversine formula. Used sparingly (e.g. geofence checks) — most
        /// distance math in this project should happen in local XZ space
        /// using ordinary Vector2/Vector3 distance instead.
        /// </summary>
        public static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            double dLon = (lon2 - lon1) * Mathf.Deg2Rad;

            double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                       System.Math.Cos(lat1 * Mathf.Deg2Rad) * System.Math.Cos(lat2 * Mathf.Deg2Rad) *
                       System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
            double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));

            return EarthRadiusMeters * c;
        }
    }
}
