using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Zebra 이동: (3,2) 방향으로 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementZebraSO", menuName = "Chess/Piece Movement/Upgradable/Zebra Move")]
    public class MovementZebraSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // Zebra 이동: (3,2), (3,-2), (-3,2), (-3,-2), (2,3), (2,-3), (-2,3), (-2,-3)
            Vector2Int[] offsets = {
                new Vector2Int(3, 2), new Vector2Int(3, -2),
                new Vector2Int(-3, 2), new Vector2Int(-3, -2),
                new Vector2Int(2, 3), new Vector2Int(2, -3),
                new Vector2Int(-2, 3), new Vector2Int(-2, -3)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}