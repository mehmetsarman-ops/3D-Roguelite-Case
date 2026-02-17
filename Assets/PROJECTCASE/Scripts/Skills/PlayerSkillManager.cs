using UnityEngine;
using RogueliteGame.Skills.Config;
using RogueliteGame.Combat;

namespace RogueliteGame.Skills
{
    public class PlayerSkillManager : MonoBehaviour
    {
        [Header("Skill Configs (ScriptableObject assets)")]
        [SerializeField] private MultiArrowConfig multiArrowConfig;
        [SerializeField] private BurnDamageConfig burnDamageConfig;
        [SerializeField] private AttackSpeedConfig attackSpeedConfig;
        [SerializeField] private RageModeConfig rageModeConfig;
        [SerializeField] private ChainBounceConfig chainBounceConfig;

        [Header("References")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private GameObject arrowPrefab;

        private MultiArrowSkill multiArrowSkill;
        private BurnDamageSkill burnDamageSkill;
        private AttackSpeedSkill attackSpeedSkill;
        private RageModeSkill rageModeSkill;
        private ChainBounceSkill chainBounceSkill;

        private SkillBase[] allSkills;

        private void Awake()
        {
            multiArrowSkill  = multiArrowConfig  != null ? new MultiArrowSkill(multiArrowConfig)   : null;
            burnDamageSkill  = burnDamageConfig  != null ? new BurnDamageSkill(burnDamageConfig)   : null;
            attackSpeedSkill = attackSpeedConfig != null ? new AttackSpeedSkill(attackSpeedConfig) : null;
            rageModeSkill    = rageModeConfig    != null ? new RageModeSkill(rageModeConfig)       : null;
            chainBounceSkill = chainBounceConfig != null ? new ChainBounceSkill(chainBounceConfig) : null;

            allSkills = new SkillBase[]
            {
                multiArrowSkill,
                burnDamageSkill,
                attackSpeedSkill,
                rageModeSkill,
                chainBounceSkill
            };

            ResolveEnemyLayerIfNeeded();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < allSkills.Length; i++)
            {
                allSkills[i]?.Tick(dt);
            }
        }

        private void ResolveEnemyLayerIfNeeded()
        {
            if (enemyLayer.value != 0) return;
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer >= 0) enemyLayer = 1 << layer;
        }

        public bool ActivateSkill(SkillType type)
        {
            SkillBase skill = GetSkill(type);
            return skill != null && skill.TryActivate();
        }

        public void ActivateMultiArrow()  => ActivateSkill(SkillType.MultiArrow);
        public void ActivateBurnDamage()  => ActivateSkill(SkillType.BurnDamage);
        public void ActivateAttackSpeed() => ActivateSkill(SkillType.AttackSpeed);
        public void ActivateRageMode()    => ActivateSkill(SkillType.RageMode);
        public void ActivateChainBounce() => ActivateSkill(SkillType.ChainBounce);

        public int GetArrowCount()
        {
            if (multiArrowSkill == null || !multiArrowSkill.IsActive) return 1;

            var cfg = multiArrowSkill.TypedConfig;
            return IsRageActive ? cfg.rageArrowCount : cfg.baseArrowCount;
        }

        public float GetArrowSpreadOffset()
        {
            if (multiArrowSkill == null || !multiArrowSkill.IsActive) return 0f;
            return multiArrowSkill.TypedConfig.spreadOffset;
        }

        public float GetAttackSpeedMultiplier()
        {
            if (attackSpeedSkill == null || !attackSpeedSkill.IsActive) return 1f;

            var cfg = attackSpeedSkill.TypedConfig;
            return IsRageActive ? cfg.rageSpeedMultiplier : cfg.baseSpeedMultiplier;
        }

        public bool IsBurnActive => burnDamageSkill != null && burnDamageSkill.IsActive;
        public bool IsChainBounceActive => chainBounceSkill != null && chainBounceSkill.IsActive;
        public bool IsRageActive => rageModeSkill != null && rageModeSkill.IsActive;

        public ArrowSkillData BuildArrowSkillData()
        {
            var data = new ArrowSkillData();

            if (burnDamageSkill != null && burnDamageSkill.IsActive)
            {
                var cfg = burnDamageSkill.TypedConfig;
                data.hasBurn          = true;
                data.burnDamagePerTick = cfg.damagePerTick;
                data.burnDuration     = IsRageActive ? cfg.rageBurnDuration : cfg.baseBurnDuration;
                data.burnTickInterval = cfg.tickInterval;
                data.burnMaxStacks    = cfg.maxStacks;

                data.burnStackIcon        = cfg.burnStackIcon;
                data.burnStackIconSize    = cfg.stackIconSize;
                data.burnStackIconSpacing = cfg.stackIconSpacing;
                data.burnStackIconYOffset = cfg.stackIconYOffset;
            }

            if (chainBounceSkill != null && chainBounceSkill.IsActive)
            {
                var cfg = chainBounceSkill.TypedConfig;
                data.bounceCount  = IsRageActive ? cfg.rageBounceCount : cfg.baseBounceCount;
                data.bounceRadius = cfg.bounceRadius;
            }

            data.enemyLayerMask = enemyLayer;
            data.arrowPrefab    = arrowPrefab;

            return data;
        }

        public float GetSkillCooldownFill(SkillType type)
        {
            SkillBase skill = GetSkill(type);
            return skill?.GetCooldownFillAmount() ?? 1f;
        }

        public bool IsSkillActive(SkillType type)
        {
            SkillBase skill = GetSkill(type);
            return skill != null && skill.IsActive;
        }

        public bool IsSkillReady(SkillType type)
        {
            SkillBase skill = GetSkill(type);
            return skill != null && skill.IsReady;
        }

        public SkillConfigBase GetSkillConfig(SkillType type)
        {
            SkillBase skill = GetSkill(type);
            return skill?.Config;
        }

        public SkillBase[] GetAllSkills() => allSkills;

        public SkillType GetSkillType(SkillBase skill)
        {
            if (skill == multiArrowSkill)  return SkillType.MultiArrow;
            if (skill == burnDamageSkill)  return SkillType.BurnDamage;
            if (skill == attackSpeedSkill) return SkillType.AttackSpeed;
            if (skill == rageModeSkill)    return SkillType.RageMode;
            if (skill == chainBounceSkill) return SkillType.ChainBounce;
            return (SkillType)(-1);
        }

        private SkillBase GetSkill(SkillType type)
        {
            switch (type)
            {
                case SkillType.MultiArrow:  return multiArrowSkill;
                case SkillType.BurnDamage:  return burnDamageSkill;
                case SkillType.AttackSpeed: return attackSpeedSkill;
                case SkillType.RageMode:    return rageModeSkill;
                case SkillType.ChainBounce: return chainBounceSkill;
                default: return null;
            }
        }
    }
}
