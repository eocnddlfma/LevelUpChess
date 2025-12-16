using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Board;
using LevelUpChess.Pieces;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 연대의 힘: 아군 기물이 인접해 있을 때 공격력/방어력 보너스
    /// 밀집 진형 전략에 유리
    /// </summary>
    [CreateAssetMenu(fileName = "SolidarityUpgrade", menuName = "LevelUpChess/Upgrades/Global/Solidarity")]
    public class SolidarityUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "솔리다리티";
        private const string DEFAULT_DESC = "모든 아군 체력 +1";

        [Header("Solidarity Settings")]
        [Tooltip("인접 아군 1개당 공격력 보너스")]
        [SerializeField] private int attackBonusPerAlly = 1;
        
        [Tooltip("인접 아군 1개당 방어력 보너스")]
        [SerializeField] private int defenseBonusPerAlly = 1;
        
        [Tooltip("최대 보너스 적용 인접 아군 수")]
        [SerializeField] private int maxAllyCount = 4;
        
        [Tooltip("대각선 인접도 포함")]
        [SerializeField] private bool includeDiagonals = true;

        // 연대가 적용된 팀
        private HashSet<int> _affectedTeams = new HashSet<int>();

        private static readonly Vector2Int[] FourDirections = new Vector2Int[]
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 0), new Vector2Int(-1, 0)
        };

        private static readonly Vector2Int[] EightDirections = new Vector2Int[]
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            _affectedTeams.Add(teamId);
            Debug.Log($"[Solidarity] 팀 {teamId}에 연대의 힘 적용");
        }

        public override void RemoveFromTeam(int teamId, List<ChessPiece> pieces)
        {
            _affectedTeams.Remove(teamId);
            Debug.Log($"[Solidarity] 팀 {teamId}에서 연대의 힘 제거");
        }

        public override void ApplyGlobalEffect(Team team)
        {
            _affectedTeams.Add((int)team);
            Debug.Log($"[Solidarity] 팀 {team}에 연대의 힘 적용 (ApplyGlobalEffect)");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            _affectedTeams.Remove((int)team);
            Debug.Log($"[Solidarity] 팀 {team}에서 연대의 힘 제거 (RemoveGlobalEffect)");
        }

        /// <summary>
        /// 현재 인접 아군 수 기반 보너스 계산
        /// </summary>
        public (int attackBonus, int defenseBonus) CalculateBonus(ChessPiece piece, BoardManager boardManager)
        {
            if (piece == null || boardManager == null) return (0, 0);
            if (!_affectedTeams.Contains((int)piece.Team)) return (0, 0);

            int adjacentAllies = CountAdjacentAllies(piece, boardManager);
            adjacentAllies = Mathf.Min(adjacentAllies, maxAllyCount);

            int attackBonus = adjacentAllies * attackBonusPerAlly;
            int defenseBonus = adjacentAllies * defenseBonusPerAlly;

            return (attackBonus, defenseBonus);
        }

        /// <summary>
        /// 인접 아군 수 계산
        /// </summary>
        public int CountAdjacentAllies(ChessPiece piece, BoardManager boardManager)
        {
            if (piece == null || boardManager == null || piece.CurrentTile == null) return 0;

            Vector2Int[] directions = includeDiagonals ? EightDirections : FourDirections;
            int count = 0;

            foreach (var dir in directions)
            {
                Vector2Int adjacentPos = piece.CurrentTile.coordinate + dir;
                var adjacentTile = boardManager.GetTileAt(adjacentPos);
                
                if (adjacentTile != null && adjacentTile.OccupyingPiece != null)
                {
                    if ((int)adjacentTile.OccupyingPiece.Team == (int)piece.Team)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 해당 팀에 연대의 힘이 적용되었는지 확인
        /// </summary>
        public bool IsTeamAffected(int teamId)
        {
            return _affectedTeams.Contains(teamId);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
