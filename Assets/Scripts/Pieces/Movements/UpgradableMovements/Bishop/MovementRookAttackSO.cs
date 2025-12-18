using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Rook처럼 공격하는 업그레이드 가능한 무브먼트 (공격 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementRookAttackSO", menuName = "Chess/Piece Movement/Upgradable/Rook Attack")]
    public class MovementRookAttackSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.AttackOnly;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif

        private static readonly Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            return GetSlidingMoves(piece, Directions);
        }
    }
}