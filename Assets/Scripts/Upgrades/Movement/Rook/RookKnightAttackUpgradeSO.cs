using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 룩 전용: 나이트 어택 - 나이트 방향 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Rook_KnightAttack", menuName = "LevelUpChess/Upgrades/Movement/Rook Knight Attack")]
    public class RookKnightAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "나이트 어택";
        private const string DEFAULT_DESC = "나이트 공격";

        private void Reset()
        {
            upgradeId = "movement_rook_knight_attack";
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
