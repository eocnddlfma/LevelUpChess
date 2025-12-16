using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 업그레이드 풀 - 공통/피스별 업그레이드 및 뽑기 시스템 관리
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradePool", menuName = "LevelUpChess/Upgrades/UpgradePool")]
    public class UpgradePoolSO : ScriptableObject
    {
        [Header("=== 공통 업그레이드 (모든 피스 적용 가능) ===")]
        [SerializeField] private List<UpgradeBaseSO> commonMovementUpgrades = new List<UpgradeBaseSO>();
        [SerializeField] private List<UpgradeBaseSO> commonStatUpgrades = new List<UpgradeBaseSO>();
        [SerializeField] private List<UpgradeBaseSO> commonAbilityUpgrades = new List<UpgradeBaseSO>();
        
        [Header("=== 전역 업그레이드 (팀 전체 영향) ===")]
        [SerializeField] private List<UpgradeBaseSO> globalUpgrades = new List<UpgradeBaseSO>();

        [Header("=== 피스별 전용 업그레이드 ===")]
        [SerializeField] private PieceUpgradePool pawnUpgrades = new PieceUpgradePool { pieceType = PieceType.Pawn };
        [SerializeField] private PieceUpgradePool knightUpgrades = new PieceUpgradePool { pieceType = PieceType.Knight };
        [SerializeField] private PieceUpgradePool bishopUpgrades = new PieceUpgradePool { pieceType = PieceType.Bishop };
        [SerializeField] private PieceUpgradePool rookUpgrades = new PieceUpgradePool { pieceType = PieceType.Rook };
        [SerializeField] private PieceUpgradePool queenUpgrades = new PieceUpgradePool { pieceType = PieceType.Queen };
        [SerializeField] private PieceUpgradePool kingUpgrades = new PieceUpgradePool { pieceType = PieceType.King };

        [Header("=== 뽑기 설정 ===")]
        [SerializeField] private UpgradeWeightSettings weightSettings = new UpgradeWeightSettings();

        #region Properties
        
        public UpgradeWeightSettings WeightSettings => weightSettings;
        
        public List<UpgradeBaseSO> CommonMovementUpgrades => commonMovementUpgrades;
        public List<UpgradeBaseSO> CommonStatUpgrades => commonStatUpgrades;
        public List<UpgradeBaseSO> CommonAbilityUpgrades => commonAbilityUpgrades;
        public List<UpgradeBaseSO> GlobalUpgrades => globalUpgrades;
        
        #endregion

        #region Pool Access

        /// <summary>
        /// 피스 타입에 해당하는 전용 풀 반환
        /// </summary>
        public PieceUpgradePool GetPiecePool(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => pawnUpgrades,
                PieceType.Knight => knightUpgrades,
                PieceType.Bishop => bishopUpgrades,
                PieceType.Rook => rookUpgrades,
                PieceType.Queen => queenUpgrades,
                PieceType.King => kingUpgrades,
                _ => null
            };
        }

        /// <summary>
        /// 모든 공통 업그레이드 반환
        /// </summary>
        public List<UpgradeBaseSO> GetAllCommonUpgrades()
        {
            var all = new List<UpgradeBaseSO>();
            all.AddRange(commonMovementUpgrades);
            all.AddRange(commonStatUpgrades);
            all.AddRange(commonAbilityUpgrades);
            return all;
        }

        /// <summary>
        /// 모든 업그레이드 반환 (공통 + 전용 + 전역)
        /// </summary>
        public List<UpgradeBaseSO> GetAllUpgrades()
        {
            var all = new List<UpgradeBaseSO>();
            
            // 공통 업그레이드
            all.AddRange(commonMovementUpgrades);
            all.AddRange(commonStatUpgrades);
            all.AddRange(commonAbilityUpgrades);
            
            // 전역 업그레이드
            all.AddRange(globalUpgrades);
            
            // 피스별 전용 업그레이드
            all.AddRange(pawnUpgrades.GetAllUpgrades());
            all.AddRange(knightUpgrades.GetAllUpgrades());
            all.AddRange(bishopUpgrades.GetAllUpgrades());
            all.AddRange(rookUpgrades.GetAllUpgrades());
            all.AddRange(queenUpgrades.GetAllUpgrades());
            all.AddRange(kingUpgrades.GetAllUpgrades());

            return all;
        }

        /// <summary>
        /// 특정 타입의 공통 업그레이드만 반환
        /// </summary>
        public List<UpgradeBaseSO> GetCommonUpgradesByType(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Movement => new List<UpgradeBaseSO>(commonMovementUpgrades),
                UpgradeType.Stat => new List<UpgradeBaseSO>(commonStatUpgrades),
                UpgradeType.Ability => new List<UpgradeBaseSO>(commonAbilityUpgrades),
                UpgradeType.Global => new List<UpgradeBaseSO>(globalUpgrades),
                _ => new List<UpgradeBaseSO>()
            };
        }

        #endregion

        #region Draw System (뽑기 시스템)

        /// <summary>
        /// 피스에 대해 가중치 기반 뽑기 실행
        /// </summary>
        /// <param name="piece">대상 피스</param>
        /// <param name="count">뽑을 개수</param>
        /// <param name="excludeIds">제외할 업그레이드 ID 목록</param>
        /// <param name="maxRarity">최대 희귀도</param>
        /// <returns>뽑기 결과 리스트</returns>
        public List<UpgradeDrawResult> DrawUpgrades(ChessPiece piece, int count, List<string> excludeIds = null, int maxRarity = 5)
        {
            var results = new List<UpgradeDrawResult>();
            var availableUpgrades = GetWeightedPool(piece, excludeIds, maxRarity);
            
            if (availableUpgrades.Count == 0)
            {
                Debug.LogWarning($"[UpgradePool] {piece.name}에 대한 사용 가능한 업그레이드가 없습니다.");
                return results;
            }

            // 중복 방지를 위한 임시 제외 리스트
            var tempExclude = new HashSet<string>(excludeIds ?? new List<string>());

            for (int i = 0; i < count && availableUpgrades.Count > 0; i++)
            {
                var drawn = DrawOneWeighted(availableUpgrades);
                if (drawn.Upgrade != null)
                {
                    results.Add(drawn);
                    tempExclude.Add(drawn.Upgrade.UpgradeHash);
                    
                    // 뽑힌 업그레이드 제거
                    availableUpgrades.RemoveAll(x => x.upgrade.UpgradeHash == drawn.Upgrade.UpgradeHash);
                }
            }

            return results;
        }

        /// <summary>
        /// 가중치가 적용된 풀 생성
        /// </summary>
        private List<(UpgradeBaseSO upgrade, float weight, bool isCommon)> GetWeightedPool(
            ChessPiece piece, List<string> excludeIds, int maxRarity)
        {
            var pool = new List<(UpgradeBaseSO upgrade, float weight, bool isCommon)>();
            var excludeSet = new HashSet<string>(excludeIds ?? new List<string>());

            // 공통 업그레이드 추가
            AddToWeightedPool(pool, GetAllCommonUpgrades(), piece, excludeSet, maxRarity, isCommon: true);

            // 피스 전용 업그레이드 추가
            var piecePool = GetPiecePool(piece.PieceType);
            if (piecePool != null)
            {
                AddToWeightedPool(pool, piecePool.GetAllUpgrades(), piece, excludeSet, maxRarity, isCommon: false);
            }

            return pool;
        }

        /// <summary>
        /// 가중치 풀에 업그레이드 추가
        /// </summary>
        private void AddToWeightedPool(
            List<(UpgradeBaseSO upgrade, float weight, bool isCommon)> pool,
            List<UpgradeBaseSO> upgrades,
            ChessPiece piece,
            HashSet<string> excludeIds,
            int maxRarity,
            bool isCommon)
        {
            foreach (var upgrade in upgrades)
            {
                if (upgrade == null) continue;
                if (excludeIds.Contains(upgrade.UpgradeHash)) continue;
                if (upgrade.Rarity > maxRarity) continue;
                if (!upgrade.CanApplyTo(piece)) continue;

                // 가중치 계산
                float weight = CalculateWeight(upgrade, isCommon);
                pool.Add((upgrade, weight, isCommon));
            }
        }

        /// <summary>
        /// 업그레이드 가중치 계산
        /// </summary>
        private float CalculateWeight(UpgradeBaseSO upgrade, bool isCommon)
        {
            float weight = 1f;
            
            // 희귀도 가중치
            weight *= weightSettings.GetRarityWeight(upgrade.Rarity);
            
            // 타입 가중치
            weight *= weightSettings.GetTypeWeight(upgrade.UpgradeType);
            
            // 공통/전용 가중치
            if (isCommon)
            {
                weight *= weightSettings.commonPoolChance;
            }
            else
            {
                weight *= (1f - weightSettings.commonPoolChance);
            }

            return weight;
        }

        /// <summary>
        /// 가중치 기반 단일 뽑기
        /// </summary>
        private UpgradeDrawResult DrawOneWeighted(List<(UpgradeBaseSO upgrade, float weight, bool isCommon)> pool)
        {
            if (pool.Count == 0)
            {
                return new UpgradeDrawResult(null, false, 0f);
            }

            // 총 가중치 계산
            float totalWeight = 0f;
            foreach (var item in pool)
            {
                totalWeight += item.weight;
            }

            // 랜덤 값으로 선택
            float randomValue = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var item in pool)
            {
                cumulative += item.weight;
                if (randomValue <= cumulative)
                {
                    return new UpgradeDrawResult(item.upgrade, item.isCommon, item.weight);
                }
            }

            // 폴백 (마지막 아이템)
            var last = pool[pool.Count - 1];
            return new UpgradeDrawResult(last.upgrade, last.isCommon, last.weight);
        }

        #endregion

        #region Legacy Methods (하위 호환)

        /// <summary>
        /// 특정 기물에 적용 가능한 업그레이드 반환 (레거시)
        /// </summary>
        public List<UpgradeBaseSO> GetUpgradesForPiece(ChessPiece piece, List<string> excludeIds = null)
        {
            var applicable = new List<UpgradeBaseSO>();
            
            // 공통 업그레이드
            foreach (var upgrade in GetAllCommonUpgrades())
            {
                if (excludeIds != null && excludeIds.Contains(upgrade.UpgradeHash)) continue;
                if (upgrade.CanApplyTo(piece))
                {
                    applicable.Add(upgrade);
                }
            }

            // 피스 전용 업그레이드
            var piecePool = GetPiecePool(piece.PieceType);
            if (piecePool != null)
            {
                foreach (var upgrade in piecePool.GetAllUpgrades())
                {
                    if (excludeIds != null && excludeIds.Contains(upgrade.UpgradeHash)) continue;
                    if (upgrade.CanApplyTo(piece))
                    {
                        applicable.Add(upgrade);
                    }
                }
            }

            return applicable;
        }

        /// <summary>
        /// 희귀도 필터링
        /// </summary>
        public List<UpgradeBaseSO> FilterByRarity(List<UpgradeBaseSO> upgrades, int maxRarity)
        {
            var filtered = new List<UpgradeBaseSO>();
            
            foreach (var upgrade in upgrades)
            {
                if (upgrade.Rarity <= maxRarity)
                {
                    filtered.Add(upgrade);
                }
            }

            return filtered;
        }

        /// <summary>
        /// ID로 업그레이드 찾기
        /// </summary>
        public UpgradeBaseSO GetUpgradeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            foreach (var upgrade in GetAllUpgrades())
            {
                if (upgrade != null && upgrade.UpgradeHash == id)
                {
                    return upgrade;
                }
            }
            return null;
        }

        /// <summary>
        /// ID(guid)로 먼저 찾고, 없으면 이름으로 찾기
        /// </summary>
        public UpgradeBaseSO GetUpgradeByIdOrName(string idOrName)
        {
            // 1. id(guid)로 먼저 찾기
            var upgrade = GetUpgradeById(idOrName);
            if (upgrade != null)
                return upgrade;

            // 2. 이름으로 찾기 (중복 주의)
            foreach (var u in GetAllUpgrades())
            {
                if (u.UpgradeName == idOrName)
                    return u;
            }
            return null;
        }

        /// <summary>
        /// 업그레이드 인덱스 반환 (네트워크 동기화용)
        /// </summary>
        public int GetUpgradeIndex(UpgradeBaseSO upgrade)
        {
            var all = GetAllUpgrades();
            return all.IndexOf(upgrade);
        }

        /// <summary>
        /// 인덱스로 업그레이드 반환 (네트워크 동기화용)
        /// </summary>
        public UpgradeBaseSO GetUpgradeByIndex(int index)
        {
            var all = GetAllUpgrades();
            if (index >= 0 && index < all.Count)
            {
                return all[index];
            }
            return null;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 풀 통계 정보
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            var stats = new PoolStatistics();
            
            stats.TotalUpgrades = GetAllUpgrades().Count;
            stats.CommonCount = GetAllCommonUpgrades().Count;
            stats.GlobalCount = globalUpgrades.Count;
            
            stats.PawnCount = pawnUpgrades.Count;
            stats.KnightCount = knightUpgrades.Count;
            stats.BishopCount = bishopUpgrades.Count;
            stats.RookCount = rookUpgrades.Count;
            stats.QueenCount = queenUpgrades.Count;
            stats.KingCount = kingUpgrades.Count;

            // 희귀도별 카운트
            foreach (var upgrade in GetAllUpgrades())
            {
                if (upgrade == null) continue;
                stats.RarityCounts[Mathf.Clamp(upgrade.Rarity, 0, 4)]++;
            }

            // 타입별 카운트
            stats.MovementCount = commonMovementUpgrades.Count;
            stats.StatCount = commonStatUpgrades.Count;
            stats.AbilityCount = commonAbilityUpgrades.Count;

            foreach (var pool in new[] { pawnUpgrades, knightUpgrades, bishopUpgrades, rookUpgrades, queenUpgrades, kingUpgrades })
            {
                stats.MovementCount += pool.movementUpgrades.Count;
                stats.StatCount += pool.statUpgrades.Count;
                stats.AbilityCount += pool.abilityUpgrades.Count;
            }

            return stats;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Validate Upgrades")]
        private void ValidateUpgrades()
        {
            var all = GetAllUpgrades();
            var ids = new HashSet<string>();
            int nullCount = 0;
            int emptyIdCount = 0;
            int duplicateCount = 0;
            
            foreach (var upgrade in all)
            {
                if (upgrade == null)
                {
                    nullCount++;
                    continue;
                }

                if (string.IsNullOrEmpty(upgrade.UpgradeHash))
                {
                    Debug.LogError($"[UpgradePool] {upgrade.name}의 해시가 비어있습니다!");
                    emptyIdCount++;
                    continue;
                }

                if (ids.Contains(upgrade.UpgradeHash))
                {
                    Debug.LogError($"[UpgradePool] 중복된 해시: {upgrade.UpgradeHash}");
                    duplicateCount++;
                }
                else
                {
                    ids.Add(upgrade.UpgradeHash);
                }
            }

            var stats = GetStatistics();
            Debug.Log($"[UpgradePool] 검증 완료\n" +
                $"총 업그레이드: {stats.TotalUpgrades}개\n" +
                $"공통: {stats.CommonCount}, 전역: {stats.GlobalCount}\n" +
                $"폰: {stats.PawnCount}, 나이트: {stats.KnightCount}, 비숍: {stats.BishopCount}\n" +
                $"룩: {stats.RookCount}, 퀸: {stats.QueenCount}, 킹: {stats.KingCount}\n" +
                $"희귀도 - Common:{stats.RarityCounts[0]}, Uncommon:{stats.RarityCounts[1]}, " +
                $"Rare:{stats.RarityCounts[2]}, Epic:{stats.RarityCounts[3]}, Legendary:{stats.RarityCounts[4]}\n" +
                $"오류 - Null:{nullCount}, 빈ID:{emptyIdCount}, 중복:{duplicateCount}");
        }

        [ContextMenu("Print Draw Probabilities")]
        private void PrintDrawProbabilities()
        {
            Debug.Log("[UpgradePool] 뽑기 확률 (기본 설정 기준)\n" +
                $"공통 풀 확률: {weightSettings.commonPoolChance * 100}%\n" +
                $"전용 풀 확률: {(1f - weightSettings.commonPoolChance) * 100}%\n\n" +
                $"타입 가중치 - 행마법:{weightSettings.movementWeight}, 스탯:{weightSettings.statWeight}, 능력:{weightSettings.abilityWeight}\n\n" +
                $"희귀도 가중치 - Common:{weightSettings.commonWeight}, Uncommon:{weightSettings.uncommonWeight}, " +
                $"Rare:{weightSettings.rareWeight}, Epic:{weightSettings.epicWeight}, Legendary:{weightSettings.legendaryWeight}");
        }
#endif
    }

    /// <summary>
    /// 풀 통계 구조체
    /// </summary>
    public struct PoolStatistics
    {
        public int TotalUpgrades;
        public int CommonCount;
        public int GlobalCount;
        
        public int PawnCount;
        public int KnightCount;
        public int BishopCount;
        public int RookCount;
        public int QueenCount;
        public int KingCount;

        public int MovementCount;
        public int StatCount;
        public int AbilityCount;

        public int[] RarityCounts;

        public PoolStatistics(int dummy = 0)
        {
            TotalUpgrades = 0;
            CommonCount = 0;
            GlobalCount = 0;
            PawnCount = 0;
            KnightCount = 0;
            BishopCount = 0;
            RookCount = 0;
            QueenCount = 0;
            KingCount = 0;
            MovementCount = 0;
            StatCount = 0;
            AbilityCount = 0;
            RarityCounts = new int[5];
        }
    }
}
