using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 은신: 이동한 후 1턴간 무적 상태가 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StealthAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/Stealth")]
    public class StealthAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "은신";
        private const string DEFAULT_DESC = "이동한 후 1턴간 무적 상태가 됩니다.";

        [Header("Stealth Settings")]
        [Tooltip("무적 지속 턴수")]
        [SerializeField] private int invincibleTurns = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[Stealth] {piece.name}에게 은신 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[Stealth] {piece.name}에서 은신 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;

            // 이동 후 무적 부여
            if (context.Trigger == AbilityTrigger.OnAfterMove)
            {
                context.Owner.GrantInvincibility(invincibleTurns);
                Debug.Log($"[Stealth] {context.Owner.name} {invincibleTurns}턴 무적!");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAfterMove;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
