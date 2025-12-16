using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 퀸 전용: 리플렉트 어택 - 벽에 튕기는 반사 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Queen_ReflectAttack", menuName = "LevelUpChess/Upgrades/Movement/Queen Reflect Attack")]
    public class QueenReflectAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "리플렉트 어택";
        private const string DEFAULT_DESC = "공격 반사 어택";

        private void Reset()
        {
            upgradeName = "movement_queen_reflect_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 4;
            pieceFilter = PieceTypeFilter.Queen;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif

        public override bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Queen) return false;
            return base.CanApplyTo(piece);
        }
    }
}
