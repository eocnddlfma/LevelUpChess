using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 전체 공격력 증가: 모든 아군 기물 공격력 증가
    /// </summary>
    [CreateAssetMenu(fileName = "GlobalAttackUpgrade", menuName = "LevelUpChess/Upgrades/Global/AttackUp")]
    public class GlobalAttackUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "전체 공격력 업그레이드";
        private const string DEFAULT_DESC = "모든 아군 공격력 +1";

        [Header("Settings")]
        [Tooltip("공격력 증가량")]
        [SerializeField] private int attackBonus = 2;

        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalAttackUp] {team} 팀 전체 공격력 +{attackBonus}");
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    piece.Stats.AddModifier(StatType.Attack, attackBonus);
                    _buffedPieces.Add(piece);
                }
            }
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[GlobalAttackUp] {team} 팀 전체 공격력 버프 제거");
            
            foreach (var piece in _buffedPieces)
            {
                if (piece != null)
                {
                    piece.Stats.RemoveModifier(StatType.Attack, attackBonus);
                }
            }
            _buffedPieces.Clear();
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            if (piece.Team == targetTeam)
            {
                piece.Stats.AddModifier(StatType.Attack, attackBonus);
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
