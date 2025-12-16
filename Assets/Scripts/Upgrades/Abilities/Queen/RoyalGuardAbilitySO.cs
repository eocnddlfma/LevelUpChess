using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Grants nearby allies a defense buff each turn.
    /// </summary>
    [CreateAssetMenu(fileName = "RoyalGuardAbility", menuName = "LevelUpChess/Upgrades/Abilities/Queen/RoyalGuard")]
    public class RoyalGuardAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "왕실 근위대";
        private const string DEFAULT_DESC = "퀸이 공격받을 때 주변 아군에게 방어력 +2";

        [SerializeField] private int defenseBonus = 1;

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnTurnStart) return;
            if (context.Owner == null) return;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var tile = context.Owner.CurrentTile;
            if (tile == null) return;

            Vector2Int[] offsets = {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            foreach (var offset in offsets)
            {
                var t = boardManager.GetTileAt(tile.coordinate + offset);
                var ally = t?.OccupyingPiece;
                if (ally != null && ally.Team == context.Owner.Team)
                {
                    ally.Stats.AddModifier(StatType.Defense, defenseBonus);
                    Debug.Log($"[RoyalGuard] {ally.name} defense +{defenseBonus}");
                }
            }
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
