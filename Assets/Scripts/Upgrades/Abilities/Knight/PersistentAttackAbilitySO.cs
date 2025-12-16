using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Adds a small follow-up hit on every attack.
    /// </summary>
    [CreateAssetMenu(fileName = "PersistentAttackAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/PersistentAttack")]
    public class PersistentAttackAbilitySO : AbilityBaseSO
    {
        [SerializeField] private int bonusDamage = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[PersistentAttack] {piece.name} gained Persistent Attack");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PersistentAttack] {piece.name} lost Persistent Attack");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            if (context.Owner == null || context.Target == null) return;
            if (!context.Target.IsAlive) return;

            context.Target.TakeDamage(bonusDamage, context.Owner);
            Debug.Log($"[PersistentAttack] {context.Owner.name} dealt follow-up {bonusDamage} damage to {context.Target.name}");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
