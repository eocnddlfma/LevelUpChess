using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Promote a pawn directly to a queen when this upgrade is acquired.
    /// </summary>
    [CreateAssetMenu(fileName = "DragonFromStreamAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/DragonFromStream")]
    public class DragonFromStreamAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "개천에서 용난다";
        private const string DEFAULT_DESC = "퀸으로 승급";

        public override void OnApply(ChessPiece piece)
        {
            // Promote immediately on acquisition.
            PromoteToQueen(piece);
        }

        public override void OnRemove(ChessPiece piece)
        {
        }

        public override void Execute(AbilityContext context) { }

        private void PromoteToQueen(ChessPiece pawn)
        {
            if (pawn == null || pawn.CurrentTile == null) return;

            Debug.Log($"[DragonFromStream] {pawn.name} promoted to Queen!");

            var currentTile = pawn.CurrentTile;
            var parent = pawn.transform.parent;
            int currentHealth = pawn.Stats.CurrentHealth;
            int currentMaxHealth = pawn.Stats.MaxHealth;
            int currentAttack = pawn.Stats.Attack;
            bool hasMoved = pawn.HasMoved;

            // Free the tile for the new piece.
            currentTile.OccupyingPiece = null;

            var queen = PieceFactory.Create(PieceType.Queen, pawn.Team, currentTile, parent);
            if (queen != null)
            {
                queen.SetMaxHealth(Mathf.Max(queen.MaxHealth, currentMaxHealth));
                queen.SetHealth(Mathf.Min(queen.MaxHealth, currentHealth));
                queen.SetAttackPower(Mathf.Max(queen.AttackPower, currentAttack));
                queen.HasMoved = hasMoved;
                queen.Level = pawn.Level;
            }

            Object.Destroy(pawn.gameObject);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive;
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif
    }
}
