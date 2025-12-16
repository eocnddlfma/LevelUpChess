using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 나이트 전용: 제브라 - 제브라 이동(2,3) 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Knight_Zebra", menuName = "LevelUpChess/Upgrades/Movement/Knight Zebra")]
    public class KnightZebraUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "제브라";
        private const string DEFAULT_DESC = "제브라 패턴 이동";

        private void Reset()
        {
            upgradeName = "movement_knight_zebra";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 3;
            pieceFilter = PieceTypeFilter.Knight;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Knight) return false;
            return base.CanApplyTo(piece);
        }
    }
}
