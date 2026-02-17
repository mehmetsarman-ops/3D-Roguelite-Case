using UnityEngine;
using UnityEngine.UI;

namespace RogueliteGame.Enemy
{
    public enum EnemyLevel
    {
        Level1_Static = 1,
        Level2_Wanderer = 2,
        Level3_Chaser = 3
    }

    public class Enemy : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] private EnemyLevel level = EnemyLevel.Level1_Static;
        [SerializeField] private float maxHealth = 100f;

        [Header("Level 2 — Wandering")]
        [SerializeField] private float wanderSpeed = 2f;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderPauseTime = 2f;

        [Header("Level 3 — Chase & Attack")]
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 2f;

        [Header("Health Bar")]
        [SerializeField] private float healthBarYOffset = 1.5f;

        private float currentHealth;
        private bool isAlive = true;
        private Transform playerTransform;
        private Vector3 spawnPosition;
        private EnemySpawner spawner;
        private bool initializedBySpawner;

        private Vector3 wanderTarget;
        private float wanderTimer;
        private bool isWandering;

        private float lastAttackTime = -999f;

        private Transform healthBarRoot;
        private Image healthBarFill;
        private UnityEngine.Camera mainCamera;

        public EnemyLevel Level => level;
        public bool IsAlive => isAlive;

        public void Initialize(EnemyLevel enemyLevel, Transform player, EnemySpawner spawnerRef)
        {
            level = enemyLevel;
            playerTransform = player;
            spawner = spawnerRef;
            initializedBySpawner = true;
            EnsureRuntimeReferences();

            spawnPosition = transform.position;
            currentHealth = maxHealth;
            isAlive = true;

            CreateHealthBar();
            RefreshHealthBar();
        }

        private void Start()
        {
            EnsureRuntimeReferences();

            // Sahneye manuel yerleştirilmiş düşmanlar için spawner kullanılmadıysa burada başlattım
            if (!initializedBySpawner)
            {
                spawnPosition = transform.position;
                currentHealth = maxHealth;
                isAlive = true;
            }

            CreateHealthBar();
            RefreshHealthBar();
        }

        private void Update()
        {
            if (!isAlive) return;

            switch (level)
            {
                case EnemyLevel.Level1_Static:
                    break;
                case EnemyLevel.Level2_Wanderer:
                    HandleWandering();
                    break;
                case EnemyLevel.Level3_Chaser:
                    HandleChaseAndAttack();
                    break;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
            if (healthBarRoot != null && mainCamera != null)
            {
                healthBarRoot.forward = mainCamera.transform.forward;
            }
        }

        private void HandleWandering()
        {
            if (!isWandering)
            {
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    PickNewWanderTarget();
                    isWandering = true;
                }
                return;
            }

            Vector3 dir = wanderTarget - transform.position;
            dir.y = 0f;

            if (dir.magnitude < 0.3f)
            {
                isWandering = false;
                wanderTimer = wanderPauseTime;
                return;
            }

            Vector3 move = dir.normalized * wanderSpeed * Time.deltaTime;
            transform.position += move;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        private void PickNewWanderTarget()
        {
            Vector2 rnd = Random.insideUnitCircle * wanderRadius;
            wanderTarget = spawnPosition + new Vector3(rnd.x, 0f, rnd.y);
        }

        private void HandleChaseAndAttack()
        {
            if (playerTransform == null) return;

            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }

            if (dist > attackRange)
            {
                transform.position += dir.normalized * chaseSpeed * Time.deltaTime;
            }
            else if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                AttackPlayer();
            }
        }

        private void AttackPlayer()
        {
            if (playerTransform == null) return;

            var player = playerTransform.GetComponent<RogueliteGame.Player.PlayerMovement>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }

        public void TakeDamage(float damage)
        {
            if (!isAlive) return;

            currentHealth -= damage;
            RefreshHealthBar();

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            isAlive = false;

            if (spawner != null)
            {
                spawner.OnEnemyDied(this);
            }

            gameObject.SetActive(false);
        }

        public void Respawn()
        {
            currentHealth = maxHealth;
            isAlive = true;
            isWandering = false;
            wanderTimer = wanderPauseTime;
            lastAttackTime = -999f;
            spawnPosition = transform.position;
            EnsureRuntimeReferences();
            CreateHealthBar();
            CleanupBurnEffect();

            gameObject.SetActive(true);
            RefreshHealthBar();
        }

        private void CleanupBurnEffect()
        {
            var burn = GetComponent<RogueliteGame.Combat.BurnEffect>();
            if (burn != null)
            {
                burn.ClearAllStacks();
                Destroy(burn);
            }
        }

        private void CreateHealthBar()
        {
            if (healthBarRoot != null && healthBarFill != null) return;

            GameObject canvasGO = new GameObject("HealthBar");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0f, healthBarYOffset, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 12f);

            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            healthBarFill = fillGO.AddComponent<Image>();
            healthBarFill.color = Color.green;

            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            healthBarRoot = canvasGO.transform;
        }

        private void EnsureRuntimeReferences()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            if (playerTransform == null)
            {
                var player = FindObjectOfType<RogueliteGame.Player.PlayerMovement>();
                if (player != null)
                    playerTransform = player.transform;
            }
        }

        private void RefreshHealthBar()
        {
            if (healthBarFill == null) return;

            float ratio = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            healthBarFill.color = Color.Lerp(Color.red, Color.green, ratio);
        }
    }
}
