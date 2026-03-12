using System.Linq;
using UnityEngine;
using HistoricalGreece.Core;
using HistoricalGreece.Location;
using HistoricalGreece.AR;
using HistoricalGreece.UI;
using HistoricalGreece.UI.Screens;
using HistoricalGreece.UI.Components;

namespace HistoricalGreece
{
    /// <summary>
    /// Top-level application manager. Acts as the central coordinator
    /// connecting all subsystems: data, location, AR, and UI.
    ///
    /// This is the single entry point that bootstraps the app.
    /// Attach to a persistent root GameObject in the scene.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("The site database containing all curated historical sites")]
        [SerializeField] private SiteDatabase m_SiteDatabase;

        [Header("Core Services")]
        [SerializeField] private GPSLocationService m_LocationService;
        [SerializeField] private ProximityDetector m_ProximityDetector;
        [SerializeField] private ARExperienceManager m_ARExperienceManager;

        [Header("UI")]
        [SerializeField] private AppNavigationManager m_NavigationManager;
        [SerializeField] private ExploreScreenManager m_ExploreScreen;
        [SerializeField] private NearbyScreenManager m_NearbyScreen;
        [SerializeField] private SiteDetailScreenManager m_SiteDetailScreen;
        [SerializeField] private ARHUDScreenManager m_ARHUDScreen;
        [SerializeField] private NotificationBanner m_NotificationBanner;

        [Header("First Launch")]
        [Tooltip("Has the user completed onboarding? Uses PlayerPrefs.")]
        private const string k_OnboardingCompleteKey = "HistoricalGR_OnboardingComplete";

        [Tooltip("Show welcome screen on first launch")]
        [SerializeField] private bool m_ShowWelcomeOnFirstLaunch = true;

        // --- Public State ---

        /// <summary>Current app state.</summary>
        public AppState CurrentState { get; private set; } = AppState.Initializing;

        /// <summary>The site currently selected for detail view / AR.</summary>
        public HistoricalSite SelectedSite { get; private set; }

        /// <summary>Singleton-style access (optional — prefer DI where possible).</summary>
        public static AppManager Instance { get; private set; }

        // --- Events ---
        public System.Action<AppState> OnAppStateChanged;
        public System.Action<HistoricalSite> OnSiteSelected;

        // --- Lifecycle ---

        private void Awake()
        {
            // Singleton guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set target frame rate for smooth AR
            Application.targetFrameRate = 60;

            // Keep screen on during AR usage
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            SetState(AppState.Initializing);
            InitializeSystems();

            // Check first launch
            if (m_ShowWelcomeOnFirstLaunch && !HasCompletedOnboarding())
            {
                SetState(AppState.Onboarding);
                if (m_NavigationManager != null)
                    m_NavigationManager.ShowWelcome();
            }
            else
            {
                SetState(AppState.Browsing);
            }
        }

        private void OnDestroy()
        {
            CleanupSubscriptions();
            if (Instance == this)
                Instance = null;
        }

        // --- Initialization ---

        private void InitializeSystems()
        {
            // Initialize Explore screen filters
            if (m_ExploreScreen != null)
            {
                m_ExploreScreen.InitializeFilters();
                m_ExploreScreen.OnSiteSelected = HandleSiteSelected;
            }

            // Wire Nearby screen
            if (m_NearbyScreen != null)
            {
                m_NearbyScreen.OnSiteSelected = HandleSiteSelected;
            }

            // Wire notification banner action
            if (m_NotificationBanner != null)
            {
                m_NotificationBanner.OnActionTapped = HandleNotificationAction;
            }

            // Wire navigation events
            if (m_NavigationManager != null)
            {
                m_NavigationManager.OnTabChanged += HandleTabChanged;
                m_NavigationManager.OnScreenChanged += HandleScreenChanged;
            }

            // Wire AR events
            if (m_ARExperienceManager != null)
            {
                m_ARExperienceManager.OnModeChanged += HandleARModeChanged;
            }

            // Wire proximity events
            if (m_ProximityDetector != null)
            {
                m_ProximityDetector.OnEnteredSiteRadius += HandleEnteredSiteRadius;
            }

            // Start GPS tracking
            if (m_LocationService != null)
            {
                m_LocationService.StartTracking();
            }

            Debug.Log("[AppManager] All systems initialized.");
        }

        private void CleanupSubscriptions()
        {
            if (m_NavigationManager != null)
            {
                m_NavigationManager.OnTabChanged -= HandleTabChanged;
                m_NavigationManager.OnScreenChanged -= HandleScreenChanged;
            }

            if (m_ARExperienceManager != null)
            {
                m_ARExperienceManager.OnModeChanged -= HandleARModeChanged;
            }

            if (m_ProximityDetector != null)
            {
                m_ProximityDetector.OnEnteredSiteRadius -= HandleEnteredSiteRadius;
            }
        }

