using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Board;
using LevelUpChess.Pieces;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 퀸 보호막 능력 (어머니의 보호): 치명적인 피해를 막아줌
    /// 퀸 전용 또는 고급 기물에게 적합
    /// </summary>
    [CreateAssetMenu(fileName = "MothersProtectionAbility", menuName = "LevelUpChess/Upgrades/Abilities/MothersProtection")]
    public class MothersProtectionAbilitySO : AbilityBaseSO
    {
        [Header("Protection Settings")]
        [Tooltip("보호막 사용 가능 횟수")]
        [SerializeField] private int protectionCharges = 3;
        
        [Tooltip("보호막 발동 후 남는 체력 비율 (0~1)")]
        [SerializeField] private float surviveHealthPercent = 0.1f;
        
        [Tooltip("보호막 발동 후 무적 시간 (초)")]
        [SerializeField] private float invincibilityAfterTrigger = 1.0f;
        
        [Tooltip("보호막 발동 시 주변 적 밀쳐내기")]
        [SerializeField] private bool knockbackOnTrigger = true;
        
        [Tooltip("밀쳐내기 거리")]
        [SerializeField] private int knockbackDistance = 1;

        // 런타임 데이터 - 각 기물별 남은 충전 횟수
        // 실제로는 PieceCombat이나 별도 컴포넌트에서 관리해야 함
        private Dictionary<int, int> _remainingCharges = new Dictionary<int, int>();

        public new string AbilityId => "ability_mothers_protection";
        public new string AbilityName => "어머니의 보호";
        public new string Description => 
            $"치명적인 피해를 {protectionCharges}회 막아주고, 체력 {surviveHealthPercent * 100}%로 생존합니다." +
            (knockbackOnTrigger ? " 발동 시 주변 적을 밀쳐냅니다." : "");

        public override void OnApply(ChessPiece piece)
        {
            if (piece.Combat == null) return;
            // 초기 충전 횟수 설정
            int instanceId = piece.Combat.GetInstanceID();
            _remainingCharges[instanceId] = protectionCharges;
            
            Debug.Log($"[MothersProtection] {piece.name}에게 어머니의 보호 적용 (충전: {protectionCharges}회)");
        }

        public override void OnRemove(ChessPiece piece)
        {
            if (piece.Combat == null) return;
            int instanceId = piece.Combat.GetInstanceID();
            _remainingCharges.Remove(instanceId);
            
            Debug.Log($"[MothersProtection] {piece.name}에서 어머니의 보호 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null)
            {
                Debug.LogWarning("[MothersProtection] Owner가 없습니다.");
                return;
            }

            int instanceId = context.Owner.GetInstanceID();
            
            // 남은 충전 확인
            if (!_remainingCharges.TryGetValue(instanceId, out int charges) || charges <= 0)
            {
                Debug.Log("[MothersProtection] 보호막 충전이 소진되었습니다.");
                return;
            }

            // 치명적 피해인지 확인 (체력이 0 이하로 떨어지는 경우)
            int currentHealth = context.Owner.CurrentHealth;
            int incomingDamage = context.Damage;
            
            if (currentHealth - incomingDamage > 0)
            {
                // 치명적이지 않은 피해 - 보호막 발동하지 않음
                return;
            }

            // 보호막 발동!
            _remainingCharges[instanceId] = charges - 1;
            
            Debug.Log($"[MothersProtection] {context.Owner.name} 보호막 발동! (남은 충전: {_remainingCharges[instanceId]})");

            // 피해 상쇄 - 데미지를 줄여서 생존
            int maxHealth = context.Owner.MaxHealth;
            int surviveHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * surviveHealthPercent));
            
            // 데미지를 현재 체력 - 생존 체력으로 조정
            int adjustedDamage = Mathf.Max(0, currentHealth - surviveHealth);
            context.Damage = adjustedDamage;
            
            Debug.Log($"[MothersProtection] 피해 조정: {incomingDamage} -> {adjustedDamage}, 생존 체력: {surviveHealth}");

            // 밀쳐내기 발동
            if (knockbackOnTrigger)
            {
                TriggerKnockback(context);
            }

            // 무적 상태 적용 (추후 구현)
            if (invincibilityAfterTrigger > 0)
            {
                // TODO: 무적 상태 적용
                // context.Owner.ApplyInvincibility(invincibilityAfterTrigger);
                Debug.Log($"[MothersProtection] {invincibilityAfterTrigger}초 무적 적용");
            }

            // 이펙트/사운드 재생 (추후 구현)
            // PlayProtectionEffect(context.Owner);
        }

        private void TriggerKnockback(AbilityContext context)
        {
            var chessPiece = context.Owner.GetComponent<ChessPiece>();
            if (chessPiece == null || chessPiece.CurrentTile == null) return;

            Vector2Int centerPos = chessPiece.CurrentTile.coordinate;
            
            if (context.CustomData == null || !(context.CustomData is BoardManager boardManager))
            {
                Debug.LogWarning("[MothersProtection] BoardManager를 찾을 수 없어 밀쳐내기 생략");
                return;
            }

            // 8방향 인접 적 확인 및 밀쳐내기
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                Vector2Int adjacentPos = centerPos + dir;
                var adjacentTile = boardManager.GetTileAt(adjacentPos);
                
                if (adjacentTile != null && adjacentTile.OccupyingPiece != null)
                {
                    var targetPiece = adjacentTile.OccupyingPiece;
                    
                    // 적인 경우에만 밀쳐내기
                    if ((int)targetPiece.Team != (int)chessPiece.Team)
                    {
                        // 밀쳐낼 위치 계산
                        Vector2Int knockbackTarget = adjacentPos + (dir * knockbackDistance);
                        var knockbackTile = boardManager.GetTileAt(knockbackTarget);
                        
                        if (knockbackTile != null && knockbackTile.OccupyingPiece == null)
                        {
                            Debug.Log($"[MothersProtection] {targetPiece.name}을(를) {knockbackTarget}으로 밀쳐냄!");
                            // targetPiece.ForceMoveToTile(knockbackTile);
                        }
                        else
                        {
                            Debug.Log($"[MothersProtection] {targetPiece.name} 밀쳐내기 실패 (목표 위치 막힘)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 남은 보호막 충전 횟수 확인
        /// </summary>
        public int GetRemainingCharges(PieceCombat combat)
        {
            int instanceId = combat.GetInstanceID();
            return _remainingCharges.TryGetValue(instanceId, out int charges) ? charges : 0;
        }

        /// <summary>
        /// 보호막 충전 추가
        /// </summary>
        public void AddCharges(PieceCombat combat, int amount)
        {
            int instanceId = combat.GetInstanceID();
            if (_remainingCharges.ContainsKey(instanceId))
            {
                _remainingCharges[instanceId] += amount;
                Debug.Log($"[MothersProtection] {combat.name} 보호막 충전 추가 (+{amount}, 총: {_remainingCharges[instanceId]})");
            }
        }
    }
}
