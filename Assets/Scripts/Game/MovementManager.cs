using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Events;

public class MovementManager : MonoBehaviour
{
    private ChessPiece selectedPiece;
    private List<Tile> indicatedTiles = new List<Tile>();
    private bool isMoving = false;  // 중복 이동 방지

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
        if (GameManager.Instance.IsGameOver)
            return;

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
        if (selectedPiece == piece)
        {
            ClearSelection();
            return;
        }

        if (selectedPiece != null && indicatedTiles.Contains(piece.currentTile))
        {
            MovePiece(piece.currentTile);
            return;
        }

        SelectPiece(piece);
    }

    private void HandleTileClick(Tile tile)
    {
        if (selectedPiece != null && indicatedTiles.Contains(tile))
        {
            MovePiece(tile);
            return;
        }

        ClearSelection();
    }

    private void SelectPiece(ChessPiece piece)
    {
        if (piece.team != GameManager.Instance.CurrentTeam)
            return;

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

    public ChessPiece GetSelectedPiece()
    {
        return selectedPiece;
    }

    private void MovePiece(Tile targetTile)
    {
        if (selectedPiece == null || targetTile == null)
            return;

        if (isMoving)  // 중복 이동 방지
            return;

        isMoving = true;

        Vector2Int fromPos = selectedPiece.currentTile.coordinate;
        Vector2Int toPos = targetTile.coordinate;
        Move usedMove = GetMoveInfo(selectedPiece, toPos);

        // 캡처 대상이 있으면 먼저 제거
        ChessPiece capturedPiece = targetTile.OccupyingPiece;
        if (capturedPiece != null && capturedPiece.team != selectedPiece.team)
        {
            capturedPiece.Die();
        }

        HandleCastlingBeforeMove(usedMove);

        Tween moveTween = selectedPiece.MoveToTile(targetTile);

        moveTween.OnComplete(() =>
        {
            HandleEnPassant(usedMove);
            
            // 캐슬링: Rook 애니메이션 완료 대기
            if (usedMove.isCastling)
            {
                HandleCastlingAfterMove(usedMove, () =>
                {
                    CompleteMove(fromPos, toPos);
                });
            }
            else
            {
                CompleteMove(fromPos, toPos);
            }
        });
    }

    private void CompleteMove(Vector2Int fromPos, Vector2Int toPos)
    {
        GameManager.Instance.RecordLastMove(selectedPiece, fromPos, toPos);
        ClearSelection();
        GameManager.Instance.EndTurn();
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

    private void HandleCastling(Move move)
    {
        if (!move.isCastling)
            return;

        ChessPiece rook = BoardManager.Instance.GetPieceAt(move.rookFromPos);
        if (rook != null && rook.pieceType == PieceType.Rook)
        {
            Tile rookTargetTile = BoardManager.Instance.GetTileAt(move.rookToPos);
            if (rookTargetTile != null)
                rook.MoveToTile(rookTargetTile);
        }
    }

    private void HandleCastlingBeforeMove(Move move)
    {
        // 캐슬링 전에 필요한 처리 (현재는 없음)
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

    private Move GetMoveInfo(ChessPiece piece, Vector2Int targetPos)
    {
        List<Move> availableMoves = piece.GetAvailableMoves();
        foreach (var move in availableMoves)
        {
            if (move.to == targetPos)
                return move;
        }
        return new Move();
    }
}