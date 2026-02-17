using UnityEngine;
using RogueliteGame.Skills.Config;

namespace RogueliteGame.Skills
{
    public enum SkillType
    {
        MultiArrow   = 0,
        BurnDamage   = 1,
        AttackSpeed  = 2,
        RageMode     = 3,
        ChainBounce  = 4
    }

    // MonoBehaviour yapmadım çünkü PlayerSkillManager zaten her frame Tick çağırıyor
    public abstract class SkillBase
    {
        protected readonly SkillConfigBase config;

        private float durationTimer;
        private float cooldownTimer;
        private bool isActive;

        public bool IsActive => isActive;
        public bool IsReady => !isActive && cooldownTimer <= 0f;
        public bool IsCoolingDown => !isActive && cooldownTimer > 0f;
        public SkillConfigBase Config => config;

        public float DurationRemainingRatio =>
            isActive && config.duration > 0f
                ? Mathf.Clamp01(durationTimer / config.duration)
                : 0f;

        public float CooldownRemainingRatio =>
            !isActive && cooldownTimer > 0f && config.cooldown > 0f
                ? Mathf.Clamp01(cooldownTimer / config.cooldown)
                : 0f;

        protected SkillBase(SkillConfigBase config)
        {
            this.config = config;
        }

        public bool TryActivate()
        {
            if (!IsReady) return false;

            isActive = true;
            durationTimer = config.duration;
            OnActivate();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (isActive)
            {
                durationTimer -= deltaTime;
                if (durationTimer <= 0f)
                {
                    isActive = false;
                    cooldownTimer = config.cooldown;
                    OnDeactivate();
                }
            }
            else if (cooldownTimer > 0f)
            {
                cooldownTimer -= deltaTime;
                if (cooldownTimer < 0f)
                    cooldownTimer = 0f;
            }
        }

        // Dodge butonlarıyla aynı fill mantığını kullandım: 0 = kullanılamaz, 1 = hazır
        public float GetCooldownFillAmount()
        {
            float totalCycle = config.TotalCycleTime;

            if (isActive)
            {
                float remaining = durationTimer + config.cooldown;
                return Mathf.Clamp01(1f - (remaining / totalCycle));
            }

            if (cooldownTimer > 0f)
            {
                return Mathf.Clamp01(1f - (cooldownTimer / totalCycle));
            }

            return 1f;
        }

        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
    }
}
