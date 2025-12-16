using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 특수 능력의 기본 ScriptableObject 클래스
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "LevelUpChess/Upgrades/Ability Base")]
    public class AbilityBaseSO : UpgradeBaseSO, IAbility
    {
        [Header("능력 설정")]
        [SerializeField] protected AbilityTrigger trigger = AbilityTrigger.Passive;
        [SerializeField] protected bool stackable = false;
        [SerializeField] protected int maxStacks = 1;
        
        // IAbility Implementation
        public string AbilityId => UpgradeHash;
        public string AbilityName => upgradeName;
        string IAbility.Description => description;
        public virtual AbilityTrigger Trigger => trigger;
        public bool Stackable => stackable;
        public int MaxStacks => maxStacks;
        
        public override void Apply(ChessPiece piece)
        {
            if (piece == null || piece.Combat == null) return;
            
            piece.Combat.AddAbility(this);
            Debug.Log($"[Ability] {upgradeName} applied to {piece.name}");
        }
        
        public override void Remove(ChessPiece piece)
        {
            if (piece == null || piece.Combat == null) return;
            
            piece.Combat.RemoveAbility(UpgradeHash);
            Debug.Log($"[Ability] {upgradeName} removed from {piece.name}");
        }
        
        public virtual void OnApply(ChessPiece piece)
        {
            // 서브클래스에서 오버라이드
        }
        
        public virtual void OnRemove(ChessPiece piece)
        {
            // 서브클래스에서 오버라이드
        }
        
        public virtual void Execute(AbilityContext context)
        {
            // 서브클래스에서 오버라이드
        }

        // IAbility.Execute(ChessPiece, AbilityContext) 구현
        void IAbility.Execute(ChessPiece piece, AbilityContext context)
        {
            Execute(context);
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            upgradeType = UpgradeType.Ability;
        }
#endif
    }
}
