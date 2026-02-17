using UnityEngine;

namespace RogueliteGame.Skills.Config
{
    [CreateAssetMenu(fileName = "BurnDamageConfig", menuName = "RogueliteGame/Skills/Burn Damage")]
    public class BurnDamageConfig : SkillConfigBase
    {
        [Header("Burn")]
        [Min(0.1f)]
        public float damagePerTick = 5f;

        [Min(0.1f)]
        public float baseBurnDuration = 3f;

        [Min(0.1f)]
        public float rageBurnDuration = 6f;

        [Min(0.1f)]
        public float tickInterval = 0.5f;

        [Min(1)]
        public int maxStacks = 3;

        [Header("Stack Indicator UI")]
        public Sprite burnStackIcon;
        public Vector2 stackIconSize = new Vector2(20f, 20f);
        public float stackIconSpacing = 4f;
        public float stackIconYOffset = 1.85f;
    }
}
