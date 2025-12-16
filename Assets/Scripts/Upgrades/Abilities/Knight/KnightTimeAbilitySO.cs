using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Events;
using LevelUpChess.Managers;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 나이트 타임: 현재 턴수를 3으로 나눈 값이 2일 경우 데미지를 받지 않고 공격력 +5가 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "KnightTimeAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/KnightTime")]
    public class KnightTimeAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "나이트 타임";
        private const string DEFAULT_DESC = "현재 턴수를 3으로 나눈 값이 2일 경우 데미지를 받지 않고 공격력 +5가 됩니다.";

        [Header("Knight Time Settings")]
        [SerializeField] private int attackBonus = 5;

        private ChessPiece _owner;

        public override void OnApply(ChessPiece piece)
        {
            _owner = piece;
            Bus<OnTurnStart>.OnEvent += OnTurnChanged;
            UpdateAttackBonus();
            Debug.Log($"[KnightTime] {piece.name}에게 나이트 타임 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Bus<OnTurnStart>.OnEvent -= OnTurnChanged;
            RemoveAttackBonus();
            _owner = null;
            Debug.Log($"[KnightTime] {piece.name}에서 나이트 타임 제거");
        }

        private void OnTurnChanged(OnTurnStart evt)
        {
            UpdateAttackBonus();
        }

        private void UpdateAttackBonus()
        {
            if (_owner == null) return;
            var gameManager = LevelUpChess.Core.ServiceLocator.Get<NetworkGameManager>();
            if (gameManager.TurnCount % 3 == 2)
            {
                _owner.Stats.AddModifier(StatType.Attack, attackBonus);
                Debug.Log($"[KnightTime] {gameManager.TurnCount}턴 - 공격력 +{attackBonus}");
            }
            else
            {
                _owner.Stats.RemoveModifier(StatType.Attack, attackBonus);
                Debug.Log($"[KnightTime] {gameManager.TurnCount}턴 - 공격력 -{attackBonus}");
            }
        }

        private void RemoveAttackBonus()
        {
            if (_owner != null)
            {
                _owner.Stats.RemoveModifier(StatType.Attack, attackBonus);
            }
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnHit) return;

            // 턴 조건 체크: 현재 턴수를 3으로 나눈 나머지가 2일 경우
            var gameManager = LevelUpChess.Core.ServiceLocator.Get<NetworkGameManager>();
            if (gameManager.TurnCount % 3 != 2) return;

            // 데미지 무효화
            context.ShouldEvade = true;
            Debug.Log("[KnightTime] 데미지 무효화!");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnHit;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
