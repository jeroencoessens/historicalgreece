using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using HistoricalGreece.Core;

namespace HistoricalGreece.AR
{
    /// <summary>
    /// Handles the tap-to-place interaction for Preview mode.
    /// Raycasts against detected planes and lets the user place/reposition
    /// the scaled AR model with simple touch gestures.
    ///
    /// Tourist-friendly: single tap to place, drag to rotate, pinch to scale.
    /// </summary>
    [RequireComponent(typeof(ARExperienceManager))]
    public class PreviewPlacementManager : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARRaycastManager m_RaycastManager;
        [SerializeField] private Camera m_ARCamera;

        [Header("Interaction Settings")]
        [Tooltip("Allow the user to reposition after initial placement by tapping again")]
        [SerializeField] private bool m_AllowReposition = true;

        [Tooltip("Minimum pinch-to-scale factor")]
        [SerializeField] private float m_MinScale = 0.01f;

        [Tooltip("Maximum pinch-to-scale factor")]
        [SerializeField] private float m_MaxScale = 2.0f;

        [Tooltip("Rotation speed multiplier for drag-to-rotate")]
        [SerializeField] private float m_RotationSpeed = 0.5f;

        [Header("Visual Feedback")]
        [Tooltip("Optional placement indicator prefab (reticle shown before placement)")]
        [SerializeField] private GameObject m_PlacementIndicatorPrefab;

        // --- Internal ---
        private ARExperienceManager m_ExperienceManager;
        private GameObject m_PlacementIndicator;
        private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
        private bool m_IsActive;
        private bool m_IsRotating;
        private float m_PreviousTouchAngle;
        private float m_InitialPinchDistance;
        private float m_InitialScale;

        // --- Lifecycle ---

        private void Awake()
        {
            m_ExperienceManager = GetComponent<ARExperienceManager>();

            if (m_PlacementIndicatorPrefab != null)
            {
                m_PlacementIndicator = Instantiate(m_PlacementIndicatorPrefab);
                m_PlacementIndicator.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (m_ExperienceManager != null)
            {
                m_ExperienceManager.OnModeChanged += HandleModeChanged;
            }
        }

        private void OnDisable()
        {
            if (m_ExperienceManager != null)
            {
                m_ExperienceManager.OnModeChanged -= HandleModeChanged;
            }

            if (m_PlacementIndicator != null)
            {
                m_PlacementIndicator.SetActive(false);
            }
        }

        private void Update()
        {
            if (!m_IsActive) return;

            // Show placement indicator when content is not yet placed
            if (!m_ExperienceManager.IsContentPlaced)
            {
                UpdatePlacementIndicator();
                HandlePlacementTap();
            }
            else
            {
                // Handle manipulation gestures on placed content
                HandleRotation();
                HandlePinchScale();

                if (m_AllowReposition)
                {
                    HandleRepositionTap();
                }
            }
        }

        // --- Mode Handling ---

        private void HandleModeChanged(ARMode mode)
        {
            m_IsActive = (mode == ARMode.Preview);

            if (m_PlacementIndicator != null)
            {
                m_PlacementIndicator.SetActive(false);
            }
        }

        // --- Placement Indicator ---

        /// <summary>
        /// Shows a reticle on the nearest detected plane to guide the user.
        /// </summary>
        private void UpdatePlacementIndicator()
        {
            if (m_RaycastManager == null || m_ARCamera == null) return;

            // Raycast from screen center
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            if (m_RaycastManager.Raycast(screenCenter, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                var hit = s_Hits[0];
                if (m_PlacementIndicator != null)
                {
                    m_PlacementIndicator.SetActive(true);
                    m_PlacementIndicator.transform.position = hit.pose.position;
                    m_PlacementIndicator.transform.rotation = hit.pose.rotation;
                }
            }
            else
            {
                if (m_PlacementIndicator != null)
                {
                    m_PlacementIndicator.SetActive(false);
                }
            }
        }

        // --- Tap to Place ---

        /// <summary>
        /// Single-finger tap places the model on the detected surface.
        /// </summary>
        private void HandlePlacementTap()
        {
            if (Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended) return;

            // Don't place if touch is over UI
            if (IsPointerOverUI(touch.position)) return;

            if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                var hit = s_Hits[0];
                m_ExperienceManager.PlaceContent(hit.pose.position, hit.pose.rotation);

                if (m_PlacementIndicator != null)
                {
                    m_PlacementIndicator.SetActive(false);
                }
            }
        }

        // --- Repositioning ---

        private void HandleRepositionTap()
        {
            if (Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended) return;
            if (touch.deltaTime > 0.3f) return; // Ignore long presses (those are for rotation)
            if (IsPointerOverUI(touch.position)) return;

            if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                var hit = s_Hits[0];
                if (m_ExperienceManager.CurrentARContent != null)
                {
                    m_ExperienceManager.CurrentARContent.transform.position = hit.pose.position;
                }
            }
        }

        // --- Rotation ---

        /// <summary>
        /// Single finger drag rotates the model around its Y axis.
        /// Only activates on horizontal drags to avoid conflict with UI scrolling.
        /// </summary>
        private void HandleRotation()
        {
            if (Input.touchCount != 1) return;

            var touch = Input.GetTouch(0);
            if (IsPointerOverUI(touch.position)) return;

            if (touch.phase == TouchPhase.Moved && m_ExperienceManager.CurrentARContent != null)
            {
                float rotationAmount = touch.deltaPosition.x * m_RotationSpeed;
                m_ExperienceManager.CurrentARContent.transform.Rotate(Vector3.up, -rotationAmount, Space.World);
            }
        }

        // --- Pinch to Scale ---

        /// <summary>
        /// Two-finger pinch scales the placed model.
        /// </summary>
        private void HandlePinchScale()
        {
            if (Input.touchCount != 2) return;
            if (m_ExperienceManager.CurrentARContent == null) return;

            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                m_InitialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                m_InitialScale = m_ExperienceManager.CurrentARContent.transform.localScale.x;
                return;
            }

            if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                if (Mathf.Approximately(m_InitialPinchDistance, 0f)) return;

                float scaleFactor = currentDistance / m_InitialPinchDistance;
                float newScale = Mathf.Clamp(m_InitialScale * scaleFactor, m_MinScale, m_MaxScale);

                m_ExperienceManager.CurrentARContent.transform.localScale = Vector3.one * newScale;
            }
        }

        // --- Utility ---

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;

            var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem)
            {
                position = screenPosition
            };

            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            return results.Count > 0;
        }
    }
}
