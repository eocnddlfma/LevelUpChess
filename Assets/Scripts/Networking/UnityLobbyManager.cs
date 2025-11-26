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
            
            await WaitForOpponentAsync();
        }
        catch (LobbyServiceException lobbyEx)
        {
            Debug.LogError($"[UnityLobby] HOST: LobbyServiceException creating lobby: {lobbyEx.Message}");
            Debug.LogError($"[UnityLobby] HOST: Reason: {lobbyEx.Reason}");
            OnError?.Invoke($"Lobby creation failed: {lobbyEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLobby] HOST: Exception creating lobby: {ex.GetType().Name} - {ex.Message}");
            Debug.LogError($"[UnityLobby] HOST: Stack: {ex.StackTrace}");
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
            
            await WaitForRelayCodeAsync();
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
            await WaitForRelayCodeAsync();
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

    private async Task WaitForOpponentAsync()
    {
        Debug.Log("[UnityLobby] HOST: Waiting for opponent to join...");
        int maxAttempts = 60;  // 180초 대기 (3초 간격)
        
        for (int i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(3000);  // 3초마다 체크 (레이트 제한 방지)
            
            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                int playerCount = currentLobby.Players.Count;
                
                Debug.Log($"[UnityLobby] HOST: Poll attempt {i+1}/{maxAttempts} - Players: {playerCount}/{maxPlayers}");

                if (playerCount >= maxPlayers)
                {
                    Debug.Log("[UnityLobby] ✓ HOST: Opponent joined!");
                    OnStatusUpdate?.Invoke("Opponent found!");
                    NotifyMatchFound();
                    return;
                }

                if ((i + 1) % 10 == 0)
                    OnStatusUpdate?.Invoke($"Waiting for opponent... ({(i+1) * 3}s)");
            }
            catch (LobbyServiceException lobbyEx)
            {
                Debug.LogError($"[UnityLobby] HOST: LobbyServiceException during poll (attempt {i+1}): {lobbyEx.Message}");
                Debug.LogError($"[UnityLobby] HOST: Reason: {lobbyEx.Reason}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityLobby] HOST: Exception during poll (attempt {i+1}): {ex.GetType().Name} - {ex.Message}");
            }
        }

        Debug.LogError("[UnityLobby] ✗ HOST: Timeout waiting for opponent (180 seconds)");
        OnStatusUpdate?.Invoke("Timeout waiting for opponent");
        OnError?.Invoke("Timeout waiting for opponent");
        await LeaveLobbyAsync();
    }

    private async Task WaitForRelayCodeAsync()
    {
        Debug.Log("[UnityLobby] CLIENT: Waiting for relay code from HOST...");
        int maxAttempts = 30;  // 60초 대기 (2초 간격)
        int attempts = 0;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(2000);
            attempts++;
            
            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                Debug.Log($"[UnityLobby] CLIENT: Lobby refreshed (attempt {attempts}/{maxAttempts})");

                if (currentLobby.Data != null)
                {
                    Debug.Log($"[UnityLobby] CLIENT: Lobby data keys: {string.Join(", ", currentLobby.Data.Keys)}");
                    
                    if (currentLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData))
                    {
                        if (!string.IsNullOrEmpty(relayData.Value))
                        {
                            Debug.Log($"[UnityLobby] ✓ CLIENT: Relay code received!");
                            OnStatusUpdate?.Invoke("Relay code received!");
                            NotifyMatchFound();
                            return;
                        }
                        else
                        {
                            Debug.LogWarning($"[UnityLobby] CLIENT: Relay code key exists but value is empty (attempt {attempts})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[UnityLobby] CLIENT: KEY_RELAY_CODE not found in lobby data (attempt {attempts})");
                    }
                }
                else
                {
                    Debug.LogError("[UnityLobby] CLIENT: currentLobby.Data is null!");
                }
            }
            catch (LobbyServiceException lobbyEx)
            {
                Debug.LogError($"[UnityLobby] CLIENT: LobbyServiceException during poll (attempt {attempts}): {lobbyEx.Message}");
                Debug.LogError($"[UnityLobby] CLIENT: Reason: {lobbyEx.Reason}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityLobby] CLIENT: Exception during poll (attempt {attempts}): {ex.GetType().Name} - {ex.Message}");
                Debug.LogError($"[UnityLobby] CLIENT: Stack: {ex.StackTrace}");
            }
        }

        Debug.LogError("[UnityLobby] ✗ CLIENT: Timeout waiting for relay code from HOST (60 seconds)");
        OnStatusUpdate?.Invoke("Timeout waiting for relay code");
        OnError?.Invoke("Failed to receive relay code from host (timeout)");
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
