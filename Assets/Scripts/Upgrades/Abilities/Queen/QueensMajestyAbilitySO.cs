using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Queen's Majesty: debuffs nearby enemies' attack and defense.
    /// </summary>
    [CreateAssetMenu(fileName = "QueensMajestyAbility", menuName = "LevelUpChess/Upgrades/Abilities/Queen/QueensMajesty")]
    public class QueensMajestyAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "퀸의 위엄";
        private const string DEFAULT_DESC = "퀸이 공격할 때 공격력 +3";

        [Header("Majesty Settings")]
        [SerializeField] private int attackDebuff = 1;
        [SerializeField] private int defenseDebuff = 1;
        [SerializeField] private int radius = 2;

        private readonly List<ChessPiece> _affected = new List<ChessPiece>();

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[QueensMajesty] {piece.name} aura active");
            ApplyAura(piece);
        }

        public override void OnRemove(ChessPiece piece)
        {
            ClearAura();
            Debug.Log($"[QueensMajesty] {piece?.name} aura removed");
        }

        public override void Execute(AbilityContext context)
        {
            // Refresh aura each turn start to keep list in sync.
            if (context.Trigger == AbilityTrigger.OnTurnStart)
            {
                ApplyAura(context.Owner);
            }
        }

        private void ApplyAura(ChessPiece queen)
        {
            ClearAura();
            if (queen == null) return;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var origin = queen.CurrentTile?.coordinate ?? Vector2Int.zero;

            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var p in allPieces)
            {
                if (p == null || p.Team == queen.Team) continue;
                var tile = p.CurrentTile;
                if (tile == null) continue;

                var delta = tile.coordinate - origin;
                // Within radius on straight or diagonal lines.
                if (Mathf.Abs(delta.x) <= radius && Mathf.Abs(delta.y) <= radius &&
                    (delta.x == 0 || delta.y == 0 || Mathf.Abs(delta.x) == Mathf.Abs(delta.y)))
                {
                    p.Stats.AddModifier(StatType.Attack, -attackDebuff);
                    p.Stats.AddModifier(StatType.Defense, -defenseDebuff);
                    _affected.Add(p);
                    Debug.Log($"[QueensMajesty] Debuffed {p.name} ATK-{attackDebuff} DEF-{defenseDebuff}");
                }
            }
        }

        private void ClearAura()
        {
            foreach (var p in _affected)
            {
                if (p != null)
                {
                    p.Stats.AddModifier(StatType.Attack, attackDebuff);
                    p.Stats.AddModifier(StatType.Defense, defenseDebuff);
                }
            }
            _affected.Clear();
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
