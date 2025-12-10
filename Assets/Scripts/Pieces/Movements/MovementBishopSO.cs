using System.Collections.Generic;
using LevelUpChess.Pieces;
using UnityEngine;

namespace Pieces.Movements
{
    [CreateAssetMenu(fileName = "MovementBishopSO", menuName = "Chess/Piece Movement/Bishop")]
    public class MovementBishopSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.Normal;
        }

        private static readonly Vector2Int[] DiagonalDirections = {
            Vector2Int.one, 
            new Vector2Int(1, -1), 
            new Vector2Int(-1, 1), 
            new Vector2Int(-1, -1)
        };

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            return GetSlidingMoves(piece, DiagonalDirections);
        }
    }
}

