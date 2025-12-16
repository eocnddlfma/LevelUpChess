using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 텔레포트 능력: 지정된 위치로 순간 이동
    /// </summary>
    [CreateAssetMenu(fileName = "TeleportAbility", menuName = "LevelUpChess/Upgrades/Abilities/Teleport")]
    public class TeleportAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "텔레포트";
        private const string DEFAULT_DESC = "지정된 위치로 순간 이동";

        [Header("Teleport Settings")]
        [Tooltip("텔레포트 최대 거리")]
        [SerializeField] private int maxTeleportDistance = 3;

        [Tooltip("텔레포트 쿨다운 (턴)")]
        [SerializeField] private int cooldownTurns = 2;

        [Tooltip("적 위치로 텔레포트 가능")]
        [SerializeField] private bool canTeleportToEnemy = false;

        public new string AbilityId => "ability_teleport";

        public int MaxTeleportDistance => maxTeleportDistance;
        public int CooldownTurns => cooldownTurns;
        public bool CanTeleportToEnemy => canTeleportToEnemy;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
        }
#endif
    }
}