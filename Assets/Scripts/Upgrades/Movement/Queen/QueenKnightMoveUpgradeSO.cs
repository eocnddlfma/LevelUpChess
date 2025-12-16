using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 퀸 전용: 나이트 무브 - 나이트처럼 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Queen_KnightMove", menuName = "LevelUpChess/Upgrades/Movement/Queen Knight Move")]
    public class QueenKnightMoveUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "나이트 무브";
        private const string DEFAULT_DESC = "나이트 이동";

        private void Reset()
        {
            upgradeId = "movement_queen_knight_move";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 4;
            pieceFilter = PieceTypeFilter.Queen;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Queen) return false;
            return base.CanApplyTo(piece);
        }
    }
}
