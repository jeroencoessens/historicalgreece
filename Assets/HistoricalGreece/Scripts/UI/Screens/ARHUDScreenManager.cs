using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HistoricalGreece.AR;

namespace HistoricalGreece.UI.Screens
{
    /// <summary>
    /// Minimal AR HUD shown during active AR experiences (both on-location and preview).
    /// Designed for tourists: only essential controls, no clutter.
    /// Shows site info, placement guidance, and a clean exit button.
    /// </summary>
    public class ARHUDScreenManager : MonoBehaviour
    {
        [Header("Site Info")]
        [Tooltip("Name of the site currently being viewed")]
        [SerializeField] private TMP_Text m_SiteNameLabel;

        [Tooltip("Period/tagline subtitle")]
        [SerializeField] private TMP_Text m_SiteSubtitle;

        [Header("Guidance")]
        [Tooltip("Panel shown while scanning for surfaces")]
        [SerializeField] private GameObject m_ScanningPanel;

        [Tooltip("Text for scanning guidance")]
        [SerializeField] private TMP_Text m_ScanningText;

        [Tooltip("Panel shown after content is placed (with interaction hints)")]
        [SerializeField] private GameObject m_PlacedPanel;

        [Tooltip("Panel for on-location mode guidance")]
        [SerializeField] private GameObject m_OnLocationGuidance;

        [Header("Action Buttons")]
        [Tooltip("Button to exit AR and go back")]
        [SerializeField] private Button m_ExitButton;

        [Tooltip("Button to reset/reposition the AR content")]
        [SerializeField] private Button m_ResetButton;

        [Tooltip("Button to capture/screenshot the AR view")]
        [SerializeField] private Button m_CaptureButton;

        [Tooltip("Button to toggle the info panel")]
        [SerializeField] private Button m_InfoToggleButton;

        [Header("Info Panel")]
        [Tooltip("Collapsible panel with full site description")]
        [SerializeField] private GameObject m_InfoPanel;

        [SerializeField] private TMP_Text m_InfoDescription;

        [Header("Mode Indicator")]
        [Tooltip("Badge showing current mode: 'ON LOCATION' or 'PREVIEW'")]
        [SerializeField] private TMP_Text m_ModeBadge;

        [SerializeField] private Image m_ModeBadgeBackground;
        [SerializeField] private Color m_OnLocationColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color m_PreviewColor = new Color(0.3f, 0.5f, 0.9f);

        [Header("References")]
        [SerializeField] private ARExperienceManager m_ARExperienceManager;
        [SerializeField] private AppNavigationManager m_NavigationManager;

        // --- State ---
        private bool m_InfoPanelVisible;

        // --- Lifecycle ---

        private void OnEnable()
        {
            // Wire buttons
            if (m_ExitButton != null) m_ExitButton.onClick.AddListener(OnExitPressed);
            if (m_ResetButton != null) m_ResetButton.onClick.AddListener(OnResetPressed);
            if (m_CaptureButton != null) m_CaptureButton.onClick.AddListener(OnCapturePressed);
            if (m_InfoToggleButton != null) m_InfoToggleButton.onClick.AddListener(OnInfoTogglePressed);

            // Subscribe to AR events
            if (m_ARExperienceManager != null)
            {
                m_ARExperienceManager.OnModeChanged += HandleModeChanged;
                m_ARExperienceManager.OnContentPlaced += HandleContentPlaced;
                m_ARExperienceManager.OnContentRemoved += HandleContentRemoved;
                m_ARExperienceManager.OnSurfaceDetected += HandleSurfaceDetected;

                // Initialize based on current state
                UpdateForCurrentState();
            }
        }

        private void OnDisable()
        {
            if (m_ExitButton != null) m_ExitButton.onClick.RemoveListener(OnExitPressed);
            if (m_ResetButton != null) m_ResetButton.onClick.RemoveListener(OnResetPressed);
            if (m_CaptureButton != null) m_CaptureButton.onClick.RemoveListener(OnCapturePressed);
            if (m_InfoToggleButton != null) m_InfoToggleButton.onClick.RemoveListener(OnInfoTogglePressed);

            if (m_ARExperienceManager != null)
            {
                m_ARExperienceManager.OnModeChanged -= HandleModeChanged;
                m_ARExperienceManager.OnContentPlaced -= HandleContentPlaced;
                m_ARExperienceManager.OnContentRemoved -= HandleContentRemoved;
                m_ARExperienceManager.OnSurfaceDetected -= HandleSurfaceDetected;
            }
        }

