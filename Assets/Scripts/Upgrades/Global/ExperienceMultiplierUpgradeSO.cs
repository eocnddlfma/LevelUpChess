using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 경험치 배율 증가: 적 처치시 추가 경험치 획득
    /// </summary>
    [CreateAssetMenu(fileName = "ExperienceMultiplierUpgrade", menuName = "LevelUpChess/Upgrades/Global/ExperienceMultiplier")]
    public class ExperienceMultiplierUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "경험치 배율 증가";
        private const string DEFAULT_DESC = "모든 아군 경험치 획득량 배율 증가";

        [Header("Settings")]
        [Tooltip("경험치 배율 증가 (%)")]
        [Range(0, 100)]
        [SerializeField] private int multiplierPercent = 20;

        private Team _activeTeam;
        private bool _isActive = false;

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[ExperienceMultiplier] {team} 팀 경험치 배율 +{multiplierPercent}%");
            _activeTeam = team;
            _isActive = true;
            // Note: 실제 경험치 배율 적용은 PlayerLevel.cs 등에서 처리해야 함
        }

        public override void RemoveGlobalEffect(Team team)
        {
            if (_isActive && team == _activeTeam)
            {
                Debug.Log($"[ExperienceMultiplier] {team} 팀 경험치 배율 제거");
                _isActive = false;
            }
        }

        public int GetMultiplierPercent() => multiplierPercent;

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