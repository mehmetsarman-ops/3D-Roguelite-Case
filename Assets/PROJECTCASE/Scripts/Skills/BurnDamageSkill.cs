using RogueliteGame.Skills.Config;

namespace RogueliteGame.Skills
{
    public sealed class BurnDamageSkill : SkillBase
    {
        public BurnDamageConfig TypedConfig => (BurnDamageConfig)config;

        public BurnDamageSkill(BurnDamageConfig config) : base(config) { }
    }
}
