using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Board;
using LevelUpChess.Pieces;
using System.Collections.Generic;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 왕의 영역: 킹 주변 기물들에게 버프 제공
    /// 킹 중심 진형 전략
    /// </summary>
    [CreateAssetMenu(fileName = "KingsPrestigeUpgrade", menuName = "LevelUpChess/Upgrades/Global/KingsPrestige")]
    public class KingsPrestigeUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "왕의 영역";
        private const string DEFAULT_DESC = "킹 주변 기물들에게 버프 제공";

        [Header("King's Prestige Settings")]
        [Tooltip("킹 주변 버프 범위 (칸)")]
        [SerializeField] private int auraRange = 2;
        
        [Tooltip("범위 내 아군 공격력 보너스")]
        [SerializeField] private int attackBonus = 3;
        
        [Tooltip("범위 내 아군 방어력 보너스")]
        [SerializeField] private int defenseBonus = 2;
        
        [Tooltip("범위 내 아군 체력 재생 보너스")]
        [SerializeField] private int regenBonus = 1;

        // 적용된 팀과 킹 참조
        private Dictionary<int, ChessPiece> _teamKings = new Dictionary<int, ChessPiece>();
        private List<ChessPiece> _buffedPieces = new List<ChessPiece>();

        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            if (pieces == null || pieces.Count == 0)
            {
                Debug.LogWarning("[KingsPrestige] 적용할 기물이 없습니다.");
                return;
            }

            // 킹 찾기
            ChessPiece king = null;
            foreach (var piece in pieces)
            {
                if (piece.PieceType == PieceType.King)
                {
                    king = piece;
                    break;
                }
            }

            if (king == null)
            {
                Debug.LogWarning("[KingsPrestige] 팀에 킹이 없습니다.");
                return;
            }

            _teamKings[teamId] = king;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogWarning("[KingsPrestige] BoardManager not found");
            }

            // 주변 기물에 버프 적용
            var piecesList = pieces;
            if (piecesList == null || piecesList.Count == 0)
            {
                // fallback - get from board manager
                if (boardManager != null)
                    piecesList = boardManager.GetPiecesByTeam((Team)teamId);
            }

            if (piecesList != null)
            {
                foreach (var p in piecesList)
                {
                    if (p == null || p == king) continue;
                    if (IsInKingsAura(p, boardManager))
                    {
                        p.Stats.AddModifier(StatType.Attack, attackBonus);
                        p.Stats.AddModifier(StatType.Defense, defenseBonus);
                        p.Stats.AddModifier(StatType.HealthRegeneration, regenBonus);
                        _buffedPieces.Add(p);
                    }
                }
            }

            Debug.Log($"[KingsPrestige] 팀 {teamId}에 왕의 위엄 적용 - 범위: {auraRange}칸");
        }

        public override void RemoveFromTeam(int teamId, List<ChessPiece> pieces)
        {
            // remove piece buffs
            foreach (var p in _buffedPieces)
            {
                if (p != null && (int)p.Team == teamId)
                {
                    p.Stats.RemoveModifier(StatType.Attack, attackBonus);
                    p.Stats.RemoveModifier(StatType.Defense, defenseBonus);
                    p.Stats.RemoveModifier(StatType.HealthRegeneration, regenBonus);
                }
            }
            _buffedPieces.RemoveAll(p => p == null || (int)p.Team == teamId);
            _teamKings.Remove(teamId);
            Debug.Log($"[KingsPrestige] 팀 {teamId}에서 왕의 위엄 제거");
        }

        public override void ApplyGlobalEffect(Team team)
        {
            int teamId = (int)team;
            var boardManager = ServiceLocator.Get<BoardManager>();
            var pieces = boardManager?.GetPiecesByTeam(team) ?? new List<ChessPiece>();
            ApplyToTeam(teamId, pieces);
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            RemoveFromTeam(teamId, null);
        }

        public override void OnPieceAdded(ChessPiece piece)
        {
            if (piece == null) return;
            int teamId = (int)piece.Team;
            if (!_teamKings.ContainsKey(teamId)) return; // no king tracked for team

            var boardManager = ServiceLocator.Get<BoardManager>();

            // If the new piece is the king, apply buff
            if (piece.PieceType == PieceType.King)
            {
                _teamKings[teamId] = piece;
                return;
            }

            // If piece is in aura, apply buffs
            if (IsInKingsAura(piece, boardManager))
            {
                piece.Stats.AddModifier(StatType.Attack, attackBonus);
                piece.Stats.AddModifier(StatType.Defense, defenseBonus);
                piece.Stats.AddModifier(StatType.HealthRegeneration, regenBonus);
                _buffedPieces.Add(piece);
            }
        }

        public override void OnPieceRemoved(ChessPiece piece)
        {
            if (piece == null) return;
            int teamId = (int)piece.Team;

            if (_buffedPieces.Contains(piece))
            {
                piece.Stats.RemoveModifier(StatType.Attack, attackBonus);
                piece.Stats.RemoveModifier(StatType.Defense, defenseBonus);
                piece.Stats.RemoveModifier(StatType.HealthRegeneration, regenBonus);
                _buffedPieces.Remove(piece);
            }
        }

        /// <summary>
        /// 기물이 킹의 오라 범위 내에 있는지 확인
        /// </summary>
        public bool IsInKingsAura(ChessPiece piece, BoardManager boardManager)
        {
            if (piece == null || piece.CurrentTile == null) return false;
            if (!_teamKings.TryGetValue((int)piece.Team, out var king)) return false;
            if (king == null || king.CurrentTile == null) return false;

            // 자기 자신(킹)은 오라 대상 아님
            if (piece == king) return false;

            Vector2Int kingPos = king.CurrentTile.coordinate;
            Vector2Int piecePos = piece.CurrentTile.coordinate;
            
            int distance = Mathf.Max(Mathf.Abs(kingPos.x - piecePos.x), Mathf.Abs(kingPos.y - piecePos.y));
            
            return distance <= auraRange;
        }

        /// <summary>
        /// 왕의 오라로 인한 보너스 계산
        /// </summary>
        public (int attackBonus, int defenseBonus, int regenBonus) GetAuraBonus(ChessPiece piece, BoardManager boardManager)
        {
            if (!IsInKingsAura(piece, boardManager))
            {
                return (0, 0, 0);
            }

            return (attackBonus, defenseBonus, regenBonus);
        }

        /// <summary>
        /// 킹 주변 기물 목록 반환
        /// </summary>
        public List<ChessPiece> GetPiecesInAura(int teamId, BoardManager boardManager)
        {
            var result = new List<ChessPiece>();
            
            if (!_teamKings.TryGetValue(teamId, out var king)) return result;
            if (king == null || king.CurrentTile == null) return result;

            Vector2Int kingPos = king.CurrentTile.coordinate;

            // 범위 내 모든 타일 확인
            for (int x = -auraRange; x <= auraRange; x++)
            {
                for (int y = -auraRange; y <= auraRange; y++)
                {
                    if (x == 0 && y == 0) continue; // 킹 자신 제외
                    
                    Vector2Int checkPos = kingPos + new Vector2Int(x, y);
                    var tile = boardManager.GetTileAt(checkPos);
                    
                    if (tile != null && tile.OccupyingPiece != null && 
                        (int)tile.OccupyingPiece.Team == teamId)
                    {
                        result.Add(tile.OccupyingPiece);
                    }
                }
            }

            return result;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
