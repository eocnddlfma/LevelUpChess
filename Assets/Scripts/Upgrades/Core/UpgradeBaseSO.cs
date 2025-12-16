using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 모든 업그레이드의 기본 ScriptableObject 클래스
    /// </summary>
    public abstract class UpgradeBaseSO : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] protected string upgradeId;
        [SerializeField] protected string upgradeName;
        [SerializeField, TextArea(2, 4)] protected string description;
        [SerializeField] protected Sprite icon;
        
        [Header("업그레이드 분류")]
        [SerializeField] protected UpgradeType upgradeType;
        [SerializeField] protected UpgradeTarget target = UpgradeTarget.Piece;
        [SerializeField] protected PieceTypeFilter pieceFilter = PieceTypeFilter.Any;
        
        [Header("레어도")]
        [SerializeField, Range(1, 5)] protected int rarity = 1;
        
        // Properties
        public string UpgradeId => upgradeId;
        public string UpgradeName => upgradeName;
        public string Description => description;
        public Sprite Icon => icon;
        public UpgradeType UpgradeType => upgradeType;
        public UpgradeTarget Target => target;
        public PieceTypeFilter PieceFilter => pieceFilter;
        public int Rarity => rarity;
        
        /// <summary>
        /// 이 업그레이드가 해당 기물에 적용 가능한지 확인
        /// </summary>
        public virtual bool CanApplyTo(ChessPiece piece)
        {
            if (piece == null) return false;
            
            if (pieceFilter == PieceTypeFilter.Any)
                return true;
            
            return (int)pieceFilter - 1 == (int)piece.PieceType;
        }
        
        /// <summary>
        /// 기물에 업그레이드 적용
        /// </summary>
        public abstract void Apply(ChessPiece piece);
        
        /// <summary>
        /// 팀 전체에 업그레이드 적용
        /// </summary>
        public virtual void ApplyToTeam(Team team)
        {
            // 기본 구현: 해당 팀의 모든 기물에 적용
            var pieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in pieces)
            {
                if (piece.Team == team && CanApplyTo(piece))
                {
                    Apply(piece);
                }
            }
        }
        
        /// <summary>
        /// 기물에서 업그레이드 제거
        /// </summary>
        public virtual void Remove(ChessPiece piece) { }
        
        /// <summary>
        /// 툴팁용 포맷된 설명 반환
        /// </summary>
        public virtual string GetFormattedDescription()
        {
            return description;
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                upgradeId = name.ToLower().Replace(" ", "_");
            }
            SetDefaultNameAndDescription();
        }
#endif

        /// <summary>
        /// 기본 이름과 설명을 설정하는 메서드. 서브클래스에서 오버라이드하여 사용.
        /// </summary>
        protected virtual void SetDefaultNameAndDescription() { }
    }
}
