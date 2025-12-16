using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Bishop 방향으로 3칸까지 이동하는 업그레이드 가능한 무브먼트 (이동 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementBishop3SO", menuName = "Chess/Piece Movement/Upgradable/Bishop 3 Move")]
    public class MovementBishop3SO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
        }

        private static readonly Vector2Int[] Directions = {
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
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
                        moves.Add(new Move(pos, cur));
                    }
                    else
                    {
                        // 이동 전용이므로 기물이 있어도 이동 불가
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