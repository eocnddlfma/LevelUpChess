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

    /// <summary>
    /// 뽑기 가중치 설정
    /// </summary>
    [System.Serializable]
    public class UpgradeWeightSettings
    {
        [Header("타입별 가중치")]
        [Tooltip("행마법 업그레이드 등장 가중치")]
        [Range(0f, 10f)]
        public float movementWeight = 1f;
        
        [Tooltip("스탯 업그레이드 등장 가중치")]
        [Range(0f, 10f)]
        public float statWeight = 2f;
        
        [Tooltip("능력 업그레이드 등장 가중치")]
        [Range(0f, 10f)]
        public float abilityWeight = 1.5f;

        [Header("희귀도별 가중치")]
        [Tooltip("Common (0) 등장 가중치")]
        public float commonWeight = 50f;
        
        [Tooltip("Uncommon (1) 등장 가중치")]
        public float uncommonWeight = 30f;
        
        [Tooltip("Rare (2) 등장 가중치")]
        public float rareWeight = 15f;
        
        [Tooltip("Epic (3) 등장 가중치")]
        public float epicWeight = 4f;
        
        [Tooltip("Legendary (4) 등장 가중치")]
        public float legendaryWeight = 1f;

        [Header("공통/전용 비율")]
        [Tooltip("공통 업그레이드 등장 확률 (0~1)")]
        [Range(0f, 1f)]
        public float commonPoolChance = 0.5f;

        /// <summary>
        /// 희귀도에 따른 가중치 반환
        /// </summary>
        public float GetRarityWeight(int rarity)
        {
            return rarity switch
            {
                0 => commonWeight,
                1 => uncommonWeight,
                2 => rareWeight,
                3 => epicWeight,
                4 => legendaryWeight,
                _ => commonWeight
            };
        }

        /// <summary>
        /// 타입에 따른 가중치 반환
        /// </summary>
        public float GetTypeWeight(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Movement => movementWeight,
                UpgradeType.Stat => statWeight,
                UpgradeType.Ability => abilityWeight,
                _ => 1f
            };
        }
    }

    /// <summary>
    /// 뽑기 결과
    /// </summary>
    public struct UpgradeDrawResult
    {
        public UpgradeBaseSO Upgrade;
        public bool IsFromCommonPool;
        public float DrawWeight;

        public UpgradeDrawResult(UpgradeBaseSO upgrade, bool isCommon, float weight)
        {
            Upgrade = upgrade;
            IsFromCommonPool = isCommon;
            DrawWeight = weight;
        }
    }
}
