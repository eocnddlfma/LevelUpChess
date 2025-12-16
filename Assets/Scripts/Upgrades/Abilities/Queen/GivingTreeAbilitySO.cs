using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Core;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 아낌없이 주는 나무: 회복력이 2 증가합니다. 매턴 같은 팀의 체력이 가장 낮은 기물의 체력을 최대로 채우고, 본인의 체력을 깎습니다. 체력이 부족하다면 퀸의 체력을 1만큼 남기고 나머지만큼 회복시킵니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GivingTreeAbility", menuName = "LevelUpChess/Abilities/Queen/GivingTree")]
    public class GivingTreeAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "아낌없이 주는 나무";
        private const string DEFAULT_DESC = "회복력이 2 증가합니다. 매턴 같은 팀의 체력이 가장 낮은 기물의 체력을 최대로 채우고, 본인의 체력을 깎습니다. 체력이 부족하다면 퀸의 체력을 1만큼 남기고 나머지만큼 회복시킵니다.";

        public override AbilityTrigger Trigger => AbilityTrigger.OnTurnStart;

        public override void OnApply(ChessPiece piece)
        {
            piece.Stats.AddModifier(StatType.HealthRegeneration, 2);
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.Stats.RemoveModifier(StatType.HealthRegeneration, 2);
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnTurnStart)
                return;

            if (context.Owner == null)
                return;

            // 모든 아군 중 가장 낮은 체력 비율을 가진 유닛 찾기
            var allies = GetAllAllies(context.Owner);
            ChessPiece lowestHealthAlly = null;
            float lowestHealthRatio = float.MaxValue;

            foreach (var ally in allies)
            {
                if (ally == null || !ally.IsAlive)
                    continue;

                float healthRatio = (float)ally.CurrentHealth / ally.MaxHealth;
                
                // 이미 풀 체력인 유닛은 제외
                if (healthRatio >= 1f)
                    continue;

                if (healthRatio < lowestHealthRatio)
                {
                    lowestHealthRatio = healthRatio;
                    lowestHealthAlly = ally;
                }
            }

            // 가장 낮은 체력의 아군 풀힐
            if (lowestHealthAlly != null)
            {
                int healAmount = lowestHealthAlly.MaxHealth - lowestHealthAlly.CurrentHealth;
                
                if (healAmount > 0)
                {
                    // 퀸의 체력이 충분한지 확인
                    if (context.Owner.CurrentHealth > healAmount + 1)
                    {
                        // 충분하면 풀힐하고 퀸 체력 깎음
                        lowestHealthAlly.Heal(healAmount);
                        context.Owner.TakeDamage(healAmount, null);
                        Debug.Log($"[GivingTree] {context.Owner.name}이 {lowestHealthAlly.name}을 풀힐 (+{healAmount} HP), 퀸 체력 -{healAmount}");
                    }
                    else
                    {
                        // 부족하면 1 남기고 나머지만큼 힐
                        int availableHeal = context.Owner.CurrentHealth - 1;
                        lowestHealthAlly.Heal(availableHeal);
                        context.Owner.SetHealth(1);
                        Debug.Log($"[GivingTree] {context.Owner.name}이 {lowestHealthAlly.name}을 부분 힐 (+{availableHeal} HP), 퀸 체력 → 1");
                    }
                }
            }
        }

        private List<ChessPiece> GetAllAllies(ChessPiece owner)
        {
            var allies = new List<ChessPiece>();
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);

            foreach (var piece in allPieces)
            {
                if (piece != null && piece.IsAlive && piece.Team == owner.Team)
                {
                    allies.Add(piece);
                }
            }

            return allies;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnTurnStart;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif
    }
}
