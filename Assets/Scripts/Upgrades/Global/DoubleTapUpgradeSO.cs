using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 때린곳 더 때리기: 공격시 공격력 1.2배 상승합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DoubleTapUpgrade", menuName = "LevelUpChess/Upgrades/Global/DoubleTap")]
    public class DoubleTapUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "때린곳 더 때리기";
        private const string DEFAULT_DESC = "공격시 공격력 1.2배 상승합니다.";
        [SerializeField] private float attackMultiplier = 1.2f;
        
        private HashSet<int> affectedTeams = new HashSet<int>();
        private Dictionary<ChessPiece, int> originalAttacks = new Dictionary<ChessPiece, int>();
        private List<ChessPiece> affectedPieces = new List<ChessPiece>();

        public override void ApplyGlobalEffect(Team team)
        {
            int teamId = (int)team;
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece != null && (int)piece.Team == teamId)
                {
                    ApplyAttackBoost(piece);
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[DoubleTap] Team {team}에 때린곳 더 때리기 적용 (공격력 x{attackMultiplier})");
        }

        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);

            foreach (var piece in pieces)
            {
                if (piece != null)
                {
                    ApplyAttackBoost(piece);
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[DoubleTap] Team {teamId}에 때린곳 더 때리기 적용 (공격력 x{attackMultiplier})");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    RemoveAttackBoost(piece);
                }
            }
            affectedPieces.RemoveAll(p => (int)p.Team == teamId);
        }

        private void ApplyAttackBoost(ChessPiece piece)
        {
            if (originalAttacks.ContainsKey(piece))
                return;

            originalAttacks[piece] = piece.BaseAttack;
            
            int bonusAttack = Mathf.RoundToInt(piece.BaseAttack * (attackMultiplier - 1f));
            piece.Stats.AddModifier(StatType.Attack, bonusAttack);
        }

        private void RemoveAttackBoost(ChessPiece piece)
        {
            if (!originalAttacks.TryGetValue(piece, out int original))
                return;

            int bonusAttack = Mathf.RoundToInt(original * (attackMultiplier - 1f));
            piece.Stats.RemoveModifier(StatType.Attack, bonusAttack);
            
            originalAttacks.Remove(piece);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
        }
#endif
    }
}
