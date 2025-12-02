using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Pieces;
using LevelUpChess.UI;

namespace LevelUpChess.Managers
{
    public class NetworkMovementManager : NetworkBehaviour
    {
        private ChessPiece selectedPiece;
        private List<Tile> indicatedTiles = new List<Tile>();
        private bool isMoving;

        // Lazy 캐싱: 한 번 가져온 후 재사용
        private NetworkGameManager _networkGameManager;
        private BoardManager _boardManager;
        private GameMessageUI _gameMessageUI;

        private NetworkGameManager NetworkGameManager 
            => _networkGameManager ??= ServiceLocator.Get<NetworkGameManager>();
        private BoardManager BoardManager 
            => _boardManager ??= ServiceLocator.Get<BoardManager>();
        private GameMessageUI GameMessageUI 
            => _gameMessageUI ??= ServiceLocator.Get<GameMessageUI>();

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
            Debug.Log($"[NetworkMovementManager] OnClickableSelected called. " +
                      $"NetworkGameManager: {(NetworkGameManager != null ? "exists" : "NULL")}, " +
                      $"IsGameOver: {NetworkGameManager?.IsGameOver}, " +
                      $"IsMyTurn: {NetworkGameManager?.IsMyTurn}, " +
                      $"LocalPlayerTeam: {NetworkGameManager?.LocalPlayerTeam}, " +
                      $"CurrentTurn: {NetworkGameManager?.CurrentTurn}");
            
            if (NetworkGameManager == null)
            {
                Debug.LogWarning("[NetworkMovementManager] Blocked: NetworkGameManager is NULL");
                return;
            }
            
            if (NetworkGameManager.IsGameOver)
            {
                Debug.LogWarning("[NetworkMovementManager] Blocked: Game is over");
                return;
            }
            
            if (!NetworkGameManager.IsMyTurn)
            {
                Debug.LogWarning($"[NetworkMovementManager] Blocked: Not my turn. CurrentTurn={NetworkGameManager.CurrentTurn}, LocalPlayerTeam={NetworkGameManager.LocalPlayerTeam}");
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
                HandleTileClick(tile);
        }

        private void HandlePieceClick(ChessPiece piece)
        {
            if (selectedPiece == piece)
            {
                ClearSelection();
                return;
            }

            if (selectedPiece != null && indicatedTiles.Contains(piece.CurrentTile))
            {
                RequestMove(piece.CurrentTile);
                return;
            }

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
            Debug.Log($"[NetworkMovementManager] SelectPiece called for {piece?.name}");
            
            if (piece == null || !NetworkGameManager.IsLocalPlayerTeam(piece.Team))
            {
                Debug.LogWarning($"[NetworkMovementManager] Cannot select: piece={piece?.name}, pieceTeam={piece?.Team}, " +
                                 $"LocalPlayerTeam={NetworkGameManager.LocalPlayerTeam}, " +
                                 $"IsLocalPlayerTeam={NetworkGameManager.IsLocalPlayerTeam(piece?.Team ?? Team.White)}");
                ShowMessage("Cannot select opponent's piece");
                return;
            }

            if (selectedPiece != null)
                ClearIndicators();

            selectedPiece = piece;
            List<Move> availableMoves = piece.GetAvailableMoves();
            Debug.Log($"[NetworkMovementManager] Available moves count: {availableMoves.Count}");

            if (availableMoves.Count == 0)
            {
                Debug.LogWarning("[NetworkMovementManager] No available moves!");
                return;
            }

            Debug.Log($"[NetworkMovementManager] CurrentTile: {piece.CurrentTile?.coordinate}");
            piece.CurrentTile.SetHighlight(true);
            indicatedTiles.Add(piece.CurrentTile);

            foreach (var move in availableMoves)
            {
                Tile targetTile = BoardManager.GetTileAt(move.to);
                Debug.Log($"[NetworkMovementManager] Move to {move.to}, targetTile: {(targetTile != null ? targetTile.coordinate.ToString() : "NULL")}");
                if (targetTile == null)
                    continue;

                if (move.isCapture)
                    targetTile.SetAttackable(true);
                else
                    targetTile.SetMoveable(true);

                indicatedTiles.Add(targetTile);
            }

            Bus<PieceSelectedEvent>.Raise(new PieceSelectedEvent { Piece = piece, AvailableMoves = availableMoves });
        }

