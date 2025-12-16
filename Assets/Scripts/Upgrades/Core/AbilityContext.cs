using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 능력 실행 시 전달되는 컨텍스트 정보
    /// </summary>
    public class AbilityContext
    {
        /// <summary>
        /// 공격자 (능력 발동 주체)
        /// </summary>
        public ChessPiece Owner { get; set; }
        
        /// <summary>
        /// 공격 대상
        /// </summary>
        public ChessPiece Target { get; set; }
        
        /// <summary>
        /// 공격자
        /// </summary>
        public ChessPiece Attacker { get; set; }
        
        /// <summary>
        /// 이동 시작 타일
        /// </summary>
        public Tile FromTile { get; set; }
        
        /// <summary>
        /// 이동 목표 타일
        /// </summary>
        public Tile ToTile { get; set; }
        
        /// <summary>
        /// 처리된 대미지
        /// </summary>
        public int Damage { get; set; }
        
        /// <summary>
        /// 대상이 사망했는지 여부
        /// </summary>
        public bool TargetDied { get; set; }
        
        /// <summary>
        /// 능력이 기본 동작을 취소할지 여부
        /// </summary>
        public bool CancelAction { get; set; }
        
        /// <summary>
        /// 대미지 수정치 (곱연산)
        /// </summary>
        public float DamageMultiplier { get; set; }
        
        /// <summary>
        /// 대미지 수정치 (합연산)
        /// </summary>
        public int DamageBonus { get; set; }
        
        /// <summary>
        /// 이동 후 위치 이동 취소 (히트앤런 등)
        /// </summary>
        public bool PreventMoveAfterKill { get; set; }
        
        /// <summary>
        /// 추가 데이터 (능력별 커스텀)
        /// </summary>
        public object CustomData { get; set; }
        
        /// <summary>
        /// 능력 트리거 타입
        /// </summary>
        public AbilityTrigger Trigger { get; set; }
        
        /// <summary>
        /// 추가 피해량 (합연산용)
        /// </summary>
        public int BonusDamage { get; set; }
        
        /// <summary>
        /// 공격 회피 여부
        /// </summary>
        public bool Evaded { get; set; }
        
        /// <summary>
        /// 피해를 회피할지 설정
        /// </summary>
        public bool ShouldEvade { get; set; }
        
        public AbilityContext()
        {
            DamageMultiplier = 1f;
            DamageBonus = 0;
            PreventMoveAfterKill = false;
            CustomData = null;
            Trigger = AbilityTrigger.Passive;
            BonusDamage = 0;
            Evaded = false;
            ShouldEvade = false;
        }
        
        public AbilityContext(ChessPiece owner) : this()
        {
            Owner = owner;
        }
    }
}
