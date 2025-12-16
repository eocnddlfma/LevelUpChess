using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 가속도: 이동할때마다 공격력이 2 증가합니다. 공격시 초기화됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AccelerationAbility", menuName = "LevelUpChess/Upgrades/Abilities/Common/Acceleration")]
    public class AccelerationAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "가속도";
        private const string DEFAULT_DESC = "이동할때마다 공격력이 2 증가합니다. 공격시 초기화됩니다.";

        [Header("Acceleration Settings")]
        [SerializeField] private int attackBonusPerMove = 2;
        
        // 스택된 공격력 보너스 추적 (피스별)
        private System.Collections.Generic.Dictionary<ChessPiece, int> _stackedBonus = 
            new System.Collections.Generic.Dictionary<ChessPiece, int>();

        public override void OnApply(ChessPiece piece)
        {
            _stackedBonus[piece] = 0;
            Debug.Log($"[Acceleration] {piece.name}에게 가속도 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            if (_stackedBonus.TryGetValue(piece, out int bonus) && bonus > 0)
            {
                // 보너스 제거
                piece.Combat.IncreaseAttackPower(-bonus);
            }
            _stackedBonus.Remove(piece);
            Debug.Log($"[Acceleration] {piece.name}에서 가속도 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;

            switch (context.Trigger)
            {
                case AbilityTrigger.OnAfterMove:
                    // 이동 후 공격력 증가
                    if (!_stackedBonus.ContainsKey(context.Owner))
                        _stackedBonus[context.Owner] = 0;
                    
                    _stackedBonus[context.Owner] += attackBonusPerMove;
                    context.Owner.Combat.IncreaseAttackPower(attackBonusPerMove);
                    Debug.Log($"[Acceleration] {context.Owner.name} 공격력 +{attackBonusPerMove} (총 보너스: {_stackedBonus[context.Owner]})");
                    break;
                    
                case AbilityTrigger.OnAttackHit:
                    // 공격 시 보너스 초기화
                    if (_stackedBonus.TryGetValue(context.Owner, out int totalBonus) && totalBonus > 0)
                    {
                        context.Owner.Combat.IncreaseAttackPower(-totalBonus);
                        _stackedBonus[context.Owner] = 0;
                        Debug.Log($"[Acceleration] {context.Owner.name} 공격력 보너스 초기화 (-{totalBonus})");
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive; // Passive로 설정하여 모든 트리거에서 실행
        }
#endif
    }
}
