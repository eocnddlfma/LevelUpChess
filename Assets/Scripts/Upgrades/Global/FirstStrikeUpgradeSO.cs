using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 선공권: 매 라운드 첫 턴 우선권 획득
    /// </summary>
    [CreateAssetMenu(fileName = "FirstStrikeUpgrade", menuName = "LevelUpChess/Upgrades/Global/FirstStrike")]
    public class FirstStrikeUpgradeSO : GlobalUpgradeSO
    {
        private Team _activeTeam;
        private bool _isActive = false;

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[FirstStrike] {team} 팀 선공권 획득");
            _activeTeam = team;
            _isActive = true;
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[FirstStrike] {team} 팀 선공권 제거");
            _isActive = false;
        }

        /// <summary>
        /// TurnManager에서 호출 - 선공 팀 결정
        /// </summary>
        public Team GetFirstTeam(Team defaultTeam)
        {
            if (_isActive)
            {
                return _activeTeam;
            }
            return defaultTeam;
        }
    }
}
