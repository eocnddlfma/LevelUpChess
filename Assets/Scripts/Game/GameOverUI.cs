using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Events;

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
        // 지연 트윈 (타임스케일 영향 받지 않도록 ignoreTimeScale: true)
        _delayTween = DOVirtual.DelayedCall(0.5f, () =>
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (winnerText != null)
                winnerText.text = $"{eventData.WinnerTeam} Win!";

            Time.timeScale = 0f;
        }, ignoreTimeScale: true);
    }

    private void OnReplayClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance.Replay();
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
