using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 동족 포식: 같은 팀을 공격할 경우 공격당한 팀원은 즉시 사망합니다. 사망한 팀원으로부터 얻는 경험치는 3배가 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CannibalismUpgrade", menuName = "LevelUpChess/Upgrades/Global/Cannibalism")]
    public class CannibalismUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "동족 포식";
        private const string DEFAULT_DESC = "같은 팀을 공격할 경우 공격당한 팀원은 즉시 사망합니다. 사망한 팀원으로부터 얻는 경험치는 3배가 됩니다.";

        [SerializeField] private float expMultiplier = 3f;

        private HashSet<int> affectedTeams = new HashSet<int>();
        private List<ChessPiece> affectedPieces = new List<ChessPiece>();

        public override void ApplyGlobalEffect(Team team)
        {
            int teamId = (int)team;
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);
            
            // 모든 팀원에게 아군 공격 가능 플래그 설정
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece != null && (int)piece.Team == teamId)
                {
                    piece.CanAttackAllies = true;
                    piece.OnAttackHit += OnAllyAttack;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[Cannibalism] Team {team}에 동족 포식 적용");
        }

        // Optional helper: bulk apply to list
        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);

            foreach (var piece in pieces)
            {
                if (piece != null)
                {
                    piece.CanAttackAllies = true;
                    piece.OnAttackHit += OnAllyAttack;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[Cannibalism] Team {teamId}에 동족 포식 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    piece.CanAttackAllies = false;
                    piece.OnAttackHit -= OnAllyAttack;
                }
            }
            affectedPieces.RemoveAll(p => p.Team == team);
        }

        private void OnAllyAttack(ChessPiece attacker, ChessPiece target, int damage)
        {
            if (attacker == null || target == null)
                return;

            // 같은 팀이면 즉사
            if (attacker.Team == target.Team)
            {
                target.ForceKill();
                
                // 3배 경험치 부여
                int baseExp = target.PieceValue;
                int bonusExp = Mathf.RoundToInt(baseExp * (expMultiplier - 1));
                attacker.GainExperience(bonusExp);
                
                Debug.Log($"[Cannibalism] {attacker.name}이 {target.name}을 포식, +{bonusExp} 추가 경험치");
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
