using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Managers;

namespace LevelUpChess.Pieces
{
    [CreateAssetMenu(fileName = "MovementPawnSO", menuName = "Chess/Piece Movement/Pawn")]
    public class MovementPawnSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.Normal;
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null) 
                return moves;

            int dir = (piece.Team == Team.White) ? 1 : -1;
            Vector2Int pos = piece.CurrentTile.coordinate;

            AddForwardMoves(piece, moves, pos, dir);
            AddCaptureMoves(piece, moves, pos, dir);
            CheckEnPassant(piece, moves, pos, dir);

            return moves;
        }

        private void AddForwardMoves(ChessPiece piece, List<Move> moves, Vector2Int pos, int dir)
        {
            var boardManager = ServiceLocator.Get<BoardManager>();
            Vector2Int one = new Vector2Int(pos.x, pos.y + dir);
            var oneTile = boardManager.GetTileAt(one);

            if (oneTile != null && oneTile.OccupyingPiece == null)
            {
                moves.Add(new Move(pos, one));

                if (!piece.HasMoved)
                {
                    Vector2Int two = new Vector2Int(pos.x, pos.y + 2 * dir);
                    var twoTile = boardManager.GetTileAt(two);
                    if (twoTile != null && twoTile.OccupyingPiece == null)
                        moves.Add(new Move(pos, two));
                }
            }
        }

        private void AddCaptureMoves(ChessPiece piece, List<Move> moves, Vector2Int pos, int dir)
        {
            var boardManager = ServiceLocator.Get<BoardManager>();
            foreach (int dx in new[] { -1, 1 })
            {
                Vector2Int diag = new Vector2Int(pos.x + dx, pos.y + dir);
                var diagTile = boardManager.GetTileAt(diag);
                if (diagTile != null && diagTile.OccupyingPiece != null && 
                    diagTile.OccupyingPiece.Team != piece.Team)
                {
                    moves.Add(new Move(pos, diag) { isCapture = true });
                }
            }
        }

        private void CheckEnPassant(ChessPiece piece, List<Move> moves, Vector2Int pos, int dir)
        {
            var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
            if (networkGameManager == null || networkGameManager.LastMovedPiece == null)
                return;

            if (networkGameManager.LastMovedPiece.Team == piece.Team || 
                networkGameManager.LastMovedPiece.PieceType != PieceType.Pawn)
                return;

            if (networkGameManager.LastMovedPiece.CurrentTile.coordinate.y != pos.y)
                return;

            Vector2Int lastFrom = networkGameManager.LastMoveFrom;
            Vector2Int lastTo = networkGameManager.LastMoveTo;

            if (Mathf.Abs(lastTo.y - lastFrom.y) != 2)
                return;

            if (Mathf.Abs(networkGameManager.LastMovedPiece.CurrentTile.coordinate.x - pos.x) != 1)
                return;

            Vector2Int enPassantTarget = new Vector2Int(
                networkGameManager.LastMovedPiece.CurrentTile.coordinate.x, 
                pos.y + dir
            );

            var boardManager = ServiceLocator.Get<BoardManager>();
            var targetTile = boardManager.GetTileAt(enPassantTarget);
            if (targetTile != null && targetTile.OccupyingPiece == null)
            {
                Move enPassantMove = new Move(pos, enPassantTarget)
                {
                    isCapture = true,
                    isEnPassant = true,
                    enPassantCapturePos = networkGameManager.LastMovedPiece.CurrentTile.coordinate
                };
                moves.Add(enPassantMove);
            }
        }
    }
}
