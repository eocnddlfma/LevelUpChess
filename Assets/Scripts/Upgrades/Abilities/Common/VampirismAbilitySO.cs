using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 흡혈 능력: 공격 시 피해량의 일부만큼 체력 회복
    /// </summary>
    [CreateAssetMenu(fileName = "VampirismAbility", menuName = "LevelUpChess/Upgrades/Abilities/Vampirism")]
    public class VampirismAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "흡혈";
        private const string DEFAULT_DESC = "공격시 공격력/4를 회복합니다.";

        [Header("Vampirism Settings")]
        [Tooltip("흡혈 비율 (0~1)")]
        [SerializeField] private float lifeStealPercent = 0.25f;
        
        [Tooltip("킬 시 추가 흡혈량")]
        [SerializeField] private int bonusHealOnKill = 5;
        
        [Tooltip("최대 체력 초과 회복 허용")]
        [SerializeField] private bool allowOverheal = false;
        
        [Tooltip("초과 회복 시 보너스 체력으로 전환")]
        [SerializeField] private bool overhealAsShield = true;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
        }
#endif

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[Vampirism] {piece.name}에게 흡혈 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[Vampirism] {piece.name}에서 흡혈 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null)
            {
                Debug.LogWarning("[Vampirism] Owner가 없습니다.");
                return;
            }

            // 실제로 가한 피해량 기준 흡혈
            int damageDealt = context.Damage;
            int healAmount = Mathf.RoundToInt(damageDealt * lifeStealPercent);
            
            // 킬 시 추가 회복
            if (context.TargetDied && bonusHealOnKill > 0)
            {
                healAmount += bonusHealOnKill;
                Debug.Log($"[Vampirism] 처치 보너스 회복! +{bonusHealOnKill}");
            }

            if (healAmount <= 0)
            {
                return;
            }

            int currentHealth = context.Owner.CurrentHealth;
            int maxHealth = context.Owner.MaxHealth;
            int healable = maxHealth - currentHealth;

            if (allowOverheal || healable >= healAmount)
            {
                // 일반 회복
                context.Owner.Combat.Heal(healAmount);
                Debug.Log($"[Vampirism] {context.Owner.name}이(가) {healAmount} 흡혈 회복!");
            }
            else
            {
                // 최대 체력까지만 회복
                int actualHeal = healable;
                int overflow = healAmount - healable;
                
                if (actualHeal > 0)
                {
                    context.Owner.Combat.Heal(actualHeal);
                    Debug.Log($"[Vampirism] {context.Owner.name}이(가) {actualHeal} 흡혈 회복!");
                }

                // 초과분을 보호막으로 전환
                if (overhealAsShield && overflow > 0)
                {
                    // TODO: PieceCombat에 AddShield 메서드 추가 필요
                    // context.Owner.Combat.AddShield(overflow);
                    Debug.Log($"[Vampirism] 초과 회복 {overflow}이(가) 보호막으로 전환! (TODO: 구현 필요)");
                }
            }
        }
    }
}
