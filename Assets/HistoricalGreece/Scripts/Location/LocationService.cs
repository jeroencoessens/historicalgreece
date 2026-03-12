using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

namespace HistoricalGreece.Location
{
    /// <summary>
    /// Manages GPS location services with proper permission handling for iOS and Android.
    /// Provides continuous location updates and events for other systems to subscribe to.
    /// Attach to a persistent GameObject (e.g., AppManager).
    /// </summary>
    public class GPSLocationService : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Desired accuracy in meters. Lower = more battery usage.")]
        [SerializeField] private float m_DesiredAccuracyMeters = 10f;

        [Tooltip("Minimum distance change in meters before an update event fires.")]
        [SerializeField] private float m_UpdateDistanceMeters = 5f;

        [Tooltip("How often to poll for location updates (seconds).")]
        [SerializeField] private float m_PollIntervalSeconds = 2f;

        [Tooltip("Maximum time to wait for location service initialization (seconds).")]
        [SerializeField] private float m_InitTimeoutSeconds = 20f;

        // --- Public State ---

        /// <summary>Current GPS latitude, or 0 if unavailable.</summary>
        public double Latitude { get; private set; }

        /// <summary>Current GPS longitude, or 0 if unavailable.</summary>
        public double Longitude { get; private set; }

        /// <summary>Horizontal accuracy of the last reading in meters.</summary>
        public float Accuracy { get; private set; }

        /// <summary>Timestamp of the last successful GPS reading.</summary>
        public double LastTimestamp { get; private set; }

        /// <summary>True when GPS is actively providing location data.</summary>
        public bool IsLocationAvailable { get; private set; }

        /// <summary>Current compass heading in degrees (0 = North). -1 if unavailable.</summary>
        public float CompassHeading { get; private set; } = -1f;

        /// <summary>True if compass is enabled and providing data.</summary>
        public bool IsCompassAvailable { get; private set; }

        /// <summary>Current status of the location service.</summary>
        public GPSServiceStatus Status { get; private set; } = GPSServiceStatus.Stopped;

        // --- Events ---

        /// <summary>
        /// Fires when a new GPS position is available.
        /// Parameters: latitude, longitude, accuracy.
        /// </summary>
        public event Action<double, double, float> OnLocationUpdated;

        /// <summary>
        /// Fires when location service status changes (e.g., started, failed, permission denied).
        /// </summary>
        public event Action<GPSServiceStatus> OnStatusChanged;

        /// <summary>
        /// Fires when compass heading changes.
        /// Parameter: heading in degrees from North.
        /// </summary>
        public event Action<float> OnCompassUpdated;

        // --- Internal State ---
        private Coroutine m_LocationCoroutine;

        /// <summary>True after the user has granted location permission this session.</summary>
        public bool PermissionGranted { get; private set; }

        // --- Public API ---

        /// <summary>
        /// Begin requesting location permissions and start GPS tracking.
        /// Safe to call multiple times; will not double-start.
        /// </summary>
        public void StartTracking()
        {
            if (m_LocationCoroutine != null) return;
            m_LocationCoroutine = StartCoroutine(LocationRoutine());
        }

        /// <summary>
        /// Stop GPS tracking to save battery.
        /// </summary>
        public void StopTracking()
        {
            if (m_LocationCoroutine != null)
            {
                StopCoroutine(m_LocationCoroutine);
                m_LocationCoroutine = null;
            }

            if (Input.location.status == UnityEngine.LocationServiceStatus.Running)
            {
                Input.location.Stop();
            }

            if (IsCompassAvailable)
            {
                Input.compass.enabled = false;
                IsCompassAvailable = false;
            }

            IsLocationAvailable = false;
            SetStatus(GPSServiceStatus.Stopped);
        }

        // --- Lifecycle ---

        private void OnDisable()
        {
            StopTracking();
        }

        private void OnDestroy()
        {
            StopTracking();
        }

        // --- Core Location Routine ---

