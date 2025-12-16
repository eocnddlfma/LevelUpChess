using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 폰 전용: 사이드웨이 - 옆 칸 이동+공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Pawn_Sideway", menuName = "LevelUpChess/Upgrades/Movement/Pawn Sideway")]
    public class PawnSidewayUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "사이드웨이";
        private const string DEFAULT_DESC = "옆 칸 이동+공격";

        private void Reset()
        {
            upgradeName = "movement_pawn_sideway";
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
