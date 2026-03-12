using UnityEngine;
using HistoricalGreece.UI;

namespace HistoricalGreece.UI.Screens
{
    /// <summary>
    /// Welcome / onboarding screen shown on first app launch.
    /// Explains the app concept to tourists in 2-3 simple steps.
    /// Designed to be skippable and non-intrusive.
    /// </summary>
    public class WelcomeScreenManager : MonoBehaviour
    {
        [Header("Onboarding Steps")]
        [Tooltip("The step panels, shown one at a time")]
        [SerializeField] private GameObject[] m_Steps;

        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Button m_NextButton;
        [SerializeField] private UnityEngine.UI.Button m_SkipButton;
        [SerializeField] private TMPro.TMP_Text m_NextButtonLabel;

        [Header("Page Dots")]
        [SerializeField] private GameObject[] m_PageDots;
        [SerializeField] private Color m_ActiveDotColor = Color.white;
        [SerializeField] private Color m_InactiveDotColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("References")]
        [SerializeField] private AppManager m_AppManager;

        private int m_CurrentStep;

        private void OnEnable()
        {
            m_CurrentStep = 0;
            ShowStep(0);

            if (m_NextButton != null) m_NextButton.onClick.AddListener(OnNextPressed);
            if (m_SkipButton != null) m_SkipButton.onClick.AddListener(OnSkipPressed);
        }

        private void OnDisable()
        {
            if (m_NextButton != null) m_NextButton.onClick.RemoveListener(OnNextPressed);
            if (m_SkipButton != null) m_SkipButton.onClick.RemoveListener(OnSkipPressed);
        }

        private void ShowStep(int index)
        {
            for (int i = 0; i < m_Steps.Length; i++)
            {
                if (m_Steps[i] != null)
                    m_Steps[i].SetActive(i == index);
            }

            // Update page dots
            if (m_PageDots != null)
            {
                for (int i = 0; i < m_PageDots.Length; i++)
                {
                    var img = m_PageDots[i]?.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                        img.color = i == index ? m_ActiveDotColor : m_InactiveDotColor;
                }
            }

            // Update next button text on last step
            if (m_NextButtonLabel != null)
            {
                m_NextButtonLabel.text = (index >= m_Steps.Length - 1) ? "Get Started" : "Next";
            }
        }

        private void OnNextPressed()
        {
            m_CurrentStep++;
            if (m_CurrentStep >= m_Steps.Length)
            {
                CompleteOnboarding();
            }
            else
            {
                ShowStep(m_CurrentStep);
            }
        }

        private void OnSkipPressed()
        {
            CompleteOnboarding();
        }

        private void CompleteOnboarding()
        {
            if (m_AppManager != null)
            {
                m_AppManager.CompleteOnboarding();
            }
        }
    }
}
