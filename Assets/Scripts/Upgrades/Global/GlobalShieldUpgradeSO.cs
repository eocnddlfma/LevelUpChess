using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 전체 실드 증가: 모든 아군 기물 실드 증가
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalShieldUpgrade", menuName = "LevelUpChess/Upgrades/Global/ShieldUp")]
    public class GlobalShieldUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "전체 실드 업그레이드";
        private const string DEFAULT_DESC = "모든 아군 실드 +1";

        [Header("Settings")]
        [Tooltip("실드 증가량")]
        [SerializeField] private int shieldBonus = 1;

        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalShieldUp] {team} 팀 전체 실드 +{shieldBonus}");
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    piece.Stats.AddModifier(StatType.Shield, shieldBonus);
                    _buffedPieces.Add(piece);
                }
            }
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalShieldUp] {team} 팀 전체 실드 버프 제거");
            
            foreach (var piece in _buffedPieces)
            {
                if (piece != null)
                {
                    piece.Stats.RemoveModifier(StatType.Shield, shieldBonus);
                }
            }
            _buffedPieces.Clear();
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            if (piece.Team == targetTeam)
            {
                piece.Stats.AddModifier(StatType.Shield, shieldBonus);
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