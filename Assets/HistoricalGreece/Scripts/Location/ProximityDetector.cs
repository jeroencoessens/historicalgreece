using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HistoricalGreece.Core;

namespace HistoricalGreece.Location
{
    /// <summary>
    /// Monitors the user's GPS position against curated historical sites.
    /// Fires events when the user enters or exits a site's activation radius.
    /// Maintains a sorted list of nearby sites for the "Nearby" screen.
    /// </summary>
    public class ProximityDetector : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The GPS location service to read positions from")]
        [SerializeField] private GPSLocationService m_LocationService;

        [Tooltip("The database of all historical sites")]
        [SerializeField] private SiteDatabase m_SiteDatabase;

        [Header("Configuration")]
        [Tooltip("How often to re-check proximity (seconds). Lower = more responsive but more CPU.")]
        [SerializeField] private float m_CheckIntervalSeconds = 5f;

        [Tooltip("Maximum distance in km to include in the 'nearby' list")]
        [SerializeField] private float m_NearbyRadiusKm = 50f;

        [Tooltip("Number of closest sites to track in the nearby list")]
        [SerializeField] private int m_MaxNearbySites = 20;

        // --- Public State ---

        /// <summary>Sites currently within their activation radius (user is "at" the site).</summary>
        public IReadOnlyList<HistoricalSite> ActiveSites => m_ActiveSites;

        /// <summary>Sites sorted by distance, within the nearby radius.</summary>
        public IReadOnlyList<SiteWithDistance> NearbySites => m_NearbySites;

        /// <summary>The single closest site in activation range, or null.</summary>
        public HistoricalSite ClosestActiveSite { get; private set; }

        // --- Events ---

        /// <summary>Fires when the user enters a site's activation radius. Perfect for notifications.</summary>
        public event Action<HistoricalSite> OnEnteredSiteRadius;

        /// <summary>Fires when the user leaves a site's activation radius.</summary>
        public event Action<HistoricalSite> OnExitedSiteRadius;

        /// <summary>Fires when the nearby sites list is refreshed (e.g., for UI updates).</summary>
        public event Action<IReadOnlyList<SiteWithDistance>> OnNearbySitesUpdated;

        /// <summary>Fires when there's a new closest active site (or it becomes null).</summary>
        public event Action<HistoricalSite> OnClosestActiveSiteChanged;

        // --- Internal ---
        private readonly List<HistoricalSite> m_ActiveSites = new List<HistoricalSite>();
        private List<SiteWithDistance> m_NearbySites = new List<SiteWithDistance>();
        private Coroutine m_ProximityCoroutine;

        // --- Lifecycle ---

        private void OnEnable()
        {
            if (m_LocationService != null)
            {
                m_LocationService.OnLocationUpdated += HandleLocationUpdated;
                m_LocationService.OnStatusChanged += HandleLocationStatusChanged;
            }
        }

        private void OnDisable()
        {
            if (m_LocationService != null)
            {
                m_LocationService.OnLocationUpdated -= HandleLocationUpdated;
                m_LocationService.OnStatusChanged -= HandleLocationStatusChanged;
            }

            if (m_ProximityCoroutine != null)
            {
                StopCoroutine(m_ProximityCoroutine);
                m_ProximityCoroutine = null;
            }
        }

        // --- Event Handlers ---

        private void HandleLocationStatusChanged(GPSServiceStatus status)
        {
            if (status == GPSServiceStatus.Running)
            {
                StartProximityChecks();
            }
            else
            {
                StopProximityChecks();
            }
        }

        private void HandleLocationUpdated(double lat, double lon, float accuracy)
        {
            // Immediate check on significant location change
            // The periodic coroutine handles regular interval checks
        }

        // --- Proximity Checking ---

        private void StartProximityChecks()
        {
            if (m_ProximityCoroutine != null) return;
            m_ProximityCoroutine = StartCoroutine(ProximityCheckRoutine());
        }

