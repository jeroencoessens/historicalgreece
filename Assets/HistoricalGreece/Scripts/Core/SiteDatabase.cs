using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HistoricalGreece.Core
{
    /// <summary>
    /// Central database of all historical sites in the app.
    /// Create one instance via Assets > Create > Historical Greece > Site Database.
    /// Referenced by managers to query sites by proximity, region, period, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "SiteDatabase", menuName = "Historical Greece/Site Database", order = 1)]
    public class SiteDatabase : ScriptableObject
    {
        [Tooltip("All registered historical sites")]
        public List<HistoricalSite> allSites = new List<HistoricalSite>();

        /// <summary>
        /// Returns only sites marked as active/published.
        /// </summary>
        public IEnumerable<HistoricalSite> ActiveSites =>
            allSites.Where(s => s != null && s.isActive);

        /// <summary>
        /// Filter sites by civilization type (Greek, Roman, etc.)
        /// </summary>
        public IEnumerable<HistoricalSite> GetSitesByCivilization(CivilizationType civilization)
        {
            return ActiveSites.Where(s => s.civilization == civilization);
        }

        /// <summary>
        /// Filter sites by historical period.
        /// </summary>
        public IEnumerable<HistoricalSite> GetSitesByPeriod(HistoricalPeriod period)
        {
            return ActiveSites.Where(s => s.period == period);
        }

        /// <summary>
        /// Filter sites by category.
        /// </summary>
        public IEnumerable<HistoricalSite> GetSitesByCategory(SiteCategory category)
        {
            return ActiveSites.Where(s => s.category == category);
        }

        /// <summary>
        /// Filter sites by country.
        /// </summary>
        public IEnumerable<HistoricalSite> GetSitesByCountry(string country)
        {
            return ActiveSites.Where(s =>
                s.countryName.Equals(country, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Filter sites by region.
        /// </summary>
        public IEnumerable<HistoricalSite> GetSitesByRegion(string region)
        {
            return ActiveSites.Where(s =>
                s.regionName.Equals(region, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all unique region names for the filter UI.
        /// </summary>
        public IEnumerable<string> GetAllRegions()
        {
            return ActiveSites
                .Select(s => s.regionName)
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .OrderBy(r => r);
        }

        /// <summary>
        /// Get all unique country names for the filter UI.
        /// </summary>
        public IEnumerable<string> GetAllCountries()
        {
            return ActiveSites
                .Select(s => s.countryName)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c);
        }

        /// <summary>
        /// Returns sites sorted by distance from a GPS coordinate.
        /// Useful for the "Nearby" tab.
        /// </summary>
        public List<SiteWithDistance> GetSitesByProximity(double userLat, double userLon, float maxDistanceKm = float.MaxValue)
        {
            var results = new List<SiteWithDistance>();

            foreach (var site in ActiveSites)
            {
                float distKm = GeoUtils.HaversineDistanceKm(userLat, userLon, site.latitude, site.longitude);
                if (distKm <= maxDistanceKm)
                {
                    results.Add(new SiteWithDistance(site, distKm));
                }
            }

            results.Sort((a, b) => a.distanceKm.CompareTo(b.distanceKm));
            return results;
        }

        /// <summary>
        /// Find sites within activation radius of a GPS position.
        /// Used by ProximityDetector to trigger on-location AR.
        /// </summary>
        public List<HistoricalSite> GetSitesInActivationRange(double userLat, double userLon)
        {
            var results = new List<HistoricalSite>();

            foreach (var site in ActiveSites)
            {
                float distMeters = GeoUtils.HaversineDistanceKm(userLat, userLon, site.latitude, site.longitude) * 1000f;
                if (distMeters <= site.activationRadiusMeters)
                {
                    results.Add(site);
                }
            }

            return results;
        }

        /// <summary>
        /// Search sites by name or description keywords.
        /// </summary>
        public IEnumerable<HistoricalSite> SearchSites(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return ActiveSites;

            string lowerQuery = query.ToLowerInvariant();
            return ActiveSites.Where(s =>
                s.siteName.ToLowerInvariant().Contains(lowerQuery) ||
                s.description.ToLowerInvariant().Contains(lowerQuery) ||
                s.tagline.ToLowerInvariant().Contains(lowerQuery) ||
                s.regionName.ToLowerInvariant().Contains(lowerQuery));
        }
    }

    /// <summary>
    /// Wraps a HistoricalSite with its computed distance from the user.
    /// </summary>
    [System.Serializable]
    public struct SiteWithDistance
    {
        public HistoricalSite site;
        public float distanceKm;

        public SiteWithDistance(HistoricalSite site, float distanceKm)
        {
            this.site = site;
            this.distanceKm = distanceKm;
        }

        /// <summary>
        /// Returns a human-friendly distance string (e.g. "350 m" or "2.4 km")
        /// </summary>
        public string FormattedDistance
        {
            get
            {
                if (distanceKm < 1f)
                    return $"{Mathf.RoundToInt(distanceKm * 1000f)} m";
                else if (distanceKm < 100f)
                    return $"{distanceKm:F1} km";
                else
                    return $"{Mathf.RoundToInt(distanceKm)} km";
            }
        }
    }

    /// <summary>
    /// Geographic utility functions.
    /// </summary>
    public static class GeoUtils
    {
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// Calculates the great-circle distance between two GPS coordinates
        /// using the Haversine formula. Returns distance in kilometers.
        /// </summary>
        public static float HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                       System.Math.Cos(DegreesToRadians(lat1)) * System.Math.Cos(DegreesToRadians(lat2)) *
                       System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);

            double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
            return (float)(EarthRadiusKm * c);
        }

        /// <summary>
        /// Calculates the initial bearing from point 1 to point 2.
        /// Returns degrees from North (0-360).
        /// </summary>
        public static float BearingDegrees(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = DegreesToRadians(lon2 - lon1);
            double lat1Rad = DegreesToRadians(lat1);
            double lat2Rad = DegreesToRadians(lat2);

            double y = System.Math.Sin(dLon) * System.Math.Cos(lat2Rad);
            double x = System.Math.Cos(lat1Rad) * System.Math.Sin(lat2Rad) -
                       System.Math.Sin(lat1Rad) * System.Math.Cos(lat2Rad) * System.Math.Cos(dLon);

            double bearing = System.Math.Atan2(y, x);
            return (float)((RadiansToDegrees(bearing) + 360) % 360);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * System.Math.PI / 180.0;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / System.Math.PI;
        }
    }
}
