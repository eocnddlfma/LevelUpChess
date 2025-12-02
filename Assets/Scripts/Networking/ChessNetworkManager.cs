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
using LevelUpChess.Board;
using LevelUpChess.Managers;
using LevelUpChess.Core;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
    public class ChessNetworkManager : MonoBehaviour
    {
    private const int MAX_PLAYERS = 2;
    private const float RELAY_CODE_TIMEOUT = 30f;
    private const float CLIENT_CONNECTION_TIMEOUT = 60f;
    
#if UNITY_WEBGL
    private const string CONNECTION_TYPE = "wss";
#else
    private const string CONNECTION_TYPE = "dtls";
#endif

    [SerializeField] private UnityLobbyManager lobbyManager;
    [SerializeField] private string gameSceneName = "ChessScene";

    private string playerId;
    private bool isInitializing;
    private UnityLobbyManager.LobbyMatchResult matchResult;
    private Allocation allocation;
    private JoinAllocation joinAllocation;
    private string joinCode;
    private bool isHost;

    public event System.Action<bool, string, string> OnGameReady;
    public event System.Action<string> OnError;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        playerId = System.Guid.NewGuid().ToString().Substring(0, 8);
        
        ValidateComponents();
        SubscribeToLobbyEvents();
        InitializeServices();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromLobbyEvents();
        UnsubscribeFromNetworkEvents();
    }

    private void ValidateComponents()
    {
        if (lobbyManager == null)
            lobbyManager = GetComponent<UnityLobbyManager>();
        if (lobbyManager == null)
            lobbyManager = gameObject.AddComponent<UnityLobbyManager>();
    }

    private void SubscribeToLobbyEvents()
    {
        lobbyManager.OnMatchFound += OnLobbyMatchFound;
        lobbyManager.OnError += OnLobbyError;
        lobbyManager.OnStatusUpdate += OnLobbyStatusUpdate;
    }

    private void UnsubscribeFromLobbyEvents()
    {
        if (lobbyManager == null)
            return;

        lobbyManager.OnMatchFound -= OnLobbyMatchFound;
        lobbyManager.OnError -= OnLobbyError;
        lobbyManager.OnStatusUpdate -= OnLobbyStatusUpdate;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[ChessNetwork] Client {clientId} connected");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.LogWarning($"[ChessNetwork] Client {clientId} disconnected");
    }

    private async void InitializeServices()
    {
        try
        {
            Debug.Log("[ChessNetwork] Starting service initialization...");
            
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // WebGL에서 각 탭이 다른 플레이어로 인식되도록 InitializationOptions 사용
                var options = new InitializationOptions();
                
#if UNITY_WEBGL
                // WebGL: 각 세션마다 고유한 프로필 생성 (같은 브라우저에서 여러 탭 지원)
                string uniqueProfile = $"Player_{Guid.NewGuid().ToString().Substring(0, 8)}";
                options.SetProfile(uniqueProfile);
                Debug.Log($"[ChessNetwork] WebGL: Using unique profile: {uniqueProfile}");
#endif
                
                await UnityServices.InitializeAsync(options);
                Debug.Log("[ChessNetwork] Unity Services initialized");
            }

            // 이미 로그인되어 있으면 로그아웃 후 새로 로그인
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[ChessNetwork] Already signed in, signing out first...");
                AuthenticationService.Instance.SignOut();
            }

#if !UNITY_WEBGL
            // 데스크톱: 세션 토큰 클리어
            AuthenticationService.Instance.ClearSessionToken();
