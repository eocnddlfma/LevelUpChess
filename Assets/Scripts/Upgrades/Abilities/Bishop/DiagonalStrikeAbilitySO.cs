using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Deals bonus damage on attack hits.
    /// </summary>
    [CreateAssetMenu(fileName = "DiagonalStrikeAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/DiagonalStrike")]
    public class DiagonalStrikeAbilitySO : AbilityBaseSO
    {
        [SerializeField] private int bonusDamage = 2;

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            if (context.Owner == null || context.Target == null) return;

            context.Target.TakeDamage(bonusDamage, context.Owner);
            Debug.Log($"[DiagonalStrike] {context.Owner.name} dealt bonus {bonusDamage} damage to {context.Target.name}");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
