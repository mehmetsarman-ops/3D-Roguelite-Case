using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RogueliteGame.Combat
{
    [DisallowMultipleComponent]
    public class BurnEffect : MonoBehaviour
    {
        private struct BurnStack
        {
            public float remainingDuration;
            public float damagePerTick;
            public float tickInterval;
            public float nextTickTime;
        }

        private readonly List<BurnStack> stacks = new List<BurnStack>();
        private RogueliteGame.Enemy.Enemy enemy;

        private Transform indicatorRoot;
        private Image[] iconImages;
        private int maxIndicatorSlots;
        private int lastDisplayedCount = -1;
        private UnityEngine.Camera mainCamera;

        public int StackCount => stacks.Count;

        public void SetupStackIndicator(Sprite icon, Vector2 iconSize, float spacing, float yOffset, int maxSlots)
        {
            maxIndicatorSlots = Mathf.Max(1, maxSlots);
            CreateIndicatorUI(icon, iconSize, spacing, yOffset);
        }

        // Max stack'e ulaşınca en eski stack'i yeniledim, yeni eklememek için
        public void AddStack(float damagePerTick, float duration, float tickInterval, int maxStacks)
        {
            if (stacks.Count >= maxStacks && stacks.Count > 0)
            {
                var oldest = stacks[0];
                oldest.remainingDuration = duration;
                oldest.damagePerTick = damagePerTick;
                oldest.tickInterval = tickInterval;
                stacks[0] = oldest;
            }
            else
            {
                stacks.Add(new BurnStack
                {
                    remainingDuration = duration,
                    damagePerTick = damagePerTick,
                    tickInterval = tickInterval,
                    nextTickTime = Time.time + tickInterval
                });
            }

            RefreshIndicatorUI();
        }

        public void ClearAllStacks()
        {
            stacks.Clear();
            DestroyIndicatorUI();
        }

        private void Awake()
        {
            enemy = GetComponent<RogueliteGame.Enemy.Enemy>();
            if (enemy == null)
                enemy = GetComponentInParent<RogueliteGame.Enemy.Enemy>();
            mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (enemy == null || !enemy.IsAlive)
            {
                stacks.Clear();
                DestroyIndicatorUI();
                Destroy(this);
                return;
            }

            float now = Time.time;
            bool stackCountChanged = false;

            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                var stack = stacks[i];
                stack.remainingDuration -= Time.deltaTime;

                if (stack.remainingDuration <= 0f)
                {
                    stacks.RemoveAt(i);
                    stackCountChanged = true;
                    continue;
                }

                if (now >= stack.nextTickTime)
                {
                    enemy.TakeDamage(stack.damagePerTick);
                    stack.nextTickTime = now + stack.tickInterval;
                }

                stacks[i] = stack;
            }

            if (stackCountChanged)
                RefreshIndicatorUI();

            if (indicatorRoot != null)
            {
                if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                    indicatorRoot.forward = mainCamera.transform.forward;
            }

            if (stacks.Count == 0)
            {
                DestroyIndicatorUI();
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            DestroyIndicatorUI();
        }

        private void CreateIndicatorUI(Sprite icon, Vector2 iconSize, float spacing, float yOffset)
        {
            if (indicatorRoot != null) return;

            GameObject canvasGO = new GameObject("BurnStackIndicator");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0f, yOffset, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 3;

            float totalWidth = maxIndicatorSlots * iconSize.x + (maxIndicatorSlots - 1) * spacing;
            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(totalWidth, iconSize.y);

            iconImages = new Image[maxIndicatorSlots];
            float startX = -(totalWidth * 0.5f) + iconSize.x * 0.5f;

            for (int i = 0; i < maxIndicatorSlots; i++)
            {
                GameObject iconGO = new GameObject($"BurnIcon_{i}");
                iconGO.transform.SetParent(canvasGO.transform, false);

                Image img = iconGO.AddComponent<Image>();
                img.raycastTarget = false;

                if (icon != null)
                {
                    img.sprite = icon;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = new Color(1f, 0.5f, 0f, 0.9f);
                }

                RectTransform rt = iconGO.GetComponent<RectTransform>();
                rt.sizeDelta = iconSize;
                float xPos = startX + i * (iconSize.x + spacing);
                rt.anchoredPosition = new Vector2(xPos, 0f);

                iconGO.SetActive(false);
                iconImages[i] = img;
            }

            indicatorRoot = canvasGO.transform;
            lastDisplayedCount = -1;
        }

        private void RefreshIndicatorUI()
        {
            if (iconImages == null) return;

            int count = stacks.Count;
            if (count == lastDisplayedCount) return;
            lastDisplayedCount = count;

            for (int i = 0; i < iconImages.Length; i++)
            {
                if (iconImages[i] != null)
                    iconImages[i].gameObject.SetActive(i < count);
            }
        }

        private void DestroyIndicatorUI()
        {
            if (indicatorRoot != null)
            {
                Destroy(indicatorRoot.gameObject);
                indicatorRoot = null;
            }
            iconImages = null;
            lastDisplayedCount = -1;
        }
    }
}
