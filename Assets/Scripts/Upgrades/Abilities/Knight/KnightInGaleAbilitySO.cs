using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 나이트 인 게일: 나이트가 아군을 공격할 경우 공격력만큼 체력을 회복합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "KnightInGaleAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/KnightInGale")]
    public class KnightInGaleAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "나이트 인 게일";
        private const string DEFAULT_DESC = "나이트가 아군을 공격할 경우 공격력만큼 체력을 회복합니다.";

        public override void OnApply(ChessPiece piece)
        {
            piece.CanAttackAllies = true;
            Debug.Log($"[KnightInGale] {piece.name}에게 나이트 인 게일 적용 - 아군 힐 가능!");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.CanAttackAllies = false;
            Debug.Log($"[KnightInGale] {piece.name}에서 나이트 인 게일 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            // 아군을 공격한 경우만 처리
            if (context.Target.Team != context.Owner.Team) return;

            // 피해 무효화
            context.ShouldEvade = true;

            // 아군 힐
            int healAmount = context.Owner.Stats.Attack;
            context.Target.Stats.Heal(healAmount);
            
            Debug.Log($"[KnightInGale] {context.Target.name}을(를) {healAmount} 힐!");
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
