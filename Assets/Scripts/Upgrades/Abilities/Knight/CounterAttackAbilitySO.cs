using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 반격: 공격을 받은 경우 대상에게 공격합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CounterAttackAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/CounterAttack")]
    public class CounterAttackAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "반격";
        private const string DEFAULT_DESC = "공격을 받은 경우 대상에게 공격합니다.";

        [Header("Counter Attack Settings")]
        [Tooltip("반격 데미지 비율")]
        [Range(0f, 1f)]
        [SerializeField] private float counterDamageRatio = 1f;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[CounterAttack] {piece.name}에게 반격 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[CounterAttack] {piece.name}에서 반격 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnHit) return;
            if (context.Attacker == null) return;
            if (!context.Attacker.IsAlive) return;

            int counterDamage = Mathf.RoundToInt(context.Owner.Stats.Attack * counterDamageRatio);
            context.Attacker.TakeDamage(counterDamage, context.Owner);
            
            Debug.Log($"[CounterAttack] {context.Owner.name}이 {context.Attacker.name}에게 반격! 데미지: {counterDamage}");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
