using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 연계 공격: 아군을 공격할 수 있습니다. 
    /// 공격한 아군의 공격범위에 적이 있다면 해당 아군은 피해를 입지 않고 비숍의 데미지를 적에게 가합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ChainAttackAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/ChainAttack")]
    public class ChainAttackAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "연계 공격";
        private const string DEFAULT_DESC = "아군을 공격할 수 있습니다. 공격한 아군의 공격범위에 적이 있다면 해당 아군은 피해를 입지 않고 비숍의 데미지를 적에게 가합니다.";

        public override void OnApply(ChessPiece piece)
        {
            piece.CanAttackAllies = true;
            Debug.Log($"[ChainAttack] {piece.name}에게 연계 공격 적용 - 아군 공격 가능!");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.CanAttackAllies = false;
            Debug.Log($"[ChainAttack] {piece.name}에서 연계 공격 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            // 아군을 공격한 경우만 처리
            if (context.Target.Team != context.Owner.Team) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            // 아군 피해 무효화
            context.ShouldEvade = true;

            // 아군의 공격 범위 내 적 찾기
            var attackableTiles = context.Target.GetAttackableTiles();
            List<ChessPiece> enemies = new List<ChessPiece>();

            foreach (var tile in attackableTiles)
            {
                if (tile.OccupyingPiece != null && 
                    tile.OccupyingPiece.Team != context.Owner.Team)
                {
                    enemies.Add(tile.OccupyingPiece);
                }
            }

            // 적들에게 비숍 데미지 전달
            foreach (var enemy in enemies)
            {
                enemy.TakeDamage(context.Damage, context.Owner);
                Debug.Log($"[ChainAttack] {context.Target.name} 경유 → {enemy.name}에게 {context.Damage} 데미지!");
            }

            if (enemies.Count == 0)
            {
                Debug.Log($"[ChainAttack] {context.Target.name}의 공격 범위 내 적 없음");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
