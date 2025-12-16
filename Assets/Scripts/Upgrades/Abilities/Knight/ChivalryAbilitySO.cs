using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 기사도 정신: 방어력 +1, 이동할 수 있는 위치의 아군이 공격받는 다면 해당 아군과 위치를 바꾸고 데미지를 받습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ChivalryAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/Chivalry")]
    public class ChivalryAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "기사도 정신";
        private const string DEFAULT_DESC = "방어력 +1, 이동할 수 있는 위치의 아군이 공격받는 다면 해당 아군과 위치를 바꾸고 데미지를 받습니다.";

        [Header("Chivalry Settings")]
        [SerializeField] private int defenseBonus = 1;

        public override void OnApply(ChessPiece piece)
        {
            piece.Stats.AddModifier(StatType.Defense, defenseBonus);
            Debug.Log($"[Chivalry] {piece.name} gained chivalry - Defense +{defenseBonus}");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.Stats.RemoveModifier(StatType.Defense, defenseBonus);
            Debug.Log($"[Chivalry] {piece.name} lost chivalry");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAllyHit) return;
            if (context.Owner == null || context.Target == null) return;

            var ally = context.Target;
            var knight = context.Owner;

            if (ally.Team != knight.Team) return;
            if (ally == knight) return;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            // Check if knight can move to ally's tile
            var knightMoves = knight.GetAvailableMoves();
            bool canReach = false;
            foreach (var move in knightMoves)
            {
                var tile = boardManager.GetTileAt(move.to);
                if (tile != null && tile == ally.CurrentTile)
                {
                    canReach = true;
                    break;
                }
            }

            if (!canReach) return;

            var knightTile = knight.CurrentTile;
            var allyTile = ally.CurrentTile;
            if (knightTile == null || allyTile == null) return;

            // Swap positions
            knight.UpdateTileInfo(allyTile);
            ally.UpdateTileInfo(knightTile);

            // Mitigate damage for the ally by forcing an evade and taking the hit on the knight
            context.ShouldEvade = true;
            knight.TakeDamage(context.Damage, context.Attacker);

            Debug.Log($"[Chivalry] {knight.name} swapped with {ally.name} and took the hit!");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAllyHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
