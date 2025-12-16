using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 비숍 전용: 나이트 어택 - 나이트 방향 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Bishop_KnightAttack", menuName = "LevelUpChess/Upgrades/Movement/Bishop Knight Attack")]
    public class BishopKnightAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "나이트 어택";
        private const string DEFAULT_DESC = "나이트 어택";

        private void Reset()
        {
            upgradeId = "movement_bishop_knight_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 3;
            pieceFilter = PieceTypeFilter.Bishop;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Bishop) return false;
            return base.CanApplyTo(piece);
        }
    }
}
