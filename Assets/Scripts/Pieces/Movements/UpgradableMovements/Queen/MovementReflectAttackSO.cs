using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces.Movements.UpgradableMovements
{
    /// <summary>
    /// 이동이 벽에 반사되는 업그레이드 가능한 무브먼트 (공격 전용)
    /// </summary>
    [CreateAssetMenu(fileName = "MovementReflectAttackSO", menuName = "Chess/Piece Movement/Upgradable/Reflect Attack")]
    public class MovementReflectAttackSO : PieceMovementSO
    {
        private void OnEnable()
        {
            moveType = MoveType.MoveOnly;
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
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            // 대각선 방향으로 반사 공격 (칸 수 제한 없음)
            Vector2Int[] directions = {
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                // 각 방향으로 반사하며 이동/공격 (1번만 튕김)
                Vector2Int current = pos + dir;
                Vector2Int currentDir = dir;
                bool hasBounced = false;

                while (true)
                {
                    var tile = boardManager.GetTileAt(current);
                    if (tile == null) break; // 보드 밖이면 멈춤

                    if (tile.OccupyingPiece != null)
                    {
                        if (tile.OccupyingPiece.Team != piece.Team)
                        {
                            // 적 기물이 있으면 공격 가능
                            moves.Add(new Move(pos, current) { isCapture = true });
                        }
                        // 아군이나 적 기물이 있으면 이동 불가, 멈춤
                        break;
                    }
                    else
                    {
                        // 빈 칸이면 이동 가능
                        moves.Add(new Move(pos, current));
                    }

                    // 다음 위치 계산
                    Vector2Int next = current + currentDir;

                    // 보드 경계 체크 및 반사 (1번만)
                    bool bounced = false;
                    if (next.x < 0 || next.x >= boardManager.Width)
                    {
                        currentDir.x = -currentDir.x; // x 방향 반전
                        bounced = true;
                    }
                    if (next.y < 0 || next.y >= boardManager.Height)
                    {
                        currentDir.y = -currentDir.y; // y 방향 반전
                        bounced = true;
                    }

                    if (bounced)
                    {
                        if (hasBounced) break; // 이미 튕겼으면 멈춤
                        hasBounced = true;
                    }

                    current += currentDir;
                }
            }

            return moves;
        }
    }
}