using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 은밀하게 위대하게: 움직이지 않은 턴만큼 데미지가 증가합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PatientHunterAbility", menuName = "LevelUpChess/Upgrades/Abilities/Bishop/PatientHunter")]
    public class PatientHunterAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "은밀하게 위대하게";
        private const string DEFAULT_DESC = "움직이지 않은 턴만큼 데미지가 증가합니다.";

        [Header("Patient Hunter Settings")]
        [Tooltip("턴당 추가 데미지")]
        [SerializeField] private int damagePerTurn = 3;
        
        private int turnsWithoutMoving = 0;

        public override void OnApply(ChessPiece piece)
        {
            turnsWithoutMoving = 0;
            Debug.Log($"[PatientHunter] {piece.name}에게 은밀하게 위대하게 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PatientHunter] {piece.name}에서 은밀하게 위대하게 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;

            // 턴 종료시 이동하지 않았으면 카운트 증가
            if (context.Trigger == AbilityTrigger.OnTurnEnd)
            {
                // 이동 여부는 Owner에서 확인 가능하다고 가정
                if (!context.Owner.HasMovedThisTurn)
                {
                    turnsWithoutMoving++;
                    Debug.Log($"[PatientHunter] 대기 턴: {turnsWithoutMoving}");
                }
            }

            // 이동 후 카운트 리셋
            if (context.Trigger == AbilityTrigger.OnAfterMove)
            {
                turnsWithoutMoving = 0;
            }

            // 공격시 보너스 데미지
            if (context.Trigger == AbilityTrigger.OnAttackHit && turnsWithoutMoving > 0)
            {
                int bonusDamage = turnsWithoutMoving * damagePerTurn;
                context.BonusDamage += bonusDamage;
                Debug.Log($"[PatientHunter] {turnsWithoutMoving}턴 대기! 보너스 데미지 +{bonusDamage}!");
                turnsWithoutMoving = 0; // 공격 후 리셋
            }
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
