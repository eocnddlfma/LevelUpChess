using UnityEngine;
using UnityEngine.UI;

public class MultiplayerUIManager : MonoBehaviour
{
    [SerializeField] private Canvas matchmakingCanvas;
    [SerializeField] private Canvas gameplayCanvas;
    
    [SerializeField] private Button playButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text playerInfoText;
    [SerializeField] private Text chatLogText; // 채팅 로그

    [SerializeField] private Image whiteColorIndicator;
    [SerializeField] private Image blackColorIndicator;

    private ChessNetworkManager networkManager;
    private bool isMatching = false;
    
    public static MultiplayerUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        
        if (Instance == this)
            Instance = null;
    }
    
    /// <summary>
    /// 채팅 로그에 메시지 추가
    /// </summary>
    public void AddChatMessage(string message)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[UI] MultiplayerUIManager Instance is null!");
            return;
        }

        if (Instance.chatLogText != null)
        {
            Instance.chatLogText.text += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
            Debug.Log($"[UI] Chat message added: {message}");
        }
        else
        {
            Debug.LogError("[UI] Chat log text is NULL! Please assign it in the Inspector.");
        }
    }
}
