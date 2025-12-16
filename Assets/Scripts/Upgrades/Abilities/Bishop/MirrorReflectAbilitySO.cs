using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Reflects a portion of received damage back to the attacker.
    /// </summary>
    [CreateAssetMenu(fileName = "MirrorReflectAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/MirrorReflect")]
    public class MirrorReflectAbilitySO : AbilityBaseSO
    {
        [SerializeField, Range(0f, 1f)] private float reflectRatio = 0.5f;

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnHit) return;
            if (context.Owner == null || context.Attacker == null) return;

            int reflectDamage = Mathf.RoundToInt(context.Damage * reflectRatio);
            if (reflectDamage > 0)
            {
                context.Attacker.TakeDamage(reflectDamage, context.Owner);
                Debug.Log($"[MirrorReflect] {context.Owner.name} reflected {reflectDamage} damage to {context.Attacker.name}");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            trigger = AbilityTrigger.OnHit;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
