using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 초장거리 저격: 5칸 이상 거리에 있는 적을 공격할 경우 즉사합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "LongRangeSniperAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/LongRangeSniper")]
    public class LongRangeSniperAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "초장거리 저격";
        private const string DEFAULT_DESC = "5칸 이상 거리에 있는 적을 공격할 경우 즉사합니다";

        [Header("Long Range Sniper Settings")]
        [Tooltip("즉사 최소 거리")]
        [SerializeField] private int minDistanceForInstantKill = 5;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[LongRangeSniper] {piece.name}에게 초장거리 저격 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[LongRangeSniper] {piece.name}에서 초장거리 저격 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            var ownerTile = context.Owner.CurrentTile;
            var targetTile = context.Target.CurrentTile;

            if (ownerTile == null || targetTile == null) return;

            // 거리 계산 (대각선은 체비셰프 거리)
            int dx = Mathf.Abs(ownerTile.coordinate.x - targetTile.coordinate.x);
            int dy = Mathf.Abs(ownerTile.coordinate.y - targetTile.coordinate.y);
            int distance = Mathf.Max(dx, dy); // 비숍은 대각선이므로 체비셰프 거리 사용

            if (distance >= minDistanceForInstantKill)
            {
                context.Target.ForceKill();
                Debug.Log($"[LongRangeSniper] {distance}칸 거리! {context.Target.name} 즉사!");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
