using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 밀어: 공격시 상대방을 바라보는 방향으로 공격력/5만큼 밀칩니다. 밀칠 공간이 없을 경우 날아가야 하는 길이만큼 데미지를 입습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PushbackUpgrade", menuName = "LevelUpChess/Upgrades/Global/Pushback")]
    public class PushbackUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "밀어";
        private const string DEFAULT_DESC = "공격시 상대방을 바라보는 방향으로 공격력/5만큼 밀칩니다. 밀칠 공간이 없을 경우 날아가야 하는 길이만큼 데미지를 입습니다.";

        private HashSet<int> affectedTeams = new HashSet<int>();
        private List<ChessPiece> affectedPieces = new List<ChessPiece>();

        public override void ApplyGlobalEffect(Team team)
        {
            int teamId = (int)team;
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece != null && (int)piece.Team == teamId)
                {
                    piece.OnAttackHit += OnAttackHit;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[Pushback] Team {team}에 밀어 적용");
        }

        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);

            foreach (var piece in pieces)
            {
                if (piece != null)
                {
                    piece.OnAttackHit += OnAttackHit;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[Pushback] Team {teamId}에 밀어 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    piece.OnAttackHit -= OnAttackHit;
                }
            }
            affectedPieces.RemoveAll(p => (int)p.Team == teamId);
        }

        private void OnAttackHit(ChessPiece attacker, ChessPiece target, int damage)
        {
            if (attacker == null || target == null || !target.IsAlive)
                return;

            // 밀치는 거리 계산 (공격력 / 5)
            int pushDistance = Mathf.Max(1, attacker.AttackPower / 5);
            
            // 공격자에서 타겟 방향 계산
            Vector2Int attackerPos = attacker.CurrentTile.coordinate;
            Vector2Int targetPos = target.CurrentTile.coordinate;
            
            Vector2Int direction = new Vector2Int(
                Mathf.Clamp(targetPos.x - attackerPos.x, -1, 1),
                Mathf.Clamp(targetPos.y - attackerPos.y, -1, 1)
            );

            // 밀릴 위치 계산
            Tile finalTile = null;
            var boardManager = ServiceLocator.Get<BoardManager>();
            int actualPushDistance = 0;
            
            for (int i = 1; i <= pushDistance; i++)
            {
                Vector2Int checkPos = targetPos + direction * i;
                var tile = boardManager?.GetTileAt(checkPos);
                
                if (tile == null)
                    break; // 보드 밖
                    
                if (tile.OccupyingPiece != null)
                    break; // 다른 기물이 있음
                    
                finalTile = tile;
                actualPushDistance = i;
            }

            // 밀기 실행
            if (finalTile != null && finalTile != target.CurrentTile)
            {
                target.MoveToTile(finalTile);
                Debug.Log($"[Pushback] {attacker.name}이 {target.name}을 {actualPushDistance}칸 밀침");
            }

            // 공간 부족 시 데미지
            int blockedDistance = pushDistance - actualPushDistance;
            if (blockedDistance > 0)
            {
                target.TakeDamage(blockedDistance, attacker);
                Debug.Log($"[Pushback] {target.name}이 벽에 부딪혀 {blockedDistance} 데미지");
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
