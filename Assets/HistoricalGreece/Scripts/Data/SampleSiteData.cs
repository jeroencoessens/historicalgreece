using UnityEngine;
using HistoricalGreece.Core;

namespace HistoricalGreece.Data
{
    /// <summary>
    /// Utility to create sample HistoricalSite ScriptableObjects for testing.
    /// Run via the Unity menu: Historical Greece > Create Sample Sites.
    ///
    /// These are real-world coordinates and data for Greek historical sites.
    /// Replace the placeholder prefabs with actual 3D reconstructed models.
    /// </summary>
    public static class SampleSiteData
    {
        /// <summary>
        /// Reference data for the initial curated sites.
        /// Use this to create ScriptableObjects in the editor.
        /// </summary>
        public static readonly SiteDefinition[] Sites = new[]
        {
            new SiteDefinition
            {
                name = "The Parthenon",
                tagline = "Temple of Athena, 447–432 BC",
                description = "The Parthenon is the most iconic monument of Ancient Greece, dedicated to the goddess Athena Parthenos. " +
                    "Built between 447 and 432 BC during the height of the Athenian Empire, it stands atop the Acropolis of Athens. " +
                    "The temple represents the pinnacle of Doric architecture and once housed a magnificent gold and ivory statue of Athena. " +
                    "Through AR, experience the Parthenon restored to its original splendor — complete with painted sculptures and intact pediments.",
                period = HistoricalPeriod.Classical,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Temple,
                latitude = 37.9715,
                longitude = 23.7267,
                activationRadius = 150f,
                headingOffset = 0f,
                region = "Athens",
                country = "Greece",
                travelTips = "Open daily. Summer: 8AM–8PM, Winter: 8AM–5PM. " +
                    "Combined Acropolis ticket available. Arrive early to avoid crowds. " +
                    "Comfortable shoes recommended for uneven surfaces.",
                visitMinutes = 120,
                accessibility = 4
            },
            new SiteDefinition
            {
                name = "Temple of Olympian Zeus",
                tagline = "The Olympieion, 6th century BC – 2nd century AD",
                description = "The Temple of Olympian Zeus (Olympieion) was one of the largest temples in the ancient world. " +
                    "Construction began in the 6th century BC but wasn't completed until Emperor Hadrian finished it in 131 AD. " +
                    "Originally featuring 104 colossal Corinthian columns, only 16 remain standing today. " +
                    "In AR, witness the temple in its complete form with all columns and the massive chryselephantine statue of Zeus.",
                period = HistoricalPeriod.Classical,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Temple,
                latitude = 37.9693,
                longitude = 23.7331,
                activationRadius = 120f,
                headingOffset = 0f,
                region = "Athens",
                country = "Greece",
                travelTips = "Located southeast of the Acropolis. Included in the combined ticket. " +
                    "The nearby Hadrian's Arch is free to visit.",
                visitMinutes = 45,
                accessibility = 2
            },
            new SiteDefinition
            {
                name = "Ancient Agora of Athens",
                tagline = "The civic heart of classical Athens",
                description = "The Ancient Agora served as the commercial, political, and social center of Athens for over a thousand years. " +
                    "It was here that democracy was born, philosophers debated, and merchants traded. " +
                    "Key structures include the well-preserved Temple of Hephaestus, the Stoa of Attalos (reconstructed), " +
                    "and the remains of civic buildings. AR brings the bustling Classical-era agora back to life.",
                period = HistoricalPeriod.Classical,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Agora,
                latitude = 37.9747,
                longitude = 23.7228,
                activationRadius = 200f,
                headingOffset = 0f,
                region = "Athens",
                country = "Greece",
                travelTips = "Included in combined Acropolis ticket. The Stoa of Attalos houses an excellent museum. " +
                    "Allow at least an hour to explore the full site.",
                visitMinutes = 90,
                accessibility = 3
            },
            new SiteDefinition
            {
                name = "Theater of Epidaurus",
                tagline = "Masterpiece of ancient acoustics, 4th century BC",
                description = "The Theater of Epidaurus is renowned for its exceptional acoustics and harmonious proportions. " +
                    "Built in the 4th century BC, it could seat up to 14,000 spectators. A whisper from the orchestra circle " +
                    "can be heard clearly in the highest rows. The theater is still used today for performances during the " +
                    "annual Athens & Epidaurus Festival. In AR, see the theater complete with its original painted stage building.",
                period = HistoricalPeriod.Classical,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Theater,
                latitude = 37.5960,
                longitude = 23.0790,
                activationRadius = 200f,
                headingOffset = 0f,
                region = "Peloponnese",
                country = "Greece",
                travelTips = "About 2 hours from Athens by car. Part of the Sanctuary of Asklepios UNESCO site. " +
                    "Summer festival performances sell out — book early.",
                visitMinutes = 60,
                accessibility = 3
            },
            new SiteDefinition
            {
                name = "Palace of Knossos",
                tagline = "Seat of the Minoan civilization, ~1700 BC",
                description = "The Palace of Knossos on Crete was the ceremonial and political center of the Minoan civilization, " +
                    "Europe's first advanced civilization. The sprawling complex featured over 1,000 rooms, elaborate frescoes, " +
                    "and advanced plumbing. Associated with the myth of the Labyrinth and Minotaur, " +
                    "the site was partially reconstructed by Arthur Evans in the early 20th century. " +
                    "AR reveals the palace in its Minoan glory with vibrant frescoes and grand ceremonial halls.",
                period = HistoricalPeriod.Archaic,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Palace,
                latitude = 35.2979,
                longitude = 25.1633,
                activationRadius = 250f,
                headingOffset = 0f,
                region = "Crete",
                country = "Greece",
                travelTips = "5 km south of Heraklion. Open daily 8AM–7PM (summer). " +
                    "Visit the Heraklion Archaeological Museum for original frescoes. " +
                    "Morning visits recommended to beat tour groups.",
                visitMinutes = 120,
                accessibility = 3
            },
            new SiteDefinition
            {
                name = "Temple of Apollo at Delphi",
                tagline = "Center of the ancient world, 4th century BC",
                description = "The Temple of Apollo at Delphi was considered the navel of the world (omphalos) in ancient Greece. " +
                    "Home to the famous Oracle, the Pythia, kings and generals from across the Mediterranean came to seek prophecy. " +
                    "The current temple ruins date to the 4th century BC, the third temple built on this site. " +
                    "AR reconstructs the temple with its iconic Doric columns and the sacred inner chamber.",
                period = HistoricalPeriod.Classical,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Temple,
                latitude = 38.4824,
                longitude = 22.5012,
                activationRadius = 200f,
                headingOffset = 0f,
                region = "Central Greece",
                country = "Greece",
                travelTips = "About 2.5 hours from Athens. The Delphi Archaeological Museum is a must-see. " +
                    "The site is on a hillside — wear sturdy shoes.",
                visitMinutes = 150,
                accessibility = 4
            },
            new SiteDefinition
            {
                name = "Ancient Olympia",
                tagline = "Birthplace of the Olympic Games, 776 BC",
                description = "Ancient Olympia in the Peloponnese was the site of the original Olympic Games from 776 BC to 393 AD. " +
                    "The sanctuary includes the remains of the Temple of Zeus (which once housed one of the Seven Wonders of the Ancient World), " +
                    "the Temple of Hera, the gymnasium, and the original stadium. " +
                    "AR brings back the grandeur of the sacred precinct during the games.",
                period = HistoricalPeriod.Classical,
                civilization = CivilizationType.Greek,
                category = SiteCategory.Stadium,
                latitude = 37.6388,
                longitude = 21.6300,
                activationRadius = 300f,
                headingOffset = 0f,
                region = "Peloponnese",
                country = "Greece",
                travelTips = "The Archaeological Museum of Olympia is world-class. " +
                    "Run in the original stadium for the full experience. Allow half a day.",
                visitMinutes = 180,
                accessibility = 2
            },
        };

        /// <summary>
        /// Lightweight data container used for sample site creation.
        /// </summary>
        public struct SiteDefinition
        {
            public string name;
            public string tagline;
            public string description;
            public HistoricalPeriod period;
            public CivilizationType civilization;
            public SiteCategory category;
            public double latitude;
            public double longitude;
            public float activationRadius;
            public float headingOffset;
            public string region;
            public string country;
            public string travelTips;
            public int visitMinutes;
            public int accessibility;
        }
    }
}
