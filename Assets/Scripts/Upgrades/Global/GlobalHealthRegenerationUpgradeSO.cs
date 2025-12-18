using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 전체 체력 증가: 모든 아군 기물 최대 체력 증가
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalHealthRegenerationUpgrade", menuName = "LevelUpChess/Upgrades/Global/GlobalHealthRegeneration")]
    public class GlobalHealthRegenerationUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "전체 체력 회복력 업그레이드";
        private const string DEFAULT_DESC = "모든 아군 최대 체력 +5";

        [Header("Settings")]
        [Tooltip("체력 증가량")]
        [SerializeField] private int healthBonus = 5;

        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalHealthRegeneration] {team} 팀 전체 체력 +{healthBonus}");
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    piece.Stats.AddModifier(StatType.MaxHealth, healthBonus);
                    piece.Stats.Heal(healthBonus); // 최대 체력 증가분만큼 회복
                    _buffedPieces.Add(piece);
                }
            }
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalHealthRegeneration] {team} 팀 전체 체력 버프 제거");
            
            foreach (var piece in _buffedPieces)
            {
                if (piece != null)
                {
                    piece.Stats.RemoveModifier(StatType.MaxHealth, healthBonus);
                }
            }
            _buffedPieces.Clear();
        }

        public int GetHealthBonus() => healthBonus;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
#endif
    }
}