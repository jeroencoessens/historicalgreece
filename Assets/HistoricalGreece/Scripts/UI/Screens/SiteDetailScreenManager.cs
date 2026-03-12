using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HistoricalGreece.Core;
using HistoricalGreece.AR;

namespace HistoricalGreece.UI.Screens
{
    /// <summary>
    /// Displays detailed information about a historical site.
    /// Shows as an overlay pushed on top of any tab.
    /// Includes full description, gallery, travel info, and AR launch buttons.
    /// </summary>
    public class SiteDetailScreenManager : MonoBehaviour
    {
        [Header("Site Info")]
        [SerializeField] private TMP_Text m_SiteName;
        [SerializeField] private TMP_Text m_Tagline;
        [SerializeField] private TMP_Text m_Period;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private Image m_HeroImage;
        [SerializeField] private Image m_AccentBar;

        [Header("Gallery")]
        [Tooltip("Container for gallery image thumbnails")]
        [SerializeField] private Transform m_GalleryContainer;

        [Tooltip("Prefab for a gallery thumbnail")]
        [SerializeField] private GameObject m_GalleryImagePrefab;

        [Header("Travel Info")]
        [SerializeField] private TMP_Text m_RegionCountry;
        [SerializeField] private TMP_Text m_TravelTips;
        [SerializeField] private TMP_Text m_VisitDuration;
        [SerializeField] private TMP_Text m_Accessibility;
        [SerializeField] private GameObject m_TravelInfoSection;

        [Header("Category & Civilization")]
        [SerializeField] private TMP_Text m_Category;
        [SerializeField] private TMP_Text m_Civilization;

        [Header("Action Buttons")]
        [Tooltip("Button to launch AR Preview from home")]
        [SerializeField] private Button m_PreviewARButton;

        [Tooltip("Button to launch on-location AR (only shown when nearby)")]
        [SerializeField] private Button m_OnLocationARButton;

        [Tooltip("Button to navigate/get directions to the site")]
        [SerializeField] private Button m_DirectionsButton;

        [Header("Navigation")]
        [SerializeField] private Button m_BackButton;
        [SerializeField] private AppNavigationManager m_NavigationManager;
        [SerializeField] private ARExperienceManager m_ARExperienceManager;

        // --- State ---
        private HistoricalSite m_CurrentSite;

        // --- Lifecycle ---

        private void OnEnable()
        {
            if (m_BackButton != null)
                m_BackButton.onClick.AddListener(OnBackPressed);
            if (m_PreviewARButton != null)
                m_PreviewARButton.onClick.AddListener(OnPreviewARPressed);
            if (m_OnLocationARButton != null)
                m_OnLocationARButton.onClick.AddListener(OnLocationARPressed);
            if (m_DirectionsButton != null)
                m_DirectionsButton.onClick.AddListener(OnDirectionsPressed);
        }

        private void OnDisable()
        {
            if (m_BackButton != null)
                m_BackButton.onClick.RemoveListener(OnBackPressed);
            if (m_PreviewARButton != null)
                m_PreviewARButton.onClick.RemoveListener(OnPreviewARPressed);
            if (m_OnLocationARButton != null)
                m_OnLocationARButton.onClick.RemoveListener(OnLocationARPressed);
            if (m_DirectionsButton != null)
                m_DirectionsButton.onClick.RemoveListener(OnDirectionsPressed);
        }

        // --- Public API ---

