using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 광전사 능력: 체력이 낮을수록 공격력 증가
    /// </summary>
    [CreateAssetMenu(fileName = "BerserkerAbility", menuName = "LevelUpChess/Upgrades/Abilities/Berserker")]
    public class BerserkerAbilitySO : AbilityBaseSO
    {
        [Header("Berserker Settings")]
        [Tooltip("효과 발동 체력 기준 (이 비율 이하일 때 발동)")]
        [SerializeField] private float activationHealthThreshold = 0.5f;
        
        [Tooltip("체력 1% 손실당 공격력 증가율")]
        [SerializeField] private float attackBonusPerMissingPercent = 0.02f;
        
        [Tooltip("최대 공격력 증가 배율")]
        [SerializeField] private float maxAttackMultiplier = 2.0f;

        // 각 기물의 기본 공격력 저장
        private Dictionary<int, int> _baseAttackPower = new Dictionary<int, int>();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            base.SetDefaultNameAndDescription();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = "광전사";
            if (string.IsNullOrEmpty(description)) description = 
                $"체력이 {activationHealthThreshold * 100}% 이하일 때, 잃은 체력 1%당 공격력이 {attackBonusPerMissingPercent * 100}% 증가합니다. (최대 {maxAttackMultiplier}배)";
            trigger = AbilityTrigger.Passive;
        }
#endif

        public override void OnApply(ChessPiece piece)
        {
            if (piece.Combat == null) return;
            int instanceId = piece.Combat.GetInstanceID();
            _baseAttackPower[instanceId] = piece.Combat.AttackPower;
            
            Debug.Log($"[Berserker] {piece.name}에게 광전사 능력 적용 (기본 공격력: {piece.Combat.AttackPower})");
        }

        public override void OnRemove(ChessPiece piece)
        {
            if (piece.Combat == null) return;
            int instanceId = piece.Combat.GetInstanceID();
            _baseAttackPower.Remove(instanceId);
            
            Debug.Log($"[Berserker] {piece.name}에서 광전사 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null)
            {
                Debug.LogWarning("[Berserker] Owner가 없습니다.");
                return;
            }

            int instanceId = context.Owner.GetInstanceID();
            if (!_baseAttackPower.TryGetValue(instanceId, out int baseAttack))
            {
                baseAttack = context.Owner.AttackPower;
                _baseAttackPower[instanceId] = baseAttack;
            }

            float healthPercent = (float)context.Owner.CurrentHealth / context.Owner.MaxHealth;
            
            // 체력이 기준 이상이면 효과 없음
            if (healthPercent > activationHealthThreshold)
            {
                // 데미지 배율 리셋
                context.DamageMultiplier = 1.0f;
                return;
            }

            // 잃은 체력 비율 계산 (0~1)
            float missingHealthPercent = 1f - healthPercent;
            
            // 공격력 증가 배율 계산
            float attackMultiplier = 1f + (missingHealthPercent * 100f * attackBonusPerMissingPercent);
            attackMultiplier = Mathf.Min(attackMultiplier, maxAttackMultiplier);
            
            // 데미지 배율 적용
            context.DamageMultiplier *= attackMultiplier;
            
            Debug.Log($"[Berserker] {context.Owner.name} 광전사 발동! 체력: {healthPercent * 100:F1}%, 공격력 배율: {attackMultiplier:F2}x");
        }

        /// <summary>
        /// 현재 광전사 버프 배율 계산 (UI 표시용)
        /// </summary>
        public float GetCurrentMultiplier(PieceCombat combat)
        {
            float healthPercent = (float)combat.CurrentHealth / combat.MaxHealth;
            
            if (healthPercent > activationHealthThreshold)
            {
                return 1.0f;
            }

            float missingHealthPercent = 1f - healthPercent;
            float multiplier = 1f + (missingHealthPercent * 100f * attackBonusPerMissingPercent);
            return Mathf.Min(multiplier, maxAttackMultiplier);
        }
    }
}
