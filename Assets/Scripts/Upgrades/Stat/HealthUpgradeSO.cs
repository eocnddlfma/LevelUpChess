using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Stat
{
    /// <summary>
    /// 체력 증가 업그레이드
    /// </summary>
    [CreateAssetMenu(fileName = "HealthUpgrade", menuName = "LevelUpChess/Upgrades/Stat/HealthUp")]
    public class HealthUpgradeSO : StatUpgradeSO
    {

        public override void Apply(ChessPiece piece)
        {
            if (piece == null) return;
            
            base.Apply(piece);
            
            Debug.Log($"[HealthUp] {piece.name} 최대 체력 +{FlatBonus}");
        }

        public override void Remove(ChessPiece piece)
        {
            if (piece == null) return;
            
            piece.Stats.RemoveModifier(StatType.MaxHealth, FlatBonus);
            Debug.Log($"[HealthUp] {piece.name} 체력 버프 제거");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            base.SetDefaultNameAndDescription();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = "체력 증가";
            if (string.IsNullOrEmpty(description)) description = $"최대 체력 +{FlatBonus}";
        }

        protected override int GetDefaultFlatBonus() => 5;
        protected override StatType GetDefaultStatType() => StatType.MaxHealth;
#endif
    }
}
