using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 폭탄 투하: 공격한 범위 기준 십자로 스플래시 데미지
    /// </summary>
    [CreateAssetMenu(fileName = "BombDropAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/BombDrop")]
    public class BombDropAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "폭탄 투하";
        private const string DEFAULT_DESC = "공격한 범위 기준 십자로 스플뎀";

        [Header("Bomb Drop Settings")]
        [Tooltip("스플래시 데미지 비율")]
        [SerializeField] private float splashDamageRatio = 0.5f;
        
        [Tooltip("스플래시 범위")]
        [SerializeField] private int splashRange = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[BombDrop] {piece.name}에게 폭탄 투하 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[BombDrop] {piece.name}에서 폭탄 투하 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            if (context.Target?.CurrentTile == null) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            Vector2Int targetPos = context.Target.CurrentTile.coordinate;
            int splashDamage = Mathf.RoundToInt(context.Damage * splashDamageRatio);

            // 십자 방향 스플래시
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var dir in directions)
            {
                for (int i = 1; i <= splashRange; i++)
                {
                    Vector2Int splashPos = targetPos + dir * i;
                    var tile = boardManager.GetTileAt(splashPos);
                    
                    if (tile?.OccupyingPiece != null && 
                        tile.OccupyingPiece.Team != context.Owner.Team &&
                        tile.OccupyingPiece != context.Target)
                    {
                        tile.OccupyingPiece.Stats.TakeDamage(splashDamage, context.Owner);
                        Debug.Log($"[BombDrop] {tile.OccupyingPiece.name}에게 스플래시 데미지 {splashDamage}!");
                    }
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
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
