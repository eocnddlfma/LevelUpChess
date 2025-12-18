using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 더 넓은 공격 범위를 제공하는 업그레이드 가능한 무브먼트 (공격 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementLargerAttackSpaceSO", menuName = "Chess/Piece Movement/Upgradable/Larger Attack Space")]
    public class MovementLargerAttackSpaceSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.AttackOnly;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // 주변 2칸 공격 (더 넓은 범위)
            Vector2Int[] offsets = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1),
                new Vector2Int(2, 0), new Vector2Int(-2, 0),
                new Vector2Int(0, 2), new Vector2Int(0, -2)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}