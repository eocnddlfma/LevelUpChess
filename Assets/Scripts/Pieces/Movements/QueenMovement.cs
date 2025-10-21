using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀸의 이동 방식을 정의합니다 (룩 + 비숍)
/// </summary>
[CreateAssetMenu(fileName = "QueenMovement", menuName = "Chess/Piece Movement/Queen")]
public class QueenMovement : PieceMovement
{
    private static readonly Vector2Int[] AllDirections = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        Vector2Int.one, 
        new Vector2Int(1, -1), 
        new Vector2Int(-1, 1), 
        new Vector2Int(-1, -1)
    };

    public override List<Move> GetAvailableMoves(ChessPiece piece)
    {
        return GetSlidingMoves(piece, AllDirections);
    }
}
