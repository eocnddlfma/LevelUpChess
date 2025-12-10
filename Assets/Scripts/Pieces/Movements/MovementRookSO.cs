using System.Collections.Generic;
using UnityEngine;

namespace LevelUpChess.Pieces
{
    [CreateAssetMenu(fileName = "MovementRookSO", menuName = "Chess/Piece Movement/Rook")]
    public class MovementRookSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.Normal;
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
