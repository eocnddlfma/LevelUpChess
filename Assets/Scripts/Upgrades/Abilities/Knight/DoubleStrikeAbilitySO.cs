using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Deals an extra hit when attacking.
    /// </summary>
    [CreateAssetMenu(fileName = "DoubleStrikeAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/DoubleStrike")]
    public class DoubleStrikeAbilitySO : AbilityBaseSO
    {
        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[DoubleStrike] {piece.name} gained Double Strike");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[DoubleStrike] {piece.name} lost Double Strike");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            if (context.Owner == null || context.Target == null) return;
            if (!context.Target.IsAlive) return;

            int bonusDamage = context.Owner.AttackPower;
            context.Target.TakeDamage(bonusDamage, context.Owner);
            Debug.Log($"[DoubleStrike] {context.Owner.name} dealt extra {bonusDamage} damage to {context.Target.name}");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
