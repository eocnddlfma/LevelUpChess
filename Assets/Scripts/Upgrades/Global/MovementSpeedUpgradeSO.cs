using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 이동 속도 증가: 모든 아군 기물 이동 속도 증가
    /// </summary>
    [CreateAssetMenu(fileName = "MovementSpeedUpgrade", menuName = "LevelUpChess/Upgrades/Global/MovementSpeed")]
    public class MovementSpeedUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "이동 속도 증가";
        private const string DEFAULT_DESC = "모든 아군 이동 속도 증가";

        [Header("Settings")]
        [Tooltip("이동 속도 증가량 (%)")]
        [Range(0, 100)]
        [SerializeField] private int speedBonusPercent = 20;

        private Team _activeTeam;
        private bool _isActive = false;

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[MovementSpeed] {team} 팀 이동 속도 +{speedBonusPercent}%");
            _activeTeam = team;
            _isActive = true;
            // Note: 실제 이동 속도 적용은 ChessPiece의 이동 로직에서 처리해야 함
        }

        public override void RemoveGlobalEffect(Team team)
        {
            if (_isActive && team == _activeTeam)
            {
                Debug.Log($"[MovementSpeed] {team} 팀 이동 속도 증가 제거");
                _isActive = false;
            }
        }

        public int GetSpeedBonusPercent() => speedBonusPercent;

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