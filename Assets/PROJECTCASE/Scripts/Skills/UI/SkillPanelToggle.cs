using UnityEngine;
using UnityEngine.UI;

namespace RogueliteGame.Skills.UI
{
    public class SkillPanelToggle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (openButton != null)
                openButton.onClick.AddListener(OpenPanel);
        }

        private void Start()
        {
            OpenPanel();
        }

        public void OpenPanel()
        {
            if (skillPanel != null) skillPanel.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(true);
            if (openButton != null) openButton.gameObject.SetActive(false);
        }

        public void ClosePanel()
        {
            if (skillPanel != null) skillPanel.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            if (openButton != null) openButton.gameObject.SetActive(true);
        }
    }
}
