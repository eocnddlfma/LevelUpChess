using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 폰 전용: 라저 어택 스페이스 - 앞 대각선 2칸까지 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Pawn_LargerAttackSpace", menuName = "LevelUpChess/Upgrades/Movement/Pawn Larger Attack Space")]
    public class PawnLargerAttackSpaceUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "라저 어택 스페이스";
        private const string DEFAULT_DESC = "앞 1칸 옆 2칸 공격";

        private void Reset()
        {
            upgradeName = "movement_pawn_larger_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 3;
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
