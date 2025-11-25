using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class ChessNetworkManager : MonoBehaviour
{
    private const int MAX_PLAYERS = 2;
    private const string CONNECTION_TYPE = "dtls";

    [Header("Networking")]
    [SerializeField] private UnityLobbyManager lobbyManager;
    
    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "ChessScene";

    private string playerId;
    private bool isInitializing = false;
    private UnityLobbyManager.LobbyMatchResult matchResult;
    private Allocation allocation;
    private JoinAllocation joinAllocation;
    private string joinCode;
    private bool isHost;

    public delegate void OnGameReadyDelegate(bool isHost, string opponentId, string color);
    public event OnGameReadyDelegate OnGameReady;

    public event System.Action<string> OnError;

    private void Start()
    {
        // 씬 전환 시 파괴되지 않도록 설정
        DontDestroyOnLoad(gameObject);
        
// #if UNITY_EDITOR
//         playerId = System.Guid.NewGuid().ToString().Substring(0, 8);
// #else
//         playerId = PlayerPrefs.GetString("PlayerId");
//         if (string.IsNullOrEmpty(playerId))
//         {
//             playerId = System.Guid.NewGuid().ToString().Substring(0, 8);
//             PlayerPrefs.SetString("PlayerId", playerId);
//             PlayerPrefs.Save();
//         }
// #endif
        playerId = System.Guid.NewGuid().ToString().Substring(0, 8);

        // LobbyManager 초기화
        if (lobbyManager == null)
            lobbyManager = GetComponent<UnityLobbyManager>();
        if (lobbyManager == null)
            lobbyManager = gameObject.AddComponent<UnityLobbyManager>();
            
        // 이벤트 구독
        lobbyManager.OnMatchFound += OnLobbyMatchFound;
        lobbyManager.OnError += OnLobbyError;
        lobbyManager.OnStatusUpdate += OnLobbyStatusUpdate;

        InitializeServices();
    }
    
    private void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnMatchFound -= OnLobbyMatchFound;
            lobbyManager.OnError -= OnLobbyError;
            lobbyManager.OnStatusUpdate -= OnLobbyStatusUpdate;
        }
    }

    /// <summary>
    /// Unity Services 초기화
    /// </summary>
    private async void InitializeServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            // 한 컴퓨터에서 여러 빌드를 실행할 때 필수!!!
            // 기존 토큰을 완전히 삭제해야 새로운 Anonymous 계정으로 로그인됨
            AuthenticationService.Instance.ClearSessionToken();

            Debug.Log("[ChessNetwork] Signing in as NEW anonymous user...");
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"[ChessNetwork] Signed in as new player: {AuthenticationService.Instance.PlayerId}");
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Chat.AddMessage($"Player ID: {AuthenticationService.Instance.PlayerId.Substring(0, 8)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Failed to initialize services: {ex.Message}");
            Chat.AddMessage($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 매칭 시작 - 버튼 한 번으로 방 생성 또는 참가
    /// </summary>
    public async void StartMatchmaking()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[ChessNetwork] Already matchmaking");
            return;
        }
        
        isInitializing = true;
        matchResult = null;
        
        // Unity Lobby 퀵매치 시작
        await lobbyManager.QuickMatchAsync();
    }

    /// <summary>
    /// 매칭 취소
    /// </summary>
    public void CancelMatchmaking()
    {
        lobbyManager.CancelMatchmaking();
        isInitializing = false;
    }

    /// <summary>
    /// Lobby 이벤트 핸들러 - 매치 발견
    /// </summary>
    private async void OnLobbyMatchFound(UnityLobbyManager.LobbyMatchResult result)
    {
        matchResult = result;
        isHost = result.isHost;
        
        Debug.Log($"[ChessNetwork] Match Found! IsHost: {isHost}, Opponent: {result.opponentId}");
        AddChatMessage("[OK] Opponent found!");

        try
        {
            await SetupRelayConnection();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Relay setup failed: {ex.Message}");
            AddChatMessage($"Connection error: {ex.Message}");
            OnError?.Invoke(ex.Message);
            isInitializing = false;
        }
    }
    
    /// <summary>
    /// Lobby 이벤트 핸들러 - 에러
    /// </summary>
    private void OnLobbyError(string error)
    {
        AddChatMessage($"Error: {error}");
        OnError?.Invoke(error);
        isInitializing = false;
    }
    
    /// <summary>
    /// Lobby 이벤트 핸들러 - 상태 업데이트
    /// </summary>
    private void OnLobbyStatusUpdate(string status)
    {
        AddChatMessage(status);
    }

    /// <summary>
    /// Relay 연결 설정
    /// </summary>
    private async Task SetupRelayConnection()
    {
        if (isHost)
        {
            await SetupAsHost();
        }
        else
        {
            await SetupAsClient();
        }
    }

    /// <summary>
    /// 호스트로 Relay 설정
    /// </summary>
    private async Task SetupAsHost()
    {
        try
        {
            AddChatMessage("[HOST] Setting up host...");

            // 1. Relay Allocation 생성
            AddChatMessage("[HOST] Creating Relay allocation...");
            allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
            AddChatMessage("[HOST] [OK] Relay allocation created");
            
            AddChatMessage("[HOST] Generating join code...");
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[ChessNetwork] Relay Join Code: {joinCode}");
            AddChatMessage($"[HOST] [OK] Join code: {joinCode}");

            // 2. Lobby에 Join Code 저장 (클라이언트가 가져갈 수 있도록)
            AddChatMessage("[HOST] Sharing connection info with lobby...");
            await lobbyManager.UpdateRelayCodeAsync(joinCode);
            AddChatMessage("[HOST] [OK] Connection info shared");

            // 3. Transport 설정
            AddChatMessage("[HOST] Configuring transport...");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
                throw new Exception("UnityTransport not found");

            transport.SetRelayServerData(allocation.ToRelayServerData(CONNECTION_TYPE));
            AddChatMessage("[HOST] [OK] Transport configured");

#if UNITY_WEBGL
            transport.UseWebSockets = true;
#endif

            // 4. Host 시작
            AddChatMessage("[HOST] Starting network host...");
            bool hostStarted = NetworkManager.Singleton.StartHost();
            if (!hostStarted)
                throw new Exception("Failed to start host");

            AddChatMessage("[HOST] [OK] Network host started - Waiting for client...");
            Debug.Log("[ChessNetwork] Host setup completed successfully");

            // 5. 완료 처리
            FinalizeNetworkSetup();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Host setup failed: {ex.Message}");
            AddChatMessage($"[HOST] Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 클라이언트로 Relay 설정
    /// </summary>
    private async Task SetupAsClient()
    {
        try
        {
            AddChatMessage("[CLIENT] Setting up client...");

            // 1. Lobby에서 Join Code 가져오기 (이미 OnMatchFound에서 받음)
            string code = matchResult.relayJoinCode;

            // Join Code가 없으면 다시 로비에서 가져오기
            if (string.IsNullOrEmpty(code))
            {
                AddChatMessage("[CLIENT] Waiting for relay code from host...");
                code = await WaitForRelayCode();
            }
            else
            {
                AddChatMessage($"[CLIENT] [OK] Relay code received: {code}");
            }

            if (string.IsNullOrEmpty(code))
                throw new Exception("Failed to get relay join code");

            Debug.Log($"[ChessNetwork] Got Join Code: {code}");
            AddChatMessage("[CLIENT] [OK] Connection info received");

            // 2. Relay에 참가
            AddChatMessage("[CLIENT] Joining Relay server...");
            joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            AddChatMessage("[CLIENT] [OK] Joined Relay server");

            // 3. Transport 설정
            AddChatMessage("[CLIENT] Configuring transport...");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
                throw new Exception("UnityTransport not found");

            transport.SetRelayServerData(joinAllocation.ToRelayServerData(CONNECTION_TYPE));
            AddChatMessage("[CLIENT] [OK] Transport configured");

#if UNITY_WEBGL
            transport.UseWebSockets = true;
#endif

            // 4. Client 시작
            AddChatMessage("[CLIENT] Starting network client...");
            bool clientStarted = NetworkManager.Singleton.StartClient();
            if (!clientStarted)
                throw new Exception("Failed to start client");

            AddChatMessage("[CLIENT] [OK] Network client started - Connecting to host...");
            Debug.Log("[ChessNetwork] Client setup completed successfully");

            // 5. 완료 처리
            FinalizeNetworkSetup();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Client setup failed: {ex.Message}");
            AddChatMessage($"[CLIENT] Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Relay Join Code 대기 (Lobby에서 가져오기)
    /// </summary>
    private async Task<string> WaitForRelayCode()
    {
        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            string code = lobbyManager.GetRelayJoinCode();
            if (!string.IsNullOrEmpty(code))
            {
                return code;
            }

            await Task.Delay(500);
            elapsed += 0.5f;
        }

        return null;
    }

    /// <summary>
    /// 네트워크 설정 완료 후 공통 처리
    /// </summary>
    private void FinalizeNetworkSetup()
    {
        AddChatMessage("========================================");
        AddChatMessage("[OK] Network ready!");
        
        string opponentId = matchResult?.opponentId ?? "Unknown";
        string color = isHost ? "white" : "black";
        string role = isHost ? "[HOST]" : "[CLIENT]";
        
        AddChatMessage($"{role} Connected as: {color.ToUpper()}");
        AddChatMessage($"{role} Opponent ID: {opponentId}");
        AddChatMessage("========================================");
        
        OnGameReady?.Invoke(isHost, opponentId, color);
        
        // 씬 로드 콜백 등록
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        Debug.Log($"[ChessNetwork] Scene load callback registered - IsHost: {isHost}");
        
        // 모든 클라이언트가 연결될 때까지 대기 후 씬 로드
        if (isHost)
        {
            StartCoroutine(WaitForClientsAndLoadScene());
        }
        else
        {
            AddChatMessage("[CLIENT] Waiting for host to load the game scene...");
            Debug.Log("[ChessNetwork] Client: Waiting for host to trigger scene load...");
        }
        
        isInitializing = false;
    }

    /// <summary>
    /// 게임 씬 로드
    /// </summary>
    private void LoadGameScene()
    {
        // NetworkManager를 DontDestroyOnLoad로 설정
        if (NetworkManager.Singleton != null)
        {
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
        }

        if (isHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
            AddChatMessage("Loading game scene...");
            Debug.Log("[ChessNetwork] Host: Loading scene with Additive mode...");
        }
        else
        {
            AddChatMessage("Waiting for host to load scene...");
            Debug.Log("[ChessNetwork] Client: Waiting for host to load scene...");
        }
    }
    
    /// <summary>
    /// <summary>
    /// <summary>
    /// UI에 상태 메시지 표시
    /// </summary>
    private void AddChatMessage(string message)
    {
        // Chat.cs를 통해 메시지 표시
        Chat.AddMessage(message);
        
        // MultiplayerUIManager가 있으면 사용 (게임 씬)
        if (MultiplayerUIManager.Instance != null)
        {
            MultiplayerUIManager.Instance.AddChatMessage(message);
        }
        
        Debug.Log($"[ChessNetwork] {message}");
    }
    
    /// <summary>
    /// Host: 모든 클라이언트가 연결될 때까지 대기 후 씬 로드
    /// </summary>
    private IEnumerator WaitForClientsAndLoadScene()
    {
        AddChatMessage("[HOST] Waiting for all players to connect...");
        AddChatMessage($"[HOST] Current connections: {NetworkManager.Singleton.ConnectedClients.Count}/{MAX_PLAYERS}");
        
        float timeout = 10f;
        float elapsed = 0f;
        
        while (NetworkManager.Singleton.ConnectedClients.Count < MAX_PLAYERS && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            if (elapsed % 2 < 0.1f) // Every 2 seconds
            {
                AddChatMessage($"[HOST] Connections: {NetworkManager.Singleton.ConnectedClients.Count}/{MAX_PLAYERS} ({(int)elapsed}s elapsed)");
            }
        }
        
        if (NetworkManager.Singleton.ConnectedClients.Count >= MAX_PLAYERS)
        {
            AddChatMessage($"[HOST] [OK] All {MAX_PLAYERS} players connected!");
            AddChatMessage("[HOST] Starting game in 2 seconds...");
            yield return new WaitForSeconds(2f);
            LoadGameScene();
        }
        else
        {
            AddChatMessage($"[HOST] Warning: Only {NetworkManager.Singleton.ConnectedClients.Count}/{MAX_PLAYERS} players connected after {timeout}s");
            AddChatMessage("[HOST] Starting game with available players...");
            yield return new WaitForSeconds(1f);
            LoadGameScene();
        }
    }
    
    /// <summary>
    /// 씬 로드 완료 콜백
    /// </summary>
    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == gameSceneName)
        {
            string teamName = isHost ? "흰색 팀 (White)" : "검정 팀 (Black)";
            Debug.Log($"[ChessNetwork] ========================================");
            Debug.Log($"[ChessNetwork] Scene loaded: {sceneName}");
            Debug.Log($"[ChessNetwork] IsHost: {isHost}");
            Debug.Log($"[ChessNetwork] Team: {teamName}");
            Debug.Log($"[ChessNetwork] ========================================");
            
            // 이전 씬 언로드
            UnloadPreviousScene();
            
            // 게임 초기화 (네트워크 객체가 준비될 때까지 대기)
            StartCoroutine(InitializeGameWithDelay());
            
            // 콜백 해제
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }
    }
    
    /// <summary>
    /// 게임 초기화를 지연시켜 모든 NetworkObject가 준비되도록 함
    /// </summary>
    private System.Collections.IEnumerator InitializeGameWithDelay()
    {
        // NetworkObject와 NetworkBehaviour가 준비될 때까지 대기
        yield return new WaitForSeconds(1f);
        
        // 보드 생성
        BoardGenerator boardGenerator = FindFirstObjectByType<BoardGenerator>();
        if (boardGenerator != null)
        {
            Debug.Log($"[ChessNetwork] Generating board...");
            boardGenerator.GenerateBoard();
        }
        else
        {
            Debug.LogError("[ChessNetwork] BoardGenerator not found!");
        }
        
        // NetworkGameManager 설정
        if (NetworkGameManager.Instance != null)
        {
            Debug.Log($"[ChessNetwork] Setting team from network...");
            NetworkGameManager.Instance.SetTeamFromNetwork(isHost);
        }
        else
        {
            Debug.LogError("[ChessNetwork] NetworkGameManager not found in scene!");
        }
    }
    
    /// <summary>
    /// 이전 씬 언로드
    /// </summary>
    private void UnloadPreviousScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != gameSceneName && scene.isLoaded)
            {
                Debug.Log($"[ChessNetwork] Unloading previous scene: {scene.name}");
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}

