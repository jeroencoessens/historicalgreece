using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HistoricalGreece.Core;
using HistoricalGreece.AR;

namespace HistoricalGreece.UI.Screens
{
    /// <summary>
    /// Manages the Explore tab — a browsable catalog of historical sites.
    /// Supports filtering by period, civilization, category, and search.
    /// Tourist-friendly card-based layout.
    /// </summary>
    public class ExploreScreenManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SiteDatabase m_SiteDatabase;

        [Header("UI References")]
        [Tooltip("Parent container for site cards (Content of a ScrollView)")]
        [SerializeField] private Transform m_CardContainer;

        [Tooltip("Prefab for a site card in the browse list")]
        [SerializeField] private GameObject m_SiteCardPrefab;

        [Tooltip("Search input field")]
        [SerializeField] private TMP_InputField m_SearchField;

        [Header("Filter UI")]
        [Tooltip("Period filter dropdown")]
        [SerializeField] private TMP_Dropdown m_PeriodFilter;

        [Tooltip("Category filter dropdown")]
        [SerializeField] private TMP_Dropdown m_CategoryFilter;

        [Tooltip("Region filter dropdown")]
        [SerializeField] private TMP_Dropdown m_RegionFilter;

        [Header("Empty State")]
        [Tooltip("Shown when no sites match the current filters")]
        [SerializeField] private GameObject m_EmptyStatePanel;

        [Header("Navigation")]
        [SerializeField] private AppNavigationManager m_NavigationManager;
        [SerializeField] private ARExperienceManager m_ARExperienceManager;

        // --- Internal ---
        private List<HistoricalSite> m_FilteredSites = new List<HistoricalSite>();
        private readonly List<GameObject> m_SpawnedCards = new List<GameObject>();

        /// <summary>
        /// Invoked by SiteCard when user wants to see details.
        /// Set this from AppManager so the detail screen gets the right data.
        /// </summary>
        public System.Action<HistoricalSite> OnSiteSelected;

        // --- Lifecycle ---

        private void OnEnable()
        {
            RefreshCatalog();

            if (m_SearchField != null)
                m_SearchField.onValueChanged.AddListener(OnSearchChanged);
            if (m_PeriodFilter != null)
                m_PeriodFilter.onValueChanged.AddListener(OnFilterChanged);
            if (m_CategoryFilter != null)
                m_CategoryFilter.onValueChanged.AddListener(OnFilterChanged);
            if (m_RegionFilter != null)
                m_RegionFilter.onValueChanged.AddListener(OnFilterChanged);
        }

        private void OnDisable()
        {
            if (m_SearchField != null)
                m_SearchField.onValueChanged.RemoveListener(OnSearchChanged);
            if (m_PeriodFilter != null)
                m_PeriodFilter.onValueChanged.RemoveListener(OnFilterChanged);
            if (m_CategoryFilter != null)
                m_CategoryFilter.onValueChanged.RemoveListener(OnFilterChanged);
            if (m_RegionFilter != null)
                m_RegionFilter.onValueChanged.RemoveListener(OnFilterChanged);
        }

        // --- Public API ---

        /// <summary>
        /// Rebuild the card list based on current filters.
        /// </summary>
        public void RefreshCatalog()
        {
            if (m_SiteDatabase == null) return;

            // Apply filters
            IEnumerable<HistoricalSite> results = m_SiteDatabase.ActiveSites;

            // Search text filter
            if (m_SearchField != null && !string.IsNullOrWhiteSpace(m_SearchField.text))
            {
                results = m_SiteDatabase.SearchSites(m_SearchField.text);
            }

            // Period filter (index 0 = "All Periods")
            if (m_PeriodFilter != null && m_PeriodFilter.value > 0)
            {
                var selectedPeriod = (HistoricalPeriod)(m_PeriodFilter.value - 1);
                results = results.Where(s => s.period == selectedPeriod);
            }

            // Category filter (index 0 = "All Categories")
            if (m_CategoryFilter != null && m_CategoryFilter.value > 0)
            {
                var selectedCategory = (SiteCategory)(m_CategoryFilter.value - 1);
                results = results.Where(s => s.category == selectedCategory);
            }

            // Region filter (index 0 = "All Regions")
            if (m_RegionFilter != null && m_RegionFilter.value > 0)
            {
                string selectedRegion = m_RegionFilter.options[m_RegionFilter.value].text;
                results = results.Where(s =>
                    s.regionName.Equals(selectedRegion, System.StringComparison.OrdinalIgnoreCase));
            }

            m_FilteredSites = results.ToList();

            // Rebuild UI
            RebuildCards();
        }

        /// <summary>
        /// Populate the filter dropdowns with available options from the database.
        /// Call once during initialization or when the database changes.
        /// </summary>
        public void InitializeFilters()
        {
            if (m_SiteDatabase == null) return;

            // Period dropdown
            if (m_PeriodFilter != null)
            {
                m_PeriodFilter.ClearOptions();
                var options = new List<string> { "All Periods" };
                options.AddRange(System.Enum.GetValues(typeof(HistoricalPeriod))
                    .Cast<HistoricalPeriod>()
                    .Select(p => p.ToDisplayString()));
                m_PeriodFilter.AddOptions(options);
            }

            // Category dropdown
            if (m_CategoryFilter != null)
            {
                m_CategoryFilter.ClearOptions();
                var options = new List<string> { "All Categories" };
                options.AddRange(System.Enum.GetNames(typeof(SiteCategory)));
                m_CategoryFilter.AddOptions(options);
            }

            // Region dropdown
            if (m_RegionFilter != null)
            {
                m_RegionFilter.ClearOptions();
                var options = new List<string> { "All Regions" };
                options.AddRange(m_SiteDatabase.GetAllRegions());
                m_RegionFilter.AddOptions(options);
            }
        }

        // --- Internal ---

        private void RebuildCards()
        {
            // Clear existing cards
            foreach (var card in m_SpawnedCards)
            {
                if (card != null) Destroy(card);
            }
            m_SpawnedCards.Clear();

            // Show/hide empty state
            if (m_EmptyStatePanel != null)
            {
                m_EmptyStatePanel.SetActive(m_FilteredSites.Count == 0);
            }

            if (m_SiteCardPrefab == null || m_CardContainer == null) return;

            // Spawn cards
            foreach (var site in m_FilteredSites)
            {
                var cardObj = Instantiate(m_SiteCardPrefab, m_CardContainer);
                var card = cardObj.GetComponent<SiteCard>();
                if (card != null)
                {
                    card.Setup(site, OnCardDetailsTapped, OnCardPreviewTapped);
                }
                m_SpawnedCards.Add(cardObj);
            }
        }

        private void OnCardDetailsTapped(HistoricalSite site)
        {
            OnSiteSelected?.Invoke(site);
            if (m_NavigationManager != null)
            {
                m_NavigationManager.ShowSiteDetail();
            }
        }

        private void OnCardPreviewTapped(HistoricalSite site)
        {
            // Launch AR preview directly from the card
            if (m_ARExperienceManager != null)
            {
                m_ARExperienceManager.StartPreviewExperience(site);
            }
            if (m_NavigationManager != null)
            {
                m_NavigationManager.EnterARExperience();
            }
        }

        private void OnSearchChanged(string query)
        {
            RefreshCatalog();
        }

        private void OnFilterChanged(int _)
        {
            RefreshCatalog();
        }
    }
}
