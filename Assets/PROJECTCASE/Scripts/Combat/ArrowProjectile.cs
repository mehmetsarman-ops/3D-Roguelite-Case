using UnityEngine;
using System.Collections.Generic;

namespace RogueliteGame.Combat
{
    [DisallowMultipleComponent]
    public class ArrowProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float maxLifetime = 4f;
        [SerializeField] private float hitDistance = 0.2f;
        [SerializeField] private bool rotateToVelocity = true;

        private Transform target;
        private Vector3 targetOffset;
        private float damage;
        private float spawnTime;
        private bool hasHit;
        private bool targetLost;
        private float targetLostTime;
        private ArrowSkillData skillData;

        private const float ORPHAN_LIFETIME = 0.5f;

        // Multi-arrow ile aynı düşmana aynı anda birden fazla ok isabet edince chain bounce çoğalıyordu, bunu önlemek için deduplicate yaptım
        private static readonly Dictionary<Transform, float> lastBounceSourceTime = new Dictionary<Transform, float>();
        private const float BOUNCE_DEDUP_WINDOW = 0.15f;

        public void Initialize(
            Transform targetTransform,
            float damageAmount,
            Vector3 aimOffset,
            float speedOverride,
            float lifetimeOverride)
        {
            Initialize(targetTransform, damageAmount, aimOffset, speedOverride, lifetimeOverride, default);
        }

        public void Initialize(
            Transform targetTransform,
            float damageAmount,
            Vector3 aimOffset,
            float speedOverride,
            float lifetimeOverride,
            ArrowSkillData arrowSkillData)
        {
            target = targetTransform;
            damage = damageAmount;
            targetOffset = aimOffset;
            skillData = arrowSkillData;

            if (speedOverride > 0f) speed = speedOverride;
            if (lifetimeOverride > 0f) maxLifetime = lifetimeOverride;

            spawnTime = Time.time;
        }

        private void Awake()
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (hasHit) return;

            if (Time.time - spawnTime >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Hedef yok olursa referansı kalıcı olarak kopardım, respawn eden düşmanı yanlışlıkla hedef almasın diye
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (!targetLost)
                {
                    targetLost = true;
                    targetLostTime = Time.time;
                    target = null;
                }

                if (Time.time - targetLostTime >= ORPHAN_LIFETIME)
                {
                    Destroy(gameObject);
                    return;
                }

                transform.position += transform.forward * speed * Time.deltaTime;
                return;
            }

            Vector3 targetPosition = target.position + targetOffset;
            Vector3 toTarget = targetPosition - transform.position;
            float step = speed * Time.deltaTime;
            float hitDistanceSqr = hitDistance * hitDistance;

            if (toTarget.sqrMagnitude <= hitDistanceSqr || toTarget.magnitude <= step)
            {
                transform.position = targetPosition;
                HitTarget();
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * step;

            if (rotateToVelocity && direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private void HitTarget()
        {
            if (hasHit) return;
            hasHit = true;

            if (target != null)
            {
                var enemy = target.GetComponentInParent<RogueliteGame.Enemy.Enemy>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(damage);
                    ApplyBurn(enemy);
                    ChainBounce(enemy.transform);
                }
            }

            Destroy(gameObject);
        }

        private void ApplyBurn(RogueliteGame.Enemy.Enemy enemy)
        {
            if (!skillData.hasBurn) return;

            var burnEffect = enemy.GetComponent<BurnEffect>();
            if (burnEffect == null)
            {
                burnEffect = enemy.gameObject.AddComponent<BurnEffect>();
                burnEffect.SetupStackIndicator(
                    skillData.burnStackIcon,
                    skillData.burnStackIconSize,
                    skillData.burnStackIconSpacing,
                    skillData.burnStackIconYOffset,
                    skillData.burnMaxStacks);
            }

            burnEffect.AddStack(
                skillData.burnDamagePerTick,
                skillData.burnDuration,
                skillData.burnTickInterval,
                skillData.burnMaxStacks);
        }

        private void ChainBounce(Transform hitEnemy)
        {
            if (skillData.bounceCount <= 0) return;
            if (skillData.arrowPrefab == null) return;

            float now = Time.time;
            if (lastBounceSourceTime.TryGetValue(hitEnemy, out float lastTime)
                && now - lastTime < BOUNCE_DEDUP_WINDOW)
                return;
            lastBounceSourceTime[hitEnemy] = now;
            CleanupStaleEntries(now);

            int layerMask = skillData.enemyLayerMask.value;
            if (layerMask == 0) return;

            Collider[] nearby = Physics.OverlapSphere(
                hitEnemy.position, skillData.bounceRadius, layerMask);

            int bounced = 0;
            foreach (var col in nearby)
            {
                if (bounced >= skillData.bounceCount) break;

                var enemy = col.GetComponentInParent<RogueliteGame.Enemy.Enemy>();
                if (enemy == null || !enemy.IsAlive || enemy.transform == hitEnemy)
                    continue;

                SpawnBounceArrow(enemy.transform);
                bounced++;
            }
        }

        private static void CleanupStaleEntries(float now)
        {
            List<Transform> stale = null;
            foreach (var kvp in lastBounceSourceTime)
            {
                if (now - kvp.Value > BOUNCE_DEDUP_WINDOW * 2f)
                {
                    if (stale == null) stale = new List<Transform>();
                    stale.Add(kvp.Key);
                }
            }
            if (stale != null)
            {
                foreach (var key in stale)
                    lastBounceSourceTime.Remove(key);
            }
        }

        private void SpawnBounceArrow(Transform bounceTarget)
        {
            Vector3 spawnPos = transform.position;
            Vector3 dir = bounceTarget.position + targetOffset - spawnPos;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(dir.normalized);
            GameObject arrowGO = Instantiate(skillData.arrowPrefab, spawnPos, rotation);

            Rigidbody rb = arrowGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var projectile = arrowGO.GetComponent<ArrowProjectile>();
            if (projectile == null)
                projectile = arrowGO.AddComponent<ArrowProjectile>();

            // Sıçrayan okların tekrar sıçramaması için bounceCount'u sıfırladım
            var bounceData = skillData;
            bounceData.bounceCount = 0;

            projectile.Initialize(bounceTarget, damage, targetOffset, speed, maxLifetime, bounceData);
        }
    }
}
