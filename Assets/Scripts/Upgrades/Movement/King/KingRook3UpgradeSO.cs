using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 킹 전용: 룩 최대 3칸 - 직선 최대 3칸 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_King_Rook3", menuName = "LevelUpChess/Upgrades/Movement/King Rook3")]
    public class KingRook3UpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "룩 최대 3칸";
        private const string DEFAULT_DESC = "룩처럼 최대 3칸 이동/공격";

        private void Reset()
        {
            upgradeId = "movement_king_rook3";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 3;
            pieceFilter = PieceTypeFilter.King;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.King;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.King) return false;
            return base.CanApplyTo(piece);
        }
    }
}
