using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 전체 방어력 증가: 모든 아군 기물 방어력 증가
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalDefenseUpgrade", menuName = "LevelUpChess/Upgrades/Global/DefenseUp")]
    public class GlobalDefenseUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "글로벌 디펜스 업그레이드";
        private const string DEFAULT_DESC = "모든 아군 방어력 +1";

        [Header("Settings")]
        [Tooltip("방어력 증가량")]
        [SerializeField] private int defenseBonus = 2;

        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalDefenseUp] {team} 팀 전체 방어력 +{defenseBonus}");
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    piece.Stats.AddModifier(StatType.Defense, defenseBonus);
                    _buffedPieces.Add(piece);
                }
            }
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalDefenseUp] {team} 팀 전체 방어력 버프 제거");
            
            foreach (var piece in _buffedPieces)
            {
                if (piece != null)
                {
                    piece.Stats.RemoveModifier(StatType.Defense, defenseBonus);
                }
            }
            _buffedPieces.Clear();
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            if (piece.Team == targetTeam)
            {
                piece.Stats.AddModifier(StatType.Defense, defenseBonus);
                _buffedPieces.Add(piece);
            }
        }

        public override void OnPieceRemoved(ChessPiece piece)
        {
            _buffedPieces.Remove(piece);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
