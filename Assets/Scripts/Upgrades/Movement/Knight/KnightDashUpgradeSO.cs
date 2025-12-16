using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 나이트 전용: 대시 - 앞 두칸 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Knight_Dash", menuName = "LevelUpChess/Upgrades/Movement/Knight Dash")]
    public class KnightDashUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "대시 앞두칸";
        private const string DEFAULT_DESC = "앞으로 두 칸 돌진";

        private void Reset()
        {
            upgradeName = "movement_knight_dash";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 2;
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
