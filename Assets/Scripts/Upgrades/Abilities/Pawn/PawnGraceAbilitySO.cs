using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 폰은정: 1회 공격을 받아도 해당 공격을 무효화함. 
    /// 이 피스 2회 행동시 효과 충전됨.
    /// </summary>
    [CreateAssetMenu(fileName = "PawnGraceAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/PawnGrace")]
    public class PawnGraceAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "폰은정";
        private const string DEFAULT_DESC = "1회 공격을 받아도 해당 공격을 무효화함. 이 피스 2회 행동시 효과 충전됨.";

        [Header("Pawn Grace Settings")]
        [Tooltip("충전에 필요한 행동 횟수")]
        [SerializeField] private int actionsToRecharge = 2;

        private int currentActions = 0;
        private bool shieldActive = true;

        public override void OnApply(ChessPiece piece)
        {
            shieldActive = true;
            currentActions = 0;
            Debug.Log($"[PawnGrace] {piece.name}에게 폰은정 적용 - 보호막 활성화!");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PawnGrace] {piece.name}에서 폰은정 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;

            // 피격시 - 보호막 소모
            if (context.Trigger == AbilityTrigger.OnHit && shieldActive)
            {
                context.ShouldEvade = true;
                shieldActive = false;
                currentActions = 0;
                Debug.Log($"[PawnGrace] {context.Owner.name} 공격 무효화! 보호막 소진됨");
                return;
            }

            // 행동 후 - 충전 카운트
            if (context.Trigger == AbilityTrigger.OnAfterMove || context.Trigger == AbilityTrigger.OnAttackHit)
            {
                if (!shieldActive)
                {
                    currentActions++;
                    Debug.Log($"[PawnGrace] 충전 진행: {currentActions}/{actionsToRecharge}");
                    
                    if (currentActions >= actionsToRecharge)
                    {
                        shieldActive = true;
                        currentActions = 0;
                        Debug.Log($"[PawnGrace] {context.Owner.name} 보호막 재충전 완료!");
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive; // Passive로 설정하여 다중 트리거 처리
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif
    }
}
