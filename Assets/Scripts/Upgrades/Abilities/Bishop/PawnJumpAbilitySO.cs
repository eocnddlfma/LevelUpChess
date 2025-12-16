using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 이폰은 이제 제껍니다: 이동칸을 계산할때 폰이 있는 경우 폰을 띄어 넘어서 공격할 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "PawnJumpAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/PawnJump")]
    public class PawnJumpAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "이폰은 이제 제껍니다";
        private const string DEFAULT_DESC = "이동칸을 계산할때 폰이 있는 경우 폰을 띄어 넘어서 공격할 수 있다";

        public override void OnApply(ChessPiece piece)
        {
            // 이동 규칙 수정 (ChessPiece의 이동 계산에서 처리)
            piece.CanJumpOverPawns = true;
            Debug.Log($"[PawnJump] {piece.name}에게 이폰은 이제 제껍니다 적용 - 폰 점프 가능!");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.CanJumpOverPawns = false;
            Debug.Log($"[PawnJump] {piece.name}에서 이폰은 이제 제껍니다 제거");
        }

        public override void Execute(AbilityContext context)
        {
            // 패시브 능력 - 이동 계산에서 처리됨
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive;
            pieceFilter = PieceTypeFilter.Bishop;
        }
#endif
    }
}
