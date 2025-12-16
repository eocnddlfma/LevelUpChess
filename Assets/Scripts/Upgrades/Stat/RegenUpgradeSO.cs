using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Stat
{
    /// <summary>
    /// 체력 회복량 증가 업그레이드
    /// </summary>
    [CreateAssetMenu(fileName = "RegenUpgrade", menuName = "LevelUpChess/Upgrades/Stat/RegenUp")]
    public class RegenUpgradeSO : StatUpgradeSO
    {
        public override void Apply(ChessPiece piece)
        {
            if (piece == null) return;
            
            base.Apply(piece);
            Debug.Log($"[RegenUp] {piece.name} 체력 재생 +{FlatBonus}");
        }

        public override void Remove(ChessPiece piece)
        {
            if (piece == null) return;
            
            piece.Stats.RemoveModifier(StatType.HealthRegeneration, FlatBonus);
            Debug.Log($"[RegenUp] {piece.name} 체력 재생 버프 제거");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            base.SetDefaultNameAndDescription();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = "체력 회복 증가";
            if (string.IsNullOrEmpty(description)) description = $"턴당 체력 회복 +{FlatBonus}";
        }

        protected override int GetDefaultFlatBonus() => 1;
        protected override StatType GetDefaultStatType() => StatType.HealthRegeneration;
#endif
    }
}
