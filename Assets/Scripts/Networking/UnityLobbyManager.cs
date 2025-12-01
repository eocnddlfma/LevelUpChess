using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
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

    public event Action<LobbyMatchResult> OnMatchFound;
    public event Action<string> OnError;
    public event Action<string> OnStatusUpdate;

    [System.Serializable]
    public class LobbyMatchResult
    {
        public bool isHost;
        public string lobbyId;
        public string relayJoinCode;
        public string opponentId;
    }

    public bool IsHost => isHost;
    public Lobby CurrentLobby => currentLobby;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (currentLobby != null && isHost)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                heartbeatTimer = heartbeatInterval;
                _ = SendHeartbeat();
            }
        }
    }

    public async Task QuickMatchAsync()
    {
        try
        {
            Debug.Log("[UnityLobby] QuickMatchAsync started");
            await InitializeAsync();
            Debug.Log("[UnityLobby] Initialization complete");
            
            OnStatusUpdate?.Invoke("Searching for available lobbies...");

            var availableLobby = await FindAvailableLobby();
            if (availableLobby != null)
            {
                Debug.Log($"[UnityLobby] Found available lobby: {availableLobby.Id}");
                await JoinLobbyAsync(availableLobby);
            }
            else
            {
                Debug.Log("[UnityLobby] No available lobby, creating new one");
                await CreateLobbyAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] ✗ QUICKMATCH ERROR: {ex.GetType().Name}");
            Debug.LogError($"[UnityLobby] Message: {ex.Message}");
            Debug.LogError($"[UnityLobby] Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[UnityLobby] InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                Debug.LogError($"[UnityLobby] InnerStack: {ex.InnerException.StackTrace}");
            }
            OnError?.Invoke($"Error: {ex.Message}");
        }
    }

    public void CancelMatchmaking()
    {
        _ = LeaveLobbyAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Debug.Log("[UnityLobby] Initializing services...");
            
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("[UnityLobby] UnityServices not initialized, initializing...");
                await UnityServices.InitializeAsync();
                Debug.Log("[UnityLobby] UnityServices initialized");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[UnityLobby] Not signed in, signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UnityLobby] Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.Log($"[UnityLobby] Already signed in as: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] Initialize error: {ex}");
            throw;
        }
    }

    private async Task<Lobby> FindAvailableLobby()
    {
        try
        {
            Debug.Log("[UnityLobby] Finding available lobbies...");
            
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

            Debug.Log("[UnityLobby] Calling LobbyService.QueryLobbiesAsync...");
            var response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[UnityLobby] ✓ Query result: {response.Results.Count} lobbies found");
            
            return response.Results.Count > 0 ? response.Results[0] : null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] ✗ FindAvailableLobby error: {ex.GetType().Name} - {ex.Message}");
            Debug.LogError($"[UnityLobby] Stack: {ex.StackTrace}");
            return null;
        }
    }

    private async Task CreateLobbyAsync()
    {
        try
        {
            Debug.Log("[UnityLobby] HOST: Creating lobby...");
            OnStatusUpdate?.Invoke("Creating lobby...");
            
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[UnityLobby] HOST: PlayerId: {playerId}");

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player { Data = new Dictionary<string, PlayerDataObject> { } },
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") },
                    { KEY_GAME_STARTED, new DataObject(DataObject.VisibilityOptions.Public, "false", DataObject.IndexOptions.S1) }
                }
            };

            Debug.Log("[UnityLobby] HOST: Calling LobbyService.CreateLobbyAsync...");
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            isHost = true;
            heartbeatTimer = heartbeatInterval;

            Debug.Log($"[UnityLobby] ✓ HOST: Lobby created successfully!");
            Debug.Log($"[UnityLobby] HOST: LobbyId: {currentLobby.Id}");
            Debug.Log($"[UnityLobby] HOST: LobbyCode: {currentLobby.LobbyCode}");
            Debug.Log($"[UnityLobby] HOST: Players: {currentLobby.Players.Count}/{maxPlayers}");
            
            OnStatusUpdate?.Invoke("Waiting for opponent...");
            
            Debug.Log("[UnityLobby] HOST: About to call WaitForOpponent...");
            WaitForOpponent();
            Debug.Log("[UnityLobby] HOST: WaitForOpponent started (coroutine-based)");
        }
        catch (LobbyServiceException lobbyEx)
        {
            Debug.LogError($"[UnityLobby] ✗ HOST: LobbyServiceException creating lobby: {lobbyEx.Message}");
            Debug.LogError($"[UnityLobby] HOST: Reason: {lobbyEx.Reason}");
            Debug.LogError($"[UnityLobby] HOST: Stack: {lobbyEx.StackTrace}");
            if (lobbyEx.InnerException != null)
            {
                Debug.LogError($"[UnityLobby] HOST: InnerException: {lobbyEx.InnerException.GetType().Name} - {lobbyEx.InnerException.Message}");
            }
            OnError?.Invoke($"Lobby creation failed: {lobbyEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] ✗ HOST: Exception creating lobby: {ex.GetType().Name} - {ex.Message}");
            Debug.LogError($"[UnityLobby] HOST: Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[UnityLobby] HOST: InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
            OnError?.Invoke($"Error: {ex.Message}");
            throw;
        }
    }

    private async Task JoinLobbyAsync(Lobby lobby)
    {
        try
        {
            Debug.Log($"[UnityLobby] CLIENT: Joining lobby: {lobby.Id}");
            OnStatusUpdate?.Invoke("Joining lobby...");
            
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player { Data = new Dictionary<string, PlayerDataObject> { } }
            };

            Debug.Log("[UnityLobby] CLIENT: Calling LobbyService.JoinLobbyByIdAsync...");
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
            isHost = false;

            Debug.Log($"[UnityLobby] ✓ CLIENT: Joined lobby successfully!");
            Debug.Log($"[UnityLobby] CLIENT: Players: {currentLobby.Players.Count}/{maxPlayers}");
            OnStatusUpdate?.Invoke("Waiting for relay code...");
            
            WaitForRelayCode();
        }
        catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyFull)
        {
            Debug.LogWarning("[UnityLobby] CLIENT: Lobby is full, retrying...");
            OnStatusUpdate?.Invoke("Lobby full, retrying...");
            await QuickMatchAsync();
        }
        catch (LobbyServiceException ex) when (ex.Message.Contains("already a member"))
        {
            Debug.LogWarning("[UnityLobby] CLIENT: Already a member of this lobby");
            currentLobby = lobby;
            isHost = false;
            WaitForRelayCode();
        }
        catch (LobbyServiceException lobbyEx)
        {
            Debug.LogError($"[UnityLobby] CLIENT: LobbyServiceException joining lobby: {lobbyEx.Message}");
            Debug.LogError($"[UnityLobby] CLIENT: Reason: {lobbyEx.Reason}");
            OnError?.Invoke($"Failed to join lobby: {lobbyEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] CLIENT: Exception joining lobby: {ex.GetType().Name} - {ex.Message}");
            Debug.LogError($"[UnityLobby] CLIENT: Stack: {ex.StackTrace}");
            OnError?.Invoke($"Error: {ex.Message}");
            throw;
        }
    }

    private void WaitForOpponent()
    {
        Debug.Log("[UnityLobby] HOST: ========== WaitForOpponent START ==========");
        Debug.Log("[UnityLobby] HOST: Waiting for opponent to join...");
        Debug.Log($"[UnityLobby] HOST: Lobby ID = {currentLobby.Id}");
        Debug.Log($"[UnityLobby] HOST: Current player count = {currentLobby.Players.Count}");
        
        // 코루틴으로 폴링 시작 (WebGL에서 Task.Delay가 작동 안 하므로)
        Debug.Log("[UnityLobby] HOST: Starting coroutine-based polling...");
        StartCoroutine(PollForOpponentCoroutine());
        
        // 코루틴이 완료될 때까지 대기하지 않고 즉시 반환
        // 코루틴에서 NotifyMatchFound() 호출
    }
    
    private IEnumerator PollForOpponentCoroutine()
    {
        Debug.Log("[UnityLobby] HOST: PollForOpponentCoroutine started");
        
        int maxAttempts = 60;
        int errorCount = 0;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            Debug.Log($"[UnityLobby] HOST: Poll attempt {i+1}/{maxAttempts}");
            
            // Unity의 WaitForSeconds 사용 (WebGL에서 안정적)
            if (i > 0)
            {
                Debug.Log($"[UnityLobby] HOST: Waiting 3 seconds...");
                yield return new WaitForSeconds(3f);
                Debug.Log($"[UnityLobby] HOST: Wait complete, polling now");
            }
            
            // GetLobbyAsync 호출 (비동기를 동기처럼 처리)
            bool pollComplete = false;
            bool pollSuccess = false;
            int playerCount = 0;
            
            // 비동기 작업을 시작
            PollLobbyOnce((success, count, error) =>
            {
                pollSuccess = success;
                playerCount = count;
                if (!success)
                {
                    errorCount++;
                    Debug.LogError($"[UnityLobby] HOST: Poll error: {error}");
                }
                else
                {
                    errorCount = 0;
                    Debug.Log($"[UnityLobby] HOST: Poll result - Players: {count}");
                }
                pollComplete = true;
            });
            
            // 폴링 완료 대기 (WebGL에서 async 호출이 느릴 수 있으므로 충분한 시간 부여)
            float waitTime = 0;
            while (!pollComplete && waitTime < 20f)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }
            
            if (!pollComplete)
            {
                Debug.LogWarning($"[UnityLobby] HOST: Poll timeout at attempt {i+1} (waited {waitTime:F1}s)");
                errorCount++;
            }
            else if (pollSuccess && playerCount >= maxPlayers)
            {
                Debug.Log($"[UnityLobby] ✓ HOST: OPPONENT FOUND! Players: {playerCount}/{maxPlayers}");
                OnStatusUpdate?.Invoke("Opponent found!");
                NotifyMatchFound();
                yield break;
            }
            
            if (errorCount >= 3)
            {
                Debug.LogError("[UnityLobby] HOST: Too many errors, aborting");
                OnError?.Invoke("Too many lobby errors");
                _ = LeaveLobbyAsync();
                yield break;
            }
            
            if ((i + 1) % 5 == 0)
                OnStatusUpdate?.Invoke($"Waiting for opponent... ({(i+1) * 3}s)");
        }
        
        Debug.LogError("[UnityLobby] ✗ HOST: TIMEOUT - No opponent found after 180 seconds");
        OnStatusUpdate?.Invoke("Timeout waiting for opponent");
        OnError?.Invoke("Timeout waiting for opponent");
        _ = LeaveLobbyAsync();
    }
    
    private async void PollLobbyOnce(Action<bool, int, string> callback)
    {
        bool callbackInvoked = false;
        try
        {
            Debug.Log("[UnityLobby] HOST: PollLobbyOnce - Starting API call...");
            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            Debug.Log("[UnityLobby] HOST: PollLobbyOnce - API call completed");
            currentLobby = lobby;
            
            foreach (var player in currentLobby.Players)
            {
                Debug.Log($"[UnityLobby] HOST:   - Player: {player.Id}");
            }
            
            callbackInvoked = true;
            callback?.Invoke(true, currentLobby.Players.Count, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] HOST: PollLobbyOnce error: {ex.Message}");
            if (!callbackInvoked)
            {
                callbackInvoked = true;
                callback?.Invoke(false, 0, ex.Message);
            }
        }
    }

    private void WaitForRelayCode()
    {
        Debug.Log("[UnityLobby] CLIENT: ========== WaitForRelayCode START ==========");
        Debug.Log("[UnityLobby] CLIENT: Waiting for relay code from HOST...");
        Debug.Log($"[UnityLobby] CLIENT: Lobby ID = {currentLobby.Id}");
        
        // 코루틴으로 폴링 시작 (WebGL에서 Task.Delay가 작동 안 하므로)
        Debug.Log("[UnityLobby] CLIENT: Starting coroutine-based polling...");
        StartCoroutine(PollForRelayCodeCoroutine());
        
        // 코루틴이 완료될 때까지 대기하지 않고 즉시 반환
        // 코루틴에서 NotifyMatchFound() 호출
    }
    
    private IEnumerator PollForRelayCodeCoroutine()
    {
        Debug.Log("[UnityLobby] CLIENT: PollForRelayCodeCoroutine started");
        
        int maxAttempts = 30;  // 60초 (2초 간격)
        int errorCount = 0;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            Debug.Log($"[UnityLobby] CLIENT: Poll attempt {i+1}/{maxAttempts}");
            
            // Unity의 WaitForSeconds 사용
            if (i > 0)
            {
                Debug.Log($"[UnityLobby] CLIENT: Waiting 2 seconds...");
                yield return new WaitForSeconds(2f);
                Debug.Log($"[UnityLobby] CLIENT: Wait complete, polling now");
            }
            
            // GetLobbyAsync 호출
            bool pollComplete = false;
            bool pollSuccess = false;
            string relayCode = "";
            
            // 비동기 작업 시작
            PollLobbyForRelayCodeOnce((success, code, error) =>
            {
                pollSuccess = success;
                relayCode = code;
                if (!success)
                {
                    errorCount++;
                    Debug.LogError($"[UnityLobby] CLIENT: Poll error: {error}");
                }
                else
                {
                    errorCount = 0;
                    if (!string.IsNullOrEmpty(code))
                    {
                        Debug.Log($"[UnityLobby] CLIENT: Relay code found!");
                    }
                    else
                    {
                        Debug.Log($"[UnityLobby] CLIENT: Lobby polled but no relay code yet");
                    }
                }
                pollComplete = true;
            });
            
            // 폴링 완료 대기 (WebGL에서 async 호출이 느릴 수 있으므로 충분한 시간 부여)
            float waitTime = 0;
            while (!pollComplete && waitTime < 20f)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }
            
            if (!pollComplete)
            {
                Debug.LogWarning($"[UnityLobby] CLIENT: Poll timeout at attempt {i+1} (waited {waitTime:F1}s)");
                errorCount++;
            }
            else if (pollSuccess && !string.IsNullOrEmpty(relayCode))
            {
                Debug.Log($"[UnityLobby] ✓ CLIENT: RELAY CODE RECEIVED!");
                OnStatusUpdate?.Invoke("Relay code received!");
                NotifyMatchFound();
                yield break;
            }
            
            if (errorCount >= 3)
            {
                Debug.LogError("[UnityLobby] CLIENT: Too many errors, aborting");
                OnError?.Invoke("Too many lobby errors");
                _ = LeaveLobbyAsync();
                yield break;
            }
        }
        
        Debug.LogError("[UnityLobby] ✗ CLIENT: TIMEOUT - No relay code received after 60 seconds");
        OnStatusUpdate?.Invoke("Timeout waiting for relay code");
        OnError?.Invoke("Failed to receive relay code from host (timeout)");
        _ = LeaveLobbyAsync();
    }
    
    private async void PollLobbyForRelayCodeOnce(Action<bool, string, string> callback)
    {
        bool callbackInvoked = false;
        try
        {
            Debug.Log("[UnityLobby] CLIENT: PollLobbyForRelayCodeOnce - Starting API call...");
            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            Debug.Log("[UnityLobby] CLIENT: PollLobbyForRelayCodeOnce - API call completed");
            currentLobby = lobby;
            
            Debug.Log($"[UnityLobby] CLIENT: Poll result - Lobby data keys: {(lobby.Data != null ? string.Join(", ", lobby.Data.Keys) : "null")}");
            
            if (lobby.Data != null && lobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData))
            {
                string code = relayData.Value;
                Debug.Log($"[UnityLobby] CLIENT: RelayCode from lobby: {(string.IsNullOrEmpty(code) ? "(empty)" : code)}");
                callbackInvoked = true;
                callback?.Invoke(true, code, null);
            }
            else
            {
                Debug.Log("[UnityLobby] CLIENT: RelayCode not found in lobby data yet");
                callbackInvoked = true;
                callback?.Invoke(true, "", null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] CLIENT: PollLobbyForRelayCodeOnce error: {ex.Message}");
            if (!callbackInvoked)
            {
                callbackInvoked = true;
                callback?.Invoke(false, "", ex.Message);
            }
        }
    }

    private void NotifyMatchFound()
    {
        try
        {
            Debug.Log("[UnityLobby] NotifyMatchFound: START");
            string opponentId = "";
            string myId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[UnityLobby] NotifyMatchFound: myId = {myId}");

            foreach (var player in currentLobby.Players)
            {
                Debug.Log($"[UnityLobby] NotifyMatchFound: Checking player {player.Id}");
                if (player.Id != myId)
                {
                    opponentId = player.Id;
                    Debug.Log($"[UnityLobby] NotifyMatchFound: Found opponent = {opponentId}");
                    break;
                }
            }

            string relayCode = "";
            currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData);
            if (relayData != null)
                relayCode = relayData.Value;
            
            Debug.Log($"[UnityLobby] NotifyMatchFound: relayCode = {relayCode}");

            Debug.Log($"[UnityLobby] NotifyMatchFound: Invoking OnMatchFound event with isHost={isHost}, opponentId={opponentId}");
            OnMatchFound?.Invoke(new LobbyMatchResult
            {
                isHost = isHost,
                lobbyId = currentLobby.Id,
                relayJoinCode = relayCode,
                opponentId = opponentId
            });
            Debug.Log("[UnityLobby] NotifyMatchFound: Event invoked successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] ✗ NotifyMatchFound error: {ex.GetType().Name} - {ex.Message}");
            Debug.LogError($"[UnityLobby] Stack: {ex.StackTrace}");
            throw;
        }
    }

    public async Task UpdateRelayCodeAsync(string joinCode)
    {
        if (currentLobby == null)
        {
            Debug.LogError("[UnityLobby] Cannot update relay code: currentLobby is null");
            return;
        }

        if (!isHost)
        {
            Debug.LogWarning("[UnityLobby] Only host can update relay code");
            return;
        }

        try
        {
            Debug.Log($"[UnityLobby] Updating lobby with relay code: {joinCode}");
            
            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
                    { KEY_GAME_STARTED, new DataObject(DataObject.VisibilityOptions.Public, "true", DataObject.IndexOptions.S1) }
                }
            };

            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            Debug.Log($"[UnityLobby] ✓ Successfully updated lobby with relay code");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"[UnityLobby] ✗ Failed to update relay code - LobbyServiceException: {ex.Message}");
            Debug.LogError($"[UnityLobby] Reason: {ex.Reason}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] ✗ Failed to update relay code - {ex.GetType().Name}: {ex.Message}");
            Debug.LogError($"[UnityLobby] Stack: {ex.StackTrace}");
            throw;
        }
    }

    public string GetRelayJoinCode()
    {
        if (currentLobby == null)
        {
            Debug.LogWarning("[UnityLobby] GetRelayJoinCode: currentLobby is null");
            return null;
        }

        if (currentLobby.Data == null)
        {
            Debug.LogWarning("[UnityLobby] GetRelayJoinCode: currentLobby.Data is null");
            return null;
        }

        if (currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData))
        {
            string code = relayData.Value;
            Debug.Log($"[UnityLobby] Retrieved relay code: {(string.IsNullOrEmpty(code) ? "[EMPTY]" : code.Substring(0, Math.Min(10, code.Length)) + "...")}");
            return code;
        }

        Debug.LogWarning($"[UnityLobby] KEY_RELAY_CODE not found in lobby data. Available keys: {string.Join(", ", currentLobby.Data.Keys)}");
        return null;
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
    }
}
