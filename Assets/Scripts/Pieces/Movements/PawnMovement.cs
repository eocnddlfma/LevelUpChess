using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PawnMovement", menuName = "Chess/Piece Movement/Pawn")]
public class PawnMovement : PieceMovement
{
    public override List<Move> GetAvailableMoves(ChessPiece piece)
    {
        var moves = new List<Move>();
        if (piece.currentTile == null) 
            return moves;

        int dir = (piece.team == Team.White) ? 1 : -1;
        Vector2Int pos = piece.currentTile.coordinate;

        AddForwardMoves(piece, moves, pos, dir);
        AddCaptureMoves(piece, moves, pos, dir);
        CheckEnPassant(piece, moves, pos, dir);

        return moves;
    }

    private void AddForwardMoves(ChessPiece piece, List<Move> moves, Vector2Int pos, int dir)
    {
        Vector2Int one = new Vector2Int(pos.x, pos.y + dir);
        var oneTile = BoardManager.Instance.GetTileAt(one);

        if (oneTile != null && oneTile.OccupyingPiece == null)
        {
            moves.Add(new Move(pos, one));

            if (!piece.HasMoved)
            {
                Vector2Int two = new Vector2Int(pos.x, pos.y + 2 * dir);
                var twoTile = BoardManager.Instance.GetTileAt(two);
                if (twoTile != null && twoTile.OccupyingPiece == null)
                    moves.Add(new Move(pos, two));
            }
        }
    }

    private void AddCaptureMoves(ChessPiece piece, List<Move> moves, Vector2Int pos, int dir)
    {
        foreach (int dx in new[] { -1, 1 })
        {
            Vector2Int diag = new Vector2Int(pos.x + dx, pos.y + dir);
            var diagTile = BoardManager.Instance.GetTileAt(diag);
            if (diagTile != null && diagTile.OccupyingPiece != null && 
                diagTile.OccupyingPiece.team != piece.team)
            {
                moves.Add(new Move(pos, diag) { isCapture = true });
            }
        }
    }

    private void CheckEnPassant(ChessPiece piece, List<Move> moves, Vector2Int pos, int dir)
    {
        if (GameManager.Instance.LastMovedPiece == null)
            return;

        if (GameManager.Instance.LastMovedPiece.team == piece.team || 
            GameManager.Instance.LastMovedPiece.pieceType != PieceType.Pawn)
            return;

        if (GameManager.Instance.LastMovedPiece.currentTile.coordinate.y != pos.y)
            return;

        Vector2Int lastFrom = GameManager.Instance.LastMoveFrom;
        Vector2Int lastTo = GameManager.Instance.LastMoveTo;

        if (Mathf.Abs(lastTo.y - lastFrom.y) != 2)
            return;

        if (Mathf.Abs(GameManager.Instance.LastMovedPiece.currentTile.coordinate.x - pos.x) != 1)
            return;

        Vector2Int enPassantTarget = new Vector2Int(
            GameManager.Instance.LastMovedPiece.currentTile.coordinate.x, 
            pos.y + dir
        );

        var targetTile = BoardManager.Instance.GetTileAt(enPassantTarget);
        if (targetTile != null && targetTile.OccupyingPiece == null)
        {
            Move enPassantMove = new Move(pos, enPassantTarget)
            {
                isCapture = true,
                isEnPassant = true,
                enPassantCapturePos = GameManager.Instance.LastMovedPiece.currentTile.coordinate
            };
            moves.Add(enPassantMove);
        }
    }
}
