using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 옆 방향으로 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementSidewaySO", menuName = "Chess/Piece Movement/Upgradable/Sideway Move")]
    public class MovementSidewaySO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // 옆 방향 오프셋
            Vector2Int[] offsets = {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}