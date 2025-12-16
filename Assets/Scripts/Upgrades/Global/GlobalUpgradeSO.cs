using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 전역 강화 업그레이드 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewGlobalUpgrade", menuName = "LevelUpChess/Upgrades/Global Upgrade")]
    public class GlobalUpgradeSO_Old : UpgradeBaseSO
    {
        [Header("전역 강화 설정")]
        [SerializeField] private bool hasNegativeEffect = false;
        [SerializeField, TextArea(2, 4)] private string negativeEffectDescription;
        
        public bool HasNegativeEffect => hasNegativeEffect;
        public string NegativeEffectDescription => negativeEffectDescription;
        
        public override void Apply(ChessPiece piece)
        {
            // 전역 강화는 ApplyToTeam을 통해 적용
            Debug.LogWarning("[GlobalUpgrade] Use ApplyToTeam instead of Apply for global upgrades");
        }
        
        public override void ApplyToTeam(Team team)
        {
            Debug.Log($"[GlobalUpgrade] {upgradeName} applied to team {team}");
            // 서브클래스에서 구체적인 효과 구현
        }

        /// <summary>
        /// 팀 전체에 업그레이드 적용 (피스 리스트 버전)
        /// </summary>
        public virtual void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            // 서브클래스에서 구체적인 효과 구현
        }

        /// <summary>
        /// 팀에서 업그레이드 제거 (피스 리스트 버전)
        /// </summary>
        public virtual void RemoveFromTeam(int teamId, List<ChessPiece> pieces)
        {
            // 서브클래스에서 구체적인 효과 구현
        }
        
        public override string GetFormattedDescription()
        {
            string desc = description;
            if (hasNegativeEffect && !string.IsNullOrEmpty(negativeEffectDescription))
            {
                desc += $"\n<color=red>{negativeEffectDescription}</color>";
            }
            return desc;
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeType = UpgradeType.Global;
            target = UpgradeTarget.Player;
        }
#endif
    }
}
