using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces
{
    public abstract class PieceMovement : ScriptableObject
    {
        public abstract List<Move> GetAvailableMoves(ChessPiece piece);

        protected List<Move> GetSlidingMoves(ChessPiece piece, Vector2Int[] directions)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            foreach (var d in directions)
            {
                Vector2Int cur = pos + d;
                while (true)
                {
                    var t = boardManager.GetTileAt(cur);
                    if (t == null) break;
                    
                    if (t.OccupyingPiece == null)
                    {
                        moves.Add(new Move(pos, cur));
                    }
                    else
                    {
                        if (t.OccupyingPiece.Team != piece.Team)
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
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            foreach (var offset in offsets)
            {
                Vector2Int target = pos + offset;
                var t = boardManager.GetTileAt(target);
                if (t != null)
                {
                    if (t.OccupyingPiece == null)
                    {
                        moves.Add(new Move(pos, target));
                    }
                    else if (t.OccupyingPiece.Team != piece.Team)
                    {
                        moves.Add(new Move(pos, target) { isCapture = true });
                    }
                }
            }

            return moves;
        }
    }
}
