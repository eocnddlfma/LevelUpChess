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
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

#if !UNITY_WEBGL
            AuthenticationService.Instance.ClearSessionToken();
#endif

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            string userId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[ChessNetwork] Signed in: {userId.Substring(0, 8)}");
            NetworkLogUI.Log($"Player: {userId.Substring(0, 8)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Service init failed: {ex.Message}");
            NetworkLogUI.Log($"??Init Error: {ex.Message}");
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
        
        Debug.Log($"[ChessNetwork] Match found - Host: {isHost}");
        NetworkLogUI.Log("[OK] Opponent found!");
        
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
                await SetupAsHost();
            else
                await SetupAsClient();
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
            NetworkLogUI.Log("[HOST] Setting up...");

            allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            NetworkLogUI.Log($"[HOST] Join code: {joinCode}");
            await lobbyManager.UpdateRelayCodeAsync(joinCode);
            NetworkLogUI.Log("[HOST] Connection info shared");

            ConfigureTransport(allocation.ToRelayServerData(CONNECTION_TYPE));
            
            if (!NetworkManager.Singleton.StartHost())
                throw new Exception("Failed to start host");

            NetworkLogUI.Log("[HOST] Network started - Waiting for client...");
            FinalizeNetworkSetup();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Host setup failed: {ex.Message}");
            NetworkLogUI.Log($"[HOST] ??Error: {ex.Message}");
            throw;
        }
    }

    private async Task SetupAsClient()
    {
        try
        {
            NetworkLogUI.Log("[CLIENT] Setting up...");

            string code = matchResult.relayJoinCode;
            if (string.IsNullOrEmpty(code))
            {
                NetworkLogUI.Log("[CLIENT] Waiting for relay code...");
                code = await WaitForRelayCode();
            }

            if (string.IsNullOrEmpty(code))
                throw new Exception("Failed to get relay code");

            NetworkLogUI.Log("[CLIENT] Joining relay...");
            joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            NetworkLogUI.Log("[CLIENT] Relay joined");

            ConfigureTransport(joinAllocation.ToRelayServerData(CONNECTION_TYPE));

            if (!NetworkManager.Singleton.StartClient())
                throw new Exception("Failed to start client");

            NetworkLogUI.Log("[CLIENT] Network started...");
            FinalizeNetworkSetup();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChessNetwork] Client setup failed: {ex.Message}");
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

    private async Task<string> WaitForRelayCode()
    {
        float startTime = Time.realtimeSinceStartup;
        
        while (Time.realtimeSinceStartup - startTime < RELAY_CODE_TIMEOUT)
        {
            string code = lobbyManager.GetRelayJoinCode();
            if (!string.IsNullOrEmpty(code))
                return code;

            await Task.Delay(100);
        }

        Debug.LogError("[ChessNetwork] Relay code timeout");
        NetworkLogUI.Log("[CLIENT] ??Timeout - Host may not have shared code");
        return null;
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
            Debug.Log("[ChessNetwork] Generating board...");
            boardGenerator.GenerateBoard();
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

