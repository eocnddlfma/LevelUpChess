using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 폰 전용: 다이에고널 무브 - 앞 대각선 이동 가능 (공격 없이)
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Pawn_DiagonalMove", menuName = "LevelUpChess/Upgrades/Movement/Pawn Diagonal Move")]
    public class PawnDiagonalMoveUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "다이에고널 무브";
        private const string DEFAULT_DESC = "앞 대각선 이동";

        private void Reset()
        {
            upgradeId = "movement_pawn_diagonal_move";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 2;
            pieceFilter = PieceTypeFilter.Pawn;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Pawn) return false;
            return base.CanApplyTo(piece);
        }
    }
}
