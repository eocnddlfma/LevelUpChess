using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 안아프게 맞는법: 모든 우리팀인 피스들의 방어력이 3 증가합니다
    /// </summary>
    [CreateAssetMenu(fileName = "ToughSkinUpgrade", menuName = "LevelUpChess/Upgrades/Global/ToughSkin")]
    public class ToughSkinUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "안아프게 맞는법";
        private const string DEFAULT_DESC = "모든 우리팀인 피스들의 방어력이 3 증가합니다";
        [SerializeField] private int defenseBonus = 3;
        
        private HashSet<int> affectedTeams = new HashSet<int>();
        private HashSet<ChessPiece> boostedPieces = new HashSet<ChessPiece>();
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
                    ApplyDefenseBoost(piece);
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[ToughSkin] Team {team}에 안 아프게 맞는 법 적용 (방어력 +{defenseBonus})");
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
                    ApplyDefenseBoost(piece);
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[ToughSkin] Team {teamId}에 안 아프게 맞는 법 적용 (방어력 +{defenseBonus})");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    RemoveDefenseBoost(piece);
                }
            }
            affectedPieces.RemoveAll(p => (int)p.Team == teamId);
        }

        private void ApplyDefenseBoost(ChessPiece piece)
        {
            if (boostedPieces.Contains(piece))
                return;

            boostedPieces.Add(piece);
            piece.Stats.AddModifier(StatType.Defense, defenseBonus);
        }

        private void RemoveDefenseBoost(ChessPiece piece)
        {
            if (!boostedPieces.Contains(piece))
                return;

            boostedPieces.Remove(piece);
            piece.Stats.RemoveModifier(StatType.Defense, defenseBonus);
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
