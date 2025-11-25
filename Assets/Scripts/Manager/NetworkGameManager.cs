using UnityEngine;
using Unity.Netcode;
using Events;

/// <summary>
/// 네트워크 게임 상태를 관리하고 플레이어 팀을 할당합니다
/// Host = White, Client = Black
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    // 로컬 게임 컴포넌트들
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private InputManager inputManager;
    
    // 마지막 이동 기록
    private ChessPiece lastMovedPiece;
    private Vector2Int lastMoveFrom;
    private Vector2Int lastMoveTo;
    
    // 카메라 회전 상태 추적
    private bool hasRotatedCameraForBlack = false;
    
    public ChessPiece LastMovedPiece => lastMovedPiece;
    public Vector2Int LastMoveFrom => lastMoveFrom;
    public Vector2Int LastMoveTo => lastMoveTo;
    
    private NetworkVariable<Team> currentTurnTeam = new NetworkVariable<Team>(
        Team.White, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isGameOverNetwork = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 리매치 투표
    private NetworkVariable<bool> hostWantsRematch = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<bool> clientWantsRematch = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public Team LocalPlayerTeam { get; private set; }
    public Team OpponentTeam => LocalPlayerTeam == Team.White ? Team.Black : Team.White;
    public Team CurrentTurn => currentTurnTeam.Value;
    public bool IsMyTurn => CurrentTurn == LocalPlayerTeam;
    public bool IsGameOver => isGameOverNetwork.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// ChessNetworkManager로부터 팀 정보를 설정
    /// </summary>
    public void SetTeamFromNetwork(bool isHost)
    {
        LocalPlayerTeam = isHost ? Team.White : Team.Black;
        Debug.Log($"[NetworkGameManager] SetTeamFromNetwork - IsHost: {isHost}, LocalPlayerTeam: {LocalPlayerTeam}");
        
        // 검정 팀이면 카메라 180도 회전 (한 번만 실행)
        if (LocalPlayerTeam == Team.Black && !hasRotatedCameraForBlack)
        {
            // 체스피스가 생성될 때까지 대기 후 회전
            StartCoroutine(WaitAndRotateForBlack());
        }
    }
    
    /// <summary>
    /// 체스피스 생성을 기다린 후 회전
    /// </summary>
    private System.Collections.IEnumerator WaitAndRotateForBlack()
    {
        Debug.Log("[NetworkGameManager] Waiting for chess pieces to spawn...");
        
        // 체스피스가 생성될 때까지 대기 (최대 3초)
        float timeout = 3f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            ChessPiece[] pieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            if (pieces.Length > 0)
            {
                Debug.Log($"[NetworkGameManager] Found {pieces.Length} chess pieces, rotating now");
                RotateCameraForBlack();
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        Debug.LogWarning("[NetworkGameManager] Timeout waiting for chess pieces");
    }

    /// <summary>
    /// 검정 팀용 카메라 및 체스피스 회전 (상하 반전)
    /// </summary>
    private void RotateCameraForBlack()
    {
        // 모든 카메라 찾아서 회전
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Debug.Log($"[NetworkGameManager] Found {allCameras.Length} cameras");
        
        int rotatedCount = 0;
        foreach (Camera cam in allCameras)
        {
            Debug.Log($"[NetworkGameManager] Camera: {cam.name}, Tag: {cam.tag}, Enabled: {cam.enabled}");
            
            // 활성화된 모든 카메라 회전
            if (cam.enabled)
            {
                cam.transform.rotation = Quaternion.Euler(0, 0, 180);
                Debug.Log($"[NetworkGameManager] ✓ Rotated camera: {cam.name}");
                rotatedCount++;
            }
        }
        
        Debug.Log($"[NetworkGameManager] Total {rotatedCount} cameras rotated for Black team");
        
        // 모든 체스피스 180도 회전
        RotateAllChessPieces();
        
        // 회전 완료 표시
        hasRotatedCameraForBlack = true;
    }
    
    /// <summary>
    /// 모든 체스피스 이미지를 180도 회전 (Black 팀 시점)
    /// </summary>
    private void RotateAllChessPieces()
    {
        ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        
        foreach (ChessPiece piece in allPieces)
        {
            // SpriteRenderer만 180도 회전
            SpriteRenderer spriteRenderer = piece.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 180);
            }
        }
        
        Debug.Log($"[NetworkGameManager] Rotated {allPieces.Length} chess pieces sprites for Black team");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"[NetworkGameManager] OnNetworkSpawn called - IsHost: {IsHost}");
        
        // 턴 변경 이벤트 구독
        currentTurnTeam.OnValueChanged += OnTurnChanged;
        isGameOverNetwork.OnValueChanged += OnGameOverChanged;
        
        // 클라이언트 연결 해제 감지
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        // 초기 턴 발생
        Bus<TurnChangedEvent>.Raise(new TurnChangedEvent { NewTeam = currentTurnTeam.Value });
        
        // 로컬 컴포넌트 찾기
        ValidateComponents();
        
        Debug.Log($"[NetworkGameManager] Setup complete - LocalPlayerTeam will be set by ChessNetworkManager");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentTurnTeam.OnValueChanged -= OnTurnChanged;
        isGameOverNetwork.OnValueChanged -= OnGameOverChanged;
        
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    
    /// <summary>
    /// 클라이언트 연결 해제 처리
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkGameManager] Client {clientId} disconnected");
        
        // 모든 플레이어에게 알림
        NotifyPlayerDisconnectedClientRpc(clientId);
    }
    
    /// <summary>
    /// 플레이어 연결 해제 알림
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyPlayerDisconnectedClientRpc(ulong disconnectedClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != disconnectedClientId)
        {
            Debug.Log("[NetworkGameManager] Opponent disconnected");
            
            // 리매치 투표 리셋
            if (IsServer)
            {
                hostWantsRematch.Value = false;
                clientWantsRematch.Value = false;
            }
            
            if (GameMessageUI.Instance != null)
            {
                GameMessageUI.Instance.HideMessage(); // 대기 메시지 제거
                GameMessageUI.Instance.ShowMessage("Opponent disconnected", 3f);
            }
        }
    }

    private void OnTurnChanged(Team oldTeam, Team newTeam)
    {
        Debug.Log($"[NetworkGameManager] Turn changed: {oldTeam} -> {newTeam}");
        Bus<TurnChangedEvent>.Raise(new TurnChangedEvent { NewTeam = newTeam });
    }

    private void OnGameOverChanged(bool wasOver, bool isOver)
    {
        // 게임 오버 이벤트는 NotifyGameOverClientRpc에서 처리됨
        // 여기서는 상태 변화만 로그
        if (isOver)
        {
            Debug.Log($"[NetworkGameManager] Game over state changed to: {isOver}");
        }
    }

    /// <summary>
    /// 서버에서 턴을 종료하고 다음 턴으로 넘김
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EndTurnServerRpc()
    {
        if (isGameOverNetwork.Value)
            return;

        currentTurnTeam.Value = currentTurnTeam.Value == Team.White ? Team.Black : Team.White;
        Debug.Log($"[NetworkGameManager] Server changed turn to {currentTurnTeam.Value}");
    }

    /// <summary>
    /// 게임 오버 상태를 서버에 알림
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetGameOverServerRpc(Team winnerTeam)
    {
        if (isGameOverNetwork.Value)
            return;

        isGameOverNetwork.Value = true;
        Debug.Log($"[NetworkGameManager] Game Over! Winner: {winnerTeam}");
        
        // 모든 클라이언트에 게임 오버 알림
        NotifyGameOverClientRpc(winnerTeam);
    }

    /// <summary>
    /// 모든 클라이언트에 게임 오버를 알림
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyGameOverClientRpc(Team winnerTeam)
    {
        Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = winnerTeam });
    }

    /// <summary>
    /// 리매치 투표 (각 플레이어가 호출)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void VoteRematchServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Host (clientId 0)인지 Client인지 확인
        if (clientId == 0) // Host
        {
            hostWantsRematch.Value = true;
            Debug.Log("[NetworkGameManager] Host voted for rematch");
        }
        else // Client
        {
            clientWantsRematch.Value = true;
            Debug.Log("[NetworkGameManager] Client voted for rematch");
        }
        
        // 양쪽 모두 동의했는지 확인
        if (hostWantsRematch.Value && clientWantsRematch.Value)
        {
            Debug.Log("[NetworkGameManager] Both players agreed - starting rematch!");
            StartRematch();
        }
        else
        {
            // 상대방 대기 중 알림
            NotifyWaitingForOpponentClientRpc(clientId);
        }
    }
    
    /// <summary>
    /// 상대방 대기 중 알림
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyWaitingForOpponentClientRpc(ulong votedClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == votedClientId)
        {
            Debug.Log("[NetworkGameManager] Waiting for opponent to accept rematch...");
            if (GameMessageUI.Instance != null)
            {
                GameMessageUI.Instance.ShowMessage("Waiting for opponent's decision...", 0f);
            }
        }
        else
        {
            // 상대방에게 리매치 신청이 왔다고 알림
            if (GameMessageUI.Instance != null)
            {
                GameMessageUI.Instance.ShowMessage("Opponent wants a rematch! Press Replay to accept", 0f);
            }
        }
    }
    
    /// <summary>
    /// 리매치 실행
    /// </summary>
    private void StartRematch()
    {
        // 투표 리셋
        hostWantsRematch.Value = false;
        clientWantsRematch.Value = false;
        
        // 게임 상태 리셋
        isGameOverNetwork.Value = false;
        currentTurnTeam.Value = Team.White;
        
        // 모든 클라이언트에게 리매치 실행 명령
        ExecuteRematchClientRpc();
    }
    
    /// <summary>
    /// 모든 클라이언트에서 게임 리셋 실행
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteRematchClientRpc()
    {
        Debug.Log("[NetworkGameManager] Executing rematch - resetting board");
        
        // 카메라 회전 플래그 리셋
        hasRotatedCameraForBlack = false;
        
        // GameOver UI 숨김 (WinnerTeam은 리매치에서 의미 없으므로 White 사용)
        Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = Team.White, IsRematch = true });
        
        // 대기 메시지 숨김
        if (GameMessageUI.Instance != null)
        {
            GameMessageUI.Instance.HideMessage();
            GameMessageUI.Instance.ShowMessage("Starting new game!", 2f);
        }
        
        // 보드 재생성
        if (boardGenerator != null)
        {
            boardGenerator.GenerateBoard();
            
            // Black 팀이면 다시 회전 실행
            if (LocalPlayerTeam == Team.Black)
            {
                StartCoroutine(WaitAndRotateForBlack());
            }
        }
        else
        {
            Debug.LogError("[NetworkGameManager] BoardGenerator not found!");
        }
    }

    /// <summary>
    /// 로컬 플레이어가 특정 팀인지 확인
    /// </summary>
    public bool IsLocalPlayerTeam(Team team)
    {
        return LocalPlayerTeam == team;
    }
    
    /// <summary>
    /// 마지막 이동 기록
    /// </summary>
    public void RecordLastMove(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        lastMovedPiece = piece;
        lastMoveFrom = from;
        lastMoveTo = to;
    }
    
    /// <summary>
    /// 필수 컴포넌트 검증
    /// </summary>
    private void ValidateComponents()
    {
        if (boardGenerator == null)
            boardGenerator = FindFirstObjectByType<BoardGenerator>();
        if (boardGenerator == null)
            Debug.LogError("[NetworkGameManager] No BoardGenerator found");

        if (inputManager == null)
            inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
            Debug.LogError("[NetworkGameManager] No InputManager found");
        
        var networkMovement = FindFirstObjectByType<NetworkMovementManager>();
        if (networkMovement == null)
            Debug.LogError("[NetworkGameManager] No NetworkMovementManager found");
    }
}
