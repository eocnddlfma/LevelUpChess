using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 비숍 전용: 리플렉트 어택 - 벽에 튕기는 반사 이동 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Bishop_ReflectAttack", menuName = "LevelUpChess/Upgrades/Movement/Bishop Reflect Attack")]
    public class BishopReflectAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "리플렉트 어택";
        private const string DEFAULT_DESC = "벽에 튕기는 반사 이동";

        private void Reset()
        {
            upgradeId = "movement_bishop_reflect_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 4;
            pieceFilter = PieceTypeFilter.Bishop;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Bishop) return false;
            return base.CanApplyTo(piece);
        }
    }
}
