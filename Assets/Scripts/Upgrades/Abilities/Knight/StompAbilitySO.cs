using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Damages adjacent enemies after moving.
    /// </summary>
    [CreateAssetMenu(fileName = "StompAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/Stomp")]
    public class StompAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "스톰프";
        private const string DEFAULT_DESC = "이동 후 인접한 적에게 공격력만큼 데미지를 줍니다.";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[Stomp] {piece.name} gained Stomp");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[Stomp] {piece.name} lost Stomp");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAfterMove) return;
            if (context.Owner == null) return;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var tile = context.Owner.CurrentTile;
            if (tile == null) return;

            Vector2Int[] offsets = {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            foreach (var offset in offsets)
            {
                var t = boardManager.GetTileAt(tile.coordinate + offset);
                var enemy = t?.OccupyingPiece;
                if (enemy != null && enemy.Team != context.Owner.Team)
                {
                    int damage = context.Owner.Stats.Attack;
                    enemy.TakeDamage(damage, context.Owner);
                    Debug.Log($"[Stomp] {context.Owner.name} stomped {enemy.name} for {damage} damage");
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAfterMove;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
