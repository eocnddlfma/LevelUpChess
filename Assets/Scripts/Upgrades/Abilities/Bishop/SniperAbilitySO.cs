using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Sniper: increases attack power for long-range shots.
    /// </summary>
    [CreateAssetMenu(fileName = "SniperAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/Sniper")]
    public class SniperAbilitySO : AbilityBaseSO
    {
        [SerializeField] private int attackBonus = 2;

        public override void OnApply(ChessPiece piece)
        {
            piece.Stats.AddModifier(StatType.Attack, attackBonus);
            Debug.Log($"[Sniper] {piece.name} attack +{attackBonus}");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.Stats.RemoveModifier(StatType.Attack, attackBonus);
            Debug.Log($"[Sniper] {piece.name} attack bonus removed");
        }

        public override void Execute(AbilityContext context) { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            trigger = AbilityTrigger.Passive;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
