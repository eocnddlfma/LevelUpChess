using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Unity Lobby를 사용한 자동 매치메이킹
/// 버튼 한 번으로: 빈 방이 있으면 참가, 없으면 새 방 생성
/// </summary>
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
            Debug.LogError($"[UnityLobby] QuickMatch error: {ex}");
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

            var response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[UnityLobby] Query result: {response.Results.Count} lobbies found");
            
            return response.Results.Count > 0 ? response.Results[0] : null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] FindAvailableLobby error: {ex}");
            return null;
        }
    }

    private async Task CreateLobbyAsync()
    {
        try
        {
            Debug.Log("[UnityLobby] Creating lobby...");
            OnStatusUpdate?.Invoke("Creating lobby...");
            
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[UnityLobby] Creating lobby for player: {playerId}");

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

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            isHost = true;
            heartbeatTimer = heartbeatInterval;

            Debug.Log($"[UnityLobby] Lobby created: {currentLobby.Id}, Code: {currentLobby.LobbyCode}");
            OnStatusUpdate?.Invoke("Waiting for opponent...");
            
            await WaitForOpponentAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] CreateLobby error: {ex}");
            throw;
        }
    }

    private async Task JoinLobbyAsync(Lobby lobby)
    {
        try
        {
            Debug.Log($"[UnityLobby] Joining lobby: {lobby.Id}");
            OnStatusUpdate?.Invoke("Joining lobby...");
            
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player { Data = new Dictionary<string, PlayerDataObject> { } }
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
            isHost = false;

            Debug.Log($"[UnityLobby] Joined lobby: {currentLobby.Id}");
            OnStatusUpdate?.Invoke("Waiting for relay code...");
            
            await WaitForRelayCodeAsync();
        }
        catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyFull)
        {
            Debug.LogWarning("[UnityLobby] Lobby is full, retrying...");
            await QuickMatchAsync();
        }
        catch (LobbyServiceException ex) when (ex.Message.Contains("already a member"))
        {
            Debug.LogWarning("[UnityLobby] Already a member of this lobby, using existing connection...");
            // 이미 로비에 있으면 현재 로비로 진행
            currentLobby = lobby;
            isHost = false;
            await WaitForRelayCodeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] JoinLobby error: {ex}");
            throw;
        }
    }

    private async Task WaitForOpponentAsync()
    {
        for (int i = 0; i < 60; i++)  // 120초 대기 (3초 간격)
        {
            await Task.Delay(3000);  // 3초마다 체크 (레이트 제한 방지)
            
            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                if (currentLobby.Players.Count >= maxPlayers)
                {
                    Debug.Log("[UnityLobby] Opponent joined!");
                    NotifyMatchFound();
                    return;
                }

                if (i % 10 == 0)
                    OnStatusUpdate?.Invoke($"Waiting for opponent... ({i * 3}s)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityLobby] WaitForOpponent poll error: {ex.Message}");
            }
        }

        Debug.LogError("[UnityLobby] WaitForOpponent timeout");
        OnError?.Invoke("Timeout waiting for opponent");
        await LeaveLobbyAsync();
    }

    private async Task WaitForRelayCodeAsync()
    {
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(2000);
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            if (currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData) && 
                !string.IsNullOrEmpty(relayData.Value))
            {
                NotifyMatchFound();
                return;
            }
        }

        OnError?.Invoke("Failed to receive relay code");
        await LeaveLobbyAsync();
    }

    private void NotifyMatchFound()
    {
        string opponentId = "";
        string myId = AuthenticationService.Instance.PlayerId;

        foreach (var player in currentLobby.Players)
        {
            if (player.Id != myId)
            {
                opponentId = player.Id;
                break;
            }
        }

        string relayCode = "";
        currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData);
        if (relayData != null)
            relayCode = relayData.Value;

        OnMatchFound?.Invoke(new LobbyMatchResult
        {
            isHost = isHost,
            lobbyId = currentLobby.Id,
            relayJoinCode = relayCode,
            opponentId = opponentId
        });
    }

    public async Task UpdateRelayCodeAsync(string joinCode)
    {
        if (currentLobby == null || !isHost) return;

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

    public string GetRelayJoinCode()
    {
        if (currentLobby?.Data != null && 
            currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData))
            return relayData.Value;
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
