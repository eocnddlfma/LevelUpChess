namespace LevelUpChess.Events
{
    /// <summary>
    /// 보드 생성 요청 시 발생
    /// BoardGenerator가 구독하여 보드를 생성
    /// </summary>
    public struct BoardGenerationRequestedEvent : IEvent { }

    /// <summary>
    /// 보드 생성 완료 시 발생
    /// BoardManager가 구독하여 타일 데이터를 수신
    /// </summary>
    public struct BoardGeneratedEvent : IEvent
    {
        public Board.Tile[,] Tiles;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// 보드 초기화 요청 시 발생
    /// </summary>
    public struct BoardClearRequestedEvent : IEvent { }
}
