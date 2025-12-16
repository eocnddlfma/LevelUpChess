using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 앞 방향으로 두 칸 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementTwoStepFrontSO", menuName = "Chess/Piece Movement/Upgradable/Two Step Front Move")]
    public class MovementTwoStepFrontSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // 기물의 팀에 따라 앞쪽 방향 결정
            int forwardDir = (piece.Team == Team.White) ? 1 : -1;
            Vector2Int[] offsets = {
                new Vector2Int(0, forwardDir),
                new Vector2Int(0, forwardDir * 2)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}