using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 특수 능력 인터페이스
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// 능력 고유 ID (네트워크 동기화용)
        /// </summary>
        string AbilityId { get; }
        
        /// <summary>
        /// 능력 이름
        /// </summary>
        string AbilityName { get; }
        
        /// <summary>
        /// 능력 설명
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 능력 발동 시점
        /// </summary>
        AbilityTrigger Trigger { get; }
        
        /// <summary>
        /// 능력이 기물에 적용될 때
        /// </summary>
        void OnApply(ChessPiece piece);
        
        /// <summary>
        /// 능력이 기물에서 제거될 때
        /// </summary>
        void OnRemove(ChessPiece piece);
        
        /// <summary>
        /// 능력 실행
        /// </summary>
        void Execute(ChessPiece piece, AbilityContext context);
    }
}
