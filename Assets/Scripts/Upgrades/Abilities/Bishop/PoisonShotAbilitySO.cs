using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Upgrades.Status;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 독 쏘는 맛: 공격 받은 적은 매턴 체력이 최대 체력의 10%가 감소하는 독 상태이상을 받습니다. 
    /// 유지 턴 5턴.
    /// </summary>
    [CreateAssetMenu(fileName = "PoisonShotAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/PoisonShot")]
    public class PoisonShotAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "독 쏘는 맛";
        private const string DEFAULT_DESC = "공격 받은 적은 매턴 체력이 최대 체력의 10%가 감소하는 독 상태이상을 받습니다. 유지 턴 5턴.";

        [Header("Poison Shot Settings")]
        [Tooltip("최대 체력 대비 독 데미지 비율")]
        [Range(0f, 1f)]
        [SerializeField] private float poisonRatio = 0.1f;
        
        [Tooltip("독 지속 턴수")]
        [SerializeField] private int poisonDuration = 5;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[PoisonShot] {piece.name}에게 독 쏘는 맛 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PoisonShot] {piece.name}에서 독 쏘는 맛 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            int poisonDamage = Mathf.RoundToInt(context.Target.Stats.MaxHealth * poisonRatio);
            
            context.Target.ApplyStatusEffect(new PoisonStatusEffect(
                poisonDamage, 
                poisonDuration, 
                $"독 쏘는 맛 ({context.Owner.name})"
            ));
            
            Debug.Log($"[PoisonShot] {context.Target.name}에게 독 부여! 턴당 {poisonDamage} 데미지, {poisonDuration}턴");
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
