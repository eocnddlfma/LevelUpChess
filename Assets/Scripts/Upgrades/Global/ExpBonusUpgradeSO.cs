using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 경험치 보너스: 적 처치시 추가 경험치 획득
    /// </summary>
    [CreateAssetMenu(fileName = "ExpBonusUpgrade", menuName = "LevelUpChess/Upgrades/Global/ExpBonus")]
    public class ExpBonusUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "경험치 배율 증가";
        private const string DEFAULT_DESC = "모든 아군 경험치 획득량 배율 증가";

        [Header("Settings")]
        [Tooltip("처치당 추가 경험치 (%)")]
        [Range(0, 100)]
        [SerializeField] private int bonusExpPercent = 20;

        private Team _activeTeam;
        private bool _isActive = false;

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[ExpBonus] {team} 팀 경험치 보너스 활성화: +{bonusExpPercent}%");
            _activeTeam = team;
            _isActive = true;
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[ExpBonus] {team} 팀 경험치 보너스 비활성화");
            _isActive = false;
        }

        /// <summary>
        /// 경험치 배율 반환
        /// </summary>
        public float GetExpMultiplier(Team team)
        {
            if (_isActive && team == _activeTeam)
            {
                return 1f + (bonusExpPercent / 100f);
            }
            return 1f;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
