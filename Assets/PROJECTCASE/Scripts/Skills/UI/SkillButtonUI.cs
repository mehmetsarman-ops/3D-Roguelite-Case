using UnityEngine;
using UnityEngine.UI;

namespace RogueliteGame.Skills.UI
{
    public class SkillButtonUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSkillManager skillManager;
        [SerializeField] private Image cooldownFillImage;

        [Header("Skill")]
        [SerializeField] private SkillType skillType;

        [Header("Visual Feedback")]
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0f, 0.5f);
        [SerializeField] private Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color readyColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Image iconImage;

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnSkillButtonPressed);
            }

            // Ana buton Image'ını raycast açık bıraktım, diğer child Image'ları kapattım yoksa tıklama engelliyorlardı
            Image primaryImage = null;
            if (button != null && button.targetGraphic is Image targetImg)
                primaryImage = targetImg;
            else
                primaryImage = GetComponent<Image>();

            if (primaryImage != null)
                primaryImage.raycastTarget = true;

            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img != primaryImage)
                    img.raycastTarget = false;
            }
        }

        private void Start()
        {
            SetFillAmount(1f);

            if (iconImage != null && skillManager != null)
            {
                var cfg = skillManager.GetSkillConfig(skillType);
                if (cfg != null && cfg.icon != null)
                    iconImage.sprite = cfg.icon;
            }

            UpdateFillColor();
        }

        private void Update()
        {
            if (skillManager == null) return;

            float fill = skillManager.GetSkillCooldownFill(skillType);
            SetFillAmount(fill);

            UpdateFillColor();
        }

        public void OnSkillButtonPressed()
        {
            if (skillManager == null) return;
            skillManager.ActivateSkill(skillType);
        }

        private void SetFillAmount(float amount)
        {
            if (cooldownFillImage == null) return;
            cooldownFillImage.fillAmount = Mathf.Clamp01(amount);
        }

        private void UpdateFillColor()
        {
            if (cooldownFillImage == null || skillManager == null) return;

            bool isActive = skillManager.IsSkillActive(skillType);
            bool isReady  = skillManager.IsSkillReady(skillType);

            if (isActive)
                cooldownFillImage.color = activeColor;
            else if (isReady)
                cooldownFillImage.color = readyColor;
            else
                cooldownFillImage.color = cooldownColor;
        }
    }
}
