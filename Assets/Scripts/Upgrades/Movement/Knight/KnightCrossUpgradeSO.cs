using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 나이트 전용: 크로스 - 십자 방향으로 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Knight_Cross", menuName = "LevelUpChess/Upgrades/Movement/Knight Cross")]
    public class KnightCrossUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "크로스 십자";
        private const string DEFAULT_DESC = "십자 방향으로 이동할 수 있습니다.";

        private void Reset()
        {
            upgradeName = "movement_knight_cross";
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