    private void ClearIndicators()
    {
        foreach (var tile in indicatedTiles)
            if (tile != null)
                tile.ClearIndicators();
        
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

    private void RequestMove(Tile targetTile)
    {
        if (selectedPiece == null || targetTile == null || isMoving)
            return;

        RequestMoveServerRpc(selectedPiece.CurrentTile.coordinate, targetTile.coordinate);
        ClearSelection();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestMoveServerRpc(Vector2Int fromPos, Vector2Int toPos, RpcParams rpcParams = default)
        {
            ChessPiece piece = BoardManager.GetPieceAt(fromPos);
            Tile targetTile = BoardManager.GetTileAt(toPos);

            if (piece == null || targetTile == null)
            {
                Debug.LogError($"[NetworkMovement] Invalid move: piece={piece}, tile={targetTile}");
                return;
            }

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

            ExecuteMoveClientRpc(fromPos, toPos, usedMove);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ExecuteMoveClientRpc(Vector2Int fromPos, Vector2Int toPos, Move usedMove)
        {
            ChessPiece piece = BoardManager.GetPieceAt(fromPos);
            Tile targetTile = BoardManager.GetTileAt(toPos);

            if (piece == null || targetTile == null)
            {
                Debug.LogError($"[NetworkMovement] Move failed: piece={piece}, tile={targetTile}");
                return;
            }

            ExecuteMove(piece, targetTile, usedMove);
        }

    private void ExecuteMove(ChessPiece piece, Tile targetTile, Move usedMove)
    {
        if (isMoving)
            return;

        isMoving = true;
        Vector2Int fromPos = piece.CurrentTile.coordinate;
        Vector2Int toPos = targetTile.coordinate;

        ChessPiece targetPiece = targetTile.OccupyingPiece;
        
        // 적 기물이 있으면 공격
        if (targetPiece != null && targetPiece.Team != piece.Team)
        {
            piece.AttackPiece(targetTile, targetPiece, () =>
            {
                // 공격 결과에 따라 이동했는지 확인
                if (piece.CurrentTile == targetTile)
                {
                    // 적을 죽이고 이동함
                    OnMoveComplete(piece, usedMove, fromPos, toPos);
                }
                else
                {
                    // 적이 살아남아 제자리
                    FinishMove(piece, fromPos, fromPos);
                }
            });
            return;
        }

        // 빈 칸이면 바로 이동
        piece.MoveToTile(targetTile, () => OnMoveComplete(piece, usedMove, fromPos, toPos));
    }

    private void OnMoveComplete(ChessPiece piece, Move usedMove, Vector2Int fromPos, Vector2Int toPos)
    {
        HandleEnPassant(usedMove, piece);

        if (usedMove.isCastling)
            HandleCastling(usedMove, () => FinishMove(piece, fromPos, toPos));
        else
            FinishMove(piece, fromPos, toPos);
    }

        private void FinishMove(ChessPiece piece, Vector2Int fromPos, Vector2Int toPos)
        {
            if (NetworkGameManager != null)
                NetworkGameManager.RecordLastMove(piece, fromPos, toPos);

            if (IsServer && NetworkGameManager != null)
                NetworkGameManager.EndTurnServerRpc();

            isMoving = false;
        }

        private void HandleEnPassant(Move move, ChessPiece killer)
        {
            if (!move.isEnPassant)
                return;

            ChessPiece enPassantPiece = BoardManager.GetPieceAt(move.enPassantCapturePos);
            if (enPassantPiece != null && enPassantPiece.Combat != null)
            {
                // 앙파상은 항상 폰끼리의 공격이므로 공격력으로 대미지 처리
                enPassantPiece.Combat.TakeDamage(killer.AttackPower, killer);
            }
        }

        private void HandleCastling(Move move, System.Action onComplete)
        {
            if (!move.isCastling)
            {
                onComplete?.Invoke();
                return;
            }

            ChessPiece rook = BoardManager.GetPieceAt(move.rookFromPos);
            Tile rookTargetTile = rook != null ? BoardManager.GetTileAt(move.rookToPos) : null;

            if (rook != null && rook.PieceType == PieceType.Rook && rookTargetTile != null)
            {
                rook.MoveToTile(rookTargetTile, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void ShowMessage(string message)
        {
            if (GameMessageUI != null)
                GameMessageUI.ShowMessage(message, 1.5f);
        }
    }
}
