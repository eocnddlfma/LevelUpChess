using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 도티낳음: 이동시 이동 전 자리에 폰 생성
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnPawnAbility", menuName = "LevelUpChess/Abilities/Queen/SpawnPawn")]
    public class SpawnPawnAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "도티낳음";
        private const string DEFAULT_DESC = "이동시 이동 전 자리에 폰 생성";

        public override AbilityTrigger Trigger => AbilityTrigger.OnAfterMove;

        public override void OnApply(ChessPiece piece)
        {
            // 패시브 효과 없음
        }

        public override void OnRemove(ChessPiece piece)
        {
            // 패시브 효과 없음
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAfterMove)
                return;

            if (context.Owner == null || context.FromTile == null)
                return;

            // 이동 전 위치에 폰 생성
            SpawnPawnAtTile(context.FromTile, context.Owner);
        }

        private void SpawnPawnAtTile(Tile tile, ChessPiece owner)
        {
            if (tile == null || tile.OccupyingPiece != null)
                return;

            // Use PieceFactory to properly create the Pawn and initialize it
            var pawn = LevelUpChess.Pieces.PieceFactory.Create(PieceType.Pawn, owner.Team, tile);
            if (pawn != null)
            {
                Debug.Log($"[SpawnPawn] {owner.name}이 이동 후 {tile.coordinate}에 폰 생성");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAfterMove;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif
    }
}
