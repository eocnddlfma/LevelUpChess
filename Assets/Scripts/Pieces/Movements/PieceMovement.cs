using System.Collections.Generic;
using UnityEngine;

public abstract class PieceMovement : ScriptableObject
{
    public abstract List<Move> GetAvailableMoves(ChessPiece piece);

    protected List<Move> GetSlidingMoves(ChessPiece piece, Vector2Int[] directions)
    {
        var moves = new List<Move>();
        if (piece.currentTile == null) return moves;

        Vector2Int pos = piece.currentTile.coordinate;

        foreach (var d in directions)
        {
            Vector2Int cur = pos + d;
            while (true)
            {
                var t = BoardManager.Instance.GetTileAt(cur);
                if (t == null) break;
                
                if (t.OccupyingPiece == null)
                {
                    moves.Add(new Move(pos, cur));
                }
                else
                {
                    if (t.OccupyingPiece.team != piece.team)
                        moves.Add(new Move(pos, cur) { isCapture = true });
                    break;
                }
                cur += d;
            }
        }

        return moves;
    }

    protected List<Move> GetJumpingMoves(ChessPiece piece, Vector2Int[] offsets)
    {
        var moves = new List<Move>();
        if (piece.currentTile == null) return moves;

        Vector2Int pos = piece.currentTile.coordinate;

        foreach (var offset in offsets)
        {
            Vector2Int target = pos + offset;
            var t = BoardManager.Instance.GetTileAt(target);
            if (t != null)
            {
                if (t.OccupyingPiece == null)
                {
                    moves.Add(new Move(pos, target));
                }
                else if (t.OccupyingPiece.team != piece.team)
                {
                    moves.Add(new Move(pos, target) { isCapture = true });
                }
            }
        }

        return moves;
    }
}
