using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Stat
{
    /// <summary>
    /// 방어력 증가 업그레이드
    /// </summary>
    [CreateAssetMenu(fileName = "DefenseUpgrade", menuName = "LevelUpChess/Upgrades/Stat/DefenseUp")]
    public class DefenseUpgradeSO : StatUpgradeSO
    {
        public override void Apply(ChessPiece piece)
        {
            if (piece == null) return;
            
            base.Apply(piece);
            Debug.Log($"[DefenseUp] {piece.name} 방어력 +{FlatBonus}");
        }

        public override void Remove(ChessPiece piece)
        {
            if (piece == null) return;
            
            piece.Stats.RemoveModifier(StatType.Defense, FlatBonus);
            Debug.Log($"[DefenseUp] {piece.name} 방어력 버프 제거");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            base.SetDefaultNameAndDescription();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = "방어력 증가";
            if (string.IsNullOrEmpty(description)) description = $"방어력 +{FlatBonus}";
        }

        protected override int GetDefaultFlatBonus() => 1;
        protected override StatType GetDefaultStatType() => StatType.Defense;
#endif
    }
}
