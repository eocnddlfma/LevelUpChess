using UnityEngine;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Pieces;

namespace LevelUpChess.Player
{
    /// <summary>
    /// 플레이어의 레벨과 경험치를 관리하는 컴포넌트
    /// </summary>
    public class PlayerLevel : MonoBehaviour
    {
        private Team _team;
        private int _level = 1;
        private int _currentExp = 0;
        
        // ========== Public 프로퍼티 ==========
        public Team Team => _team;
        public int Level => _level;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => 3 + (_level - 1) * 2; // Lv1→2: 3, Lv2→3: 5, Lv3→4: 7...
        
        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(Team team)
        {
            _team = team;
            _level = 1;
            _currentExp = 0;
        }
        
        private void OnEnable()
        {
            Bus<PieceDeathEvent>.OnEvent += OnPieceDeath;
        }
        
        private void OnDisable()
        {
            Bus<PieceDeathEvent>.OnEvent -= OnPieceDeath;
        }
        
        /// <summary>
        /// 기물 사망 시 호출 - 내 기물이 죽으면 경험치 획득
        /// </summary>
        private void OnPieceDeath(PieceDeathEvent eventData)
        {
            // 내 팀의 기물이 죽었을 때만 경험치 획득
            if (eventData.DeadPieceTeam == _team)
            {
                int expGain = eventData.PieceValue;
                GainExperience(expGain);
                Debug.Log($"[PlayerLevel] {_team} player gained {expGain} exp from losing {eventData.DeadPieceType}");
            }
        }
        
        /// <summary>
        /// 경험치 획득
        /// </summary>
        public void GainExperience(int amount)
        {
            int previousLevel = _level;
            
            _currentExp += amount;
            Debug.Log($"[PlayerLevel] {_team} player gained {amount} exp. Total: {_currentExp}/{ExpToNextLevel} (Level {_level})");
            
            // 레벨업 체크
            while (_currentExp >= ExpToNextLevel)
            {
                LevelUp();
            }
            
            // 경험치 변경 이벤트 발생
            Bus<PlayerExpChangedEvent>.Raise(new PlayerExpChangedEvent
            {
                Team = _team,
                Level = _level,
                CurrentExp = _currentExp,
                ExpToNextLevel = ExpToNextLevel
            });
            
            if (_level > previousLevel)
            {
                Debug.Log($"[PlayerLevel] {_team} player leveled up from {previousLevel} to {_level}!");
            }
        }
        
        /// <summary>
        /// 레벨업 처리
        /// </summary>
        private void LevelUp()
        {
            int expNeeded = ExpToNextLevel;
            _currentExp -= expNeeded;
            _level++;
            
            Debug.Log($"[PlayerLevel] {_team} player LevelUp! Used {expNeeded} exp, remaining: {_currentExp}, next level needs: {ExpToNextLevel}");
            
            // 레벨업 이벤트 발생
            Bus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent
            {
                Team = _team,
                NewLevel = _level
            });
        }
        
        /// <summary>
        /// 레벨과 경험치 리셋 (리매치용)
        /// </summary>
        public void ResetLevel()
        {
            _level = 1;
            _currentExp = 0;
            
            // 경험치 변경 이벤트 발생
            Bus<PlayerExpChangedEvent>.Raise(new PlayerExpChangedEvent
            {
                Team = _team,
                Level = _level,
                CurrentExp = _currentExp,
                ExpToNextLevel = ExpToNextLevel
            });
        }
    }
}