        /// <summary>
        /// Populate the detail screen with a specific site's data.
        /// Call this before pushing the screen via AppNavigationManager.
        /// </summary>
        public void ShowSite(HistoricalSite site, bool isNearby = false)
        {
            m_CurrentSite = site;
            if (site == null) return;

            // Identity
            SetText(m_SiteName, site.siteName);
            SetText(m_Tagline, site.tagline);
            SetText(m_Period, site.PeriodLabel);
            SetText(m_Description, site.description);
            SetText(m_Category, site.category.ToString());
            SetText(m_Civilization, site.civilization.ToString());

            // Hero image
            if (m_HeroImage != null && site.thumbnail != null)
            {
                m_HeroImage.sprite = site.thumbnail;
                m_HeroImage.gameObject.SetActive(true);
            }
            else if (m_HeroImage != null)
            {
                m_HeroImage.gameObject.SetActive(false);
            }

            // Accent color
            if (m_AccentBar != null)
            {
                m_AccentBar.color = site.accentColor;
            }

            // Travel info
            if (m_TravelInfoSection != null)
            {
                bool hasTravelInfo = !string.IsNullOrEmpty(site.regionName) ||
                                     !string.IsNullOrEmpty(site.travelTips);
                m_TravelInfoSection.SetActive(hasTravelInfo);
            }

            SetText(m_RegionCountry, $"{site.regionName}, {site.countryName}");
            SetText(m_TravelTips, site.travelTips);
            SetText(m_VisitDuration, $"~{site.estimatedVisitMinutes} min visit");
            SetText(m_Accessibility, GetAccessibilityLabel(site.accessibilityRating));

            // Gallery
            BuildGallery(site);

            // Buttons: on-location button only shown when user is nearby
            if (m_OnLocationARButton != null)
            {
                m_OnLocationARButton.gameObject.SetActive(isNearby);
            }

            // Preview is always available
            if (m_PreviewARButton != null)
            {
                m_PreviewARButton.gameObject.SetActive(site.arPrefabPreview != null || site.arPrefabOnLocation != null);
            }
        }

        // --- Internal ---

        private void BuildGallery(HistoricalSite site)
        {
            if (m_GalleryContainer == null || m_GalleryImagePrefab == null) return;

            // Clear existing
            foreach (Transform child in m_GalleryContainer)
            {
                Destroy(child.gameObject);
            }

            if (site.galleryImages == null) return;

            foreach (var sprite in site.galleryImages)
            {
                if (sprite == null) continue;
                var imgObj = Instantiate(m_GalleryImagePrefab, m_GalleryContainer);
                var img = imgObj.GetComponent<Image>();
                if (img != null) img.sprite = sprite;
            }
        }

        private void OnBackPressed()
        {
            if (m_NavigationManager != null)
                m_NavigationManager.GoBack();
        }

        private void OnPreviewARPressed()
        {
            if (m_CurrentSite == null || m_ARExperienceManager == null) return;
            m_ARExperienceManager.StartPreviewExperience(m_CurrentSite);
            if (m_NavigationManager != null)
                m_NavigationManager.EnterARExperience();
        }

        private void OnLocationARPressed()
        {
            if (m_CurrentSite == null || m_ARExperienceManager == null) return;
            m_ARExperienceManager.StartOnLocationExperience(m_CurrentSite);
            if (m_NavigationManager != null)
                m_NavigationManager.EnterARExperience();
        }

        private void OnDirectionsPressed()
        {
            if (m_CurrentSite == null) return;

            // Open native maps app with directions to the site
            string mapsUrl;
#if UNITY_IOS
            mapsUrl = $"http://maps.apple.com/?daddr={m_CurrentSite.latitude},{m_CurrentSite.longitude}";
#else
            mapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={m_CurrentSite.latitude},{m_CurrentSite.longitude}";
#endif
            Application.OpenURL(mapsUrl);
        }

        private string GetAccessibilityLabel(int rating)
        {
            return rating switch
            {
                1 => "Easy — Fully Accessible",
                2 => "Moderate — Mostly Accessible",
                3 => "Average — Some Uneven Terrain",
                4 => "Challenging — Steep / Rough Paths",
                5 => "Difficult — Mountain / Rugged Terrain",
                _ => "Unknown"
            };
        }

        private void SetText(TMP_Text field, string value)
        {
            if (field != null) field.text = value ?? "";
        }
    }
}
