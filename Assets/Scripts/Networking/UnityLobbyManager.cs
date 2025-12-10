using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace LevelUpChess.Networking
{
    public class UnityLobbyManager : MonoBehaviour
    {
        private const string KEY_RELAY_CODE = "RelayCode";
        private const string KEY_GAME_STARTED = "Started";

        [SerializeField] private string lobbyName = "Chess";
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private float heartbeatInterval = 15f;

        private Lobby currentLobby;
        private float heartbeatTimer;
        private bool isHost;
        private Coroutine pollOpponentCoroutine;
        private Coroutine pollRelayCodeCoroutine;

        public event Action<LobbyMatchResult> OnMatchFound;
        public event Action<string> OnError;
        public event Action<string> OnStatusUpdate;

        [Serializable]
        public class LobbyMatchResult
        {
            public bool isHost;
            public string lobbyId;
            public string relayJoinCode;
            public string opponentId;
        }

        public bool IsHost => isHost;
        public Lobby CurrentLobby => currentLobby;

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Update()
        {
            if (currentLobby == null || !isHost) return;
            
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                heartbeatTimer = heartbeatInterval;
                _ = SendHeartbeat();
            }
        }

        #region Public API

        public async Task QuickMatchAsync()
        {
            try
            {
                // 인증은 ChessNetworkManager에서 이미 완료됨
                OnStatusUpdate?.Invoke("Searching for lobbies...");

                var availableLobby = await FindAvailableLobby();
                if (availableLobby != null)
                    await JoinLobbyAsync(availableLobby);
                else
                    await CreateLobbyAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Lobby] QuickMatch failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public void CancelMatchmaking() => _ = LeaveLobbyAsync();

        public async Task UpdateRelayCodeAsync(string joinCode)
        {
            if (currentLobby == null || !isHost) return;

            try
            {
                var options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
                        { KEY_GAME_STARTED, new DataObject(DataObject.VisibilityOptions.Public, "true", DataObject.IndexOptions.S1) }
                    }
                };
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Lobby] UpdateRelayCode failed: {ex.Message}");
                throw;
            }
        }

        public string GetRelayJoinCode()
        {
            if (currentLobby?.Data == null) return null;
            return currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var data) ? data.Value : null;
        }

        public async Task LeaveLobbyAsync()
        {
            if (currentLobby == null) return;

            try
            {
                if (isHost)
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                else
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            catch { }
            finally
            {
                currentLobby = null;
                isHost = false;
            }
        }

        #endregion

        #region Lobby Operations

        private async Task<Lobby> FindAvailableLobby()
        {
            try
            {
                var options = new QueryLobbiesOptions
                {
                    Count = 5,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.S1, "false", QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };

                var response = await LobbyService.Instance.QueryLobbiesAsync(options);
                Debug.Log($"[Lobby] Found {response.Results.Count} available lobbies");
                if (response.Results.Count > 0)
                {
                    var lobby = response.Results[0];
                    Debug.Log($"[Lobby] Selected lobby {lobby.Id} with {lobby.Players.Count}/{lobby.MaxPlayers} players");
                    return lobby;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Lobby] FindAvailable failed: {ex.Message}");
                return null;
            }
        }

        private async Task CreateLobbyAsync()
        {
            try
            {
                OnStatusUpdate?.Invoke("Creating lobby...");

                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = new Unity.Services.Lobbies.Models.Player { Data = new Dictionary<string, PlayerDataObject>() },
                    Data = new Dictionary<string, DataObject>
                    {
                        { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") },
                        { KEY_GAME_STARTED, new DataObject(DataObject.VisibilityOptions.Public, "false", DataObject.IndexOptions.S1) }
                    }
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                isHost = true;
                heartbeatTimer = heartbeatInterval;

                Debug.Log($"[Lobby] Created lobby {currentLobby.Id} as Host");
                OnStatusUpdate?.Invoke("Waiting for opponent...");
                
                if (pollOpponentCoroutine != null)
                    StopCoroutine(pollOpponentCoroutine);
                pollOpponentCoroutine = StartCoroutine(PollForOpponentCoroutine());
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError($"[Lobby] Create failed: {ex.Reason} - {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        private async Task JoinLobbyAsync(Lobby lobby)
        {
            try
            {
                OnStatusUpdate?.Invoke("Joining lobby...");

                var options = new JoinLobbyByIdOptions
                {
                    Player = new Unity.Services.Lobbies.Models.Player { Data = new Dictionary<string, PlayerDataObject>() }
                };

                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
                isHost = false;

                Debug.Log($"[Lobby] Joined lobby {currentLobby.Id} as Client. Players: {currentLobby.Players.Count}");
                OnStatusUpdate?.Invoke("Waiting for relay code...");
                
                if (pollRelayCodeCoroutine != null)
                    StopCoroutine(pollRelayCodeCoroutine);
                pollRelayCodeCoroutine = StartCoroutine(PollForRelayCodeCoroutine());
            }
            catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyFull ||
                                                    ex.Reason == LobbyExceptionReason.LobbyConflict)
            {
                Debug.LogWarning($"[Lobby] Join failed ({ex.Reason}), retrying...");
                await TryLeaveAndRetry(lobby.Id);
            }
            catch (LobbyServiceException ex) when (ex.Message.Contains("already a member"))
            {
                currentLobby = lobby;
                isHost = false;
                StartCoroutine(PollForRelayCodeCoroutine());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Lobby] Join failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        private async Task TryLeaveAndRetry(string lobbyId)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
            }
            catch { }
            await QuickMatchAsync();
        }

        #endregion

        #region Polling Coroutines

        private IEnumerator PollForOpponentCoroutine()
        {
            const int maxAttempts = 60;
            int errorCount = 0;

            for (int i = 0; i < maxAttempts; i++)
            {
                if (i > 0) yield return new WaitForSeconds(3f);

                bool pollComplete = false;
                bool pollSuccess = false;
                int playerCount = 0;
                bool shouldBreak = false;

                PollLobbyOnce((success, count, error) =>
                {
                    pollSuccess = success;
                    playerCount = count;
                    errorCount = success ? 0 : errorCount + 1;
                    
                    if (success)
                        Debug.Log($"[Lobby] Poll #{i}: {count}/{maxPlayers} players");
                    else
                        Debug.LogWarning($"[Lobby] Poll #{i} failed: {error}");
                    
                    // 콜백 내부에서 즉시 체크
                    if (success && count >= maxPlayers)
                    {
                        Debug.Log($"[Lobby] Opponent found! Starting relay setup...");
                        shouldBreak = true;
                    }
                    
                    pollComplete = true;
                });

                yield return new WaitUntil(() => pollComplete);

                if (shouldBreak)
                {
                    OnStatusUpdate?.Invoke("Opponent found!");
                    pollOpponentCoroutine = null;
                    NotifyMatchFound();
                    yield break;
                }

                if (errorCount >= 3)
                {
                    OnError?.Invoke("Too many lobby errors");
                    pollOpponentCoroutine = null;
                    _ = LeaveLobbyAsync();
                    yield break;
                }

                if ((i + 1) % 5 == 0)
                    OnStatusUpdate?.Invoke($"Waiting for opponent... ({(i + 1) * 3}s)");
            }

            OnError?.Invoke("Timeout waiting for opponent");
            pollOpponentCoroutine = null;
            _ = LeaveLobbyAsync();
        }

        private IEnumerator PollForRelayCodeCoroutine()
        {
            const int maxAttempts = 30;
            int errorCount = 0;

            for (int i = 0; i < maxAttempts; i++)
            {
                if (i > 0) yield return new WaitForSeconds(2f);

                bool pollComplete = false;
                bool pollSuccess = false;
                string relayCode = "";
                bool shouldBreak = false;

                PollLobbyForRelayCodeOnce((success, code, error) =>
                {
                    pollSuccess = success;
                    relayCode = code;
                    errorCount = success ? 0 : errorCount + 1;
                    
                    if (success)
                        Debug.Log($"[Lobby] Poll #{i}: RelayCode={(string.IsNullOrEmpty(code) ? "empty" : "received")}");
                    else
                        Debug.LogWarning($"[Lobby] Poll #{i} failed: {error}");
                    
                    // 콜백 내부에서 즉시 체크
                    if (success && !string.IsNullOrEmpty(code))
                    {
                        Debug.Log($"[Lobby] Relay code received! Connecting...");
                        shouldBreak = true;
                    }
                    
                    pollComplete = true;
                });

                yield return new WaitUntil(() => pollComplete);

                if (shouldBreak)
                {
                    OnStatusUpdate?.Invoke("Relay code received!");
                    pollRelayCodeCoroutine = null;
                    NotifyMatchFound();
                    yield break;
                }

                if (errorCount >= 3)
                {
                    OnError?.Invoke("Too many lobby errors");
                    pollRelayCodeCoroutine = null;
                    _ = LeaveLobbyAsync();
                    yield break;
                }
            }

            OnError?.Invoke("Timeout waiting for relay code");
            pollRelayCodeCoroutine = null;
            _ = LeaveLobbyAsync();
        }

        private async void PollLobbyOnce(Action<bool, int, string> callback)
        {
            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                callback?.Invoke(true, currentLobby.Players.Count, null);
            }
            catch (Exception ex)
            {
                callback?.Invoke(false, 0, ex.Message);
            }
        }

        private async void PollLobbyForRelayCodeOnce(Action<bool, string, string> callback)
        {
            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                string code = currentLobby.Data?.TryGetValue(KEY_RELAY_CODE, out var data) == true ? data.Value : "";
                callback?.Invoke(true, code, null);
            }
            catch (Exception ex)
            {
                callback?.Invoke(false, "", ex.Message);
            }
        }

        #endregion

        #region Helpers

        private void NotifyMatchFound()
        {
            string myId = AuthenticationService.Instance.PlayerId;
            string opponentId = "";

            foreach (var player in currentLobby.Players)
            {
                if (player.Id != myId)
                {
                    opponentId = player.Id;
                    break;
                }
            }

            string relayCode = currentLobby.Data?.TryGetValue(KEY_RELAY_CODE, out var data) == true ? data.Value : "";

            OnMatchFound?.Invoke(new LobbyMatchResult
            {
                isHost = isHost,
                lobbyId = currentLobby.Id,
                relayJoinCode = relayCode,
                opponentId = opponentId
            });
        }

        private async Task SendHeartbeat()
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch { }
        }

        private void OnDestroy() => _ = LeaveLobbyAsync();
        private void OnApplicationQuit() => _ = LeaveLobbyAsync();

        #endregion
    }
}
