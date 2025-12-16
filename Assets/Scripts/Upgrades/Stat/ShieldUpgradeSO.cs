using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Stat
{
    /// <summary>
    /// 보호막 획득 업그레이드
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldUpgrade", menuName = "LevelUpChess/Upgrades/Stat/ShieldGain")]
    public class ShieldUpgradeSO : StatUpgradeSO
    {
        public override void Apply(ChessPiece piece)
        {
            if (piece == null) return;
            
            base.Apply(piece);
            Debug.Log($"[ShieldGain] {piece.name} 보호막 +{FlatBonus}");
        }

        public override void Remove(ChessPiece piece)
        {
            if (piece == null) return;
            
            piece.Stats.RemoveModifier(StatType.Shield, FlatBonus);
            Debug.Log($"[ShieldGain] {piece.name} 보호막 제거");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            base.SetDefaultNameAndDescription();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = "보호막 획득";
            if (string.IsNullOrEmpty(description)) description = $"보호막 +{FlatBonus}";
        }

        protected override int GetDefaultFlatBonus() => 5;
        protected override StatType GetDefaultStatType() => StatType.Shield;
#endif
    }
}
