using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 킹 전용: 나이트 무브 - 나이트처럼 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_King_KnightMove", menuName = "LevelUpChess/Upgrades/Movement/King Knight Move")]
    public class KingKnightMoveUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "나이트";
        private const string DEFAULT_DESC = "나이트 이동/공격";

        private void Reset()
        {
            upgradeId = "movement_king_knight_move";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 4;
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
