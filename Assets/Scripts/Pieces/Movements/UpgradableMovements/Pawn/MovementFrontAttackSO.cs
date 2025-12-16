using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 앞 방향으로 공격하는 업그레이드 가능한 무브먼트 (공격 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementFrontAttackSO", menuName = "Chess/Piece Movement/Upgradable/Front Attack")]
    public class MovementFrontAttackSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.AttackOnly;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // 기물의 팀에 따라 앞쪽 방향 결정
            int forwardDir = (piece.Team == Team.White) ? 1 : -1;
            Vector2Int[] offsets = { new Vector2Int(0, forwardDir) };

            return GetJumpingMoves(piece, offsets);
        }
    }
}