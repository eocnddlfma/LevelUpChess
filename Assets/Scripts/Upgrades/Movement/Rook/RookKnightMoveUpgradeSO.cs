using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 룩 전용: 나이트 무브 - 나이트처럼 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Rook_KnightMove", menuName = "LevelUpChess/Upgrades/Movement/Rook Knight Move")]
    public class RookKnightMoveUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "나이트 무브";
        private const string DEFAULT_DESC = "나이트 이동";

        private void Reset()
        {
            upgradeName = "movement_rook_knight_move";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 4;
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
