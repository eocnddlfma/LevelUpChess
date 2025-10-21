using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 피스 타입의 이동 방식을 정의하는 ScriptableObject
/// </summary>
public abstract class PieceMovement : ScriptableObject
{
    /// <summary>
    /// 주어진 피스의 이동 가능한 위치들을 반환합니다
    /// </summary>
    public abstract List<Move> GetAvailableMoves(ChessPiece piece);

    /// <summary>
    /// 특정 방향으로 계속 이동할 수 있는지 확인 (직선/대각선 이동용)
    /// </summary>
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

    /// <summary>
    /// 특정 위치들로만 이동할 수 있는지 확인 (나이트, 킹용)
    /// </summary>
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
