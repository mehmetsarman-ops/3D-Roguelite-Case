using UnityEngine;

namespace RogueliteGame.Combat
{
    public struct ArrowSkillData
    {
        public bool hasBurn;
        public float burnDamagePerTick;
        public float burnDuration;
        public float burnTickInterval;
        public int burnMaxStacks;

        public Sprite burnStackIcon;
        public Vector2 burnStackIconSize;
        public float burnStackIconSpacing;
        public float burnStackIconYOffset;

        public int bounceCount;
        public float bounceRadius;

        public LayerMask enemyLayerMask;
        public GameObject arrowPrefab;
    }
}
