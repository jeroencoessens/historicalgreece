#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HistoricalGreece.Core;
using HistoricalGreece.Data;

namespace HistoricalGreece.Editor
{
    /// <summary>
    /// Editor utility to quickly generate sample HistoricalSite ScriptableObjects
    /// and a SiteDatabase from the curated sample data.
    /// Access via menu: Historical Greece > Create Sample Sites.
    /// </summary>
    public static class SiteCreatorEditor
    {
        private const string SitesFolder = "Assets/HistoricalGreece/Data/Sites";
        private const string DatabasePath = "Assets/HistoricalGreece/Data/SiteDatabase.asset";

        [MenuItem("Historical Greece/Create Sample Sites", false, 100)]
        public static void CreateSampleSites()
        {
            // Ensure folders exist
            EnsureFolder("Assets/HistoricalGreece");
            EnsureFolder("Assets/HistoricalGreece/Data");
            EnsureFolder(SitesFolder);

            // Create site ScriptableObjects
            var sites = new System.Collections.Generic.List<HistoricalSite>();

            foreach (var def in SampleSiteData.Sites)
            {
                string safeName = def.name.Replace(" ", "").Replace("'", "");
                string assetPath = $"{SitesFolder}/{safeName}.asset";

                // Check if already exists
                var existing = AssetDatabase.LoadAssetAtPath<HistoricalSite>(assetPath);
                if (existing != null)
                {
                    sites.Add(existing);
                    Debug.Log($"[SiteCreator] Skipped existing: {def.name}");
                    continue;
                }

                var site = ScriptableObject.CreateInstance<HistoricalSite>();
                site.siteName = def.name;
                site.tagline = def.tagline;
                site.description = def.description;
                site.period = def.period;
                site.civilization = def.civilization;
                site.category = def.category;
                site.latitude = def.latitude;
                site.longitude = def.longitude;
                site.activationRadiusMeters = def.activationRadius;
                site.headingOffsetDegrees = def.headingOffset;
                site.regionName = def.region;
                site.countryName = def.country;
                site.travelTips = def.travelTips;
                site.estimatedVisitMinutes = def.visitMinutes;
                site.accessibilityRating = def.accessibility;
                site.isActive = true;

                AssetDatabase.CreateAsset(site, assetPath);
                sites.Add(site);
                Debug.Log($"[SiteCreator] Created: {def.name} at {assetPath}");
            }

            // Create or update the SiteDatabase
            var database = AssetDatabase.LoadAssetAtPath<SiteDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<SiteDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.allSites.Clear();
            database.allSites.AddRange(sites);
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SiteCreator] Created {sites.Count} sites and updated SiteDatabase.");
            EditorUtility.DisplayDialog(
                "Historical Greece",
                $"Created {sites.Count} sample historical sites and SiteDatabase.\n\n" +
                "Next steps:\n" +
                "1. Assign thumbnail images to each site\n" +
                "2. Create/import 3D AR reconstruction models\n" +
                "3. Assign AR prefabs to each site\n" +
                "4. Drag the SiteDatabase into AppManager",
                "OK");

            Selection.activeObject = database;
        }

        [MenuItem("Historical Greece/Select Site Database", false, 101)]
        public static void SelectSiteDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<SiteDatabase>(DatabasePath);
            if (database != null)
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
            }
            else
            {
                EditorUtility.DisplayDialog("Historical Greece",
                    "SiteDatabase not found. Use 'Create Sample Sites' first.", "OK");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path);
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
