using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 무빙맨: 이동한 칸수만큼 경험치가 증가합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MovingManAbility", menuName = "LevelUpChess/Upgrades/Abilities/Rook/MovingMan")]
    public class MovingManAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "무빙맨";
        private const string DEFAULT_DESC = "이동한 칸 수만큼 경험치가 증가합니다.";

        [Header("Moving Man Settings")]
        [Tooltip("이동 칸당 경험치 증가")]
        [SerializeField] private int expPerTile = 2;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[MovingMan] {piece.name}에게 무빙맨 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[MovingMan] {piece.name}에서 무빙맨 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnAfterMove) return;
            
            if (context.FromTile == null || context.ToTile == null)
            {
                return;
            }

            // 이동 거리 계산 (맨해튼 거리)
            int distance = Mathf.Abs(context.FromTile.coordinate.x - context.ToTile.coordinate.x) +
                          Mathf.Abs(context.FromTile.coordinate.y - context.ToTile.coordinate.y);

            if (distance > 0)
            {
                int bonusExp = distance * expPerTile;
                context.Owner.Stats.GainExperience(bonusExp);
                Debug.Log($"[MovingMan] 이동 거리 {distance}칸, 경험치 +{bonusExp}!");
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
