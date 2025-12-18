using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 룩용 비숍 어택 - 대각선 방향 공격만 가능 (이동 불가)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementBishopAttackSO", menuName = "Chess/Piece Movement/Upgradable/Bishop Attack")]
    public class MovementBishopAttackSO : PieceMovementSO
    {
        private static readonly Vector2Int[] DiagonalDirections = {
            Vector2Int.one,                 // (1, 1) 우상
            new Vector2Int(1, -1),          // 우하
            new Vector2Int(-1, 1),          // 좌상
            new Vector2Int(-1, -1)          // 좌하
        };

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif

        private void OnEnable()
        {
            moveType = MoveType.AttackOnly; // 공격만 가능
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            // 대각선 슬라이딩 이동 (비숍처럼)
            // GetSlidingMoves에서 MoveType 필터링이 적용되어 공격만 반환됨
            return GetSlidingMoves(piece, DiagonalDirections);
        }
    }
}
