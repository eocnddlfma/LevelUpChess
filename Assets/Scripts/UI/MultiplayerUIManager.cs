using UnityEngine;
using UnityEngine.UI;
using LevelUpChess.Core;
using LevelUpChess.Networking;

namespace LevelUpChess.UI
{
    public class MultiplayerUIManager : MonoBehaviour
    {
        [SerializeField] private Canvas matchmakingCanvas;
        [SerializeField] private Canvas gameplayCanvas;
        
        [SerializeField] private Button playButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text playerInfoText;
        [SerializeField] private Text chatLogText; // 채팅 로그
        [SerializeField] private ScrollRect chatScrollRect; // 채팅 스크롤뷰

        [SerializeField] private Image whiteColorIndicator;
        [SerializeField] private Image blackColorIndicator;

        private ChessNetworkManager networkManager;
        private bool isMatching = false;

    private void Awake()
    {
        if (ServiceLocator.Has<MultiplayerUIManager>())
        {
            Destroy(gameObject);
            return;
        }
        ServiceLocator.Register(this);
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // networkManager 초기화
        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<ChessNetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[UI] ChessNetworkManager not found!");
                return;
            }
        }

        // 버튼 연결
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // 이벤트 연결
        networkManager.OnGameReady += OnGameReady;
        networkManager.OnError += OnError;

        // 초기 UI 상태
        if (matchmakingCanvas != null)
            matchmakingCanvas.enabled = false;
        if (gameplayCanvas != null)
            gameplayCanvas.enabled = false;
        if (statusText != null)
            statusText.text = "준비 완료";
        
        if (chatLogText != null)
        {
            chatLogText.text = "";
            Debug.Log("[UI] Chat log initialized");
        }
        else
        {
            Debug.LogWarning("[UI] Chat log text is NULL!");
        }
    }

    public void OnPlayClicked()
    {
        if (isMatching)
        {
            Debug.LogWarning("[UI] Already matching");
            return;
        }

        isMatching = true;
        matchmakingCanvas.enabled = true;
        statusText.text = "매칭 중...";
        playButton.interactable = false;
        cancelButton.interactable = true;

        Debug.Log("[UI] Play button clicked - Starting matchmaking");
        networkManager.StartMatchmaking();
    }

    public void OnCancelClicked()
    {
        if (!isMatching)
            return;

        isMatching = false;
        matchmakingCanvas.enabled = false;
        statusText.text = "매칭 취소됨";
        playButton.interactable = true;
        cancelButton.interactable = false;

        Debug.Log("[UI] Cancel button clicked");
    }

    private void OnGameReady(bool isHost, string opponentId, string playerColor)
    {
        isMatching = false;
        
        // UI 전환
        matchmakingCanvas.enabled = false;
        gameplayCanvas.enabled = true;

        // 플레이어 정보 표시
        string colorText = playerColor == "white" ? "흰색 (선공)" : "검은색 (후공)";
        playerInfoText.text = $"상대: {opponentId}\n색상: {colorText}";

        // 색상 표시기 업데이트
        if (playerColor == "white")
        {
            whiteColorIndicator.color = new Color(1, 1, 1, 1); // 불투명
            blackColorIndicator.color = new Color(1, 1, 1, 0.3f); // 투명
        }
        else
        {
            whiteColorIndicator.color = new Color(1, 1, 1, 0.3f);
            blackColorIndicator.color = new Color(1, 1, 1, 1);
        }

        Debug.Log($"[UI] Game started - {colorText}");
    }

    private void OnError(string errorMessage)
    {
        isMatching = false;
        matchmakingCanvas.enabled = false;
        statusText.text = $"오류: {errorMessage}";
        playButton.interactable = true;
        cancelButton.interactable = false;

        Debug.LogError($"[UI] Error: {errorMessage}");
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnGameReady -= OnGameReady;
            networkManager.OnError -= OnError;
        }
        
        if (ServiceLocator.Get<MultiplayerUIManager>() == this)
            ServiceLocator.Unregister<MultiplayerUIManager>();
    }
    
    /// <summary>
    /// 채팅 로그에 메시지 추가
    /// </summary>
    public void AddChatMessage(string message)
    {
        var instance = ServiceLocator.Get<MultiplayerUIManager>();
        if (instance == null)
        {
            Debug.LogWarning("[UI] MultiplayerUIManager Instance is null!");
            return;
        }

        if (instance.chatLogText != null)
        {
            instance.chatLogText.text += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
            Debug.Log($"[UI] Chat message added: {message}");
            
            // 스크롤을 항상 아래로 이동
            instance.ScrollToBottom();
        }
        else
        {
            Debug.LogError("[UI] Chat log text is NULL! Please assign it in the Inspector.");
        }
    }
    
    /// <summary>
    /// 채팅 스크롤뷰를 맨 아래로 스크롤
    /// </summary>
    private void ScrollToBottom()
    {
        if (chatScrollRect != null)
        {
            // 레이아웃 재계산 후 스크롤 위치 설정
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    }
}
