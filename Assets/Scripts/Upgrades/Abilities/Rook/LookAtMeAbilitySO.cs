using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 룩엣미: 이 피스를 공격한 피스를 이 피스가 죽기 전까지 다른 대상을 공격할 수 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "LookAtMeAbility", menuName = "LevelUpChess/Upgrades/Abilities/Rook/LookAtMe")]
    public class LookAtMeAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "룩엣미";
        private const string DEFAULT_DESC = "이 피스를 공격한 피스를 이 피스가 죽기 전까지 다른 대상을 공격할 수 없습니다.";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[LookAtMe] {piece.name}에게 룩엣미 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[LookAtMe] {piece.name}에서 룩엣미 제거");
            piece.ClearTauntedBy();
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Trigger == AbilityTrigger.OnHit && context.Attacker != null)
            {
                // 공격받았을 때 공격자를 기록
                context.Owner.AddTauntedBy(context.Attacker);
                Debug.Log($"[LookAtMe] {context.Attacker.name}이(가) {context.Owner.name}을(를) 공격함 - 도발 적용!");
            }
            else if (context.Trigger == AbilityTrigger.OnDeath)
            {
                // 죽을 때 도발 해제
                context.Owner.ClearTauntedBy();
                Debug.Log($"[LookAtMe] {context.Owner.name} 사망 - 도발 해제!");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive;
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif
    }
}

// ChessPiece 확장 메서드를 위한 부분 클래스 예시
namespace LevelUpChess.Pieces
{
    public partial class ChessPiece
    {
        private System.Collections.Generic.HashSet<ChessPiece> _tauntedBy = new System.Collections.Generic.HashSet<ChessPiece>();

        public void AddTauntedBy(ChessPiece attacker)
        {
            if (attacker != null)
            {
                _tauntedBy.Add(attacker);
            }
        }

        public void ClearTauntedBy()
        {
            _tauntedBy.Clear();
        }

        public bool CanAttackTarget(ChessPiece target)
        {
            if (_tauntedBy.Count > 0 && !_tauntedBy.Contains(target))
            {
                return false; // 도발된 상태에서 도발자가 아닌 대상 공격 불가
            }
            return true;
        }
    }
}
