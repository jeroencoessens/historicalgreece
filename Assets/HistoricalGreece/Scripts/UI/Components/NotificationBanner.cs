using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HistoricalGreece.Core;
using HistoricalGreece.Location;

namespace HistoricalGreece.UI.Components
{
    /// <summary>
    /// Displays a slide-in notification banner when the user enters
    /// the proximity of a historical site. Tourist-friendly: shows
    /// site name, distance, and a one-tap "View in AR" action.
    ///
    /// Sits in the main UI canvas. Subscribes to ProximityDetector events.
    /// Auto-dismisses after a timeout, or user can tap to act/dismiss.
    /// </summary>
    public class NotificationBanner : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject m_BannerRoot;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_SubtitleText;
        [SerializeField] private TMP_Text m_DistanceText;
        [SerializeField] private Image m_SiteThumbnail;
        [SerializeField] private Image m_AccentBar;
        [SerializeField] private Button m_ActionButton;
        [SerializeField] private Button m_DismissButton;
        [SerializeField] private TMP_Text m_ActionButtonLabel;

        [Header("Animation")]
        [Tooltip("Animator with 'Show' and 'Hide' triggers")]
        [SerializeField] private Animator m_Animator;

        [Tooltip("If no Animator, this RectTransform will be slid in from the top")]
        [SerializeField] private RectTransform m_SlideTransform;

        [SerializeField] private float m_SlideInDuration = 0.3f;
        [SerializeField] private float m_SlideOutDuration = 0.2f;

        [Header("Behavior")]
        [Tooltip("Seconds before the banner auto-dismisses (0 = stays until tapped)")]
        [SerializeField] private float m_AutoDismissSeconds = 8f;

        [Tooltip("Minimum seconds between showing consecutive banners")]
        [SerializeField] private float m_CooldownSeconds = 30f;

        [Header("Services")]
        [SerializeField] private ProximityDetector m_ProximityDetector;

        // --- Events ---

        /// <summary>
        /// Fires when the user taps the action button on the banner.
        /// Parameter is the site that triggered the notification.
        /// </summary>
        public System.Action<HistoricalSite> OnActionTapped;

        // --- Internal ---
        private HistoricalSite m_CurrentSite;
        private Coroutine m_AutoDismissCoroutine;
        private Coroutine m_SlideCoroutine;
        private float m_LastDismissTime = -999f;
        private bool m_IsVisible;

        // --- Lifecycle ---

        private void Awake()
        {
            if (m_BannerRoot != null)
                m_BannerRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (m_ProximityDetector != null)
            {
                m_ProximityDetector.OnEnteredSiteRadius += HandleEnteredSiteRadius;
            }

            if (m_ActionButton != null)
                m_ActionButton.onClick.AddListener(OnActionPressed);
            if (m_DismissButton != null)
                m_DismissButton.onClick.AddListener(Dismiss);
        }

        private void OnDisable()
        {
            if (m_ProximityDetector != null)
            {
                m_ProximityDetector.OnEnteredSiteRadius -= HandleEnteredSiteRadius;
            }

            if (m_ActionButton != null)
                m_ActionButton.onClick.RemoveListener(OnActionPressed);
            if (m_DismissButton != null)
                m_DismissButton.onClick.RemoveListener(Dismiss);
        }

        // --- Public API ---

        /// <summary>
        /// Manually show the banner for a specific site.
        /// Used by ProximityDetector events or for testing.
        /// </summary>
        public void ShowForSite(HistoricalSite site, string distanceText = null)
        {
            if (site == null) return;

            // Respect cooldown
            if (Time.time - m_LastDismissTime < m_CooldownSeconds && m_IsVisible)
                return;

            m_CurrentSite = site;

            // Populate content
            SetText(m_TitleText, site.siteName);
            SetText(m_SubtitleText, $"You're near {site.siteName}!");
            SetText(m_DistanceText, distanceText ?? "Nearby");

            if (m_SiteThumbnail != null && site.thumbnail != null)
            {
                m_SiteThumbnail.sprite = site.thumbnail;
                m_SiteThumbnail.gameObject.SetActive(true);
            }

            if (m_AccentBar != null)
                m_AccentBar.color = site.accentColor;

            if (m_ActionButtonLabel != null)
                m_ActionButtonLabel.text = "View in AR";

            // Show banner
            Show();
        }

