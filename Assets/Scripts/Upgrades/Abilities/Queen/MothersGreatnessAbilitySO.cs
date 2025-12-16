using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Core;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 어머니의 위대함: 3회까지 죽을 상황에서 죽지 않음 (체력 1로 생존)
    /// </summary>
    [CreateAssetMenu(fileName = "MothersGreatnessAbility", menuName = "LevelUpChess/Abilities/Queen/MothersGreatness")]
    public class MothersGreatnessAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "어머니의 위대함";
        private const string DEFAULT_DESC = "0이 되는 피해를 3회까지 막아냅니다.";

        public override AbilityTrigger Trigger => AbilityTrigger.OnHit;

        [SerializeField] private int maxSurvivalCount = 3;

        private Dictionary<ChessPiece, int> survivalCounts = new Dictionary<ChessPiece, int>();

        public override void OnApply(ChessPiece piece)
        {
            if (piece != null && !survivalCounts.ContainsKey(piece))
            {
                survivalCounts[piece] = maxSurvivalCount;
            }
        }

        public override void OnRemove(ChessPiece piece)
        {
            if (piece != null)
            {
                survivalCounts.Remove(piece);
            }
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnHit)
                return;

            if (context.Owner == null)
                return;

            // 생존 횟수 확인
            if (!survivalCounts.TryGetValue(context.Owner, out int remaining) || remaining <= 0)
                return;

            // 데미지 후 체력이 0 이하가 되는지 확인
            int expectedHealth = context.Owner.CurrentHealth - context.Damage;
            
            if (expectedHealth <= 0)
            {
                // 데미지 무효화하고 체력 1로 설정
                context.ShouldEvade = true;
                context.Owner.SetHealth(1);
                
                survivalCounts[context.Owner] = remaining - 1;
                
                Debug.Log($"[MothersGreatness] {context.Owner.name}이 치명상 방지! 남은 횟수: {remaining - 1}");
            }
        }

        public int GetRemainingSurvivalCount(ChessPiece piece)
        {
            return survivalCounts.TryGetValue(piece, out int count) ? count : 0;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnHit;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif
    }
}
