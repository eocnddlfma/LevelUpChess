using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Pieces;
using LevelUpChess.Managers;
using LevelUpChess.Upgrades;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 플레이어의 레벨과 경험치 바를 표시하는 UI
    /// 내 팀 기물이 죽으면 경험치를 획득하고 레벨업
    /// </summary>
    public class PlayerExpBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Team targetTeam;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI expText;
        [SerializeField] private Image expFillImage;
        
        [Header("Settings")]
        [SerializeField] private Color fillColor = Color.yellow;
        
        public Team TargetTeam => targetTeam;
        
        // 플레이어 레벨/경험치 상태
        private int _level = 1;
        private int _currentExp = 0;
        private int ExpToNextLevel => 3 + (_level - 1) * 2; // Lv1→2: 3, Lv2→3: 5, Lv3→4: 7...
        
        private void Awake()
        {
            if (expFillImage != null)
                expFillImage.color = fillColor;
            
            UpdateUI(_level, _currentExp, ExpToNextLevel);
        }
        
        private void OnEnable()
        {
            Bus<PieceDeathEvent>.OnEvent += OnPieceDeath;
            Bus<GameOverEvent>.OnEvent += OnGameOver;
        }
        
        private void OnDisable()
        {
            Bus<PieceDeathEvent>.OnEvent -= OnPieceDeath;
            Bus<GameOverEvent>.OnEvent -= OnGameOver;
        }
        
        /// <summary>
        /// 기물 사망 시 호출 - 내 기물이 죽으면 경험치 획득
        /// </summary>
        private void OnPieceDeath(PieceDeathEvent eventData)
        {
            // 내 팀의 기물이 죽었을 때만 경험치 획득
            if (eventData.DeadPieceTeam == targetTeam)
            {
                int expGain = eventData.PieceValue;
                GainExperience(expGain);
                Debug.Log($"[PlayerExpBar] {targetTeam} player gained {expGain} exp from losing {eventData.DeadPieceType}");
            }
        }
        
        /// <summary>
        /// 게임 오버 시 리셋 (리매치용)
        /// </summary>
        private void OnGameOver(GameOverEvent eventData)
        {
            if (eventData.IsRematch)
            {
                ResetLevel();
            }
        }
        
        /// <summary>
        /// 경험치 획득
        /// </summary>
        private void GainExperience(int amount)
        {
            int previousLevel = _level;
            
            _currentExp += amount;
            Debug.Log($"[PlayerExpBar] {targetTeam} player gained {amount} exp. Total: {_currentExp}/{ExpToNextLevel} (Level {_level})");
            
            // 레벨업 체크
            while (_currentExp >= ExpToNextLevel)
            {
                LevelUp();
            }
            
            UpdateUI(_level, _currentExp, ExpToNextLevel);
            
            if (_level > previousLevel)
            {
                Debug.Log($"[PlayerExpBar] {targetTeam} player leveled up from {previousLevel} to {_level}!");
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
            
            Debug.Log($"[PlayerExpBar] {targetTeam} player LevelUp! Used {expNeeded} exp, remaining: {_currentExp}, next level needs: {ExpToNextLevel}");
            
            // 먼저 팀 강화
            BoostTeamPieces();
            
            // 그 후 업그레이드 선택을 EventQueue에 enqueue
            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                // 선택 완료 콜백 등록
                upgradeManager.OnPlayerUpgradeSelectionCompleted += OnPlayerUpgradeSelectionCompleted;

                // 플레이어 레벨업 이벤트를 큐에 쌓아서 순차 처리
                Upgrades.UI.UpgradeSelectionPanelUI.Enqueue(new PlayerLevelUpEvent
                {
                    Team = targetTeam,
                    NewLevel = _level
                });
            }
            else
            {
                Debug.LogWarning("[PlayerExpBar] UpgradeManager not found!");
                // UpgradeManager가 없으면 기존처럼 바로 브로드캐스트
                Bus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent
                {
                    Team = targetTeam,
                    NewLevel = _level
                });
            }
        }
        
        /// <summary>
        /// 팀의 살아있는 기물들 스탯 증가
        /// </summary>
        public void BoostTeamPieces()
        {
            var gameManager = ServiceLocator.Get<NetworkGameManager>();
            if (gameManager == null) return;
            
            var teamPieces = gameManager.GetPiecesOfTeam(targetTeam);
            int boostedCount = 0;
            
            foreach (var piece in teamPieces)
            {
                if (piece.IsAlive)
                {
                    // 공격력 +1, 체력 +1
                    piece.Stats.AddModifier(StatType.Attack, 1);
                    piece.Stats.AddModifier(StatType.Health, 1);
                    boostedCount++;
                    
                    Debug.Log($"[PlayerExpBar] {piece.name} boosted: +1 Attack, +1 Health");
                }
            }
            
            if (boostedCount > 0)
            {
                Debug.Log($"[PlayerExpBar] {targetTeam} player level up boosted {boostedCount} pieces");
            }
        }
        
        /// <summary>
        /// 레벨과 경험치 리셋
        /// </summary>
        private void ResetLevel()
        {
            _level = 1;
            _currentExp = 0;
            UpdateUI(_level, _currentExp, ExpToNextLevel);
            Debug.Log($"[PlayerExpBar] {targetTeam} player level reset");
        }
        
        private void UpdateUI(int level, int currentExp, int expToNextLevel)
        {
            // 레벨 텍스트
            if (levelText != null)
                levelText.text = $"Lv.{level}";
            
            // 경험치 텍스트
            if (expText != null)
                expText.text = $"{currentExp}/{expToNextLevel}";
            
            // 경험치 바
            if (expFillImage != null)
            {
                float fillAmount = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 0f;
                expFillImage.fillAmount = fillAmount;
            }
        }
        
        /// <summary>
        /// 초기 상태 설정
        /// </summary>
        public void Initialize(Team team, int level = 1, int currentExp = 0, int expToNextLevel = 10)
        {
            targetTeam = team;
            UpdateUI(level, currentExp, expToNextLevel);
        }

        private void OnPlayerUpgradeSelectionCompleted()
        {
            Debug.Log("[PlayerExpBar] Player upgrade selection completed");
            // 콜백 해제
            var upgradeManager = LevelUpChess.Upgrades.UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OnPlayerUpgradeSelectionCompleted -= OnPlayerUpgradeSelectionCompleted;
            }
            
            // 이벤트 발생
            Bus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent
            {
                Team = targetTeam,
                NewLevel = _level
            });
        }
    }
}
