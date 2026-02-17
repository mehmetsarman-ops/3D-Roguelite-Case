using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogueliteGame.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private int minEnemyCount = 5;
        [SerializeField] private float respawnDelay = 2f;
        // Oyun başladığında düşmanların hemen spawn olmaması için başlangıç gecikmesi ekledim
        public float initialSpawnDelay = 0f;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 mapSize = new Vector2(30f, 30f);
        [SerializeField] private float spawnY = 0.5f;

        [Header("Level Distribution")]
        [SerializeField] private int level1Count = 2;
        [SerializeField] private int level2Count = 2;
        [SerializeField] private int level3Count = 1;

        [Header("Enemy Prefabs")]
        [SerializeField] private Enemy level1Prefab;
        [SerializeField] private Enemy level2Prefab;
        [SerializeField] private Enemy level3Prefab;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        private readonly List<Enemy> allEnemies = new List<Enemy>();
        private int enemyLayerIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex == -1)
            {
                Debug.LogWarning("EnemySpawner: 'Enemy' layer bulunamadı.");
                enemyLayerIndex = 0;
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = FindObjectOfType<RogueliteGame.Player.PlayerMovement>();
                if (player != null) playerTransform = player.transform;
            }

            if (initialSpawnDelay > 0f)
                StartCoroutine(SpawnInitialWaveAfterDelay());
            else
                SpawnInitialWave();
        }

        private IEnumerator SpawnInitialWaveAfterDelay()
        {
            yield return new WaitForSeconds(initialSpawnDelay);
            SpawnInitialWave();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            level1Count = Mathf.Max(0, level1Count);
            level2Count = Mathf.Max(0, level2Count);
            level3Count = Mathf.Max(0, level3Count);
            minEnemyCount = Mathf.Max(0, minEnemyCount);
            respawnDelay = Mathf.Max(0f, respawnDelay);
            initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
            mapSize.x = Mathf.Max(1f, mapSize.x);
            mapSize.y = Mathf.Max(1f, mapSize.y);

            ValidatePrefabField(ref level1Prefab, nameof(level1Prefab));
            ValidatePrefabField(ref level2Prefab, nameof(level2Prefab));
            ValidatePrefabField(ref level3Prefab, nameof(level3Prefab));
        }

        private void ValidatePrefabField(ref Enemy prefab, string fieldName)
        {
            if (prefab == null) return;

            if (!PrefabUtility.IsPartOfPrefabAsset(prefab.gameObject))
            {
                Debug.LogWarning($"EnemySpawner: {fieldName} prefab asset olmalı.", this);
                prefab = null;
            }
        }
#endif

        private void SpawnInitialWave()
        {
            for (int i = 0; i < level1Count; i++) SpawnEnemy(EnemyLevel.Level1_Static);
            for (int i = 0; i < level2Count; i++) SpawnEnemy(EnemyLevel.Level2_Wanderer);
            for (int i = 0; i < level3Count; i++) SpawnEnemy(EnemyLevel.Level3_Chaser);
        }

        private Enemy SpawnEnemy(EnemyLevel level)
        {
            Enemy prefab = GetPrefabForLevel(level);
            if (prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: {level} için prefab atanmamış.", this);
                return null;
            }
            Vector3 pos = GetRandomSpawnPosition();
            Enemy enemy = Instantiate(prefab, pos, Quaternion.identity, transform);
            enemy.name = $"{prefab.name}_L{(int)level}_{allEnemies.Count}";

            SetLayerRecursively(enemy.gameObject, enemyLayerIndex);
            enemy.Initialize(level, playerTransform, this);

            allEnemies.Add(enemy);
            return enemy;
        }

        private Enemy GetPrefabForLevel(EnemyLevel level)
        {
            switch (level)
            {
                case EnemyLevel.Level2_Wanderer: return level2Prefab;
                case EnemyLevel.Level3_Chaser: return level3Prefab;
                default: return level1Prefab;
            }
        }

        public void OnEnemyDied(Enemy enemy)
        {
            if (enemy == null) return;
            StartCoroutine(RespawnAfterDelay(enemy));
        }

        private IEnumerator RespawnAfterDelay(Enemy enemy)
        {
            yield return new WaitForSeconds(respawnDelay);

            enemy.transform.position = GetRandomSpawnPosition();
            enemy.Respawn();
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = transform.position;
            float x = center.x + Random.Range(-mapSize.x * 0.5f, mapSize.x * 0.5f);
            float z = center.z + Random.Range(-mapSize.y * 0.5f, mapSize.y * 0.5f);
            return new Vector3(x, spawnY, z);
        }

        private void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null) return;

            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                if (child != null)
                    SetLayerRecursively(child.gameObject, layer);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position;
            center.y = spawnY;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawCube(center, new Vector3(mapSize.x, 0.1f, mapSize.y));

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireCube(center, new Vector3(mapSize.x, 0.1f, mapSize.y));
        }
#endif
    }
}
