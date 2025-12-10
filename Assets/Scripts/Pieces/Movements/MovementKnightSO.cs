using System.Collections.Generic;
using UnityEngine;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 나이트의 이동 방식을 정의합니다
    /// </summary>
    [CreateAssetMenu(fileName = "MovementKnightSO", menuName = "Chess/Piece Movement/Knight")]
    public class MovementKnightSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.Normal;
        }

        private static readonly Vector2Int[] KnightOffsets = {
            new Vector2Int(2, 1), new Vector2Int(2, -1),
            new Vector2Int(-2, 1), new Vector2Int(-2, -1),
            new Vector2Int(1, 2), new Vector2Int(1, -2),
            new Vector2Int(-1, 2), new Vector2Int(-1, -2)
        };

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            return GetJumpingMoves(piece, KnightOffsets);
        }
    }
}
