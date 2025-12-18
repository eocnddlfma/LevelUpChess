using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Rook용 Knight처럼 공격하는 업그레이드 가능한 무브먼트 (공격 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementRookKnightAttackSO", menuName = "Chess/Piece Movement/Upgradable/Rook Knight Attack")]
    public class MovementKnightAttackSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.AttackOnly;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            if (piece.CurrentTile == null)
                return new List<Move>();

            // Knight 공격: (2,1), (2,-1), (-2,1), (-2,-1), (1,2), (1,-2), (-1,2), (-1,-2)
            Vector2Int[] offsets = {
                new Vector2Int(2, 1), new Vector2Int(2, -1),
                new Vector2Int(-2, 1), new Vector2Int(-2, -1),
                new Vector2Int(1, 2), new Vector2Int(1, -2),
                new Vector2Int(-1, 2), new Vector2Int(-1, -2)
            };

            return GetJumpingMoves(piece, offsets);
        }
    }
}