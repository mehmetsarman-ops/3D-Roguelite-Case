using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RogueliteGame.Skills.UI
{
    public class ActiveSkillIconDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSkillManager skillManager;
        [SerializeField] private Transform healthBarRoot;

        [Header("Icon Layout")]
        [SerializeField] private Vector2 iconSize = new Vector2(22f, 22f);
        [SerializeField] private float iconSpacing = 4f;
        [SerializeField] private float yOffset = 12f;

        private class IconEntry
        {
            public GameObject root;
            public Image iconImage;
        }

        private IconEntry[] iconEntries;
        private RectTransform containerRect;

        private void Awake()
        {
            if (skillManager == null)
                skillManager = GetComponent<PlayerSkillManager>();
        }

        public void Initialize(PlayerSkillManager manager, Transform barRoot)
        {
            skillManager = manager;
            healthBarRoot = barRoot;
        }

        private void Start()
        {
            if (skillManager == null || healthBarRoot == null) return;

            CreateIconContainer();
            CreateIconSlots();
        }

        private void Update()
        {
            if (skillManager == null || iconEntries == null) return;

            SkillBase[] skills = skillManager.GetAllSkills();
            int visibleCount = 0;

            for (int i = 0; i < skills.Length; i++)
            {
                SkillBase skill = skills[i];
                IconEntry entry = iconEntries[i];

                if (skill == null || entry == null)
                    continue;

                if (skill.IsActive)
                {
                    entry.root.SetActive(true);

                    if (skill.Config != null && skill.Config.icon != null)
                        entry.iconImage.sprite = skill.Config.icon;

                    visibleCount++;
                }
                else
                {
                    entry.root.SetActive(false);
                }
            }

            if (visibleCount > 0)
                RepositionIcons(skills);
        }

        private void CreateIconContainer()
        {
            GameObject containerGO = new GameObject("ActiveSkillIcons");
            containerGO.transform.SetParent(healthBarRoot, false);

            containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, yOffset);
            containerRect.sizeDelta = Vector2.zero;
        }

        private void CreateIconSlots()
        {
            SkillBase[] skills = skillManager.GetAllSkills();
            iconEntries = new IconEntry[skills.Length];

            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] == null)
                {
                    iconEntries[i] = null;
                    continue;
                }

                iconEntries[i] = CreateSingleIcon(skills[i]);
                iconEntries[i].root.SetActive(false);
            }
        }

        private IconEntry CreateSingleIcon(SkillBase skill)
        {
            IconEntry entry = new IconEntry();

            entry.root = new GameObject("SkillIcon");
            entry.root.transform.SetParent(containerRect, false);

            RectTransform rootRect = entry.root.AddComponent<RectTransform>();
            rootRect.sizeDelta = iconSize;

            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(entry.root.transform, false);

            entry.iconImage = iconGO.AddComponent<Image>();
            entry.iconImage.raycastTarget = false;

            if (skill.Config != null && skill.Config.icon != null)
                entry.iconImage.sprite = skill.Config.icon;

            entry.iconImage.preserveAspect = true;

            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            return entry;
        }

        // Görünür ikonları yatay ortaladım, kaç tane aktifse o kadar göstersin
        private void RepositionIcons(SkillBase[] skills)
        {
            List<int> visibleIndices = new List<int>(skills.Length);
            for (int i = 0; i < skills.Length; i++)
            {
                if (iconEntries[i] != null && iconEntries[i].root.activeSelf)
                    visibleIndices.Add(i);
            }

            int count = visibleIndices.Count;
            if (count == 0) return;

            float totalWidth = count * iconSize.x + (count - 1) * iconSpacing;
            float startX = -totalWidth * 0.5f + iconSize.x * 0.5f;

            for (int v = 0; v < count; v++)
            {
                int idx = visibleIndices[v];
                RectTransform rt = iconEntries[idx].root.GetComponent<RectTransform>();
                float x = startX + v * (iconSize.x + iconSpacing);
                rt.anchoredPosition = new Vector2(x, 0f);
            }
        }
    }
}
