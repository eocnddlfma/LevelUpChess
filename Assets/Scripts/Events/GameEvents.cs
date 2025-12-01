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
}
