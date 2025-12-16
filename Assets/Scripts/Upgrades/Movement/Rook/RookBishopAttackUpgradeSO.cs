using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 룩 전용: 비숍 어택 - 대각선 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Rook_BishopAttack", menuName = "LevelUpChess/Upgrades/Movement/Rook Bishop Attack")]
    public class RookBishopAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "비숍 어택";
        private const string DEFAULT_DESC = "대각선 공격";

        private void Reset()
        {
            upgradeName = "movement_rook_bishop_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 3;
            pieceFilter = PieceTypeFilter.Rook;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Rook) return false;
            return base.CanApplyTo(piece);
        }
    }
}