        // --- Event Handlers ---

        private void HandleModeChanged(ARMode mode)
        {
            UpdateModeBadge(mode);
            UpdateGuidancePanels();
        }

        private void HandleContentPlaced(Core.HistoricalSite site, GameObject content)
        {
            // Switch from scanning guidance to placed guidance
            if (m_ScanningPanel != null) m_ScanningPanel.SetActive(false);
            if (m_PlacedPanel != null) m_PlacedPanel.SetActive(true);
            if (m_ResetButton != null) m_ResetButton.gameObject.SetActive(true);
            if (m_CaptureButton != null) m_CaptureButton.gameObject.SetActive(true);
        }

        private void HandleContentRemoved()
        {
            UpdateGuidancePanels();
        }

        private void HandleSurfaceDetected()
        {
            if (m_ScanningText != null)
            {
                m_ScanningText.text = "Surface found! Tap to place the reconstruction.";
            }
        }

        // --- UI Updates ---

        private void UpdateForCurrentState()
        {
            if (m_ARExperienceManager == null) return;

            var site = m_ARExperienceManager.CurrentSite;
            if (site != null)
            {
                SetText(m_SiteNameLabel, site.siteName);
                SetText(m_SiteSubtitle, site.tagline);
                SetText(m_InfoDescription, site.description);
            }

            UpdateModeBadge(m_ARExperienceManager.CurrentMode);
            UpdateGuidancePanels();

            // Hide info panel initially
            m_InfoPanelVisible = false;
            if (m_InfoPanel != null) m_InfoPanel.SetActive(false);
        }

        private void UpdateModeBadge(ARMode mode)
        {
            if (m_ModeBadge != null)
            {
                m_ModeBadge.text = mode == ARMode.OnLocation ? "ON LOCATION" : "PREVIEW";
            }

            if (m_ModeBadgeBackground != null)
            {
                m_ModeBadgeBackground.color = mode == ARMode.OnLocation
                    ? m_OnLocationColor
                    : m_PreviewColor;
            }
        }

        private void UpdateGuidancePanels()
        {
            if (m_ARExperienceManager == null) return;

            bool isContentPlaced = m_ARExperienceManager.IsContentPlaced;
            bool isOnLocation = m_ARExperienceManager.CurrentMode == ARMode.OnLocation;

            if (m_ScanningPanel != null)
            {
                m_ScanningPanel.SetActive(!isContentPlaced);
            }

            if (m_PlacedPanel != null)
            {
                m_PlacedPanel.SetActive(isContentPlaced && !isOnLocation);
            }

            if (m_OnLocationGuidance != null)
            {
                m_OnLocationGuidance.SetActive(isOnLocation && !isContentPlaced);
            }

            if (m_ScanningText != null && !isContentPlaced)
            {
                m_ScanningText.text = isOnLocation
                    ? "Point your camera at the site. We'll align the reconstruction."
                    : "Slowly move your phone to scan a flat surface.";
            }

            if (m_ResetButton != null)
            {
                m_ResetButton.gameObject.SetActive(isContentPlaced);
            }

            if (m_CaptureButton != null)
            {
                m_CaptureButton.gameObject.SetActive(isContentPlaced);
            }
        }

        // --- Button Handlers ---

        private void OnExitPressed()
        {
            if (m_ARExperienceManager != null)
                m_ARExperienceManager.EndExperience();
            if (m_NavigationManager != null)
                m_NavigationManager.ExitARExperience();
        }

        private void OnResetPressed()
        {
            if (m_ARExperienceManager != null)
                m_ARExperienceManager.ResetPlacement();
        }

        private void OnCapturePressed()
        {
            // Capture a screenshot for sharing
            string filename = $"HistoricalGreece_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"[AR HUD] Screenshot saved: {filename}");

            // TODO: Show a toast/notification to the user that screenshot was saved
            // TODO: Offer share sheet on iOS/Android
        }

        private void OnInfoTogglePressed()
        {
            m_InfoPanelVisible = !m_InfoPanelVisible;
            if (m_InfoPanel != null)
            {
                m_InfoPanel.SetActive(m_InfoPanelVisible);
            }
        }

        private void SetText(TMP_Text field, string value)
        {
            if (field != null) field.text = value ?? "";
        }
    }
}
