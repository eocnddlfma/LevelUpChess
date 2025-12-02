using UnityEngine;
using Unity.Netcode;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.UI;
using LevelUpChess.Pieces;

namespace LevelUpChess.Managers
{
    public class NetworkGameManager : NetworkBehaviour
    {

    [SerializeField] private BoardGenerator boardGenerator;
    
    private bool hasRotatedCameraForBlack = false;
    
    public ChessPiece LastMovedPiece { get; private set; }
    public Vector2Int LastMoveFrom { get; private set; }
    public Vector2Int LastMoveTo { get; private set; }
    
    private NetworkVariable<Team> currentTurnTeam = new(Team.White, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isGameOverNetwork = new(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> hostWantsRematch = new(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<bool> clientWantsRematch = new(false,
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
        if (ServiceLocator.Has<NetworkGameManager>())
        {
            Destroy(gameObject);
            return;
        }
        ServiceLocator.Register(this);
    }

    public void SetTeamFromNetwork(bool isHost)
    {
        LocalPlayerTeam = isHost ? Team.White : Team.Black;
        
        if (LocalPlayerTeam == Team.Black && !hasRotatedCameraForBlack)
            StartCoroutine(WaitAndRotateForBlack(LocalPlayerTeam));
    }

    private System.Collections.IEnumerator WaitAndRotateForBlack(Team localTeam)
    {
        if (localTeam != Team.Black) yield break;

        float timeout = 3f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (FindObjectsByType<ChessPiece>(FindObjectsSortMode.None).Length > 0)
            {
                RotateBoard();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        Debug.LogWarning("[NetworkGameManager] Timeout waiting for pieces");
    }

    private void RotateBoard()
    {
        RotateCameras();
        RotatePieces();
        hasRotatedCameraForBlack = true;
    }

    private void RotateCameras()
    {
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (cam.enabled)
                cam.transform.rotation = Quaternion.Euler(0, 0, 180);
    }

    private void RotatePieces()
    {
        foreach (ChessPiece piece in FindObjectsByType<ChessPiece>(FindObjectsSortMode.None))
        {
            SpriteRenderer spriteRenderer = piece.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        currentTurnTeam.OnValueChanged += OnTurnChanged;
        isGameOverNetwork.OnValueChanged += OnGameOverChanged;
        
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        Bus<TurnChangedEvent>.Raise(new TurnChangedEvent { NewTeam = currentTurnTeam.Value });
        ValidateComponents();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentTurnTeam.OnValueChanged -= OnTurnChanged;
        isGameOverNetwork.OnValueChanged -= OnGameOverChanged;
        
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        NotifyPlayerDisconnectedClientRpc(clientId);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyPlayerDisconnectedClientRpc(ulong disconnectedClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == disconnectedClientId)
            return;

        if (IsServer)
        {
            hostWantsRematch.Value = false;
            clientWantsRematch.Value = false;
        }
        
        var gameMessageUI = ServiceLocator.Get<GameMessageUI>();
        if (gameMessageUI != null)
        {
            gameMessageUI.HideMessage();
            gameMessageUI.ShowMessage("Opponent disconnected", 3f);
        }
    }

    private void OnTurnChanged(Team oldTeam, Team newTeam)
    {
        Bus<TurnChangedEvent>.Raise(new TurnChangedEvent { NewTeam = newTeam });
    }

    private void OnGameOverChanged(bool wasOver, bool isOver)
    {
        if (isOver)
            Debug.Log("[GameManager] Game over");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EndTurnServerRpc()
    {
        if (isGameOverNetwork.Value)
            return;

        currentTurnTeam.Value = currentTurnTeam.Value == Team.White ? Team.Black : Team.White;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetGameOverServerRpc(Team winnerTeam)
    {
        if (isGameOverNetwork.Value)
            return;

        isGameOverNetwork.Value = true;
        NotifyGameOverClientRpc(winnerTeam);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyGameOverClientRpc(Team winnerTeam)
    {
        Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = winnerTeam });
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void VoteRematchServerRpc(RpcParams rpcParams = default)
    {
        bool isHost = rpcParams.Receive.SenderClientId == 0;
        
        if (isHost)
            hostWantsRematch.Value = true;
        else
            clientWantsRematch.Value = true;
        
        if (hostWantsRematch.Value && clientWantsRematch.Value)
        {
            StartRematch();
        }
        else
        {
            NotifyWaitingForOpponentClientRpc(rpcParams.Receive.SenderClientId);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyWaitingForOpponentClientRpc(ulong votedClientId)
    {
        var gameMessageUI = ServiceLocator.Get<GameMessageUI>();
        if (gameMessageUI == null)
            return;

        if (NetworkManager.Singleton.LocalClientId == votedClientId)
        {
            gameMessageUI.ShowMessage("Waiting for opponent...", 0f);
        }
        else
        {
            gameMessageUI.ShowMessage("Opponent wants rematch! Press Replay", 0f);
        }
    }
    
    private void StartRematch()
    {
        hostWantsRematch.Value = false;
        clientWantsRematch.Value = false;
        isGameOverNetwork.Value = false;
        currentTurnTeam.Value = Team.White;
        
        ExecuteRematchClientRpc();
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteRematchClientRpc()
    {
        hasRotatedCameraForBlack = false;
        
        Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = Team.White, IsRematch = true });
        
        var gameMessageUI = ServiceLocator.Get<GameMessageUI>();
        if (gameMessageUI != null)
        {
            gameMessageUI.HideMessage();
            gameMessageUI.ShowMessage("Starting new game!", 2f);
        }
        
        if (boardGenerator != null)
        {
            boardGenerator.GenerateBoard();
            
            if (LocalPlayerTeam == Team.Black)
                StartCoroutine(WaitAndRotateForBlack(LocalPlayerTeam));
        }
    }

    public bool IsLocalPlayerTeam(Team team) => LocalPlayerTeam == team;
    
    public void RecordLastMove(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        LastMovedPiece = piece;
        LastMoveFrom = from;
        LastMoveTo = to;
    }
    
    private void ValidateComponents()
    {
        if (boardGenerator == null)
            boardGenerator = FindFirstObjectByType<BoardGenerator>();
        if (boardGenerator == null)
            Debug.LogError("[NetworkGameManager] No BoardGenerator found");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (ServiceLocator.Get<NetworkGameManager>() == this)
            ServiceLocator.Unregister<NetworkGameManager>();
    }
    }
}
