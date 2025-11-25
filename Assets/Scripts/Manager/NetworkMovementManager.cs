using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using Events;

/// <summary>
/// 네트워크 체스 이동을 관리합니다
/// 
/// 동기화 방식:
/// 1. 로컬: 피스 선택, 이동 가능 범위 표시, 애니메이션 (로컬에서만 처리)
/// 2. 동기화: 이동 정보(from, to, moveInfo)만 전송
/// 3. 수신: 받은 정보로 피스 이동 실행
/// 
/// 피스는 NetworkObject가 아니므로 위치 정보만 동기화
/// </summary>
public class NetworkMovementManager : NetworkBehaviour
{
    private ChessPiece selectedPiece;
    private List<Tile> indicatedTiles = new List<Tile>();
    private bool isMoving = false;

    private void OnEnable()
    {
        Bus<ClickableSelectedEvent>.OnEvent += OnClickableSelected;
    }

    private void OnDisable()
    {
        Bus<ClickableSelectedEvent>.OnEvent -= OnClickableSelected;
    }

    private void OnClickableSelected(ClickableSelectedEvent eventData)
    {
        // NetworkGameManager가 없으면 무시 (아직 스폰 안됨)
        if (NetworkGameManager.Instance == null)
        {
            Debug.LogWarning("[NetworkMovement] NetworkGameManager not ready yet");
            return;
        }

        // 게임 오버 체크
        if (NetworkGameManager.Instance.IsGameOver)
            return;

        // 내 턴이 아니면 무시
        if (!NetworkGameManager.Instance.IsMyTurn)
        {
            Debug.Log("[NetworkMovement] Not your turn!");
            if (GameMessageUI.Instance != null)
            {
                GameMessageUI.Instance.ShowMessage("Opponent's turn", 1.5f);
            }
            return;
        }

        ChessPiece piece = eventData.Clickable as ChessPiece;
        if (piece != null)
        {
            HandlePieceClick(piece);
            return;
        }

        Tile tile = eventData.Clickable as Tile;
        if (tile != null)
        {
            HandleTileClick(tile);
            return;
        }
    }

    private void HandlePieceClick(ChessPiece piece)
    {
        // 선택 토글
        if (selectedPiece == piece)
        {
            ClearSelection();
            return;
        }

        // 공격 이동
        if (selectedPiece != null && indicatedTiles.Contains(piece.currentTile))
        {
            RequestMove(piece.currentTile);
            return;
        }

        // 새 피스 선택
        SelectPiece(piece);
    }

    private void HandleTileClick(Tile tile)
    {
        if (selectedPiece != null && indicatedTiles.Contains(tile))
        {
            RequestMove(tile);
            return;
        }

        ClearSelection();
    }

    private void SelectPiece(ChessPiece piece)
    {
        // NetworkGameManager 체크
        if (NetworkGameManager.Instance == null)
            return;

        // 내 팀의 말만 선택 가능
        if (!NetworkGameManager.Instance.IsLocalPlayerTeam(piece.team))
        {
            if (GameMessageUI.Instance != null)
            {
                GameMessageUI.Instance.ShowMessage("Cannot select opponent's piece", 1.5f);
            }
            return;
        }

        if (selectedPiece != null)
            ClearIndicators();

        selectedPiece = piece;
        List<Move> availableMoves = piece.GetAvailableMoves();

        if (availableMoves.Count == 0)
            return;

        piece.currentTile.SetHighlight(true);
        indicatedTiles.Add(piece.currentTile);

        foreach (var move in availableMoves)
        {
            Tile targetTile = BoardManager.Instance.GetTileAt(move.to);
            if (targetTile == null)
                continue;

            if (move.isCapture)
                targetTile.SetAttackable(true);
            else
                targetTile.SetMoveable(true);

            indicatedTiles.Add(targetTile);
        }

        Bus<PieceSelectedEvent>.Raise(new PieceSelectedEvent
        {
            Piece = piece,
            AvailableMoves = availableMoves
        });
    }

    private void ClearIndicators()
    {
        foreach (var tile in indicatedTiles)
        {
            if (tile != null)
                tile.ClearIndicators();
        }
        indicatedTiles.Clear();
    }

    private void ClearSelection()
    {
        if (selectedPiece == null)
            return;

        ClearIndicators();
        selectedPiece = null;
        Bus<SelectionClearedEvent>.Raise(new SelectionClearedEvent());
    }

    /// <summary>
    /// 서버에 이동 요청
    /// </summary>
    private void RequestMove(Tile targetTile)
    {
        if (selectedPiece == null || targetTile == null || isMoving)
            return;

        Vector2Int fromPos = selectedPiece.currentTile.coordinate;
        Vector2Int toPos = targetTile.coordinate;

        // 서버에 이동 요청
        RequestMoveServerRpc(fromPos, toPos);
        
        ClearSelection();
    }

