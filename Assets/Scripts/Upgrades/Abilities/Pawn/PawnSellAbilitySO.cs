using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 폰 팔이: 이 피스를 제거합니다. 단체 강화 1개를 얻습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PawnSellAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/PawnSell")]
    public class PawnSellAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "폰 팔이";
        private const string DEFAULT_DESC = "이 피스를 제거합니다. 단체 강화 1개를 얻습니다.";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[PawnSell] {piece.name}에게 폰 팔이 적용 - 즉시 제거되고 단체 강화 획득!");
            ExecuteSell(piece);
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PawnSell] {piece.name}에서 폰 팔이 제거");
        }

        private void ExecuteSell(ChessPiece piece)
        {
            if (piece == null) return;

            Team team = piece.Team;

            // 단체 강화 1개 부여 (선택)
            var upgradeManager = LevelUpChess.Core.ServiceLocator.Get<UpgradeManager>();
            if (upgradeManager != null)
            {
                upgradeManager.GrantGlobalUpgradeWithChoice(team);
                Debug.Log($"[PawnSell] {team}팀에 단체 강화 선택지 제공!");
            }

            // 폰 제거
            piece.ForceKill();
            Debug.Log($"[PawnSell] {piece.name} 제거됨!");
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
