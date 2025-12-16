using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Upgrades.Status;

namespace LevelUpChess.Upgrades.Status
{
    public class PoisonStatusEffect : StatusEffect
    {
        private int damagePerTurn;

        public PoisonStatusEffect(int damage, int turns, string sourceName = null, ChessPiece source = null) : base(turns, source, sourceName)
        {
            damagePerTurn = damage;
        }

        public override void OnApply()
        {
            // optional: show UI
            Debug.Log($"[Poison] {Owner?.name} poisoned for {RemainingTurns} turns ({damagePerTurn} dmg/turn)");
        }

        public override void OnTick()
        {
            base.OnTick();
            if (Owner != null && damagePerTurn > 0 && Owner.IsAlive)
            {
                Owner.TakeDamage(damagePerTurn, Source);
                Debug.Log($"[Poison] {Owner.name} takes {damagePerTurn} poison damage. Remaining: {RemainingTurns}");
            }
        }

        public override void OnRemove()
        {
            Debug.Log($"[Poison] {Owner?.name} poison expired");
        }
    }
}
