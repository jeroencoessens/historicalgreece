using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using HistoricalGreece.Core;
using HistoricalGreece.Location;

namespace HistoricalGreece.AR
{
    /// <summary>
    /// Central manager for all AR experiences. Handles two modes:
    /// 1. On-Location: GPS-aligned full-scale reconstruction at the real site
    /// 2. Preview: Scaled model placed on a detected surface (from home)
    ///
    /// Sits on the same GameObject as the XR Origin or a child of it.
    /// Coordinates with ARPlaneManager, LocationService, and the UI layer.
    /// </summary>
    public class ARExperienceManager : MonoBehaviour
    {
        [Header("AR Foundation References")]
        [Tooltip("The AR Session in the scene")]
        [SerializeField] private ARSession m_ARSession;

        [Tooltip("The AR Plane Manager used for surface detection")]
        [SerializeField] private ARPlaneManager m_PlaneManager;

        [Tooltip("The AR Raycast Manager for tap-to-place")]
        [SerializeField] private ARRaycastManager m_RaycastManager;

        [Tooltip("The AR Anchor Manager for persistent placement")]
        [SerializeField] private ARAnchorManager m_AnchorManager;

        [Header("Location References")]
        [Tooltip("GPS service for on-location mode alignment")]
        [SerializeField] private GPSLocationService m_LocationService;

        [Header("Configuration")]
        [Tooltip("Parent transform under which AR content is spawned")]
        [SerializeField] private Transform m_ContentParent;

        [Tooltip("Maximum number of planes to detect before auto-hiding plane visualization")]
        [SerializeField] private int m_AutoHidePlanesAfterCount = 5;

        /// <summary>Public accessor for auto-hide threshold.</summary>
        public int AutoHidePlanesAfterCount => m_AutoHidePlanesAfterCount;

        // --- Public State ---

        /// <summary>Currently active AR mode.</summary>
        public ARMode CurrentMode { get; private set; } = ARMode.Inactive;

        /// <summary>The site currently being displayed in AR.</summary>
        public HistoricalSite CurrentSite { get; private set; }

        /// <summary>The instantiated AR content GameObject.</summary>
        public GameObject CurrentARContent { get; private set; }

        /// <summary>True when AR content is placed and visible.</summary>
        public bool IsContentPlaced { get; private set; }

        // --- Events ---

        /// <summary>Fires when the AR mode changes.</summary>
        public event Action<ARMode> OnModeChanged;

        /// <summary>Fires when AR content is successfully placed in the scene.</summary>
        public event Action<HistoricalSite, GameObject> OnContentPlaced;

        /// <summary>Fires when AR content is removed from the scene.</summary>
        public event Action OnContentRemoved;

        /// <summary>Fires when plane detection finds a suitable surface.</summary>
        public event Action OnSurfaceDetected;

        // --- Internal ---
        private bool m_SurfaceDetectedFired;
        private int m_DetectedPlaneCount;

        // --- Public API ---

        /// <summary>
        /// Launch AR in on-location mode. Uses GPS + compass to position
        /// the reconstruction at real-world scale and orientation.
        /// Called when the user is physically near a historical site.
        /// </summary>
        public void StartOnLocationExperience(HistoricalSite site)
        {
            if (site == null)
            {
                Debug.LogError("[ARExperienceManager] Cannot start on-location experience: site is null.");
                return;
            }

            ClearCurrentContent();
            CurrentSite = site;
            SetMode(ARMode.OnLocation);

            // Enable plane detection for ground reference
            EnablePlaneDetection(true);

            Debug.Log($"[ARExperienceManager] Starting on-location AR for: {site.siteName}");

            // In on-location mode, we wait for a ground plane, then place the content
            // aligned using GPS heading offset
        }

        /// <summary>
        /// Launch AR in preview mode. The user can place a scaled model
        /// on any detected surface — useful for exploring from home.
        /// </summary>
        public void StartPreviewExperience(HistoricalSite site)
        {
            if (site == null)
            {
                Debug.LogError("[ARExperienceManager] Cannot start preview experience: site is null.");
                return;
            }

            ClearCurrentContent();
            CurrentSite = site;
            SetMode(ARMode.Preview);

            // Enable plane detection so user can find a surface
            EnablePlaneDetection(true);

            Debug.Log($"[ARExperienceManager] Starting preview AR for: {site.siteName}");
        }

