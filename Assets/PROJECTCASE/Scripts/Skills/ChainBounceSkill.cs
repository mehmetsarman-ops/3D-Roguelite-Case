using RogueliteGame.Skills.Config;

namespace RogueliteGame.Skills
{
    public sealed class ChainBounceSkill : SkillBase
    {
        public ChainBounceConfig TypedConfig => (ChainBounceConfig)config;

        public ChainBounceSkill(ChainBounceConfig config) : base(config) { }
    }
}
