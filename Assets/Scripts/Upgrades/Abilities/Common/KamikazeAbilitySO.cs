using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 카미카제: 사망시 현재 공격력만큼의 데미지를 주변 8칸에 줌.
    /// </summary>
    [CreateAssetMenu(fileName = "KamikazeAbility", menuName = "LevelUpChess/Upgrades/Abilities/Common/Kamikaze")]
    public class KamikazeAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "카미카제";
        private const string DEFAULT_DESC = "사망시 현재 공격력만큼의 데미지를 주변 8칸에 줌.";

        [Header("Kamikaze Settings")]
        [SerializeField] private float damageMultiplier = 1f;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[Kamikaze] {piece.name}에게 카미카제 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[Kamikaze] {piece.name}에서 카미카제 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            
            if (context.Trigger == AbilityTrigger.OnDeath)
            {
                var currentTile = context.Owner.CurrentTile;
                if (currentTile == null) return;
                
                int damage = Mathf.RoundToInt(context.Owner.Combat.AttackPower * damageMultiplier);
                Vector2Int pos = currentTile.coordinate;
                
                // 주변 8칸에 데미지
                Vector2Int[] offsets = new Vector2Int[]
                {
                    new(-1, -1), new(0, -1), new(1, -1),
                    new(-1, 0),              new(1, 0),
                    new(-1, 1),  new(0, 1),  new(1, 1)
                };

                var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
                if (boardManager == null) return;

                foreach (var offset in offsets)
                {
                    Vector2Int targetPos = pos + offset;
                    var tile = boardManager.GetTileAt(targetPos);
                    
                    if (tile != null && tile.OccupyingPiece != null)
                    {
                        var target = tile.OccupyingPiece;
                        // 적군에게만 데미지 (아군 포함하려면 조건 제거)
                        if (target.Team != context.Owner.Team)
                        {
                            target.Combat.TakeDamage(damage, context.Owner);
                            Debug.Log($"[Kamikaze] {target.name}에게 {damage} 폭발 데미지!");
                        }
                    }
                }
                
                Debug.Log($"[Kamikaze] {context.Owner.name} 폭발! 주변에 {damage} 데미지");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnDeath;
        }
#endif
    }
}
