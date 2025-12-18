using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// King용 Rook 방향으로 3칸까지 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementKingRook3SO", menuName = "Chess/Piece Movement/Upgradable/King Rook 3 Move")]
    public class MovementKingRook3SO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.King;
        }
#endif

        private static readonly Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            foreach (var d in Directions)
            {
                Vector2Int cur = pos + d;
                int steps = 1;
                while (steps <= 3)
                {
                    var t = boardManager.GetTileAt(cur);
                    if (t == null) break;

                    if (t.OccupyingPiece == null)
                    {
                        moves.Add(new Move(piece, piece.CurrentTile, t, MoveType.Move));
                    }
                    else
                    {
                        if (t.OccupyingPiece.Team != piece.Team)
                        {
                            moves.Add(new Move(piece, piece.CurrentTile, t, MoveType.Attack));
                        }
                        break;
                    }

                    cur += d;
                    steps++;
                }
            }

            return moves;
        }
    }
}