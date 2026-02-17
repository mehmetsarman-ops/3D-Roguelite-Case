using RogueliteGame.Skills.Config;

namespace RogueliteGame.Skills
{
    public sealed class RageModeSkill : SkillBase
    {
        public RageModeConfig TypedConfig => (RageModeConfig)config;

        public RageModeSkill(RageModeConfig config) : base(config) { }
    }
}
