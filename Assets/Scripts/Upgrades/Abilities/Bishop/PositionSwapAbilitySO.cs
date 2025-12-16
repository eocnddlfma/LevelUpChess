using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Swaps position with a targeted ally on turn start if adjacent.
    /// </summary>
    [CreateAssetMenu(fileName = "PositionSwapAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/PositionSwap")]
    public class PositionSwapAbilitySO : AbilityBaseSO
    {
        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnTurnStart) return;
            if (context.Owner == null || context.Target == null) return;
            if (context.Target.Team != context.Owner.Team) return;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var ownerTile = context.Owner.CurrentTile;
            var targetTile = context.Target.CurrentTile;
            if (ownerTile == null || targetTile == null) return;

            // Must be adjacent to swap.
            int dist = Mathf.Abs(ownerTile.coordinate.x - targetTile.coordinate.x) +
                       Mathf.Abs(ownerTile.coordinate.y - targetTile.coordinate.y);
            if (dist != 1) return;

            context.Owner.UpdateTileInfo(targetTile);
            context.Target.UpdateTileInfo(ownerTile);
            Debug.Log($"[PositionSwap] {context.Owner.name} swapped with {context.Target.name}");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            trigger = AbilityTrigger.OnTurnStart;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
