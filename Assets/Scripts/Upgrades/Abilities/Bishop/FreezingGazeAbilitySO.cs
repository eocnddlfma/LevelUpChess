using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Freezing Gaze: applies a small damage on hit.
    /// </summary>
    [CreateAssetMenu(fileName = "FreezingGazeAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/FreezingGaze")]
    public class FreezingGazeAbilitySO : AbilityBaseSO
    {
        [SerializeField] private int bonusDamage = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[FreezingGaze] {piece.name} gained Freezing Gaze");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[FreezingGaze] {piece.name} lost Freezing Gaze");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            if (context.Owner == null || context.Target == null) return;

            context.Target.TakeDamage(bonusDamage, context.Owner);
            Debug.Log($"[FreezingGaze] {context.Owner.name} dealt extra {bonusDamage} damage to {context.Target.name}");
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
