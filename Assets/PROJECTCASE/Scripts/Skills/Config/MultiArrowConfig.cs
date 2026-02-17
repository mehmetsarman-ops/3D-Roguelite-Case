using UnityEngine;

namespace RogueliteGame.Skills.Config
{
    [CreateAssetMenu(fileName = "MultiArrowConfig", menuName = "RogueliteGame/Skills/Multi Arrow")]
    public class MultiArrowConfig : SkillConfigBase
    {
        [Header("Multi-Arrow")]
        [Min(1)]
        public int baseArrowCount = 2;

        [Min(1)]
        public int rageArrowCount = 4;

        [Min(0f)]
        public float spreadOffset = 0.35f;
    }
}
