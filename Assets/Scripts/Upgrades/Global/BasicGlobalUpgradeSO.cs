using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 기본 글로벌 업그레이드 (구체적인 효과가 없는 기본 구현)
    /// 커스텀 에디터에서 기본 글로벌 업그레이드 SO를 생성할 때 사용
    /// </summary>
    [CreateAssetMenu(fileName = "BasicGlobalUpgrade", menuName = "LevelUpChess/Upgrades/Global/Basic")]
    public class BasicGlobalUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "베이직 글로벌 업그레이드";
        private const string DEFAULT_DESC = "기본 글로벌 업그레이드";

        [Header("Basic Global Settings")]
        [Tooltip("전체 공격력 보너스")]
        [SerializeField] private int attackBonus = 0;
        
        [Tooltip("전체 방어력 보너스")]
        [SerializeField] private int defenseBonus = 0;
        
        [Tooltip("전체 체력 보너스")]
        [SerializeField] private int healthBonus = 0;
        
        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[BasicGlobal] {upgradeName} 효과 적용: {team} 팀");
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    ApplyBuffToPiece(piece);
                    _buffedPieces.Add(piece);
                }
            }
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[BasicGlobal] {upgradeName} 효과 제거: {team} 팀");
            
            foreach (var piece in _buffedPieces)
            {
                if (piece != null)
                {
                    RemoveBuffFromPiece(piece);
                }
            }
            _buffedPieces.Clear();
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            if (piece.Team == targetTeam)
            {
                ApplyBuffToPiece(piece);
                _buffedPieces.Add(piece);
            }
        }

        public override void OnPieceRemoved(ChessPiece piece)
        {
            _buffedPieces.Remove(piece);
        }

        private void ApplyBuffToPiece(ChessPiece piece)
        {
            if (attackBonus != 0)
                piece.Stats.AddModifier(StatType.Attack, attackBonus);
            if (defenseBonus != 0)
                piece.Stats.AddModifier(StatType.Defense, defenseBonus);
            if (healthBonus != 0)
            {
                piece.Stats.AddModifier(StatType.MaxHealth, healthBonus);
                piece.Stats.Heal(healthBonus);
            }
        }

        private void RemoveBuffFromPiece(ChessPiece piece)
        {
            if (attackBonus != 0)
                piece.Stats.RemoveModifier(StatType.Attack, attackBonus);
            if (defenseBonus != 0)
                piece.Stats.RemoveModifier(StatType.Defense, defenseBonus);
            if (healthBonus != 0)
                piece.Stats.RemoveModifier(StatType.MaxHealth, healthBonus);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
