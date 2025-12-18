using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// King용 Knight처럼 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementKingKnightMoveSO", menuName = "Chess/Piece Movement/Upgradable/King Knight Move")]
    public class MovementKingKnightMoveSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.King;
        }
#endif

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // Knight 이동: (2,1), (2,-1), (-2,1), (-2,-1), (1,2), (1,-2), (-1,2), (-1,-2)
            Vector2Int[] offsets = {
                new Vector2Int(2, 1), new Vector2Int(2, -1),
                new Vector2Int(-2, 1), new Vector2Int(-2, -1),
                new Vector2Int(1, 2), new Vector2Int(1, -2),
                new Vector2Int(-1, 2), new Vector2Int(-1, -2)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}