using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 나 혼자 산다: 즉시 내 팀의 모든 다른 폰들이 처치됨. 
    /// 처치된 경험치는 해당 폰이 5배로 흡수됨, 체력, 공격력이 2배가 됨.
    /// </summary>
    [CreateAssetMenu(fileName = "LoneWolfAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/LoneWolf")]
    public class LoneWolfAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "나 혼자 산다";
        private const string DEFAULT_DESC = "즉시 내 팀의 모든 다른 폰들이 처치됨. 처치된 경험치는 해당 폰이 5배로 흡수됨, 체력, 공격력이 2배가 됨.";

        [Header("Lone Wolf Settings")]
        [Tooltip("처치된 폰 경험치 배율")]
        [SerializeField] private float expMultiplier = 5f;
        
        [Tooltip("체력/공격력 배율")]
        [SerializeField] private float statMultiplier = 2f;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[LoneWolf] {piece.name}에게 나 혼자 산다 적용 - 다른 폰들 처치 시작!");
            ExecuteSacrifice(piece);
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[LoneWolf] {piece.name}에서 나 혼자 산다 제거");
        }

        private void ExecuteSacrifice(ChessPiece owner)
        {
            if (owner == null) return;

            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            int totalExp = 0;
            int killedPawns = 0;

            foreach (var piece in allPieces)
            {
                if (piece == owner) continue;
                if (piece.Team != owner.Team) continue;
                if (piece.PieceType != PieceType.Pawn) continue;
                if (!piece.IsAlive) continue;

                int pawnExp = piece.Stats.Level * 10;
                totalExp += Mathf.RoundToInt(pawnExp * expMultiplier);
                killedPawns++;

                piece.Combat.Die(owner);
                Debug.Log($"[LoneWolf] {piece.name} 처치됨!");
            }

            if (totalExp > 0)
            {
                owner.GainExperience(totalExp);
                Debug.Log($"[LoneWolf] 총 {totalExp} 경험치 흡수!");
            }

            int currentMaxHealth = owner.Stats.MaxHealth;
            int currentAttack = owner.Stats.Attack;
            
            owner.Stats.AddModifier(StatType.MaxHealth, Mathf.RoundToInt(currentMaxHealth * (statMultiplier - 1)));
            owner.Stats.AddModifier(StatType.Attack, Mathf.RoundToInt(currentAttack * (statMultiplier - 1)));
            owner.SetHealth(owner.Stats.MaxHealth);
            
            Debug.Log($"[LoneWolf] {killedPawns}마리 폰 희생! 체력/공격력 {statMultiplier}배 증가!");
        }

        public override void Execute(AbilityContext context) { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.Passive;
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif
    }
}
