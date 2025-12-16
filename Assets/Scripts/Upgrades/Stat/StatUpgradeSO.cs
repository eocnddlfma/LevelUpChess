using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 스탯 종류
    /// </summary>
    public enum StatType
    {
        MaxHealth,
        Health,
        AttackPower,
        Attack,
        Defense,
        Shield,
        HealthRegeneration,
        LifeSteal,
        MoveRange
    }
    
    /// <summary>
    /// 스탯 업그레이드 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatUpgrade", menuName = "LevelUpChess/Upgrades/Stat Upgrade")]
    public class StatUpgradeSO : UpgradeBaseSO
    {
        [Header("스탯 설정")]
        [SerializeField] private StatType statType;
        [SerializeField] private int flatBonus = 0;
        [SerializeField] private float percentBonus = 0f;
        [SerializeField] private bool isPermanent = true;
        
        public StatType StatType => statType;
        public int FlatBonus => flatBonus;
        public float PercentBonus => percentBonus;
        public bool IsPermanent => isPermanent;
        
        public override void Apply(ChessPiece piece)
        {
            if (piece == null || piece.Combat == null) return;
            
            piece.Combat.ApplyStatUpgrade(this);
            Debug.Log($"[StatUpgrade] {upgradeName} applied to {piece.name}: {statType} +{flatBonus} (+{percentBonus * 100}%)");
        }
        
        public override void Remove(ChessPiece piece)
        {
            if (piece == null || piece.Combat == null) return;
            
            piece.Combat.RemoveStatUpgrade(this);
        }
        
        public override string GetFormattedDescription()
        {
            string statName = statType switch
            {
                StatType.MaxHealth => "최대 체력",
                StatType.AttackPower => "공격력",
                StatType.Defense => "방어력",
                StatType.Shield => "보호막",
                StatType.HealthRegeneration => "체력 재생",
                StatType.LifeSteal => "흡혈",
                _ => statType.ToString()
            };
            
            string bonus = "";
            if (flatBonus != 0)
                bonus += $"+{flatBonus}";
            if (percentBonus != 0)
                bonus += $" (+{percentBonus * 100}%)";
            
            return $"{statName} {bonus}";
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            upgradeType = UpgradeType.Stat;
            flatBonus = GetDefaultFlatBonus();
            percentBonus = GetDefaultPercentBonus();
            statType = GetDefaultStatType();
        }

        protected virtual int GetDefaultFlatBonus() => 0;
        protected virtual float GetDefaultPercentBonus() => 0f;
        protected virtual StatType GetDefaultStatType() => StatType.MaxHealth;
#endif
    }
}
