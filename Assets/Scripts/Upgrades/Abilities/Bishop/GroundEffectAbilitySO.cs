using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 장판: 상대방을 공격한 위치에 장판을 깝니다. 해당 장판 위에 있는 적은 비숍 공격력의 /3을 입습니다. 장판 지속시간 4턴
    /// </summary>
    [CreateAssetMenu(fileName = "GroundEffectAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/GroundEffect")]
    public class GroundEffectAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "장판";
        private const string DEFAULT_DESC = "상대방을 공격한 위치에 장판을 깝니다. 해당 장판 위에 있는 적은 비숍 공격력의 /3을 입습니다. 장판 지속시간 4턴";

        [Header("Ground Effect Settings")]
        [Tooltip("공격력 대비 데미지 비율")]
        [SerializeField] private float damageRatio = 0.33f;
        
        [Tooltip("장판 지속 턴수")]
        [SerializeField] private int duration = 4;
        [SerializeField] private GameObject groundEffectPrefab;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[GroundEffect] {piece.name}에게 장판 깔기 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[GroundEffect] {piece.name}에서 장판 깔기 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;
            if (context.Target?.CurrentTile == null) return;

            var tile = context.Target.CurrentTile;
            int damage = Mathf.RoundToInt(context.Owner.Stats.Attack * damageRatio);

            // 장판 생성 (TileEffect 시스템 사용)
            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager != null)
            {
                boardManager.CreateGroundEffect(tile.coordinate, damage, duration, context.Owner.Team, groundEffectPrefab);
                Debug.Log($"[GroundEffect] {tile.coordinate}에 장판 생성! 데미지: {damage}, 지속: {duration}턴");
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
