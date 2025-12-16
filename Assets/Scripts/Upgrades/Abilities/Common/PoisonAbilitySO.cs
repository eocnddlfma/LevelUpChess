using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Upgrades.Status;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 독: 공격시 적에게 독 상태이상을 부여합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PoisonAbility", menuName = "LevelUpChess/Upgrades/Abilities/Common/Poison")]
    public class PoisonAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "독";
        private const string DEFAULT_DESC = "공격시 적에게 독 상태이상을 부여합니다.";

        [Header("Poison Settings")]
        [SerializeField] private int poisonDamagePerTurn = 1;
        [SerializeField] private int poisonDuration = 3;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[Poison] {piece.name}에게 독 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[Poison] {piece.name}에서 독 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            
            if (context.Trigger == AbilityTrigger.OnAttackHit && !context.TargetDied)
            {
                // 적에게 독 상태이상 부여
                var targetCombat = context.Target.Combat;
                if (targetCombat != null)
                {
                    var poisonEffect = new PoisonStatusEffect(poisonDamagePerTurn, poisonDuration, upgradeName, context.Owner);
                    targetCombat.ApplyStatusEffect(poisonEffect);
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
        }
#endif
    }
}