        private void StopProximityChecks()
        {
            if (m_ProximityCoroutine != null)
            {
                StopCoroutine(m_ProximityCoroutine);
                m_ProximityCoroutine = null;
            }
        }

        private IEnumerator ProximityCheckRoutine()
        {
            while (true)
            {
                if (m_LocationService.IsLocationAvailable && m_SiteDatabase != null)
                {
                    PerformProximityCheck();
                }

                yield return new WaitForSeconds(m_CheckIntervalSeconds);
            }
        }

        /// <summary>
        /// Core proximity logic. Checks all sites against current GPS position.
        /// </summary>
        private void PerformProximityCheck()
        {
            double userLat = m_LocationService.Latitude;
            double userLon = m_LocationService.Longitude;

            // 1. Update nearby sites list
            m_NearbySites = m_SiteDatabase.GetSitesByProximity(userLat, userLon, m_NearbyRadiusKm);
            if (m_NearbySites.Count > m_MaxNearbySites)
            {
                m_NearbySites = m_NearbySites.GetRange(0, m_MaxNearbySites);
            }
            OnNearbySitesUpdated?.Invoke(m_NearbySites);

            // 2. Check activation radii
            var sitesInRange = m_SiteDatabase.GetSitesInActivationRange(userLat, userLon);

            // Detect newly entered sites
            foreach (var site in sitesInRange)
            {
                if (!m_ActiveSites.Contains(site))
                {
                    m_ActiveSites.Add(site);
                    Debug.Log($"[ProximityDetector] Entered radius of: {site.siteName}");
                    OnEnteredSiteRadius?.Invoke(site);
                }
            }

            // Detect exited sites
            for (int i = m_ActiveSites.Count - 1; i >= 0; i--)
            {
                if (!sitesInRange.Contains(m_ActiveSites[i]))
                {
                    var exitedSite = m_ActiveSites[i];
                    m_ActiveSites.RemoveAt(i);
                    Debug.Log($"[ProximityDetector] Exited radius of: {exitedSite.siteName}");
                    OnExitedSiteRadius?.Invoke(exitedSite);
                }
            }

            // 3. Update closest active site
            HistoricalSite newClosest = null;
            if (m_ActiveSites.Count > 0)
            {
                float minDist = float.MaxValue;
                foreach (var site in m_ActiveSites)
                {
                    float dist = GeoUtils.HaversineDistanceKm(userLat, userLon, site.latitude, site.longitude);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        newClosest = site;
                    }
                }
            }

            if (newClosest != ClosestActiveSite)
            {
                ClosestActiveSite = newClosest;
                OnClosestActiveSiteChanged?.Invoke(ClosestActiveSite);
            }
        }

        // --- Public API ---

        /// <summary>
        /// Force an immediate proximity check (e.g., when returning from background).
        /// </summary>
        public void ForceCheck()
        {
            if (m_LocationService != null && m_LocationService.IsLocationAvailable && m_SiteDatabase != null)
            {
                PerformProximityCheck();
            }
        }

        /// <summary>
        /// Get the distance in km from the user to a specific site.
        /// Returns -1 if location is unavailable.
        /// </summary>
        public float GetDistanceToSite(HistoricalSite site)
        {
            if (!m_LocationService.IsLocationAvailable) return -1f;
            return GeoUtils.HaversineDistanceKm(
                m_LocationService.Latitude, m_LocationService.Longitude,
                site.latitude, site.longitude);
        }

        /// <summary>
        /// Get the bearing in degrees from the user to a specific site.
        /// Returns -1 if location is unavailable.
        /// </summary>
        public float GetBearingToSite(HistoricalSite site)
        {
            if (!m_LocationService.IsLocationAvailable) return -1f;
            return GeoUtils.BearingDegrees(
                m_LocationService.Latitude, m_LocationService.Longitude,
                site.latitude, site.longitude);
        }
    }
}
