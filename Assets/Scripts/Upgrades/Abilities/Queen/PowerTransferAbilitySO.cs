using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// When the queen dies, transfer some of her power to the nearest ally.
    /// </summary>
    [CreateAssetMenu(fileName = "PowerTransferAbility", menuName = "LevelUpChess/Upgrades/Abilities/Queen/PowerTransfer")]
    public class PowerTransferAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "힘의 전송";
        private const string DEFAULT_DESC = "퀸이 공격할 때 주변 적에게 1 데미지";

        [Header("Transfer Settings")]
        [SerializeField] private int healthBonus = 6;
        [SerializeField] private int attackBonus = 3;

        public override AbilityTrigger Trigger => AbilityTrigger.OnDeath;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[PowerTransfer] {piece.name} prepared to transfer power on death.");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PowerTransfer] {piece?.name} power transfer removed.");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnDeath) return;

            var queen = context.Owner;
            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var queenTile = queen.CurrentTile;
            if (queenTile == null) return;

            ChessPiece closest = null;
            int bestDist = int.MaxValue;

            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var p in allPieces)
            {
                if (p == null || p == queen) continue;
                if (p.Team != queen.Team) continue;
                if (!p.IsAlive) continue;
                var tile = p.CurrentTile;
                if (tile == null) continue;

                int dist = Mathf.Abs(tile.coordinate.x - queenTile.coordinate.x) + Mathf.Abs(tile.coordinate.y - queenTile.coordinate.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    closest = p;
                }
            }

            if (closest != null)
            {
                closest.Stats.AddModifier(StatType.MaxHealth, healthBonus);
                closest.Stats.AddModifier(StatType.Attack, attackBonus);
                closest.SetHealth(Mathf.Min(closest.Stats.MaxHealth, closest.Stats.CurrentHealth + healthBonus));
                Debug.Log($"[PowerTransfer] {queen.name} transferred power to {closest.name}: +{healthBonus} HP, +{attackBonus} ATK");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif
    }
}
