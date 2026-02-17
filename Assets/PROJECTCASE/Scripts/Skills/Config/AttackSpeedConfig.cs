using UnityEngine;

namespace RogueliteGame.Skills.Config
{
    [CreateAssetMenu(fileName = "AttackSpeedConfig", menuName = "RogueliteGame/Skills/Attack Speed")]
    public class AttackSpeedConfig : SkillConfigBase
    {
        [Header("Attack Speed")]
        [Min(1f)]
        public float baseSpeedMultiplier = 2f;

        [Min(1f)]
        public float rageSpeedMultiplier = 4f;
    }
}
