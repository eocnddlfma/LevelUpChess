using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Upgrades.Status;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 굿 나잇: 이 나이트에게 공격받은 적은 공격력이 절반으로 감소하는 약화 상태이상을 받습니다. 
    /// 유지 턴 5턴.
    /// </summary>
    [CreateAssetMenu(fileName = "GoodNightAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/GoodNight")]
    public class GoodNightAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "굿 나잇";
        private const string DEFAULT_DESC = "이 나이트에게 공격받은 적은 공격력이 절반으로 감소하는 약화 상태이상을 받습니다. 유지 턴 5턴.";

        [Header("Good Night Settings")]
        [Tooltip("공격력 감소 비율")]
        [Range(0f, 1f)]
        [SerializeField] private float attackReduction = 0.5f;
        
        [Tooltip("약화 지속 턴수")]
        [SerializeField] private int debuffDuration = 5;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[GoodNight] {piece.name}에게 굿 나잇 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[GoodNight] {piece.name}에서 굿 나잇 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            
            // 적만 약화
            if (context.Target.Team == context.Owner.Team) return;

            int reduction = Mathf.RoundToInt(context.Target.Stats.Attack * attackReduction);
            
            context.Target.ApplyStatusEffect(new WeakenStatusEffect(
                reduction, 
                debuffDuration, 
                $"굿 나잇 ({context.Owner.name})"
            ));
            
            Debug.Log($"[GoodNight] {context.Target.name}에게 약화 부여! 공격력 -{reduction}, {debuffDuration}턴");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
