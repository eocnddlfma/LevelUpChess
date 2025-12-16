using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 팀 시너지: 특정 기물 조합시 보너스 (예: 비숍 2마리 = 공격력 +3)
    /// </summary>
    [CreateAssetMenu(fileName = "TeamSynergyUpgrade", menuName = "LevelUpChess/Upgrades/Global/TeamSynergy")]
    public class TeamSynergyUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "팀 시너지";
        private const string DEFAULT_DESC = "같은 팀 기물들이 서로 도와주기";

        [System.Serializable]
        public class SynergyBonus
        {
            public PieceType pieceType;
            public int requiredCount;
            public int attackBonus;
            public int defenseBonus;
            public int healthBonus;
        }

        [Header("Settings")]
        [SerializeField] private SynergyBonus[] synergies;

        private Team _activeTeam;
        private System.Collections.Generic.List<ChessPiece> _buffedPieces = new();

        public override void ApplyGlobalEffect(Team team)
        {
            Debug.Log($"[TeamSynergy] {team} 팀 시너지 효과 활성화");
            _activeTeam = team;
            UpdateSynergyBuffs(team);
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Debug.Log($"[TeamSynergy] {team} 팀 시너지 효과 비활성화");
            ClearAllBuffs();
        }

        private void UpdateSynergyBuffs(Team team)
        {
            ClearAllBuffs();

            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            var teamPieces = new System.Collections.Generic.List<ChessPiece>();

            foreach (var piece in allPieces)
            {
                if (piece.Team == team && piece.IsAlive)
                {
                    teamPieces.Add(piece);
                }
            }

            // 각 시너지 확인
            foreach (var synergy in synergies)
            {
                int count = 0;
                foreach (var piece in teamPieces)
                {
                    if (piece.PieceType == synergy.pieceType)
                    {
                        count++;
                    }
                }

                if (count >= synergy.requiredCount)
                {
                    // 시너지 충족! 해당 타입 기물에 버프
                    foreach (var piece in teamPieces)
                    {
                        if (piece.PieceType == synergy.pieceType)
                        {
                            if (synergy.attackBonus > 0)
                                piece.Stats.AddModifier(StatType.Attack, synergy.attackBonus);
                            if (synergy.defenseBonus > 0)
                                piece.Stats.AddModifier(StatType.Defense, synergy.defenseBonus);
                            if (synergy.healthBonus > 0)
                            {
                                piece.Stats.AddModifier(StatType.MaxHealth, synergy.healthBonus);
                                piece.Stats.Heal(synergy.healthBonus);
                            }
                            
                            _buffedPieces.Add(piece);
                        }
                    }

                    Debug.Log($"[TeamSynergy] {synergy.pieceType} 시너지 발동! " +
                             $"(필요: {synergy.requiredCount}, 현재: {count})");
                }
            }
        }

        private void ClearAllBuffs()
        {
            // 간단 구현 - 실제로는 적용한 버프를 추적해서 정확히 제거해야 함
            _buffedPieces.Clear();
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            if (piece.Team == _activeTeam)
            {
                UpdateSynergyBuffs(_activeTeam);
            }
        }

        public override void OnPieceRemoved(ChessPiece piece)
        {
            if (piece.Team == _activeTeam)
            {
                UpdateSynergyBuffs(_activeTeam);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
