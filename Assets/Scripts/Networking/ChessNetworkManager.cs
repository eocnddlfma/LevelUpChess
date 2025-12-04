using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
    public class ChessNetworkManager : MonoBehaviour
    {
        private const int MAX_PLAYERS = 2;
        private const float RELAY_CODE_TIMEOUT = 30f;
        private const float CLIENT_CONNECTION_TIMEOUT = 60f;

        [SerializeField] private UnityLobbyManager lobbyManager;
        [SerializeField] private RelayHostManager hostManager;
        [SerializeField] private RelayClientManager clientManager;
        [SerializeField] private NetworkSceneHandler sceneHandler;

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
            if (hostManager == null)
                hostManager = GetComponent<RelayHostManager>() ?? gameObject.AddComponent<RelayHostManager>();
            if (clientManager == null)
                clientManager = GetComponent<RelayClientManager>() ?? gameObject.AddComponent<RelayClientManager>();
            if (sceneHandler == null)
                sceneHandler = GetComponent<NetworkSceneHandler>() ?? gameObject.AddComponent<NetworkSceneHandler>();
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            lobbyManager.OnMatchFound += OnLobbyMatchFound;
            lobbyManager.OnError += OnLobbyError;
            lobbyManager.OnStatusUpdate += OnLobbyStatusUpdate;
            // NetworkManager callbacks may not be available at Start; guard against null
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            }
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

            sceneHandler?.UnsubscribeFromSceneEvents();
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

            NetworkLogUI.Log($"Matched! ({(_isHost ? "Host" : "Client")})");
            _ = SetupRelayConnection();
        }

        private void OnLobbyError(string error)
        {
            OnError?.Invoke(error);
            _isInitializing = false;
        }

        private void OnLobbyStatusUpdate(string status)
        {
            NetworkLogUI.Log(status);
        }

        #endregion

        #region Network Callbacks

        private void OnClientConnected(ulong clientId) { }

        private void OnClientDisconnect(ulong clientId) { }

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
                Debug.LogError($"[ChessNetwork] {ex.Message}");
                OnError?.Invoke(ex.Message);
                _isInitializing = false;
            }
        }

        private async Task SetupAsHost()
        {
            await hostManager.SetupHostAsync(MAX_PLAYERS);
            await lobbyManager.UpdateRelayCodeAsync(hostManager.JoinCode);
            FinalizeNetworkSetup();
        }

        private async Task SetupAsClient()
        {
            string code = _matchResult.relayJoinCode;

            if (string.IsNullOrEmpty(code))
                code = await WaitForRelayCodeAsync();

            if (string.IsNullOrEmpty(code))
                throw new Exception("Failed to get relay join code");

            await clientManager.SetupClientAsync(code);
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

            while (Time.realtimeSinceStartup - startTime < RELAY_CODE_TIMEOUT)
            {
                string code = lobbyManager.GetRelayJoinCode();
                if (!string.IsNullOrEmpty(code))
                {
                    tcs.SetResult(code);
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }

            tcs.SetResult(null);
        }

        #endregion

        #region Finalization

        private void FinalizeNetworkSetup()
        {
            NetworkLogUI.Log($"Ready! ({(_isHost ? "White" : "Black")})");

            OnGameReady?.Invoke(_isHost, _matchResult?.opponentId ?? "Unknown", _isHost ? "white" : "black");

            sceneHandler.SubscribeToSceneEvents();
            sceneHandler.StartWaitingForClientsAndLoad(MAX_PLAYERS, CLIENT_CONNECTION_TIMEOUT, _isHost);

            _isInitializing = false;
        }

        #endregion
    }
}