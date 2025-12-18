using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements.Queen
{
    /// <summary>
    /// 시간역행 이동: 현재 이동 전략이 없을 때 이전에 이동/공격 가능했던 칸들로 이동/공격 가능
    /// </summary>
    [CreateAssetMenu(fileName = "MovementTimeRewindSO", menuName = "Chess/Piece Movement/Upgradable/Queen/Time Rewind")]
    public class TimeRewindMovementSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.Normal; // 이동 + 공격
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null || !piece.HasTimeRewind) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return moves;

            // 현재 이동 전략이 없으면 이전 칸들 사용
            var allStrategies = piece.MovementStrategies;
            bool hasOtherMovements = false;
            foreach (var strategy in allStrategies)
            {
                if (strategy != this) // 자신 제외
                {
                    hasOtherMovements = true;
                    break;
                }
            }

            if (!hasOtherMovements)
            {
                // 이전 칸들로 이동/공격 가능
                foreach (var tileCoord in piece.PreviousAvailableTiles)
                {
                    var tile = boardManager.GetTileAt(tileCoord);
                    if (tile == null) continue;

                    if (tile.OccupyingPiece != null)
                    {
                        // 적 기물이 있으면 공격
                        if (tile.OccupyingPiece.Team != piece.Team)
                        {
                            moves.Add(new Move(pos, tileCoord) { isCapture = true });
                        }
                        // 아군 기물은 이동 불가
                    }
                    else
                    {
                        // 빈 칸이면 이동
                        moves.Add(new Move(pos, tileCoord));
                    }
                }
            }

            return moves;
        }
    }
}