#endif

            Debug.Log("[ChessNetwork] Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            string oderId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[ChessNetwork] Signed in as: {oderId}");
            NetworkLogUI.Log($"Player: {oderId.Substring(0, 8)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Service init failed: {ex.Message}");
            Debug.LogError($"[ChessNetwork] Stack: {ex.StackTrace}");
            NetworkLogUI.Log($"Init Error: {ex.Message}");
        }
    }

    public async void StartMatchmaking()
    {
        if (isInitializing)
            return;

        isInitializing = true;
        await lobbyManager.QuickMatchAsync();
    }

    public void CancelMatchmaking()
    {
        lobbyManager.CancelMatchmaking();
        isInitializing = false;
    }

    private void OnLobbyMatchFound(UnityLobbyManager.LobbyMatchResult result)
    {
        matchResult = result;
        isHost = result.isHost;
        
        Debug.Log($"[ChessNetwork] Match Found! IsHost: {isHost}, Opponent: {result.opponentId}");
        NetworkLogUI.Log("[OK] Opponent found!");
        
        // Fire and forget - SetupRelayConnection 실행
        _ = SetupRelayConnection();
    }

    private void OnLobbyError(string error)
    {
        NetworkLogUI.Log($"Error: {error}");
        OnError?.Invoke(error);
        isInitializing = false;
    }

    private void OnLobbyStatusUpdate(string status)
    {
        NetworkLogUI.Log(status);
    }

    private async Task SetupRelayConnection()
    {
        try
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
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Relay setup failed: {ex.Message}");
            NetworkLogUI.Log($"Connection error: {ex.Message}");
            OnError?.Invoke(ex.Message);
            isInitializing = false;
        }
    }

    private async Task SetupAsHost()
    {
        try
        {
            NetworkLogUI.Log("[HOST] Setting up host...");

            // 1. Relay Allocation 생성
            NetworkLogUI.Log("[HOST] Creating Relay allocation...");
            allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
            NetworkLogUI.Log("[HOST] Relay allocation created");
            Debug.Log($"[ChessNetwork] Allocation ID: {allocation.AllocationId}");

            NetworkLogUI.Log("[HOST] Generating join code...");
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[ChessNetwork] Relay Join Code: {joinCode}");
            NetworkLogUI.Log($"[HOST] Join code: {joinCode}");

            // 2. Lobby에 Join Code 저장
            NetworkLogUI.Log("[HOST] Sharing connection info with lobby...");
            await lobbyManager.UpdateRelayCodeAsync(joinCode);
            NetworkLogUI.Log("[HOST] Connection info shared to lobby");

            // 3. Transport 설정
            NetworkLogUI.Log("[HOST] Configuring transport...");
            ConfigureTransport(allocation.ToRelayServerData(CONNECTION_TYPE));
            NetworkLogUI.Log("[HOST] Transport configured");

            // 4. Host 시작
            NetworkLogUI.Log("[HOST] Starting network host...");
            bool hostStarted = NetworkManager.Singleton.StartHost();
            if (!hostStarted)
                throw new Exception("Failed to start host");

            NetworkLogUI.Log("[HOST] Network host started - Waiting for client...");
            Debug.Log("[ChessNetwork] Host setup completed successfully");

            // 5. 완료 처리
            FinalizeNetworkSetup();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Host setup failed: {ex.Message}");
            Debug.LogError($"[ChessNetwork] Stack: {ex.StackTrace}");
            NetworkLogUI.Log($"[HOST] Error: {ex.Message}");
            throw;
        }
    }

    private async Task SetupAsClient()
    {
        try
        {
            NetworkLogUI.Log("[CLIENT] Setting up client...");

            // 1. Lobby에서 Join Code 가져오기
            string code = matchResult.relayJoinCode;

            // Join Code가 없으면 다시 로비에서 가져오기
            if (string.IsNullOrEmpty(code))
            {
                NetworkLogUI.Log("[CLIENT] Waiting for relay code from host...");
                code = await WaitForRelayCodeAsync();
            }
            else
            {
                NetworkLogUI.Log($"[CLIENT] Relay code received: {code}");
            }

            if (string.IsNullOrEmpty(code))
                throw new Exception("Failed to get relay join code");

            Debug.Log($"[ChessNetwork] Got Join Code: {code}");
            NetworkLogUI.Log("[CLIENT] Connection info received");

            // 2. Relay에 참가
            NetworkLogUI.Log("[CLIENT] Joining Relay server...");
            joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            NetworkLogUI.Log("[CLIENT] Joined Relay server");

            // 3. Transport 설정
            NetworkLogUI.Log("[CLIENT] Configuring transport...");
            ConfigureTransport(joinAllocation.ToRelayServerData(CONNECTION_TYPE));
            NetworkLogUI.Log("[CLIENT] Transport configured");

            // 4. Client 시작
            NetworkLogUI.Log("[CLIENT] Starting network client...");
            bool clientStarted = NetworkManager.Singleton.StartClient();
            if (!clientStarted)
                throw new Exception("Failed to start client");

            NetworkLogUI.Log("[CLIENT] Network client started - Connecting to host...");
            Debug.Log("[ChessNetwork] Client setup completed successfully");

            // 5. 완료 처리
            FinalizeNetworkSetup();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Client setup failed: {ex.Message}");
            Debug.LogError($"[ChessNetwork] Stack: {ex.StackTrace}");
            NetworkLogUI.Log($"[CLIENT] Error: {ex.Message}");
            throw;
        }
    }

    private void ConfigureTransport(RelayServerData relayServerData)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
            throw new Exception("UnityTransport not found");

#if UNITY_WEBGL
        transport.UseWebSockets = true;
