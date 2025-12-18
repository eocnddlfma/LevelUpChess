using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// Bishop 방향으로 3칸까지 이동하는 업그레이드 가능한 무브먼트 (이동 + 공격 가능)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementBishop3SO", menuName = "Chess/Piece Movement/Upgradable/Bishop 3 Move")]
    public class MovementBishop3SO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.Normal;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.King;
        }
#endif

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
                        if (t.OccupyingPiece.Team != piece.Team)
                        {
                            moves.Add(new Move(pos, cur) { isCapture = true });
                        }
                        break;
                    }

                    cur += d;
                    steps++;
                }
            }

            return FilterByMoveType(moves);
        }
    }
}