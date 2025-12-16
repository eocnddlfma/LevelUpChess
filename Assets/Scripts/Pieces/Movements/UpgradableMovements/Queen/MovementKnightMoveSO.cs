using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 퀸용 나이트 무브 - 나이트 방식 이동만 가능 (공격 불가)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementKnightMoveSO", menuName = "Chess/Piece Movement/Upgradable/Knight Move")]
    public class MovementKnightMoveSO : PieceMovementSO
    {
        private static readonly Vector2Int[] KnightOffsets = {
            new Vector2Int(2, 1), new Vector2Int(2, -1),
            new Vector2Int(-2, 1), new Vector2Int(-2, -1),
            new Vector2Int(1, 2), new Vector2Int(1, -2),
            new Vector2Int(-1, 2), new Vector2Int(-1, -2)
        };

        private void OnEnable()
        {
            moveType = MoveType.MoveOnly; // 이동만 가능
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            // 나이트 점프 이동
            // GetJumpingMoves에서 MoveType 필터링이 적용되어 이동만 반환됨
            return GetJumpingMoves(piece, KnightOffsets);
        }
    }
}