#endif

        transport.SetRelayServerData(relayServerData);
    }

    /// <summary>
    /// Relay Join Code 대기 (코루틴 기반, WebGL 호환)
    /// TaskCompletionSource를 사용하여 async/await과 코루틴을 연결
    /// </summary>
    private Task<string> WaitForRelayCodeAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(WaitForRelayCodeCoroutine(tcs));
        return tcs.Task;
    }

    private IEnumerator WaitForRelayCodeCoroutine(TaskCompletionSource<string> tcs)
    {
        float startTime = Time.realtimeSinceStartup;
        int checkCount = 0;

        while (Time.realtimeSinceStartup - startTime < RELAY_CODE_TIMEOUT)
        {
            string code = lobbyManager.GetRelayJoinCode();
            checkCount++;

            if (!string.IsNullOrEmpty(code))
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                Debug.Log($"[ChessNetwork] Relay code received after {elapsed:F1}s ({checkCount} checks)");
                NetworkLogUI.Log("[CLIENT] Relay code received!");
                tcs.SetResult(code);
                yield break;
            }

            if (checkCount % 10 == 0)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                NetworkLogUI.Log($"[CLIENT] Waiting for relay code... ({elapsed:F0}s)");
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError("[ChessNetwork] Timeout waiting for relay code!");
        NetworkLogUI.Log("[CLIENT] Timeout! Host may not have shared relay code");
        tcs.SetResult(null);
    }

    private void FinalizeNetworkSetup()
    {
        NetworkLogUI.Log("========================================");
        NetworkLogUI.Log("[OK] Network ready!");
        NetworkLogUI.Log($"Role: {(isHost ? "Host (White)" : "Client (Black)")}");
        NetworkLogUI.Log("========================================");
        
        OnGameReady?.Invoke(isHost, matchResult?.opponentId ?? "Unknown", isHost ? "white" : "black");
        
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        
        if (isHost)
            StartCoroutine(WaitForClientsAndLoadScene());
        else
            NetworkLogUI.Log("[CLIENT] Waiting for host to load scene...");
        
        isInitializing = false;
    }

    private IEnumerator WaitForClientsAndLoadScene()
    {
        NetworkLogUI.Log("[HOST] Waiting for all players...");
        float startTime = Time.time;

        while (NetworkManager.Singleton.ConnectedClients.Count < MAX_PLAYERS && 
               Time.time - startTime < CLIENT_CONNECTION_TIMEOUT)
        {
            yield return new WaitForSeconds(0.5f);
            
            int connections = NetworkManager.Singleton.ConnectedClients.Count;
            float elapsed = Time.time - startTime;
            
            if (Time.frameCount % 30 == 0)
                NetworkLogUI.Log($"[HOST] Players: {connections}/{MAX_PLAYERS} ({elapsed:F0}s)");
        }

        if (NetworkManager.Singleton.ConnectedClients.Count >= MAX_PLAYERS)
        {
            NetworkLogUI.Log($"[HOST] ??All {MAX_PLAYERS} players connected!");
            NetworkLogUI.Log("[HOST] Starting game...");
            yield return new WaitForSeconds(2f);
            
            LoadGameScene();
        }
        else
        {
            int connections = NetworkManager.Singleton.ConnectedClients.Count;
            string error = $"Timeout: {connections}/{MAX_PLAYERS} players";
            Debug.LogError($"[ChessNetwork] {error}");
            NetworkLogUI.Log($"[HOST] ??{error}");
            OnError?.Invoke(error);
        }
    }

    private void LoadGameScene()
    {
        if (NetworkManager.Singleton != null)
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
        NetworkLogUI.Log("[HOST] Loading game...");
        Debug.Log("[ChessNetwork] Loading scene with Additive mode");
    }

    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != gameSceneName)
            return;

        Debug.Log($"[ChessNetwork] Scene loaded: {sceneName} - Host: {isHost}");
        UnloadPreviousScene();
        StartCoroutine(InitializeGameWithDelay());
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
    }

    private System.Collections.IEnumerator InitializeGameWithDelay()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log($"[ChessNetwork] InitializeGameWithDelay - isHost: {isHost}");

        BoardGenerator boardGenerator = FindFirstObjectByType<BoardGenerator>();
        if (boardGenerator != null)
        {
            Debug.Log("[ChessNetwork] Initializing existing board...");
            boardGenerator.InitializeExistingBoard();
        }

        var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
        if (networkGameManager != null)
        {
            Debug.Log($"[ChessNetwork] Setting team... isHost: {isHost}");
            networkGameManager.SetTeamFromNetwork(isHost);
            Debug.Log($"[ChessNetwork] LocalPlayerTeam set to: {networkGameManager.LocalPlayerTeam}");
        }
        else
        {
            Debug.LogError("[ChessNetwork] NetworkGameManager is NULL!");
        }
    }
    private void UnloadPreviousScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != gameSceneName && scene.isLoaded)
            {
                Debug.Log($"[ChessNetwork] Unloading: {scene.name}");
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
    }
}

