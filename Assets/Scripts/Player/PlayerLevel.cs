using UnityEngine;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Pieces;
using LevelUpChess.Upgrades;
using LevelUpChess.Managers;

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
        /// 기물 사망 시 호출 - 상대 기물이 죽으면 경험치 획득
        /// </summary>
        private void OnPieceDeath(PieceDeathEvent eventData)
        {
            // 상대 팀의 기물이 죽었을 때만 경험치 획득
            if (eventData.DeadPieceTeam != _team)
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
            
            // 먼저 팀 강화
            BoostTeamPieces();
            
            // 그 후 업그레이드 선택을 EventQueue에 enqueue
            var upgradeManager = LevelUpChess.Upgrades.UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                // 선택 완료 콜백 등록
                upgradeManager.OnPlayerUpgradeSelectionCompleted += OnPlayerUpgradeSelectionCompleted;

                // 플레이어 레벨업 이벤트를 큐에 쌓아서 순차 처리
                EventQueue.Instance.Enqueue(new PlayerLevelUpEvent
                {
                    Team = _team,
                    NewLevel = _level
                });
            }
            else
            {
                Debug.LogWarning("[PlayerLevel] UpgradeManager not found!");
                // UpgradeManager가 없으면 기존처럼 바로 브로드캐스트
                Bus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent
                {
                    Team = _team,
                    NewLevel = _level
                });
            }
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
        /// <summary>
        /// 팀의 살아있는 기물들 스탯 증가
        /// </summary>
        private void BoostTeamPieces()
        {
            var gameManager = ServiceLocator.Get<NetworkGameManager>();
            if (gameManager == null) return;
            
            var teamPieces = gameManager.GetPiecesOfTeam(_team);
            int boostedCount = 0;
            
            foreach (var piece in teamPieces)
            {
                if (piece.IsAlive)
                {
                    // 공격력 +1, 체력 +1
                    piece.Stats.AddModifier(StatType.Attack, 1);
                    piece.Stats.AddModifier(StatType.Health, 1);
                    boostedCount++;
                    
                    Debug.Log($"[PlayerLevel] {piece.name} boosted: +1 Attack, +1 Health");
                }
            }
            
            if (boostedCount > 0)
            {
                Debug.Log($"[PlayerLevel] {_team} player level up boosted {boostedCount} pieces");
            }
        }

        private void OnPlayerUpgradeSelectionCompleted()
        {
            Debug.Log("[PlayerLevel] Player upgrade selection completed");
            // 콜백 해제
            var upgradeManager = LevelUpChess.Upgrades.UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OnPlayerUpgradeSelectionCompleted -= OnPlayerUpgradeSelectionCompleted;
            }
            
            // 이벤트 발생
            Bus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent
            {
                Team = _team,
                NewLevel = _level
            });
        }    }
}
