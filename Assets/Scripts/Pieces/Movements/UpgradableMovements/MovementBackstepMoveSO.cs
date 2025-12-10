using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 한 칸 뒤로 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementBackstepMoveSO", menuName = "Chess/Piece Movement/Upgradable/Backstep Move")]
    public class MovementBackstepMoveSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // 기물의 팀에 따라 뒤쪽 방향 결정
            int backwardDir = (piece.Team == Team.White) ? -1 : 1;
            Vector2Int[] offsets = { new Vector2Int(0, backwardDir) };

            // GetJumpingMoves가 자동으로 MoveType 필터링 처리
            return GetJumpingMoves(piece, offsets);
        }
    }
}
