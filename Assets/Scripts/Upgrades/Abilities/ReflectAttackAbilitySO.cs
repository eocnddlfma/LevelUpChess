using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 리플렉트 어택 능력: 공격 시 반사 공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "ReflectAttackAbility", menuName = "LevelUpChess/Upgrades/Abilities/ReflectAttack")]
    public class ReflectAttackAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "리플렉트 어택";
        private const string DEFAULT_DESC = "벽에 튕기는 반사 공격 가능";

        [Header("Reflect Attack Settings")]
        [Tooltip("반사 최대 거리")]
        [SerializeField] private int maxReflectDistance = 5;

        [Tooltip("반사 데미지 감소율 (%)")]
        [Range(0, 100)]
        [SerializeField] private int damageReductionPercent = 20;

        public new string AbilityId => "ability_reflect_attack";

        public int MaxReflectDistance => maxReflectDistance;
        public int DamageReductionPercent => damageReductionPercent;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
        }
#endif
    }
}