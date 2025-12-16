using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 퀸 전용: 나이트 어택 - 나이트 방향 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Queen_KnightAttack", menuName = "LevelUpChess/Upgrades/Movement/Queen Knight Attack")]
    public class QueenKnightAttackUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "나이트 어택";
        private const string DEFAULT_DESC = "나이트 어택";

        private void Reset()
        {
            upgradeName = "movement_queen_knight_attack";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 3;
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
