using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 히트앤런 능력: 적을 처치 후 원래 위치로 복귀
    /// 암살자 스타일의 플레이 가능
    /// </summary>
    [CreateAssetMenu(fileName = "HitAndRunAbility", menuName = "LevelUpChess/Upgrades/Abilities/HitAndRun")]
    public class HitAndRunAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "히트앤런";
        private const string DEFAULT_DESC = "상대방을 공격하고 상대방이 사망하더라도 이동하지 않습니다";

        [Header("Hit And Run Settings")]
        [Tooltip("처치 실패 시에도 복귀할지 여부")]
        [SerializeField] private bool returnOnMiss = false;
        
        [Tooltip("복귀 시 무적 시간 (초)")]
        [SerializeField] private float invincibilityDuration = 0.5f;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnKill;
        }
#endif

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[HitAndRun] {piece.name}에게 히트앤런 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[HitAndRun] {piece.name}에서 히트앤런 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.FromTile == null)
            {
                Debug.LogWarning("[HitAndRun] 컨텍스트가 유효하지 않습니다.");
                return;
            }

            // 처치 성공 또는 returnOnMiss가 true인 경우에만 복귀
            bool shouldReturn = context.TargetDied || returnOnMiss;
            
            if (shouldReturn)
            {
                // 이동 취소 플래그 설정 - 원래 위치에 머무름
                context.PreventMoveAfterKill = true;
                
                Debug.Log($"[HitAndRun] {context.Owner.name}이(가) 원래 위치({context.FromTile.coordinate})로 복귀합니다.");
                
                // 무적 시간이 설정되어 있다면 적용 (추후 구현 가능)
                if (invincibilityDuration > 0f)
                {
                    // TODO: 무적 상태 적용 - 추후 StatusEffect 시스템과 연동
                    // context.Owner.ApplyInvincibility(invincibilityDuration);
                }
            }
        }
    }
}
