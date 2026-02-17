using RogueliteGame.Skills.Config;

namespace RogueliteGame.Skills
{
    public sealed class MultiArrowSkill : SkillBase
    {
        public MultiArrowConfig TypedConfig => (MultiArrowConfig)config;

        public MultiArrowSkill(MultiArrowConfig config) : base(config) { }
    }
}
