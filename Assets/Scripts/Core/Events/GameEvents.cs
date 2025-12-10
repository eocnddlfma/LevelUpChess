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
    
    /// <summary>
    /// UI 메시지 표시 요청
    /// </summary>
    public struct ShowMessageEvent : IEvent
    {
        public string Message;
        public float Duration; // 0이면 지속 표시, -1이면 기본값 사용
    }
    
    /// <summary>
    /// UI 메시지 숨기기 요청
    /// </summary>
    public struct HideMessageEvent : IEvent { }
    
    /// <summary>
    /// 플레이어 경험치 변경 시 발생
    /// </summary>
    public struct PlayerExpChangedEvent : IEvent
    {
        public Team Team;
        public int Level;
        public int CurrentExp;
        public int ExpToNextLevel;
    }
    
    /// <summary>
    /// 플레이어 레벨업 시 발생
    /// </summary>
    public struct PlayerLevelUpEvent : IEvent
    {
        public Team Team;
        public int NewLevel;
    }
}
