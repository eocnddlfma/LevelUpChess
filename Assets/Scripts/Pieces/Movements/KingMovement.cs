using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces
{
    [CreateAssetMenu(fileName = "KingMovement", menuName = "Chess/Piece Movement/King")]
    public class KingMovement : PieceMovement
    {
        private static readonly Vector2Int[] OneSquareOffsets = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            Vector2Int.one, 
            new Vector2Int(1, -1), 
            new Vector2Int(-1, 1), 
            new Vector2Int(-1, -1)
        };

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            var moves = GetJumpingMoves(piece, OneSquareOffsets);
            CheckCastling(piece, moves);
            return moves;
        }

        private void CheckCastling(ChessPiece piece, List<Move> moves)
        {
            if (piece.HasMoved)
                return;

            Vector2Int kingPos = piece.CurrentTile.coordinate;

            if (kingPos.x != 4)
                return;

            int expectedY = (piece.Team == Team.White) ? 0 : 7;
            if (kingPos.y != expectedY)
                return;

            TryCastleQueenside(piece, moves, kingPos);
            TryCastleKingside(piece, moves, kingPos);
        }

        private void TryCastleQueenside(ChessPiece piece, List<Move> moves, Vector2Int kingPos)
        {
            var boardManager = ServiceLocator.Get<BoardManager>();
            bool leftClear = boardManager.GetPieceAt(new Vector2Int(1, kingPos.y)) == null &&
                            boardManager.GetPieceAt(new Vector2Int(2, kingPos.y)) == null &&
                            boardManager.GetPieceAt(new Vector2Int(3, kingPos.y)) == null;

            if (!leftClear)
                return;

            ChessPiece leftRook = boardManager.GetPieceAt(new Vector2Int(0, kingPos.y));
            if (leftRook != null && leftRook.PieceType == PieceType.Rook && 
                leftRook.Team == piece.Team && !leftRook.HasMoved)
            {
                Move castlingMove = new Move(kingPos, new Vector2Int(2, kingPos.y))
                {
                    isCastling = true,
                    rookFromPos = new Vector2Int(0, kingPos.y),
                    rookToPos = new Vector2Int(3, kingPos.y)
                };
                moves.Add(castlingMove);
            }
        }

        private void TryCastleKingside(ChessPiece piece, List<Move> moves, Vector2Int kingPos)
        {
            var boardManager = ServiceLocator.Get<BoardManager>();
            bool rightClear = boardManager.GetPieceAt(new Vector2Int(5, kingPos.y)) == null &&
                             boardManager.GetPieceAt(new Vector2Int(6, kingPos.y)) == null;

            if (!rightClear)
                return;

            ChessPiece rightRook = boardManager.GetPieceAt(new Vector2Int(7, kingPos.y));
            if (rightRook != null && rightRook.PieceType == PieceType.Rook && 
                rightRook.Team == piece.Team && !rightRook.HasMoved)
            {
                Move castlingMove = new Move(kingPos, new Vector2Int(6, kingPos.y))
                {
                    isCastling = true,
                    rookFromPos = new Vector2Int(7, kingPos.y),
                    rookToPos = new Vector2Int(5, kingPos.y)
                };
                moves.Add(castlingMove);
            }
        }
    }
}
