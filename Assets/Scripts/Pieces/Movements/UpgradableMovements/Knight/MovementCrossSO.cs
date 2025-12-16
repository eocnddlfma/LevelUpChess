using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 십자 모양으로 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementCrossSO", menuName = "Chess/Piece Movement/Upgradable/Cross Move")]
    public class MovementCrossSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // 십자 방향: 상하좌우
            Vector2Int[] offsets = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}