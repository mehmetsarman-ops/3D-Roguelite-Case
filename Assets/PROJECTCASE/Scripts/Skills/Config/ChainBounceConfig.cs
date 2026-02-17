using UnityEngine;

namespace RogueliteGame.Skills.Config
{
    [CreateAssetMenu(fileName = "ChainBounceConfig", menuName = "RogueliteGame/Skills/Chain Bounce")]
    public class ChainBounceConfig : SkillConfigBase
    {
        [Header("Chain Bounce")]
        [Min(1)]
        public int baseBounceCount = 2;

        [Min(1)]
        public int rageBounceCount = 4;

        [Min(1f)]
        public float bounceRadius = 8f;
    }
}
