using System;
using System.Collections.Generic;
using UnityEngine;

namespace HistoricalGreece.UI
{
    /// <summary>
    /// Manages the app's screen navigation with a tourist-friendly tab bar pattern.
    /// Handles screen transitions, back navigation, and the bottom tab bar state.
    ///
    /// Design: Travel-app style (3 tabs: Explore | Nearby | AR View).
    /// Each tab has its own screen root. Screens can push detail views on top.
    /// </summary>
    public class AppNavigationManager : MonoBehaviour
    {
        [Header("Tab Screens (root panels for each tab)")]
        [Tooltip("The Explore / Browse catalog screen")]
        [SerializeField] private GameObject m_ExploreScreen;

        [Tooltip("The Nearby / GPS-based sites screen")]
        [SerializeField] private GameObject m_NearbyScreen;

        [Tooltip("The AR View screen (camera feed + AR content)")]
        [SerializeField] private GameObject m_ARViewScreen;

        [Header("Overlay Screens")]
        [Tooltip("Site detail view — pushed on top of any tab")]
        [SerializeField] private GameObject m_SiteDetailScreen;

        [Tooltip("AR HUD panel — shown during active AR experience")]
        [SerializeField] private GameObject m_ARHUDScreen;

        [Tooltip("Welcome / onboarding screen (shown on first launch)")]
        [SerializeField] private GameObject m_WelcomeScreen;

        [Tooltip("Settings / about screen")]
        [SerializeField] private GameObject m_SettingsScreen;

        [Header("Tab Bar")]
        [Tooltip("The bottom tab bar container (always visible except during AR)")]
        [SerializeField] private GameObject m_TabBar;

        [Tooltip("Tab buttons in order: Explore, Nearby, AR View")]
        [SerializeField] private TabButton[] m_TabButtons;

        [Header("Transitions")]
        [Tooltip("Optional screen transition animator")]
        [SerializeField] private Animator m_TransitionAnimator;

        [Tooltip("Transition animation duration in seconds")]
        public float TransitionDuration = 0.25f;

        // --- Public State ---

        /// <summary>Currently active tab.</summary>
        public AppTab CurrentTab { get; private set; } = AppTab.Explore;

        /// <summary>Currently active screen.</summary>
        public AppScreen CurrentScreen { get; private set; } = AppScreen.Explore;

        /// <summary>True if a modal/overlay screen is on top of the current tab.</summary>
        public bool HasOverlay => m_ScreenStack.Count > 0;

        // --- Events ---

        /// <summary>Fires when the active tab changes.</summary>
        public event Action<AppTab> OnTabChanged;

        /// <summary>Fires when the active screen changes (including overlays).</summary>
        public event Action<AppScreen> OnScreenChanged;

        /// <summary>Fires when back navigation is triggered.</summary>
        public event Action OnBackPressed;

        // --- Internal ---
        private readonly Stack<AppScreen> m_ScreenStack = new Stack<AppScreen>();
        private readonly Dictionary<AppScreen, GameObject> m_ScreenObjects = new Dictionary<AppScreen, GameObject>();

        // --- Lifecycle ---

        private void Awake()
        {
            // Register all screens
            RegisterScreen(AppScreen.Explore, m_ExploreScreen);
            RegisterScreen(AppScreen.Nearby, m_NearbyScreen);
            RegisterScreen(AppScreen.ARView, m_ARViewScreen);
            RegisterScreen(AppScreen.SiteDetail, m_SiteDetailScreen);
            RegisterScreen(AppScreen.ARHUD, m_ARHUDScreen);
            RegisterScreen(AppScreen.Welcome, m_WelcomeScreen);
            RegisterScreen(AppScreen.Settings, m_SettingsScreen);
        }

        private void Start()
        {
            // Start on the Explore tab
            HideAllScreens();
            ShowTab(AppTab.Explore);
        }

        private void Update()
        {
            // Handle Android back button / iOS swipe-back gesture
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoBack();
            }
        }

        // --- Public API: Tab Navigation ---

        /// <summary>
        /// Switch to a tab. Clears any overlay screens and shows the tab's root screen.
        /// Called by tab bar buttons.
        /// </summary>
        public void ShowTab(AppTab tab)
        {
            // Clear overlay stack
            while (m_ScreenStack.Count > 0)
            {
                var overlay = m_ScreenStack.Pop();
                SetScreenActive(overlay, false);
            }

            // Hide current tab screen
            SetScreenActive(TabToScreen(CurrentTab), false);

            // Show new tab
            CurrentTab = tab;
            var screen = TabToScreen(tab);
            SetScreenActive(screen, true);
            SetScreen(screen);

            // Update tab bar visual state
            UpdateTabBarVisuals();

            // Show/hide tab bar (hidden during AR view)
            if (m_TabBar != null)
            {
                m_TabBar.SetActive(tab != AppTab.ARView);
            }

            OnTabChanged?.Invoke(tab);
        }

        /// <summary>Convenience methods for tab bar button bindings.</summary>
        public void ShowExploreTab() => ShowTab(AppTab.Explore);
        public void ShowNearbyTab() => ShowTab(AppTab.Nearby);
        public void ShowARViewTab() => ShowTab(AppTab.ARView);

        // --- Public API: Overlay Navigation ---

        /// <summary>
        /// Push an overlay screen on top of the current tab.
        /// Used for site details, settings, etc.
        /// </summary>
        public void PushScreen(AppScreen screen)
        {
            // Hide tab bar for immersive overlays
            if (screen == AppScreen.ARHUD && m_TabBar != null)
            {
                m_TabBar.SetActive(false);
            }

            m_ScreenStack.Push(screen);
            SetScreenActive(screen, true);
            SetScreen(screen);
        }

