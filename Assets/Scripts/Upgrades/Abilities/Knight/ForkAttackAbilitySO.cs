using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 포크킄: 포크(2개 이상 기물 위협)에 성공했을때 둘다 때립니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ForkAttackAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/ForkAttack")]
    public class ForkAttackAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "포크킄";
        private const string DEFAULT_DESC = "포크에 성공했을때 둘다 때립니다.";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[ForkAttack] {piece.name}에게 포크킄 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[ForkAttack] {piece.name}에서 포크킄 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            // 나이트 위치에서 공격 가능한 모든 적 찾기
            var attackableTiles = context.Owner.GetAttackableTiles();
            List<ChessPiece> enemies = new List<ChessPiece>();

            foreach (var tile in attackableTiles)
            {
                if (tile.OccupyingPiece != null && 
                    tile.OccupyingPiece.Team != context.Owner.Team &&
                    tile.OccupyingPiece != context.Target)
                {
                    enemies.Add(tile.OccupyingPiece);
                }
            }

            // 포크 성공 (2개 이상 위협)
            if (enemies.Count >= 1) // 이미 하나는 공격했으므로 추가로 1개 이상
            {
                foreach (var enemy in enemies)
                {
                    enemy.TakeDamage(context.Damage, context.Owner);
                    Debug.Log($"[ForkAttack] 포크! {enemy.name}도 데미지 {context.Damage}!");
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
