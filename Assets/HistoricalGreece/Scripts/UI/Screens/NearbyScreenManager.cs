using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using HistoricalGreece.Core;
using HistoricalGreece.Location;
using HistoricalGreece.AR;

namespace HistoricalGreece.UI.Screens
{
    /// <summary>
    /// Manages the Nearby tab — shows historical sites sorted by distance
    /// from the user's current GPS position. Updates live as the user moves.
    /// Highlights sites within activation radius with a prominent "Start AR" button.
    /// </summary>
    public class NearbyScreenManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SiteDatabase m_SiteDatabase;

        [Header("Services")]
        [SerializeField] private GPSLocationService m_LocationService;
        [SerializeField] private ProximityDetector m_ProximityDetector;

        [Header("UI References")]
        [Tooltip("Scroll view content container for nearby site cards")]
        [SerializeField] private Transform m_CardContainer;

        [Tooltip("Prefab for a nearby site card (includes distance indicator)")]
        [SerializeField] private GameObject m_NearbySiteCardPrefab;

        [Header("Status Panels")]
        [Tooltip("Shown while GPS is initializing")]
        [SerializeField] private GameObject m_LoadingPanel;

        [Tooltip("Shown when GPS permission is denied")]
        [SerializeField] private GameObject m_PermissionDeniedPanel;

        [Tooltip("Shown when GPS fails")]
        [SerializeField] private GameObject m_ErrorPanel;

        [Tooltip("Shown when no sites are nearby")]
        [SerializeField] private GameObject m_EmptyPanel;

        [Header("Active Site Banner")]
        [Tooltip("Prominent banner shown when user is within a site's activation radius")]
        [SerializeField] private GameObject m_ActiveSiteBanner;

        [SerializeField] private TMP_Text m_ActiveSiteName;
        [SerializeField] private TMP_Text m_ActiveSiteTagline;

        [Header("Navigation")]
        [SerializeField] private AppNavigationManager m_NavigationManager;
        [SerializeField] private ARExperienceManager m_ARExperienceManager;

        // --- Internal ---
        private readonly List<GameObject> m_SpawnedCards = new List<GameObject>();

        /// <summary>Callback when user taps a site for details.</summary>
        public System.Action<HistoricalSite> OnSiteSelected;

        // --- Lifecycle ---

        private void OnEnable()
        {
            // Subscribe to proximity updates
            if (m_ProximityDetector != null)
            {
                m_ProximityDetector.OnNearbySitesUpdated += HandleNearbySitesUpdated;
                m_ProximityDetector.OnClosestActiveSiteChanged += HandleClosestActiveSiteChanged;
            }

            if (m_LocationService != null)
            {
                m_LocationService.OnStatusChanged += HandleLocationStatusChanged;
            }

            // Initial UI state
            UpdateStatusPanels();
        }

        private void OnDisable()
        {
            if (m_ProximityDetector != null)
            {
                m_ProximityDetector.OnNearbySitesUpdated -= HandleNearbySitesUpdated;
                m_ProximityDetector.OnClosestActiveSiteChanged -= HandleClosestActiveSiteChanged;
            }

            if (m_LocationService != null)
            {
                m_LocationService.OnStatusChanged -= HandleLocationStatusChanged;
            }
        }

        // --- Event Handlers ---

        private void HandleLocationStatusChanged(GPSServiceStatus status)
        {
            UpdateStatusPanels();
        }

        private void HandleNearbySitesUpdated(IReadOnlyList<SiteWithDistance> nearbySites)
        {
            RebuildCards(nearbySites);
        }

        private void HandleClosestActiveSiteChanged(HistoricalSite site)
        {
            UpdateActiveSiteBanner(site);
        }

        // --- UI Updates ---