        private IEnumerator LocationRoutine()
        {
            SetStatus(GPSServiceStatus.RequestingPermission);

            // --- Permission Handling ---
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);

                // Wait a frame for the dialog, then poll for result
                yield return new WaitForSeconds(0.5f);

                float permWait = 0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && permWait < 30f)
                {
                    permWait += 0.5f;
                    yield return new WaitForSeconds(0.5f);
                }

                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    Debug.LogWarning("[LocationService] Fine location permission denied by user.");
                    SetStatus(GPSServiceStatus.PermissionDenied);
                    m_LocationCoroutine = null;
                    yield break;
                }
            }
#endif

#if UNITY_IOS
            // iOS permissions are handled automatically by Unity's Input.location.Start()
            // but we check if location services are enabled at the system level.
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[LocationService] Location services disabled in device settings.");
                SetStatus(GPSServiceStatus.PermissionDenied);
                m_LocationCoroutine = null;
                yield break;
            }
#endif

            PermissionGranted = true;
            SetStatus(GPSServiceStatus.Initializing);

            // --- Start Location Service ---
            Input.location.Start(m_DesiredAccuracyMeters, m_UpdateDistanceMeters);

            // Wait for initialization
            float initWait = 0f;
            while (Input.location.status == UnityEngine.LocationServiceStatus.Initializing
                   && initWait < m_InitTimeoutSeconds)
            {
                initWait += 1f;
                yield return new WaitForSeconds(1f);
            }

            if (Input.location.status == UnityEngine.LocationServiceStatus.Failed)
            {
                Debug.LogWarning("[GPSLocationService] Location service failed to initialize.");
                SetStatus(GPSServiceStatus.Failed);
                m_LocationCoroutine = null;
                yield break;
            }

            if (Input.location.status != UnityEngine.LocationServiceStatus.Running)
            {
                Debug.LogWarning($"[GPSLocationService] Unexpected status after init: {Input.location.status}");
                SetStatus(GPSServiceStatus.Failed);
                m_LocationCoroutine = null;
                yield break;
            }

            // --- Start Compass ---
            Input.compass.enabled = true;
            IsCompassAvailable = true;

            IsLocationAvailable = true;
            SetStatus(GPSServiceStatus.Running);
            Debug.Log("[GPSLocationService] GPS tracking started successfully.");

            // --- Continuous Polling Loop ---
            while (true)
            {
                if (Input.location.status == UnityEngine.LocationServiceStatus.Running)
                {
                    var loc = Input.location.lastData;
                    double newLat = loc.latitude;
                    double newLon = loc.longitude;
                    float newAcc = loc.horizontalAccuracy;

                    // Only fire update if position actually changed
                    if (Math.Abs(newLat - Latitude) > 0.000001 ||
                        Math.Abs(newLon - Longitude) > 0.000001)
                    {
                        Latitude = newLat;
                        Longitude = newLon;
                        Accuracy = newAcc;
                        LastTimestamp = loc.timestamp;

                        OnLocationUpdated?.Invoke(Latitude, Longitude, Accuracy);
                    }
                }

                // Update compass
                if (IsCompassAvailable && Input.compass.enabled)
                {
                    float newHeading = Input.compass.trueHeading;
                    if (Math.Abs(newHeading - CompassHeading) > 1f)
                    {
                        CompassHeading = newHeading;
                        OnCompassUpdated?.Invoke(CompassHeading);
                    }
                }

                yield return new WaitForSeconds(m_PollIntervalSeconds);
            }
        }

        private void SetStatus(GPSServiceStatus newStatus)
        {
            if (Status == newStatus) return;
            Status = newStatus;
            OnStatusChanged?.Invoke(Status);
            Debug.Log($"[GPSLocationService] Status changed to: {Status}");
        }
    }

    /// <summary>
    /// Extended status enum that includes permission states beyond Unity's built-in enum.
    /// </summary>
    public enum GPSServiceStatus
    {
        Stopped,
        RequestingPermission,
        PermissionDenied,
        Initializing,
        Running,
        Failed
    }
}
