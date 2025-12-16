using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Heals nearby allies each turn.
    /// </summary>
    [CreateAssetMenu(fileName = "HealerBishopAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/Healer")]
    public class HealerBishopAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "힐러 비숍";
        private const string DEFAULT_DESC = "턴 종료마다 주변 아군을 회복시킵니다.";

        [SerializeField] private int healAmount = 1;

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnTurnEnd) return;
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
                    ally.Heal(healAmount);
                    Debug.Log($"[HealerBishop] {ally.name} healed for {healAmount}");
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnTurnEnd;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