        private void UpdateStatusPanels()
        {
            if (m_LocationService == null) return;

            bool isLoading = m_LocationService.Status == GPSServiceStatus.Initializing ||
                             m_LocationService.Status == GPSServiceStatus.RequestingPermission;
            bool isDenied = m_LocationService.Status == GPSServiceStatus.PermissionDenied;
            bool isFailed = m_LocationService.Status == GPSServiceStatus.Failed;
            bool isRunning = m_LocationService.Status == GPSServiceStatus.Running;

            if (m_LoadingPanel != null) m_LoadingPanel.SetActive(isLoading);
            if (m_PermissionDeniedPanel != null) m_PermissionDeniedPanel.SetActive(isDenied);
            if (m_ErrorPanel != null) m_ErrorPanel.SetActive(isFailed);

            // Card container only visible when GPS is running
            if (m_CardContainer != null)
            {
                m_CardContainer.gameObject.SetActive(isRunning);
            }
        }

        private void RebuildCards(IReadOnlyList<SiteWithDistance> nearbySites)
        {
            // Clear existing
            foreach (var card in m_SpawnedCards)
            {
                if (card != null) Destroy(card);
            }
            m_SpawnedCards.Clear();

            // Empty state
            if (m_EmptyPanel != null)
            {
                m_EmptyPanel.SetActive(nearbySites.Count == 0);
            }

            if (m_NearbySiteCardPrefab == null || m_CardContainer == null) return;

            // Spawn cards for each nearby site
            foreach (var siteWithDist in nearbySites)
            {
                var cardObj = Instantiate(m_NearbySiteCardPrefab, m_CardContainer);
                var card = cardObj.GetComponent<SiteCard>();
                if (card != null)
                {
                    bool isInRange = m_ProximityDetector.ActiveSites.Contains(siteWithDist.site);
                    card.Setup(siteWithDist.site, OnCardDetailsTapped, OnCardARTapped);
                    card.SetDistance(siteWithDist.FormattedDistance);
                    card.SetInRange(isInRange);
                }
                m_SpawnedCards.Add(cardObj);
            }
        }

        private void UpdateActiveSiteBanner(HistoricalSite site)
        {
            if (m_ActiveSiteBanner == null) return;

            if (site != null)
            {
                m_ActiveSiteBanner.SetActive(true);
                if (m_ActiveSiteName != null) m_ActiveSiteName.text = site.siteName;
                if (m_ActiveSiteTagline != null) m_ActiveSiteTagline.text = site.tagline;
            }
            else
            {
                m_ActiveSiteBanner.SetActive(false);
            }
        }

        // --- Card Callbacks ---

        private void OnCardDetailsTapped(HistoricalSite site)
        {
            OnSiteSelected?.Invoke(site);
            if (m_NavigationManager != null)
            {
                m_NavigationManager.ShowSiteDetail();
            }
        }

        private void OnCardARTapped(HistoricalSite site)
        {
            if (m_ARExperienceManager == null) return;

            // If user is within activation radius → on-location mode
            // Otherwise → preview mode
            bool isOnLocation = m_ProximityDetector != null &&
                                m_ProximityDetector.ActiveSites.Contains(site);

            if (isOnLocation)
                m_ARExperienceManager.StartOnLocationExperience(site);
            else
                m_ARExperienceManager.StartPreviewExperience(site);

            if (m_NavigationManager != null)
            {
                m_NavigationManager.EnterARExperience();
            }
        }

        // --- Public API ---

        /// <summary>
        /// Launch the AR experience for the active site banner.
        /// Called by the "Start AR" button on the banner.
        /// </summary>
        public void LaunchActiveSiteAR()
        {
            if (m_ProximityDetector?.ClosestActiveSite == null) return;
            OnCardARTapped(m_ProximityDetector.ClosestActiveSite);
        }

        /// <summary>
        /// Open device location settings (for when permission is denied).
        /// </summary>
        public void OpenLocationSettings()
        {
#if UNITY_ANDROID
            try
            {
                using var intent = new AndroidJavaObject("android.content.Intent",
                    "android.settings.LOCATION_SOURCE_SETTINGS");
                using var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("startActivity", intent);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NearbyScreen] Could not open location settings: {e.Message}");
            }
#elif UNITY_IOS
            // On iOS, we can only point users to the general Settings app
            Application.OpenURL("app-settings:");
#endif
        }
    }
}
