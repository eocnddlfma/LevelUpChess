using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Camel 이동: 한 방향으로 두 칸 이동 후 그 방향의 대각선으로 한 칸 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementCamelSO", menuName = "Chess/Piece Movement/Upgradable/Camel Move")]
    public class MovementCamelSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // Camel 이동: (3,1), (3,-1), (-3,1), (-3,-1), (1,3), (1,-3), (-1,3), (-1,-3)
            Vector2Int[] offsets = {
                new Vector2Int(3, 1), new Vector2Int(3, -1),
                new Vector2Int(-3, 1), new Vector2Int(-3, -1),
                new Vector2Int(1, 3), new Vector2Int(1, -3),
                new Vector2Int(-1, 3), new Vector2Int(-1, -3)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}