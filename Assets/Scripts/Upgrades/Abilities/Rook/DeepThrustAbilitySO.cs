using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 깊은 찌르기: 공격 적중시 직선 방향 뒤에 있는 적에게도 피해
    /// </summary>
    [CreateAssetMenu(fileName = "DeepThrustAbility", menuName = "LevelUpChess/Upgrades/Abilities/Rook/DeepThrust")]
    public class DeepThrustAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "깊은 찌르기";
        private const string DEFAULT_DESC = "공격한 방향으로 공격받은 대상 뒤에 있는 대상까지 피해를 입음.";

        [Header("Deep Thrust Settings")]
        [Tooltip("뒤에 있는 적에게 가하는 피해 비율 (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float damageRatio = 0.5f;
        
        [Tooltip("관통 가능한 최대 적 수")]
        [SerializeField] private int maxPierceCount = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[DeepThrust] {piece.name}에게 깊은 찌르기 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[DeepThrust] {piece.name}에서 깊은 찌르기 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Target == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var ownerTile = context.Owner.CurrentTile;
            var targetTile = context.Target.CurrentTile;
            
            if (ownerTile == null || targetTile == null) return;

            // 공격 방향 계산
            Vector2Int direction = Vector2Int.zero;
            int dx = targetTile.coordinate.x - ownerTile.coordinate.x;
            int dy = targetTile.coordinate.y - ownerTile.coordinate.y;

            // 직선 방향인지 확인 (룩은 직선으로만 공격)
            if (dx == 0 && dy != 0)
            {
                direction = new Vector2Int(0, dy > 0 ? 1 : -1);
            }
            else if (dy == 0 && dx != 0)
            {
                direction = new Vector2Int(dx > 0 ? 1 : -1, 0);
            }
            else
            {
                // 직선이 아니면 관통 불가
                return;
            }

            // 타겟 뒤에 있는 적 찾기
            int pierceCount = 0;
            Vector2Int checkPos = targetTile.coordinate + direction;

            while (pierceCount < maxPierceCount)
            {
                var behindTile = boardManager.GetTileAt(checkPos);
                if (behindTile == null) break;

                if (behindTile.OccupyingPiece != null)
                {
                    if (behindTile.OccupyingPiece.Team != context.Owner.Team)
                    {
                        // 적에게 피해
                        int pierceDamage = Mathf.RoundToInt(context.Damage * damageRatio);
                        behindTile.OccupyingPiece.Stats.TakeDamage(pierceDamage);
                        pierceCount++;
                        Debug.Log($"[DeepThrust] 관통! {behindTile.OccupyingPiece.name}에게 {pierceDamage} 피해!");
                    }
                    else
                    {
                        // 아군이면 관통 중단
                        break;
                    }
                }

                checkPos += direction;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif
    }
}
