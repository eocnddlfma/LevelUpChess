using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Managers;
using LevelUpChess.Pieces;
using Unity.Netcode;

namespace LevelUpChess.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button exitButton;

        private Tween _delayTween;

        private void Start()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            Bus<GameOverEvent>.OnEvent += OnGameOver;

            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDisable()
        {
            // 지연 트윈 정리
            if (_delayTween != null && _delayTween.IsActive())
            {
                _delayTween.Kill();
            }

            Bus<GameOverEvent>.OnEvent -= OnGameOver;

            if (replayButton != null)
                replayButton.onClick.RemoveListener(OnReplayClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void OnGameOver(GameOverEvent eventData)
        {
            // 리매치인 경우 UI 숨김
            if (eventData.IsRematch)
            {
                if (gameOverPanel != null)
                    gameOverPanel.SetActive(false);
                
                Time.timeScale = 1f;
                return;
            }
            
            // 지연 트윈 (타임스케일 영향 받지 않도록 ignoreTimeScale: true)
            _delayTween = DOVirtual.DelayedCall(0.5f, () =>
            {
                if (gameOverPanel != null)
                    gameOverPanel.SetActive(true);

                var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
                if (winnerText != null)
                {
                    // 로컬 플레이어가 이겼는지 확인
                    if (networkGameManager != null)
                    {
                        bool isWinner = networkGameManager.LocalPlayerTeam == eventData.WinnerTeam;
                        winnerText.text = isWinner ? "You Won!" : "You Lost!";
                        Debug.Log($"[GameOverUI] Winner: {eventData.WinnerTeam}, LocalTeam: {networkGameManager.LocalPlayerTeam}, Result: {(isWinner ? "WIN" : "LOSE")}");
                    }
                    else
                    {
                        // Fallback
                        winnerText.text = $"{eventData.WinnerTeam} Win!";
                    }
                }
                
                // Replay 버튼 리셋 (새 게임에 대비)
                if (replayButton != null)
                {
                    replayButton.interactable = true;
                    replayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Replay";
                }

                Time.timeScale = 0f;
            }, ignoreTimeScale: true);
        }

        private void OnReplayClicked()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameOverUI] Voting for rematch...");
            
            // 리매치 투표
            var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
            if (networkGameManager != null)
            {
                networkGameManager.VoteRematchServerRpc();
                
                // 버튼 비활성화 (중복 투표 방지)
                if (replayButton != null)
                {
                    replayButton.interactable = false;
                    replayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Waiting...";
                }
            }
        }

        private void OnExitClicked()
        {
            Time.timeScale = 1f;
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
