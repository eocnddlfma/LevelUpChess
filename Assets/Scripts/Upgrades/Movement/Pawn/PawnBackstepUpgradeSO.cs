using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 폰 전용: 백스탭 - 한 칸 뒤로 이동 가능 (이동만)
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_Pawn_Backstep", menuName = "LevelUpChess/Upgrades/Movement/Pawn Backstep")]
    public class PawnBackstepUpgradeSO : MovementUpgradeSO
    {
        private const string DEFAULT_NAME = "백스탭";
        private const string DEFAULT_DESC = "뒤 이동";

        private void Reset()
        {
            // 에디터에서 기본값 설정
            upgradeName = "movement_pawn_backstep";
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
            rarity = 1; // Uncommon
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
            // 폰만 적용 가능
            if (piece == null) return false;
            if (piece.PieceType != PieceType.Pawn) return false;
            
            return base.CanApplyTo(piece);
        }
    }
}
