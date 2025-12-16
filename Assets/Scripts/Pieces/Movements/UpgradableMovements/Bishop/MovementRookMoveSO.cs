using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Rook처럼 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementRookMoveSO", menuName = "Chess/Piece Movement/Upgradable/Rook Move")]
    public class MovementRookMoveSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        private static readonly Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            return GetSlidingMoves(piece, Directions);
        }
    }
}