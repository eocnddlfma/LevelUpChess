using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 비숍의 이동 방식을 정의합니다
/// </summary>
[CreateAssetMenu(fileName = "BishopMovement", menuName = "Chess/Piece Movement/Bishop")]
public class BishopMovement : PieceMovement
{
    private static readonly Vector2Int[] DiagonalDirections = {
        Vector2Int.one, 
        new Vector2Int(1, -1), 
        new Vector2Int(-1, 1), 
        new Vector2Int(-1, -1)
    };

    public override List<Move> GetAvailableMoves(ChessPiece piece)
    {
        return GetSlidingMoves(piece, DiagonalDirections);
    }
}