        /// <summary>
        /// Pop the top overlay screen and return to the previous screen.
        /// </summary>
        public void PopScreen()
        {
            if (m_ScreenStack.Count == 0) return;

            var top = m_ScreenStack.Pop();
            SetScreenActive(top, false);

            // Determine what to show now
            if (m_ScreenStack.Count > 0)
            {
                SetScreen(m_ScreenStack.Peek());
            }
            else
            {
                var tabScreen = TabToScreen(CurrentTab);
                SetScreen(tabScreen);

                // Restore tab bar
                if (m_TabBar != null)
                {
                    m_TabBar.SetActive(CurrentTab != AppTab.ARView);
                }
            }
        }

        /// <summary>
        /// Navigate back. If there's an overlay, pop it. Otherwise, go to Explore tab.
        /// </summary>
        public void GoBack()
        {
            OnBackPressed?.Invoke();

            if (m_ScreenStack.Count > 0)
            {
                PopScreen();
            }
            else if (CurrentTab != AppTab.Explore)
            {
                ShowTab(AppTab.Explore);
            }
        }

        // --- Public API: Direct Screen Access ---

        /// <summary>
        /// Show the site detail screen for a specific site.
        /// Can be called from Explore, Nearby, or even AR mode.
        /// </summary>
        public void ShowSiteDetail()
        {
            PushScreen(AppScreen.SiteDetail);
        }

        /// <summary>
        /// Enter full AR experience mode with the HUD.
        /// Hides all other UI for an immersive experience.
        /// </summary>
        public void EnterARExperience()
        {
            ShowTab(AppTab.ARView);
            PushScreen(AppScreen.ARHUD);
        }

        /// <summary>
        /// Exit AR experience and return to the previous tab.
        /// </summary>
        public void ExitARExperience()
        {
            PopScreen(); // Pop ARHUD
            ShowTab(AppTab.Explore); // Return to explore
        }

        /// <summary>
        /// Show the welcome/onboarding screen.
        /// </summary>
        public void ShowWelcome()
        {
            PushScreen(AppScreen.Welcome);
        }

        /// <summary>
        /// Show settings.
        /// </summary>
        public void ShowSettings()
        {
            PushScreen(AppScreen.Settings);
        }

        // --- Internal ---

        private void RegisterScreen(AppScreen screen, GameObject obj)
        {
            if (obj != null)
            {
                m_ScreenObjects[screen] = obj;
            }
        }

        private void SetScreenActive(AppScreen screen, bool active)
        {
            if (m_ScreenObjects.TryGetValue(screen, out var obj) && obj != null)
            {
                obj.SetActive(active);
            }
        }

        private void HideAllScreens()
        {
            foreach (var kvp in m_ScreenObjects)
            {
                if (kvp.Value != null) kvp.Value.SetActive(false);
            }
        }

        private void SetScreen(AppScreen screen)
        {
            if (CurrentScreen == screen) return;
            CurrentScreen = screen;
            OnScreenChanged?.Invoke(screen);
        }

        private void UpdateTabBarVisuals()
        {
            if (m_TabButtons == null) return;
            for (int i = 0; i < m_TabButtons.Length; i++)
            {
                if (m_TabButtons[i] != null)
                {
                    m_TabButtons[i].SetSelected(i == (int)CurrentTab);
                }
            }
        }

        private AppScreen TabToScreen(AppTab tab)
        {
            return tab switch
            {
                AppTab.Explore => AppScreen.Explore,
                AppTab.Nearby  => AppScreen.Nearby,
                AppTab.ARView  => AppScreen.ARView,
                _              => AppScreen.Explore
            };
        }
    }

    // --- Enums ---

    /// <summary>
    /// The three main tabs in the bottom navigation bar.
    /// </summary>
    public enum AppTab
    {
        Explore = 0,
        Nearby  = 1,
        ARView  = 2
    }

    /// <summary>
    /// All possible screens in the app.
    /// </summary>
    public enum AppScreen
    {
        Explore,
        Nearby,
        ARView,
        SiteDetail,
        ARHUD,
        Welcome,
        Settings
    }

    /// <summary>
    /// Simple component for tab bar buttons. Handles selected/unselected visuals.
    /// Attach to each tab button in the bottom bar.
    /// </summary>
    [System.Serializable]
    public class TabButton : MonoBehaviour
    {
        [Tooltip("Icon shown when this tab is selected")]
        [SerializeField] private GameObject m_SelectedVisual;

        [Tooltip("Icon shown when this tab is not selected")]
        [SerializeField] private GameObject m_UnselectedVisual;

        [Tooltip("Optional: label text component")]
        [SerializeField] private TMPro.TMP_Text m_Label;

        [Tooltip("Color for selected state")]
        [SerializeField] private Color m_SelectedColor = new Color(0.2f, 0.45f, 0.85f);

        [Tooltip("Color for unselected state")]
        [SerializeField] private Color m_UnselectedColor = new Color(0.5f, 0.5f, 0.5f);

        public void SetSelected(bool selected)
        {
            if (m_SelectedVisual != null) m_SelectedVisual.SetActive(selected);
            if (m_UnselectedVisual != null) m_UnselectedVisual.SetActive(!selected);
            if (m_Label != null) m_Label.color = selected ? m_SelectedColor : m_UnselectedColor;
        }
    }
}
