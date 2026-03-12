using UnityEngine;

namespace HistoricalGreece.Core
{
    /// <summary>
    /// Defines a single historical site with all metadata needed for both
    /// on-location AR experiences and remote preview browsing.
    /// Create instances via Assets > Create > Historical Greece > Historical Site.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHistoricalSite", menuName = "Historical Greece/Historical Site", order = 0)]
    public class HistoricalSite : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown to the user (e.g. 'The Parthenon')")]
        public string siteName;

        [Tooltip("Short tagline for cards (e.g. 'Temple of Athena, 447 BC')")]
        public string tagline;

        [TextArea(3, 8)]
        [Tooltip("Full historical description shown in the detail view")]
        public string description;

        [Header("Classification")]
        [Tooltip("Historical period this site belongs to")]
        public HistoricalPeriod period = HistoricalPeriod.Classical;

        [Tooltip("Civilization or culture")]
        public CivilizationType civilization = CivilizationType.Greek;

        [Tooltip("Type of structure")]
        public SiteCategory category = SiteCategory.Temple;

        [Header("GPS Location")]
        [Tooltip("Latitude of the real-world site")]
        [Range(-90f, 90f)]
        public double latitude;

        [Tooltip("Longitude of the real-world site")]
        [Range(-180f, 180f)]
        public double longitude;

        [Tooltip("Radius in meters within which the on-location AR activates")]
        [Range(10f, 500f)]
        public float activationRadiusMeters = 100f;

        [Tooltip("Heading offset in degrees for AR model alignment (0 = North)")]
        [Range(0f, 360f)]
        public float headingOffsetDegrees;

        [Header("AR Content")]
        [Tooltip("The AR prefab instantiated for the full on-location experience")]
        public GameObject arPrefabOnLocation;

        [Tooltip("A smaller-scale prefab used in remote preview mode (can be same as above)")]
        public GameObject arPrefabPreview;

        [Tooltip("Scale multiplier applied in preview mode (model placed on surface)")]
        public float previewScale = 0.05f;

        [Tooltip("Scale multiplier for on-location (1.0 = real-world size)")]
        public float onLocationScale = 1.0f;

        [Header("Visuals")]
        [Tooltip("Thumbnail image for cards and lists")]
        public Sprite thumbnail;

        [Tooltip("Gallery images shown in the detail view")]
        public Sprite[] galleryImages;

        [Tooltip("Primary accent color for this site's UI elements")]
        public Color accentColor = new Color(0.85f, 0.65f, 0.25f); // warm gold

        [Header("Tourism Info")]
        [Tooltip("City or region name for grouping")]
        public string regionName;

        [Tooltip("Country name")]
        public string countryName = "Greece";

        [TextArea(2, 5)]
        [Tooltip("Practical travel tips (opening hours, tickets, getting there)")]
        public string travelTips;

        [Tooltip("Estimated visit duration in minutes")]
        [Range(15, 480)]
        public int estimatedVisitMinutes = 60;

        [Tooltip("Accessibility rating: 1 = wheelchair accessible, 5 = difficult terrain")]
        [Range(1, 5)]
        public int accessibilityRating = 3;

        [Tooltip("Is this site currently available / published in the app?")]
        public bool isActive = true;

        /// <summary>
        /// Returns a formatted string like "Classical Period (447 BC – 432 BC)"
        /// Override with custom text if needed.
        /// </summary>
        [Header("Display Overrides")]
        [Tooltip("Optional custom period label (leave empty to auto-generate from enum)")]
        public string customPeriodLabel;

        /// <summary>
        /// Get the display label for the historical period.
        /// </summary>
        public string PeriodLabel =>
            string.IsNullOrEmpty(customPeriodLabel)
                ? period.ToDisplayString()
                : customPeriodLabel;
    }

    /// <summary>
    /// Historical periods for classification and filtering.
    /// </summary>
    public enum HistoricalPeriod
    {
        Archaic,        // ~800–480 BC
        Classical,      // ~480–323 BC
        Hellenistic,    // ~323–31 BC
        Roman,          // ~31 BC – 330 AD
        Byzantine,      // ~330–1453 AD
        Ottoman,        // ~1453–1821 AD
        Modern          // 1821+
    }

    /// <summary>
    /// Civilization types — expandable for future non-Greek content.
    /// </summary>
    public enum CivilizationType
    {
        Greek,
        Roman,
        Byzantine,
        Egyptian,
        Persian,
        Other
    }

    /// <summary>
    /// Categories of historical structures.
    /// </summary>
    public enum SiteCategory
    {
        Temple,
        Theater,
        Agora,
        Fortress,
        Palace,
        Tomb,
        Stadium,
        Monument,
        Aqueduct,
        Other
    }

    /// <summary>
    /// Extension methods for enums used in display.
    /// </summary>
    public static class HistoricalPeriodExtensions
    {
        public static string ToDisplayString(this HistoricalPeriod period)
        {
            return period switch
            {
                HistoricalPeriod.Archaic     => "Archaic Period (800–480 BC)",
                HistoricalPeriod.Classical   => "Classical Period (480–323 BC)",
                HistoricalPeriod.Hellenistic => "Hellenistic Period (323–31 BC)",
                HistoricalPeriod.Roman       => "Roman Period (31 BC – 330 AD)",
                HistoricalPeriod.Byzantine   => "Byzantine Period (330–1453 AD)",
                HistoricalPeriod.Ottoman     => "Ottoman Period (1453–1821 AD)",
                HistoricalPeriod.Modern      => "Modern Era (1821–Present)",
                _                            => period.ToString()
            };
        }
    }
}
