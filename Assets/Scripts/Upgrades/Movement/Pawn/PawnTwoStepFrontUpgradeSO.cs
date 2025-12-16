using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 폰 전용: 투스탭 프론트 - 앞 2칸 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Pawn_TwoStepFront", menuName = "LevelUpChess/Upgrades/Movement/Pawn Two Step Front")]
    public class PawnTwoStepFrontUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "투스탭 프론트";
        private const string DEFAULT_DESC = "앞 2칸 이동";

        private void Reset()
        {
            upgradeName = "movement_pawn_two_step";
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
