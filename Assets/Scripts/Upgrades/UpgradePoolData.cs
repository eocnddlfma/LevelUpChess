using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 피스별 전용 업그레이드 풀
    /// </summary>
    [System.Serializable]
    public class PieceUpgradePool
    {
        [Tooltip("대상 피스 타입")]
        public PieceType pieceType;
        
        [Tooltip("이 피스 전용 행마법 업그레이드")]
        public List<UpgradeBaseSO> movementUpgrades = new List<UpgradeBaseSO>();
        
        [Tooltip("이 피스 전용 능력 업그레이드")]
        public List<UpgradeBaseSO> abilityUpgrades = new List<UpgradeBaseSO>();
        
        [Tooltip("이 피스 전용 스탯 업그레이드")]
        public List<UpgradeBaseSO> statUpgrades = new List<UpgradeBaseSO>();

        /// <summary>
        /// 이 풀의 모든 업그레이드 반환
        /// </summary>
        public List<UpgradeBaseSO> GetAllUpgrades()
        {
            var all = new List<UpgradeBaseSO>();
            all.AddRange(movementUpgrades);
            all.AddRange(abilityUpgrades);
            all.AddRange(statUpgrades);
            return all;
        }

        /// <summary>
        /// 업그레이드 총 개수
        /// </summary>
        public int Count => movementUpgrades.Count + abilityUpgrades.Count + statUpgrades.Count;
    }


}
