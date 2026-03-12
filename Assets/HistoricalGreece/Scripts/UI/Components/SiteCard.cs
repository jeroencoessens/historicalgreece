using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HistoricalGreece.Core;

namespace HistoricalGreece.UI
{
    /// <summary>
    /// Reusable site card component used in both the Explore catalog
    /// and the Nearby list. Shows thumbnail, name, period, and action buttons.
    /// Designed as a clean, travel-app-style card.
    /// </summary>
    public class SiteCard : MonoBehaviour
    {
        [Header("Card Content")]
        [SerializeField] private TMP_Text m_SiteName;
        [SerializeField] private TMP_Text m_Tagline;
        [SerializeField] private TMP_Text m_PeriodLabel;
        [SerializeField] private Image m_Thumbnail;
        [SerializeField] private Image m_AccentStripe;

        [Header("Distance (Nearby mode only)")]
        [SerializeField] private TMP_Text m_DistanceLabel;
        [SerializeField] private GameObject m_DistanceContainer;

        [Header("In-Range Indicator")]
        [Tooltip("Badge shown when user is within activation radius")]
        [SerializeField] private GameObject m_InRangeBadge;

        [Header("Category Badge")]
        [SerializeField] private TMP_Text m_CategoryLabel;
        [SerializeField] private Image m_CategoryBadge;

        [Header("Action Buttons")]
        [Tooltip("Button to open site details")]
        [SerializeField] private Button m_DetailsButton;

        [Tooltip("Button to launch AR preview directly from the card")]
        [SerializeField] private Button m_PreviewButton;

        [Tooltip("Label on the preview button (changes for on-location)")]
        [SerializeField] private TMP_Text m_PreviewButtonLabel;

        // --- State ---
        private HistoricalSite m_Site;
        private Action<HistoricalSite> m_OnDetailsCallback;
        private Action<HistoricalSite> m_OnPreviewCallback;

        // --- Public API ---

        /// <summary>
        /// Initialize the card with site data and callbacks.
        /// </summary>
        /// <param name="site">The historical site to display.</param>
        /// <param name="onDetails">Called when user taps "Details" / the card body.</param>
        /// <param name="onPreview">Called when user taps "View in AR".</param>
        public void Setup(HistoricalSite site, Action<HistoricalSite> onDetails, Action<HistoricalSite> onPreview)
        {
            m_Site = site;
            m_OnDetailsCallback = onDetails;
            m_OnPreviewCallback = onPreview;

            if (site == null) return;

            // Fill content
            SetText(m_SiteName, site.siteName);
            SetText(m_Tagline, site.tagline);
            SetText(m_PeriodLabel, site.PeriodLabel);
            SetText(m_CategoryLabel, site.category.ToString());

            if (m_Thumbnail != null)
            {
                if (site.thumbnail != null)
                {
                    m_Thumbnail.sprite = site.thumbnail;
                    m_Thumbnail.gameObject.SetActive(true);
                }
                else
                {
                    m_Thumbnail.gameObject.SetActive(false);
                }
            }

            if (m_AccentStripe != null)
            {
                m_AccentStripe.color = site.accentColor;
            }

            // Default: hide distance and in-range badge
            if (m_DistanceContainer != null) m_DistanceContainer.SetActive(false);
            if (m_InRangeBadge != null) m_InRangeBadge.SetActive(false);

            // Preview button availability
            bool hasARContent = site.arPrefabPreview != null || site.arPrefabOnLocation != null;
            if (m_PreviewButton != null)
            {
                m_PreviewButton.gameObject.SetActive(hasARContent);
            }
            if (m_PreviewButtonLabel != null)
            {
                m_PreviewButtonLabel.text = "View in AR";
            }
        }

        /// <summary>
        /// Set the distance label (used in Nearby mode).
        /// </summary>
        public void SetDistance(string distanceText)
        {
            if (m_DistanceContainer != null) m_DistanceContainer.SetActive(true);
            SetText(m_DistanceLabel, distanceText);
        }

        /// <summary>
        /// Mark this card as in-range (user is within activation radius).
        /// Changes the preview button to "Start AR Experience" and shows the badge.
        /// </summary>
        public void SetInRange(bool inRange)
        {
            if (m_InRangeBadge != null) m_InRangeBadge.SetActive(inRange);

            if (inRange && m_PreviewButtonLabel != null)
            {
                m_PreviewButtonLabel.text = "Start AR Experience";
            }
        }

        // --- Lifecycle ---

        private void OnEnable()
        {
            if (m_DetailsButton != null)
                m_DetailsButton.onClick.AddListener(OnDetailsTapped);
            if (m_PreviewButton != null)
                m_PreviewButton.onClick.AddListener(OnPreviewTapped);
        }

        private void OnDisable()
        {
            if (m_DetailsButton != null)
                m_DetailsButton.onClick.RemoveListener(OnDetailsTapped);
            if (m_PreviewButton != null)
                m_PreviewButton.onClick.RemoveListener(OnPreviewTapped);
        }

        // --- Callbacks ---

        private void OnDetailsTapped()
        {
            m_OnDetailsCallback?.Invoke(m_Site);
        }

        private void OnPreviewTapped()
        {
            m_OnPreviewCallback?.Invoke(m_Site);
        }

        private void SetText(TMP_Text field, string value)
        {
            if (field != null) field.text = value ?? "";
        }
    }
}
