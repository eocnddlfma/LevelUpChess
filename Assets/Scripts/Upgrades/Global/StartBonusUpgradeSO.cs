using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 시작 보너스: 게임 시작시 추가 자원/버프
    /// </summary>
    [CreateAssetMenu(fileName = "StartBonusUpgrade", menuName = "LevelUpChess/Upgrades/Global/StartBonus")]
    public class StartBonusUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "시작 보너스 업그레이드";
        private const string DEFAULT_DESC = "게임 시작 시 모든 아군 레벨 +1";

        [Header("Settings")]
        [Tooltip("시작 추가 골드")]
        [SerializeField] private int startingGold = 50;
        
        [Tooltip("시작 추가 체력 (모든 기물)")]
        [SerializeField] private int startingHealth = 5;

        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[StartBonus] {team} 팀 시작 보너스: 골드 +{startingGold}, 체력 +{startingHealth}");
            
            // 골드 추가
            var resourceManager = LevelUpChess.Core.ServiceLocator.Get<Abilities.ResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.AddGold(team, startingGold);
            }

            // 모든 기물 체력 증가
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    piece.Stats.AddModifier(StatType.MaxHealth, startingHealth);
                    piece.Stats.Heal(startingHealth);
                    _buffedPieces.Add(piece);
                }
            }
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[StartBonus] {team} 팀 시작 보너스 제거");
            
            foreach (var piece in _buffedPieces)
            {
                if (piece != null)
                {
                    piece.Stats.RemoveModifier(StatType.MaxHealth, startingHealth);
                }
            }
            _buffedPieces.Clear();
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            // 게임 시작 후 추가된 기물에는 적용 안함
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
