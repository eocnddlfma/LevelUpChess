using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// F=ma: 공격시 거리만큼 데미지가 증가함.
    /// </summary>
    [CreateAssetMenu(fileName = "ForceMassAccelAbility", menuName = "LevelUpChess/Upgrades/Abilities/Common/ForceMassAccel")]
    public class ForceMassAccelAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "F=ma";
        private const string DEFAULT_DESC = "공격시 거리만큼 데미지가 증가함.";

        [Header("F=ma Settings")]
        [SerializeField] private int damagePerDistance = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[F=ma] {piece.name}에게 F=ma 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[F=ma] {piece.name}에서 F=ma 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.FromTile == null || context.ToTile == null) return;
            
            if (context.Trigger == AbilityTrigger.OnAttackStart)
            {
                // 이동 거리 계산 (맨해튼 거리)
                Vector2Int from = context.FromTile.coordinate;
                Vector2Int to = context.ToTile.coordinate;
                int distance = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
                
                int bonusDamage = distance * damagePerDistance;
                context.BonusDamage += bonusDamage;
                
                Debug.Log($"[F=ma] 거리 {distance}칸 → 추가 데미지 +{bonusDamage}");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackStart;
        }
#endif
    }
}
