using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
    /// <summary>
    /// 네트워크 게임 흐름 조율자 (Orchestrator)
    /// - 인증, Relay, 로비, 씬 관리를 조율
    /// </summary>
    public class ChessNetworkManager : MonoBehaviour
    {
        private const int MAX_PLAYERS = 2;
        private const float RELAY_CODE_TIMEOUT = 30f;
        private const float CLIENT_CONNECTION_TIMEOUT = 60f;

        [SerializeField] private UnityLobbyManager lobbyManager;
        [SerializeField] private string gameSceneName = "ChessScene";

        // Managers
        private RelayHostManager _hostManager;
        private RelayClientManager _clientManager;
        private NetworkSceneHandler _sceneHandler;

        // State
        private UnityLobbyManager.LobbyMatchResult _matchResult;
        private bool _isHost;
        private bool _isInitializing;

        // Events
        public event Action<bool, string, string> OnGameReady;
        public event Action<string> OnError;

        #region Lifecycle

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            InitializeManagers();
            SubscribeToEvents();
            _ = AuthManager.InitializeAndAuthenticateAsync();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeManagers()
        {
            if (lobbyManager == null)
                lobbyManager = GetComponent<UnityLobbyManager>() ?? gameObject.AddComponent<UnityLobbyManager>();

            _hostManager = new RelayHostManager();
            _clientManager = new RelayClientManager();
            _sceneHandler = new NetworkSceneHandler(this, gameSceneName);
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            lobbyManager.OnMatchFound += OnLobbyMatchFound;
            lobbyManager.OnError += OnLobbyError;
            lobbyManager.OnStatusUpdate += OnLobbyStatusUpdate;
        }

        private void UnsubscribeFromEvents()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnMatchFound -= OnLobbyMatchFound;
                lobbyManager.OnError -= OnLobbyError;
                lobbyManager.OnStatusUpdate -= OnLobbyStatusUpdate;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }

            _sceneHandler?.UnsubscribeFromSceneEvents();
        }

        #endregion

        #region Public API

        public async void StartMatchmaking()
        {
            if (_isInitializing) return;
            
            _isInitializing = true;
            await lobbyManager.QuickMatchAsync();
        }

        public void CancelMatchmaking()
        {
            lobbyManager.CancelMatchmaking();
            _isInitializing = false;
        }

        #endregion

        #region Lobby Callbacks

        private void OnLobbyMatchFound(UnityLobbyManager.LobbyMatchResult result)
        {
            _matchResult = result;
            _isHost = result.isHost;

            Debug.Log($"[ChessNetwork] Match Found! IsHost: {_isHost}");
            NetworkLogUI.Log("[OK] Opponent found!");

            _ = SetupRelayConnection();
        }

        private void OnLobbyError(string error)
        {
            NetworkLogUI.Log($"Error: {error}");
            OnError?.Invoke(error);
            _isInitializing = false;
        }

        private void OnLobbyStatusUpdate(string status)
        {
            NetworkLogUI.Log(status);
        }

        #endregion

        #region Network Callbacks

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[ChessNetwork] Client {clientId} connected");
        }

        private void OnClientDisconnect(ulong clientId)
        {
            Debug.LogWarning($"[ChessNetwork] Client {clientId} disconnected");
        }

        #endregion

        #region Relay Setup

        private async Task SetupRelayConnection()
        {
            try
            {
                if (_isHost)
                    await SetupAsHost();
                else
                    await SetupAsClient();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChessNetwork] Relay setup failed: {ex.Message}");
                NetworkLogUI.Log($"Connection error: {ex.Message}");
                OnError?.Invoke(ex.Message);
                _isInitializing = false;
            }
        }

        private async Task SetupAsHost()
        {
            await _hostManager.SetupHostAsync(MAX_PLAYERS);

            // Lobby에 Join Code 공유
            NetworkLogUI.Log("[HOST] Sharing connection info...");
            await lobbyManager.UpdateRelayCodeAsync(_hostManager.JoinCode);
            NetworkLogUI.Log("[HOST] Connection info shared");

            FinalizeNetworkSetup();
        }

        private async Task SetupAsClient()
        {
            string code = _matchResult.relayJoinCode;

            if (string.IsNullOrEmpty(code))
            {
                NetworkLogUI.Log("[CLIENT] Waiting for relay code...");
                code = await WaitForRelayCodeAsync();
            }

            if (string.IsNullOrEmpty(code))
                throw new Exception("Failed to get relay join code");

            await _clientManager.SetupClientAsync(code);
            FinalizeNetworkSetup();
        }

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
                    NetworkLogUI.Log("[CLIENT] Relay code received!");
                    tcs.SetResult(code);
                    yield break;
                }

                if (checkCount % 10 == 0)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    NetworkLogUI.Log($"[CLIENT] Waiting... ({elapsed:F0}s)");
                }

                yield return new WaitForSeconds(0.1f);
            }

            NetworkLogUI.Log("[CLIENT] Timeout waiting for relay code");
            tcs.SetResult(null);
        }

        #endregion

        #region Finalization

        private void FinalizeNetworkSetup()
        {
            NetworkLogUI.Log("========================================");
            NetworkLogUI.Log("[OK] Network ready!");
            NetworkLogUI.Log($"Role: {(_isHost ? "Host (White)" : "Client (Black)")}");
            NetworkLogUI.Log("========================================");

            OnGameReady?.Invoke(_isHost, _matchResult?.opponentId ?? "Unknown", _isHost ? "white" : "black");

            _sceneHandler.SubscribeToSceneEvents();
            _sceneHandler.StartWaitingForClientsAndLoad(MAX_PLAYERS, CLIENT_CONNECTION_TIMEOUT, _isHost);

            _isInitializing = false;
        }

        #endregion
    }
}

