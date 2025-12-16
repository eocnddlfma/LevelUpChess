using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 나이트 오브 나이츠: 다른 나이트를 제거합니다. 
    /// 이 나이트의 체력 공격력+4 방어력+2 체력 재생+1
    /// </summary>
    [CreateAssetMenu(fileName = "KnightOfKnightsAbility", menuName = "LevelUpChess/Upgrades/Abilities/Knight/KnightOfKnights")]
    public class KnightOfKnightsAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "나이트 오브 나이츠";
        private const string DEFAULT_DESC = "다른 나이트를 제거합니다. 이 나이트의 체력 공격력+4 방어력+2 체력 재생+1";

        [Header("Knight Of Knights Settings")]
        [Tooltip("체력 증가")]
        [SerializeField] private int healthBonus = 4;
        
        [Tooltip("공격력 증가")]
        [SerializeField] private int attackBonus = 4;
        
        [Tooltip("방어력 증가")]
        [SerializeField] private int defenseBonus = 2;
        
        [Tooltip("체력 재생 증가")]
        [SerializeField] private int regenBonus = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[KnightOfKnights] {piece.name}에게 나이트 오브 나이츠 적용!");
            ExecuteSacrifice(piece);
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[KnightOfKnights] {piece.name}에서 나이트 오브 나이츠 제거");
        }

        private void ExecuteSacrifice(ChessPiece owner)
        {
            if (owner == null) return;

            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            int killedKnights = 0;

            foreach (var piece in allPieces)
            {
                if (piece == owner) continue;
                if (piece.Team != owner.Team) continue;
                if (piece.PieceType != PieceType.Knight) continue;
                if (!piece.IsAlive) continue;

                piece.ForceKill();
                killedKnights++;
                Debug.Log($"[KnightOfKnights] {piece.name} 처치됨!");
            }

            // 스탯 증가
            owner.Stats.AddModifier(StatType.MaxHealth, healthBonus);
            owner.Stats.AddModifier(StatType.Attack, attackBonus);
            owner.Stats.AddModifier(StatType.Defense, defenseBonus);
            owner.Stats.AddModifier(StatType.HealthRegeneration, regenBonus);
            owner.SetHealth(owner.Stats.MaxHealth);

            Debug.Log($"[KnightOfKnights] {killedKnights}마리 나이트 희생! 체력+{healthBonus} 공격력+{attackBonus} 방어력+{defenseBonus} 재생+{regenBonus}");
        }

        public override void Execute(AbilityContext context) { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive;
            pieceFilter = PieceTypeFilter.Knight;
        }
#endif
    }
}
