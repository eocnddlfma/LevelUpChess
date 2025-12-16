using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Movement
{
    /// <summary>
    /// 킹 전용: 제한된 비숍 - 대각선 최대 3칸 이동/공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_King_LimitedBishop", menuName = "LevelUpChess/Upgrades/Movement/King Limited Bishop")]
    public class KingLimitedBishopUpgradeSO : MovementUpgradeSO
    {
        private void Reset()
        {
            // 에디터에서 기본값 설정
            upgradeId = "movement_king_limited_bishop";
            upgradeName = "비숍의 발걸음";
            description = "대각선으로 최대 3칸까지 이동/공격할 수 있습니다.";
            rarity = 3; // Epic
            pieceFilter = PieceTypeFilter.King;
        }

        public override bool CanApplyTo(ChessPiece piece)
        {
            // 킹만 적용 가능
            if (piece == null) return false;
            if (piece.PieceType != PieceType.King) return false;
            
            return base.CanApplyTo(piece);
        }
    }
}
