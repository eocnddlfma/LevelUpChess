using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 폰 전용: 프론트 어택 - 앞 한칸 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Pawn_FrontAttack", menuName = "LevelUpChess/Upgrades/Movement/Pawn Front Attack")]
    public class PawnFrontAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "프론트 어택";
        private const string DEFAULT_DESC = "앞 한칸 공격";

        private void Reset()
        {
            upgradeName = "movement_pawn_front_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 1;
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
