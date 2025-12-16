using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 체력 회복: 턴 종료 시 체력 회복
    /// </summary>
    [CreateAssetMenu(fileName = "HealthRegenerationUpgrade", menuName = "LevelUpChess/Upgrades/Global/HealthRegeneration")]
    public class HealthRegenerationUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "체력 회복";
        private const string DEFAULT_DESC = "턴 종료 시 체력 회복";

        [Header("Settings")]
        [Tooltip("턴당 회복량")]
        [SerializeField] private int regenAmount = 2;

        [Tooltip("최대 체력 초과 회복 허용")]
        [SerializeField] private bool allowOverheal = false;

        private Team _activeTeam;
        private bool _isActive = false;

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[HealthRegeneration] {team} 팀 체력 회복 활성화: 턴당 +{regenAmount}");
            _activeTeam = team;
            _isActive = true;
            // Note: 실제 회복은 턴 시스템에서 처리해야 함
        }

        public override void RemoveGlobalEffect(Team team)
        {
            if (_isActive && team == _activeTeam)
            {
                Debug.Log($"[HealthRegeneration] {team} 팀 체력 회복 비활성화");
                _isActive = false;
            }
        }

        public int GetRegenAmount() => regenAmount;
        public bool GetAllowOverheal() => allowOverheal;

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