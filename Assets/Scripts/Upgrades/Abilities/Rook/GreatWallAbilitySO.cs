using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 만리장성: 이 룩의 방어력 1 증가, 다른 룩과 같은 가로줄에 있을 경우 해당 줄에 있는 다른 아군 피스들의 방어력이 이 피스의 방어력만큼 상승.
    /// </summary>
    [CreateAssetMenu(fileName = "GreatWallAbility", menuName = "LevelUpChess/Upgrades/Abilities/Rook/GreatWall")]
    public class GreatWallAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "만리장성";
        private const string DEFAULT_DESC = "이 룩의 방어력 1 증가, 다른 룩과 같은 가로줄에 있을 경우 해당 줄에 있는 다른 아군 피스들의 방어력이 이 피스의 방어력만큼 상승.";

        [Header("Great Wall Settings")]
        [Tooltip("기본 방어력 증가")]
        [SerializeField] private int baseDefenseBonus = 1;

        public override void OnApply(ChessPiece piece)
        {
            // 기본 방어력 증가
            piece.Stats.AddModifier(StatType.Defense, baseDefenseBonus);
            Debug.Log($"[GreatWall] {piece.name}에게 만리장성 적용 - 방어력 +{baseDefenseBonus}");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.Stats.RemoveModifier(StatType.Defense, baseDefenseBonus);
            Debug.Log($"[GreatWall] {piece.name}에서 만리장성 제거");
        }

        public override void Execute(AbilityContext context)
        {
            // 이동 후 또는 턴 시작시 효과 갱신
            if (context.Trigger != AbilityTrigger.OnAfterMove && 
                context.Trigger != AbilityTrigger.OnTurnStart) return;
            
            UpdateWallEffect(context.Owner);
        }

        private void UpdateWallEffect(ChessPiece owner)
        {
            if (owner?.CurrentTile == null) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            int ownerY = owner.CurrentTile.coordinate.y;
            int ownerDefense = owner.Stats.Defense;
            
            // 같은 가로줄에 다른 아군 룩이 있는지 확인
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            bool hasAllyRookOnRow = false;

            foreach (var piece in allPieces)
            {
                if (piece == owner) continue;
                if (piece.Team != owner.Team) continue;
                if (piece.PieceType != PieceType.Rook) continue;
                if (piece.CurrentTile == null) continue;
                if (piece.CurrentTile.coordinate.y != ownerY) continue;

                hasAllyRookOnRow = true;
                break;
            }

            if (!hasAllyRookOnRow) return;

            // 같은 줄의 모든 아군에게 방어력 버프
            foreach (var piece in allPieces)
            {
                if (piece == owner) continue;
                if (piece.Team != owner.Team) continue;
                if (piece.CurrentTile == null) continue;
                if (piece.CurrentTile.coordinate.y != ownerY) continue;

                // 임시 버프 적용 (다음 턴까지)
                piece.Stats.AddTemporaryModifier(StatType.Defense, ownerDefense, 1);
                Debug.Log($"[GreatWall] {piece.name}에게 방어력 +{ownerDefense} 부여!");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAfterMove;
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif
    }
}
