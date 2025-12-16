using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 킹용 제한된 비숍 이동 - 대각선 최대 3칸 (이동 + 공격 모두 가능)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementLimitedBishopSO", menuName = "Chess/Piece Movement/Upgradable/Limited Bishop")]
    public class MovementLimitedBishopSO : PieceMovementSO
    {
        [Header("Movement Range")]
        [Tooltip("대각선으로 이동 가능한 최대 칸 수")]
        [SerializeField] private int maxRange = 3;

        private static readonly Vector2Int[] DiagonalDirections = {
            Vector2Int.one,                 // (1, 1) 우상
            new Vector2Int(1, -1),          // 우하
            new Vector2Int(-1, 1),          // 좌상
            new Vector2Int(-1, -1)          // 좌하
        };

        private void OnEnable()
        {
            moveType = MoveType.Normal; // 이동 + 공격 모두 가능
        }

        public override List<Move> GetAvailableMoves(ChessPiece piece)
        {
            // 제한된 범위의 대각선 슬라이딩 이동
            return GetLimitedSlidingMoves(piece, DiagonalDirections, maxRange);
        }

        /// <summary>
        /// 최대 거리가 제한된 슬라이딩 이동 계산
        /// </summary>
        protected List<Move> GetLimitedSlidingMoves(ChessPiece piece, Vector2Int[] directions, int maxDistance)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            if (boardManager == null) return moves;

            foreach (var direction in directions)
            {
                for (int i = 1; i <= maxDistance; i++)
                {
                    Vector2Int targetPos = pos + direction * i;
                    Tile targetTile = boardManager.GetTileAt(targetPos);

                    if (targetTile == null)
                        break; // 보드 밖

                    if (targetTile.OccupyingPiece != null)
                    {
                        // 적 기물이면 공격 가능
                        if (targetTile.OccupyingPiece.Team != piece.Team)
                        {
                            var move = new Move(piece.CurrentTile.coordinate, targetTile.coordinate);
                            move.isCapture = true;
                            moves.Add(move);
                        }
                        break; // 기물이 있으면 더 이상 진행 불가
                    }
                    else
                    {
                        // 빈 칸이면 이동 가능
                        moves.Add(new Move(
                            piece.CurrentTile.coordinate,
                            targetTile.coordinate
                        ));
                    }
                }
            }

            return FilterByMoveType(moves);
        }
    }
}
