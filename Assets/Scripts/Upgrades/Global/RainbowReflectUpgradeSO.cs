using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 무지개 반사: 받은 데미지의 50%를 공격자에게 반사
    /// </summary>
    [CreateAssetMenu(fileName = "RainbowReflectUpgrade", menuName = "LevelUpChess/Upgrades/Global/RainbowReflect")]
    public class RainbowReflectUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "무지개반사";
        private const string DEFAULT_DESC = "받은 데미지의 절반을 반사합니다.";

        [SerializeField] private float reflectPercent = 0.5f;

        private HashSet<int> affectedTeams = new HashSet<int>();
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
                    piece.OnDamageTaken += OnDamageTaken;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[RainbowReflect] Team {team}에 무지개 반사 적용");
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
                    piece.OnDamageTaken += OnDamageTaken;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[RainbowReflect] Team {teamId}에 무지개 반사 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    piece.OnDamageTaken -= OnDamageTaken;
                }
            }
            affectedPieces.RemoveAll(p => (int)p.Team == teamId);
        }

        private void OnDamageTaken(ChessPiece victim, ChessPiece attacker, int damage)
        {
            if (victim == null || attacker == null || damage <= 0)
                return;

            // 같은 팀은 반사하지 않음
            if (victim.Team == attacker.Team)
                return;

            int reflectDamage = Mathf.RoundToInt(damage * reflectPercent);
            
            if (reflectDamage > 0)
            {
                // 반사 데미지 적용 (반사 데미지에 대한 반사 방지를 위해 null 소스)
                attacker.TakeDamage(reflectDamage, null);
                
                Debug.Log($"[RainbowReflect] {victim.name}이 {attacker.name}에게 {reflectDamage} 데미지 반사");
            }
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
