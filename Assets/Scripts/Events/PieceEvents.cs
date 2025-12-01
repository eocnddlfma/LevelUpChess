using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Events
{
    /// <summary>
    /// 체스 기물 선택 시 발생 (이동 가능한 위치 포함)
    /// </summary>
    public struct PieceSelectedEvent : IEvent
    {
        public ChessPiece Piece;
        public List<Move> AvailableMoves;
    }

    /// <summary>
    /// 선택 해제 시 발생
    /// </summary>
    public struct SelectionClearedEvent : IEvent { }
}
