using UnityEngine;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Defines stats and visuals for a ghost archetype.
    /// </summary>
    [CreateAssetMenu(fileName = "GhostTypeDefinition", menuName = "Ashlight/Ghost/Ghost Type Definition")]
    public class GhostTypeDefinition : ScriptableObject
    {
        [SerializeField] private string ghostName = "Wraith";
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float damage = 10f;
        [Range(0f, 1f)]
        [SerializeField] private float lightResistance = 0.5f;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float retreatSpeed = 5f;
        [SerializeField] private Material visualMaterial;

        /// <summary>Gets the display name of this ghost type.</summary>
        public string GhostName => ghostName;

        /// <summary>Gets the default movement speed.</summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>Gets damage dealt per attack.</summary>
        public float Damage => damage;

        /// <summary>Gets torch repel resistance from 0 to 1.</summary>
        public float LightResistance => lightResistance;

        /// <summary>Gets the base player detection range.</summary>
        public float DetectionRange => detectionRange;

        /// <summary>Gets the melee attack range.</summary>
        public float AttackRange => attackRange;

        /// <summary>Gets movement speed while retreating from torch light.</summary>
        public float RetreatSpeed => retreatSpeed;

        /// <summary>Gets the material applied to this ghost's renderer.</summary>
        public Material VisualMaterial => visualMaterial;
    }
}
