using LevelUpChess.Pieces;

namespace LevelUpChess.Events
{
    /// <summary>
    /// 턴 변경 시 발생
    /// </summary>
    public struct TurnChangedEvent : IEvent
    {
        public Team NewTeam;
    }

    /// <summary>
    /// 게임 종료 시 발생
    /// </summary>
    public struct GameOverEvent : IEvent
    {
        public Team WinnerTeam;
        public bool IsRematch;
    }
    
    /// <summary>
    /// 기물 사망 시 발생
    /// </summary>
    public struct PieceDeathEvent : IEvent
    {
        public ChessPiece DeadPiece;
        public ChessPiece Killer; // null이면 다른 원인으로 사망
        public Team DeadPieceTeam;
        public PieceType DeadPieceType;
        public int PieceValue;
    }
    
    /// <summary>
    /// 기물 레벨업 시 발생
    /// </summary>
    public struct PieceLevelUpEvent : IEvent
    {
        public ChessPiece Piece;
        public int NewLevel;
        public int AttackIncrease;
        public int HealthIncrease;
    }
}
