using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "RookMovement", menuName = "Chess/Piece Movement/Rook")]
public class RookMovement : PieceMovement
{
    private static readonly Vector2Int[] Directions = { 
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right 
    };

    public override List<Move> GetAvailableMoves(ChessPiece piece)
    {
        return GetSlidingMoves(piece, Directions);
    }
}