    /// <summary>
    /// 서버에서 이동 유효성 검증 후 모든 클라이언트에 동기화
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveServerRpc(Vector2Int fromPos, Vector2Int toPos, RpcParams rpcParams = default)
    {
        // 서버에서 이동 유효성 검증
        ChessPiece piece = BoardManager.Instance.GetPieceAt(fromPos);
        if (piece == null)
        {
            Debug.LogError($"[NetworkMovement] No piece at {fromPos}");
            return;
        }

        Tile targetTile = BoardManager.Instance.GetTileAt(toPos);
        if (targetTile == null)
        {
            Debug.LogError($"[NetworkMovement] No tile at {toPos}");
            return;
        }

        // 이동 가능한지 확인
        List<Move> availableMoves = piece.GetAvailableMoves();
        Move usedMove = new Move();
        bool isValidMove = false;

        foreach (var move in availableMoves)
        {
            if (move.to == toPos)
            {
                usedMove = move;
                isValidMove = true;
                break;
            }
        }

        if (!isValidMove)
        {
            Debug.LogError($"[NetworkMovement] Invalid move from {fromPos} to {toPos}");
            return;
        }

        // 모든 클라이언트에 이동 실행 명령
        ExecuteMoveClientRpc(fromPos, toPos, usedMove);
    }

    /// <summary>
    /// 모든 클라이언트에서 이동 실행
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteMoveClientRpc(Vector2Int fromPos, Vector2Int toPos, Move usedMove)
    {
        ChessPiece piece = BoardManager.Instance.GetPieceAt(fromPos);
        if (piece == null)
        {
            Debug.LogError($"[NetworkMovement] Client: No piece at {fromPos}");
            return;
        }

        Tile targetTile = BoardManager.Instance.GetTileAt(toPos);
        if (targetTile == null)
        {
            Debug.LogError($"[NetworkMovement] Client: No tile at {toPos}");
            return;
        }

        // 이동 실행
        ExecuteMove(piece, targetTile, usedMove);
    }

    /// <summary>
    /// 실제 이동 로직 (모든 클라이언트에서 동일하게 실행)
    /// 애니메이션, 특수 이동 처리 등 모든 비주얼 효과는 로컬에서 처리
    /// </summary>
    private void ExecuteMove(ChessPiece piece, Tile targetTile, Move usedMove)
    {
        if (isMoving)
            return;

        isMoving = true;

        Vector2Int fromPos = piece.currentTile.coordinate;
        Vector2Int toPos = targetTile.coordinate;

        // 캡처
        ChessPiece capturedPiece = targetTile.OccupyingPiece;
        if (capturedPiece != null && capturedPiece.team != piece.team)
        {
            capturedPiece.Die();
        }

        // 이동 애니메이션
        Tween moveTween = piece.MoveToTile(targetTile);

        moveTween.OnComplete(() =>
        {
            // 특수 이동 처리
            HandleEnPassant(usedMove);

            if (usedMove.isCastling)
            {
                HandleCastlingAfterMove(usedMove, () =>
                {
                    CompleteMove(piece, fromPos, toPos);
                });
            }
            else
            {
                CompleteMove(piece, fromPos, toPos);
            }
        });
    }

    private void CompleteMove(ChessPiece piece, Vector2Int fromPos, Vector2Int toPos)
    {
        // 게임 매니저에 기록
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RecordLastMove(piece, fromPos, toPos);
        }

        // 서버에서만 턴 변경
        if (IsServer)
        {
            NetworkGameManager.Instance.EndTurnServerRpc();
        }

        isMoving = false;
    }

    private void HandleEnPassant(Move move)
    {
        if (!move.isEnPassant)
            return;

        ChessPiece enPassantPiece = BoardManager.Instance.GetPieceAt(move.enPassantCapturePos);
        if (enPassantPiece != null)
            enPassantPiece.Die();
    }

    private void HandleCastlingAfterMove(Move move, System.Action onComplete = null)
    {
        if (!move.isCastling)
        {
            onComplete?.Invoke();
            return;
        }

        ChessPiece rook = BoardManager.Instance.GetPieceAt(move.rookFromPos);
        if (rook != null && rook.pieceType == PieceType.Rook)
        {
            Tile rookTargetTile = BoardManager.Instance.GetTileAt(move.rookToPos);
            if (rookTargetTile != null)
            {
                Tween rookTween = rook.MoveToTile(rookTargetTile);
                rookTween.OnComplete(() => onComplete?.Invoke());
                return;
            }
        }

        onComplete?.Invoke();
    }
}
