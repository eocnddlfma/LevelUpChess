using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 부활: 처음 죽은 기물이 부활
    /// </summary>
    [CreateAssetMenu(fileName = "ResurrectionUpgrade", menuName = "LevelUpChess/Upgrades/Global/Resurrection")]
    public class ResurrectionUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "리저렉션";
        private const string DEFAULT_DESC = "죽은 아군을 부활시키기";

        [Header("Settings")]
        [Tooltip("부활 가능 횟수")]
        [SerializeField] private int maxResurrections = 1;
        
        [Tooltip("부활시 체력 비율")]
        [Range(0f, 1f)]
        [SerializeField] private float reviveHealthRatio = 0.5f;

        private System.Collections.Generic.Dictionary<Team, int> _remainingResurrections = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[Resurrection] {team} 팀 부활 효과 활성화: {maxResurrections}회");
            _remainingResurrections[team] = maxResurrections;
            
            // 이벤트 구독
            // GameEvents.OnPieceDeath += HandlePieceDeath;
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[Resurrection] {team} 팀 부활 효과 비활성화");
            _remainingResurrections.Remove(team);
            
            // 이벤트 구독 해제
            // GameEvents.OnPieceDeath -= HandlePieceDeath;
        }

        /// <summary>
        /// 기물 사망 이벤트 핸들러
        /// </summary>
        public bool TryResurrect(ChessPiece piece)
        {
            if (piece == null) return false;
            
            if (!_remainingResurrections.ContainsKey(piece.Team)) return false;
            if (_remainingResurrections[piece.Team] <= 0) return false;

            // 부활 실행
            _remainingResurrections[piece.Team]--;
            
            int reviveHealth = Mathf.RoundToInt(piece.Stats.MaxHealth * reviveHealthRatio);
            piece.Resurrect(reviveHealth);
            
            Debug.Log($"[Resurrection] {piece.name} 부활! 체력: {reviveHealth}, 남은 횟수: {_remainingResurrections[piece.Team]}");
            
            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}

// ChessPiece 확장
namespace LevelUpChess.Pieces
{
    public partial class ChessPiece
    {
        public void Resurrect(int health)
        {
            // IsAlive 플래그 복구
            // Stats.SetHealth(health);
            Stats.Heal(health);
            Debug.Log($"[ChessPiece] {name} 부활! 체력: {health}");
        }
    }
}
