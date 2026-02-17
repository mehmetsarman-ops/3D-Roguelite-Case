using UnityEngine;

namespace RogueliteGame.Skills.Config
{
    public abstract class SkillConfigBase : ScriptableObject
    {
        [Header("General")]
        public string skillName;

        [TextArea(2, 4)]
        public string description;

        public Sprite icon;

        [Header("Timing")]
        [Min(0.1f)]
        public float duration = 5f;

        [Min(0f)]
        public float cooldown = 10f;

        public float TotalCycleTime => Mathf.Max(0.001f, duration + cooldown);
    }
}
