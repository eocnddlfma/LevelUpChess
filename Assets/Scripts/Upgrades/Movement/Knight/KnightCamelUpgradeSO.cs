using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 나이트 전용: 캐멀 - 캐멀 이동(1,3) 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Knight_Camel", menuName = "LevelUpChess/Upgrades/Movement/Knight Camel")]
    public class KnightCamelUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "스퀘어 무브 캐멀";
        private const string DEFAULT_DESC = "캐멀 형태 이동";

        private void Reset()
        {
            upgradeId = "movement_knight_camel";
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