        /// <summary>
        /// Place the AR content at the given world position and rotation.
        /// Called by the placement interaction system when the user taps a surface.
        /// </summary>
        public void PlaceContent(Vector3 position, Quaternion rotation)
        {
            if (CurrentSite == null || CurrentMode == ARMode.Inactive)
            {
                Debug.LogWarning("[ARExperienceManager] No active site to place content for.");
                return;
            }

            ClearCurrentContent();

            GameObject prefab = CurrentMode == ARMode.OnLocation
                ? CurrentSite.arPrefabOnLocation
                : CurrentSite.arPrefabPreview;

            if (prefab == null)
            {
                // Fall back to the other prefab if one is not set
                prefab = CurrentSite.arPrefabOnLocation ?? CurrentSite.arPrefabPreview;
            }

            if (prefab == null)
            {
                Debug.LogError($"[ARExperienceManager] No AR prefab assigned for site: {CurrentSite.siteName}");
                return;
            }

            // Determine scale and rotation based on mode
            float scale;
            Quaternion finalRotation;

            if (CurrentMode == ARMode.OnLocation)
            {
                scale = CurrentSite.onLocationScale;
                // Apply heading offset for real-world alignment
                float headingOffset = CurrentSite.headingOffsetDegrees;
                if (m_LocationService != null && m_LocationService.IsCompassAvailable)
                {
                    // Rotate content to align with geographic North + site-specific offset
                    finalRotation = Quaternion.Euler(0f, headingOffset, 0f);
                }
                else
                {
                    finalRotation = rotation * Quaternion.Euler(0f, headingOffset, 0f);
                }
            }
            else // Preview mode
            {
                scale = CurrentSite.previewScale;
                finalRotation = rotation;
            }

            // Instantiate content
            Transform parent = m_ContentParent != null ? m_ContentParent : transform;
            CurrentARContent = Instantiate(prefab, position, finalRotation, parent);
            CurrentARContent.transform.localScale = Vector3.one * scale;
            CurrentARContent.name = $"AR_{CurrentSite.siteName}";

            IsContentPlaced = true;
            OnContentPlaced?.Invoke(CurrentSite, CurrentARContent);

            // Optionally hide planes after placement for cleaner view
            if (CurrentMode == ARMode.Preview)
            {
                EnablePlaneDetection(false);
            }

            Debug.Log($"[ARExperienceManager] Placed {CurrentSite.siteName} at {position} (scale: {scale})");
        }

        /// <summary>
        /// Remove the current AR content and return to inactive state.
        /// </summary>
        public void EndExperience()
        {
            ClearCurrentContent();
            CurrentSite = null;
            SetMode(ARMode.Inactive);
            EnablePlaneDetection(false);
            Debug.Log("[ARExperienceManager] AR experience ended.");
        }

        /// <summary>
        /// Reset placement so the user can reposition the model.
        /// Keeps the same site and mode active.
        /// </summary>
        public void ResetPlacement()
        {
            if (CurrentARContent != null)
            {
                Destroy(CurrentARContent);
                CurrentARContent = null;
            }

            IsContentPlaced = false;
            m_SurfaceDetectedFired = false;
            EnablePlaneDetection(true);
            OnContentRemoved?.Invoke();
        }

        // --- Internal ---

        private void SetMode(ARMode newMode)
        {
            if (CurrentMode == newMode) return;
            CurrentMode = newMode;
            OnModeChanged?.Invoke(CurrentMode);
            Debug.Log($"[ARExperienceManager] Mode changed to: {CurrentMode}");
        }

        private void ClearCurrentContent()
        {
            if (CurrentARContent != null)
            {
                Destroy(CurrentARContent);
                CurrentARContent = null;
            }

            IsContentPlaced = false;
            m_SurfaceDetectedFired = false;
            OnContentRemoved?.Invoke();
        }

        private void EnablePlaneDetection(bool enabled)
        {
            if (m_PlaneManager != null)
            {
                m_PlaneManager.enabled = enabled;
            }
        }

        private void OnEnable()
        {
            if (m_PlaneManager != null)
            {
                m_PlaneManager.trackablesChanged.AddListener(OnPlanesChanged);
            }
        }

        private void OnDisable()
        {
            if (m_PlaneManager != null)
            {
                m_PlaneManager.trackablesChanged.RemoveListener(OnPlanesChanged);
            }
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            m_DetectedPlaneCount += args.added.Count;
            m_DetectedPlaneCount -= args.removed.Count;

            if (!m_SurfaceDetectedFired && args.added.Count > 0)
            {
                m_SurfaceDetectedFired = true;
                OnSurfaceDetected?.Invoke();
            }
        }
    }

    /// <summary>
    /// The two AR operating modes of the app.
    /// </summary>
    public enum ARMode
    {
        /// <summary>AR is not active.</summary>
        Inactive,

        /// <summary>User is physically at a historical site — full-scale reconstruction.</summary>
        OnLocation,

        /// <summary>User is browsing remotely — scaled model on any surface.</summary>
        Preview
    }
}