        /// <summary>
        /// Dismiss the banner with animation.
        /// </summary>
        public void Dismiss()
        {
            if (!m_IsVisible) return;

            m_IsVisible = false;
            m_LastDismissTime = Time.time;

            if (m_AutoDismissCoroutine != null)
            {
                StopCoroutine(m_AutoDismissCoroutine);
                m_AutoDismissCoroutine = null;
            }

            Hide();
        }

        // --- Event Handlers ---

        private void HandleEnteredSiteRadius(HistoricalSite site)
        {
            // Calculate distance for display
            string distText = null;
            if (m_ProximityDetector != null)
            {
                float distKm = m_ProximityDetector.GetDistanceToSite(site);
                if (distKm >= 0)
                {
                    distText = distKm < 1f
                        ? $"{Mathf.RoundToInt(distKm * 1000f)} m away"
                        : $"{distKm:F1} km away";
                }
            }

            ShowForSite(site, distText);
        }

        // --- Button Handlers ---

        private void OnActionPressed()
        {
            var site = m_CurrentSite;
            Dismiss();
            OnActionTapped?.Invoke(site);
        }

        // --- Animation ---

        private void Show()
        {
            if (m_BannerRoot != null)
                m_BannerRoot.SetActive(true);

            m_IsVisible = true;

            if (m_Animator != null)
            {
                m_Animator.SetTrigger("Show");
            }
            else if (m_SlideTransform != null)
            {
                // Simple slide-in from top
                if (m_SlideCoroutine != null) StopCoroutine(m_SlideCoroutine);
                m_SlideCoroutine = StartCoroutine(SlideIn());
            }

            // Auto-dismiss timer
            if (m_AutoDismissSeconds > 0)
            {
                if (m_AutoDismissCoroutine != null) StopCoroutine(m_AutoDismissCoroutine);
                m_AutoDismissCoroutine = StartCoroutine(AutoDismissRoutine());
            }
        }

        private void Hide()
        {
            if (m_Animator != null)
            {
                m_Animator.SetTrigger("Hide");
                // Deactivate after animation via event or coroutine
                StartCoroutine(DeactivateAfterDelay(m_SlideOutDuration));
            }
            else if (m_SlideTransform != null)
            {
                if (m_SlideCoroutine != null) StopCoroutine(m_SlideCoroutine);
                m_SlideCoroutine = StartCoroutine(SlideOut());
            }
            else
            {
                if (m_BannerRoot != null)
                    m_BannerRoot.SetActive(false);
            }
        }

        private IEnumerator SlideIn()
        {
            Vector2 startPos = new Vector2(m_SlideTransform.anchoredPosition.x, 200f);
            Vector2 endPos = new Vector2(m_SlideTransform.anchoredPosition.x, 0f);

            float elapsed = 0f;
            while (elapsed < m_SlideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / m_SlideInDuration);
                m_SlideTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            m_SlideTransform.anchoredPosition = endPos;
        }

        private IEnumerator SlideOut()
        {
            Vector2 startPos = m_SlideTransform.anchoredPosition;
            Vector2 endPos = new Vector2(startPos.x, 200f);

            float elapsed = 0f;
            while (elapsed < m_SlideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / m_SlideOutDuration);
                m_SlideTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            if (m_BannerRoot != null)
                m_BannerRoot.SetActive(false);
        }

        private IEnumerator AutoDismissRoutine()
        {
            yield return new WaitForSeconds(m_AutoDismissSeconds);
            Dismiss();
        }

        private IEnumerator DeactivateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (m_BannerRoot != null && !m_IsVisible)
                m_BannerRoot.SetActive(false);
        }

        private void SetText(TMP_Text field, string value)
        {
            if (field != null) field.text = value ?? "";
        }
    }
}