        // --- Public API ---

        /// <summary>
        /// Select a site and show its detail screen.
        /// Central method called by all UI components.
        /// </summary>
        public void SelectSite(HistoricalSite site)
        {
            HandleSiteSelected(site);
        }

        /// <summary>
        /// Launch AR preview for a site (from anywhere in the app).
        /// </summary>
        public void LaunchPreview(HistoricalSite site)
        {
            SelectedSite = site;
            if (m_ARExperienceManager != null)
                m_ARExperienceManager.StartPreviewExperience(site);
            if (m_NavigationManager != null)
                m_NavigationManager.EnterARExperience();
            SetState(AppState.ARActive);
        }

        /// <summary>
        /// Launch on-location AR for a site.
        /// </summary>
        public void LaunchOnLocation(HistoricalSite site)
        {
            SelectedSite = site;
            if (m_ARExperienceManager != null)
                m_ARExperienceManager.StartOnLocationExperience(site);
            if (m_NavigationManager != null)
                m_NavigationManager.EnterARExperience();
            SetState(AppState.ARActive);
        }

        /// <summary>
        /// Mark onboarding as complete. Called by the welcome screen.
        /// </summary>
        public void CompleteOnboarding()
        {
            PlayerPrefs.SetInt(k_OnboardingCompleteKey, 1);
            PlayerPrefs.Save();

            if (m_NavigationManager != null)
                m_NavigationManager.GoBack();

            SetState(AppState.Browsing);
        }

        // --- Event Handlers ---

        private void HandleSiteSelected(HistoricalSite site)
        {
            if (site == null) return;

            SelectedSite = site;
            OnSiteSelected?.Invoke(site);

            // Populate detail screen
            if (m_SiteDetailScreen != null)
            {
                bool isNearby = m_ProximityDetector != null &&
                                m_ProximityDetector.ActiveSites.Contains(site);
                m_SiteDetailScreen.ShowSite(site, isNearby);
            }

            // Navigate to detail screen
            if (m_NavigationManager != null)
            {
                m_NavigationManager.ShowSiteDetail();
            }
        }

        private void HandleNotificationAction(HistoricalSite site)
        {
            // User tapped "View in AR" on the proximity notification
            if (site != null)
            {
                LaunchOnLocation(site);
            }
        }

        private void HandleTabChanged(AppTab tab)
        {
            switch (tab)
            {
                case AppTab.Explore:
                case AppTab.Nearby:
                    if (CurrentState == AppState.ARActive)
                    {
                        m_ARExperienceManager?.EndExperience();
                    }
                    SetState(AppState.Browsing);
                    break;

                case AppTab.ARView:
                    // AR View tab without a specific site — show the last used or prompt
                    if (SelectedSite != null && m_ARExperienceManager != null &&
                        m_ARExperienceManager.CurrentMode == ARMode.Inactive)
                    {
                        m_ARExperienceManager.StartPreviewExperience(SelectedSite);
                    }
                    SetState(AppState.ARActive);
                    break;
            }
        }

        private void HandleScreenChanged(AppScreen screen)
        {
            // Log for analytics / debugging
            Debug.Log($"[AppManager] Screen changed to: {screen}");
        }

        private void HandleARModeChanged(ARMode mode)
        {
            if (mode == ARMode.Inactive)
            {
                SetState(AppState.Browsing);
            }
            else
            {
                SetState(AppState.ARActive);
            }
        }

        private void HandleEnteredSiteRadius(HistoricalSite site)
        {
            Debug.Log($"[AppManager] User entered radius of: {site.siteName}");
            // The NotificationBanner handles its own display via ProximityDetector events
            // This is for any additional app-level logic (analytics, etc.)
        }

        // --- Internal ---

        private void SetState(AppState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnAppStateChanged?.Invoke(newState);

            // State-specific behavior
            switch (newState)
            {
                case AppState.ARActive:
                    // Reduce GPS polling during AR for battery
                    break;

                case AppState.Browsing:
                    // Ensure GPS is tracking for nearby features
                    if (m_LocationService != null && !m_LocationService.IsLocationAvailable)
                    {
                        m_LocationService.StartTracking();
                    }
                    break;
            }
        }

        private bool HasCompletedOnboarding()
        {
            return PlayerPrefs.GetInt(k_OnboardingCompleteKey, 0) == 1;
        }

        // --- Application Lifecycle ---

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // Save state, reduce tracking
                Debug.Log("[AppManager] App paused");
            }
            else
            {
                // Resume tracking, refresh proximity
                Debug.Log("[AppManager] App resumed");
                if (m_ProximityDetector != null)
                    m_ProximityDetector.ForceCheck();
            }
        }
    }

    /// <summary>
    /// High-level application states.
    /// </summary>
    public enum AppState
    {
        Initializing,
        Onboarding,
        Browsing,
        ARActive
    }
}
