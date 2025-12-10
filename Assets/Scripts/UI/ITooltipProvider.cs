using LevelUpChess.Pieces;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 툴팅 콘텐츠를 제공하는 인터페이스
    /// </summary>
    public interface ITooltipProvider
    {
        string GetTooltipContent();
        Team? GetTooltipTeam(); // null이면 기본 색상
    }
}
