using RogueliteGame.Skills.Config;

namespace RogueliteGame.Skills
{
    public sealed class AttackSpeedSkill : SkillBase
    {
        public AttackSpeedConfig TypedConfig => (AttackSpeedConfig)config;

        public AttackSpeedSkill(AttackSpeedConfig config) : base(config) { }
    }
}
