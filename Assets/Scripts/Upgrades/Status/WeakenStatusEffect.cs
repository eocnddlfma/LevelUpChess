using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Upgrades.Status;

namespace LevelUpChess.Upgrades.Status
{
    public class WeakenStatusEffect : StatusEffect
    {
        private int attackReduction;

        public WeakenStatusEffect(int reduction, int turns, string sourceName = null, ChessPiece source = null) : base(turns, source, sourceName)
        {
            attackReduction = reduction;
        }

        public override void OnApply()
        {
            if (Owner != null)
            {
                Owner.Stats.AddModifier(StatType.Attack, -attackReduction);
                Debug.Log($"[Weaken] {Owner.name} attack reduced by {attackReduction} for {RemainingTurns} turns");
            }
        }

        public override void OnRemove()
        {
            if (Owner != null)
            {
                // We applied a negative modifier for attack; reverse with the same negative value
                Owner.Stats.RemoveModifier(StatType.Attack, -attackReduction);
                Debug.Log($"[Weaken] {Owner.name} weaken removed");
            }
        }
    }
}
