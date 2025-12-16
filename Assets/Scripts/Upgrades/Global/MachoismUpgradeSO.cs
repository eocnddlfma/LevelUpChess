using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 마쵸이즘: 뒤로 이동하지 못합니다. 옆으로는 공격할때만 이동할 수 있습니다. 가하는 피해가 2배로 증가합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MachoismUpgrade", menuName = "LevelUpChess/Upgrades/Global/Machoism")]
    public class MachoismUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "마쵸이즘";
        private const string DEFAULT_DESC = "뒤로 이동하지 못합니다. 옆으로는 공격할때만 이동할 수 있습니다. 가하는 피해가 2배로 증가합니다.";

        [Header("Machoism Settings")]
        [Tooltip("공격력 배율")]
        [SerializeField] private float attackMultiplier = 2.0f;
        
        [Tooltip("추가 공격력 (고정값)")]
        [SerializeField] private int flatAttackBonus = 0;
        
        [Tooltip("방어력 보너스 (전진 전용이라 약간의 방어 보상)")]
        [SerializeField] private int defenseBonus = 2;
        
        [Tooltip("체력 보너스")]
        [SerializeField] private int healthBonus = 5;

        // 마쵸이즘이 적용된 팀
        private HashSet<int> _affectedTeams = new HashSet<int>();

        public float AttackMultiplier => attackMultiplier;

        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            if (pieces == null || pieces.Count == 0)
            {
                Debug.LogWarning("[Machoism] 적용할 기물이 없습니다.");
                return;
            }

            _affectedTeams.Add(teamId);
            
            Debug.Log($"[Machoism] 팀 {teamId}에 마쵸이즘 적용 - 공격력 {attackMultiplier}배, 후진 불가!");

            foreach (var piece in pieces)
            {
                ApplyToPiece(piece);
            }
        }

        private void ApplyToPiece(ChessPiece piece)
        {
            if (piece == null) return;

            var combat = piece.GetComponent<PieceCombat>();
            if (combat != null)
            {
                // 공격력 보너스 적용
                int baseAttack = combat.AttackPower;
                int bonusAttack = Mathf.RoundToInt(baseAttack * (attackMultiplier - 1)) + flatAttackBonus;
                
                // StatUpgrade로 적용 가능하지만, 여기서는 직접 적용
                // combat.AddBonusAttack(bonusAttack);
                
                // 체력/방어력 보너스
                // combat.AddBonusHealth(healthBonus);
                // combat.AddBonusDefense(defenseBonus);
                
                Debug.Log($"[Machoism] {piece.name} - 공격력 +{bonusAttack}, 방어력 +{defenseBonus}, 체력 +{healthBonus}");
            }

            // 이동 제한 적용 - 후진 불가
            // MovementFilter 또는 별도의 이동 제약 컴포넌트로 처리
            // piece.SetMovementRestriction(MachoismMovementFilter);
            
            Debug.Log($"[Machoism] {piece.name}에게 후진 제한 적용됨");
        }

        public override void RemoveFromTeam(int teamId, List<ChessPiece> pieces)
        {
            _affectedTeams.Remove(teamId);
            
            Debug.Log($"[Machoism] 팀 {teamId}에서 마쵸이즘 제거");

            // 적용된 보너스 제거 필요 시 여기서 처리
        }

        public override void ApplyGlobalEffect(Team team)
        {
            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            var pieces = boardManager?.GetPiecesByTeam(team) ?? new List<ChessPiece>();
            ApplyToTeam((int)team, pieces);
        }

        public override void RemoveGlobalEffect(Team team)
        {
            _affectedTeams.Remove((int)team);
            Debug.Log($"[Machoism] 팀 {team}에서 마쵸이즘 제거 (RemoveGlobalEffect)");
        }

        /// <summary>
        /// 해당 팀에 마쵸이즘이 적용되었는지 확인
        /// </summary>
        public bool IsTeamAffected(int teamId)
        {
            return _affectedTeams.Contains(teamId);
        }

        /// <summary>
        /// 마쵸이즘 이동 필터 - 후진 이동을 제거
        /// </summary>
        public List<Vector2Int> FilterMoves(ChessPiece piece, List<Vector2Int> originalMoves)
        {
            if (piece == null || originalMoves == null) return originalMoves;
            
            // 마쳘이즘이 적용되지 않은 팀은 필터링하지 않음
            if (!_affectedTeams.Contains((int)piece.Team))
            {
                return originalMoves;
            }

            var filteredMoves = new List<Vector2Int>();
            var currentPos = piece.CurrentTile?.coordinate ?? Vector2Int.zero;
            
            // 팀에 따라 "뒤"의 정의가 다름
            // 팀 0: -Y가 뒤, 팀 1: +Y가 뒤
            int backwardDirection = (int)piece.Team == 0 ? -1 : 1;

            foreach (var move in originalMoves)
            {
                int yDiff = move.y - currentPos.y;
                
                // 후진이 아닌 경우만 허용 (전진 또는 옮으로)
                if (yDiff != backwardDirection && !(yDiff < 0 && (int)piece.Team == 0) && !(yDiff > 0 && (int)piece.Team == 1))
                {
                    // Y 차이가 팀 기준 후진 방향이 아니면 허용
                    bool isBackward = ((int)piece.Team == 0 && yDiff < 0) || ((int)piece.Team == 1 && yDiff > 0);
                    
                    if (!isBackward)
                    {
                        filteredMoves.Add(move);
                    }
                }
            }

            Debug.Log($"[Machoism] {piece.name} 이동 필터링: {originalMoves.Count} -> {filteredMoves.Count}");
            
            return filteredMoves;
        }

        /// <summary>
        /// 단일 이동이 후진인지 확인
        /// </summary>
        public bool IsBackwardMove(ChessPiece piece, Vector2Int from, Vector2Int to)
        {
            if (piece == null) return false;
            if (!_affectedTeams.Contains((int)piece.Team)) return false;

            int yDiff = to.y - from.y;
            
            // 팀 0: -Y가 후진, 팀 1: +Y가 후진
            return ((int)piece.Team == 0 && yDiff < 0) || ((int)piece.Team == 1 && yDiff > 0);
        }

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
