using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Core;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 오버로디드 컨트롤 타워: 상대방이 공격을 통해 아군 유닛을 처치하고 이 퀸이 공격할 수 있는 행마법이 존재하는 위치에 적이 있다면 턴을 소모하지 않고 자동 공격합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "OverloadedControlTowerAbility", menuName = "LevelUpChess/Abilities/Queen/OverloadedControlTower")]
    public class OverloadedControlTowerAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "오버로디드 컨트롤 타워";
        private const string DEFAULT_DESC = "상대방이 공격을 통해 아군 유닛을 처치하고 이 퀸이 공격할 수 있는 행마법이 존재하는 위치에 적이 있다면 턴을 소모하지 않고 자동 공격합니다.";

        public override AbilityTrigger Trigger => AbilityTrigger.OnAllyDeath;

        public override void OnApply(ChessPiece piece)
        {
            // 패시브 효과 없음
        }

        public override void OnRemove(ChessPiece piece)
        {
            // 패시브 효과 없음
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger != AbilityTrigger.OnAllyDeath)
                return;

            if (context.Owner == null || context.Attacker == null)
                return;

            // 공격자가 아직 살아있고, 퀸이 공격 가능한 범위에 있는지 확인
            if (!context.Attacker.IsAlive)
                return;

            var attackableTiles = context.Owner.GetAttackableTiles();
            if (attackableTiles == null)
                return;

            var attackerTile = context.Attacker.CurrentTile;
            if (attackerTile == null)
                return;

            // 공격 범위에 있는지 확인
            bool canAttack = false;
            foreach (var tile in attackableTiles)
            {
                if (tile == attackerTile)
                {
                    canAttack = true;
                    break;
                }
            }

            if (canAttack)
            {
                // 즉시 반격
                int damage = context.Owner.AttackPower;
                context.Attacker.TakeDamage(damage, context.Owner);
                
                Debug.Log($"[OverloadedControlTower] {context.Owner.name}이 아군 사망에 대한 복수로 {context.Attacker.name}에게 {damage} 데미지");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAllyDeath;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif
    }
}
