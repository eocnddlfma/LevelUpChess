using UnityEngine;
using UnityEditor;
using LevelUpChess.Upgrades;
using LevelUpChess.Upgrades.Abilities;
using LevelUpChess.Upgrades.Global;
using LevelUpChess.Upgrades.Movement;
using LevelUpChess.Upgrades.Stat;
using LevelUpChess.Pieces;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 업그레이드 ScriptableObject 자동 생성 및 Pool 등록 에디터
    /// </summary>
    public class UpgradeAutoGenerator : EditorWindow
    {
        private const string SO_BASE_PATH = "Assets/ScriptableObject/Upgrades";
        private const string POOL_PATH = "Assets/ScriptableObject/Upgrades/MainUpgradePool.asset";

        private Vector2 scrollPosition;
        private UpgradePoolSO upgradePool;
        
        // 생성할 업그레이드 타입 선택
        private bool createAbilities = true;
        private bool createMovements = true;
        private bool createGlobals = true;
        private bool createStats = true;

        [MenuItem("Tools/LevelUpChess/Auto Generate Upgrades")]
        public static void ShowWindow()
        {
            var window = GetWindow<UpgradeAutoGenerator>("Upgrade Auto Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadUpgradePool();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("업그레이드 자동 생성 및 Pool 등록", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "이 도구는 모든 업그레이드 ScriptableObject를 자동으로 생성하고 MainUpgradePool에 등록합니다.",
                MessageType.Info);

            EditorGUILayout.Space();

            // UpgradePool 참조
            GUILayout.Label("Upgrade Pool:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            upgradePool = (UpgradePoolSO)EditorGUILayout.ObjectField("Main Upgrade Pool", upgradePool, typeof(UpgradePoolSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                SaveUpgradePoolReference();
            }

            if (upgradePool == null)
            {
                EditorGUILayout.HelpBox("UpgradePool이 없습니다. 먼저 생성하거나 불러오세요.", MessageType.Warning);
                
                if (GUILayout.Button("MainUpgradePool 생성", GUILayout.Height(30)))
                {
                    CreateMainUpgradePool();
                }
            }

            EditorGUILayout.Space();

            // MovementSO 생성
            GUILayout.Label("Movement SO 생성:", EditorStyles.boldLabel);
            if (GUILayout.Button("Upgradable Movement SO 생성", GUILayout.Height(30)))
            {
                CreateUpgradableMovementSOs();
            }

            EditorGUILayout.Space();

            // 생성 옵션
            GUILayout.Label("생성할 업그레이드 타입:", EditorStyles.boldLabel);
            createAbilities = EditorGUILayout.Toggle("Ability Upgrades (능력)", createAbilities);
            createMovements = EditorGUILayout.Toggle("Movement Upgrades (이동)", createMovements);
            createGlobals = EditorGUILayout.Toggle("Global Upgrades (글로벌)", createGlobals);
            createStats = EditorGUILayout.Toggle("Stat Upgrades (스탯)", createStats);

            EditorGUILayout.Space();

            // 현재 Pool 상태 표시
            if (upgradePool != null)
            {
                DrawPoolStatus();
            }

            EditorGUILayout.Space();

            // 실행 버튼
            GUI.enabled = upgradePool != null && (createAbilities || createMovements || createGlobals || createStats);
            if (GUILayout.Button("전체 업그레이드 자동 생성 및 등록", GUILayout.Height(40)))
            {
                GenerateAllUpgrades();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            if (GUILayout.Button("Pool에서 누락된 항목만 추가", GUILayout.Height(30)))
            {
                AddMissingUpgradesToPool();
            }

            if (GUILayout.Button("Pool 정리 (null 제거)", GUILayout.Height(30)))
            {
                CleanupPool();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPoolStatus()
        {
            EditorGUILayout.LabelField("Current Pool Status:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var so = new SerializedObject(upgradePool);
            so.Update();

            int commonMovement = so.FindProperty("commonMovementUpgrades")?.arraySize ?? 0;
            int commonStat = so.FindProperty("commonStatUpgrades")?.arraySize ?? 0;
            int commonAbility = so.FindProperty("commonAbilityUpgrades")?.arraySize ?? 0;
            int globals = so.FindProperty("globalUpgrades")?.arraySize ?? 0;

            EditorGUILayout.LabelField($"Common Movement: {commonMovement}");
            EditorGUILayout.LabelField($"Common Stat: {commonStat}");
            EditorGUILayout.LabelField($"Common Ability: {commonAbility}");
            EditorGUILayout.LabelField($"Global: {globals}");

            DrawPiecePool(so.FindProperty("pawnUpgrades"), "Pawn");
            DrawPiecePool(so.FindProperty("knightUpgrades"), "Knight");
            DrawPiecePool(so.FindProperty("bishopUpgrades"), "Bishop");
            DrawPiecePool(so.FindProperty("rookUpgrades"), "Rook");
            DrawPiecePool(so.FindProperty("queenUpgrades"), "Queen");
            DrawPiecePool(so.FindProperty("kingUpgrades"), "King");

            EditorGUI.indentLevel--;
        }

        private void DrawPiecePool(SerializedProperty poolProp, string label)
        {
            if (poolProp == null)
            {
                EditorGUILayout.LabelField($"{label}: (none)");
                return;
            }

            int movement = poolProp.FindPropertyRelative("movementUpgrades")?.arraySize ?? 0;
            int stat = poolProp.FindPropertyRelative("statUpgrades")?.arraySize ?? 0;
            int ability = poolProp.FindPropertyRelative("abilityUpgrades")?.arraySize ?? 0;
            int total = movement + stat + ability;
            EditorGUILayout.LabelField($"{label}: total {total} (Movement {movement}, Stat {stat}, Ability {ability})");
        }
        private void LoadUpgradePool()
        {
            if (upgradePool == null)
            {
                upgradePool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(POOL_PATH);
            }
        }

        private void SaveUpgradePoolReference()
        {
            EditorPrefs.SetString("UpgradeAutoGenerator_PoolPath", AssetDatabase.GetAssetPath(upgradePool));
        }

        private void CreateMainUpgradePool()
        {
            if (!Directory.Exists(SO_BASE_PATH))
            {
                Directory.CreateDirectory(SO_BASE_PATH);
            }

            upgradePool = ScriptableObject.CreateInstance<UpgradePoolSO>();
            AssetDatabase.CreateAsset(upgradePool, POOL_PATH);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[UpgradeAutoGenerator] MainUpgradePool 생성 완료: {POOL_PATH}");
            EditorUtility.DisplayDialog("완료", "MainUpgradePool이 생성되었습니다!", "확인");
        }

        private void GenerateAllUpgrades()
        {
            if (upgradePool == null)
            {
                EditorUtility.DisplayDialog("오류", "UpgradePool이 설정되지 않았습니다.", "확인");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("업그레이드 생성", "폴더 생성 중...", 0f);
                CreateDirectories();

                int progress = 0;
                int total = 4;

                if (createAbilities)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 생성", "Ability 업그레이드 생성 중...", (float)progress / total);
                    GenerateAbilityUpgrades();
                    progress++;
                }

                if (createMovements)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 생성", "Movement 업그레이드 생성 중...", (float)progress / total);
                    GenerateMovementUpgrades();
                    progress++;
                }

                if (createGlobals)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 생성", "Global 업그레이드 생성 중...", (float)progress / total);
                    GenerateGlobalUpgrades();
                    progress++;
                }

                if (createStats)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 생성", "Stat 업그레이드 생성 중...", (float)progress / total);
                    GenerateStatUpgrades();
                    progress++;
                }

                EditorUtility.DisplayProgressBar("업그레이드 생성", "Pool에 등록 중...", 0.9f);
                RegisterAllUpgradesToPool();

                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("완료", 
                    "모든 업그레이드가 생성되고 Pool에 등록되었습니다!", 
                    "확인");

                Debug.Log("[UpgradeAutoGenerator] 모든 업그레이드 생성 및 등록 완료!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("오류", $"생성 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[UpgradeAutoGenerator] 오류: {e}");
            }
        }

        private void CreateDirectories()
        {
            string[] dirs = {
                SO_BASE_PATH,
                $"{SO_BASE_PATH}/Ability",
                $"{SO_BASE_PATH}/Movement",
                $"{SO_BASE_PATH}/Global",
                $"{SO_BASE_PATH}/Stat"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        private void GenerateAbilityUpgrades()
        {
            CreateAbilityDirectories();
            CreateAssetsFromTypes<AbilityBaseSO>("Ability");
        }

        private void CreateAbilityDirectories()
        {
            string[] dirs = {
                $"{SO_BASE_PATH}/Ability",
                $"{SO_BASE_PATH}/Ability/Common",
                $"{SO_BASE_PATH}/Ability/Pawn",
                $"{SO_BASE_PATH}/Ability/Rook",
                $"{SO_BASE_PATH}/Ability/Bishop",
                $"{SO_BASE_PATH}/Ability/Knight",
                $"{SO_BASE_PATH}/Ability/Queen"
            };
            
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }
        
        private void CreateSpecificAbility<T>(string subFolder, string name, string displayName, 
            string description, int rarity, PieceTypeFilter filter) where T : AbilityBaseSO
        {
            string dir = $"{SO_BASE_PATH}/Ability/{subFolder}";
            string path = $"{dir}/{name}.asset";
            
            if (File.Exists(path)) return;
            
            var so = ScriptableObject.CreateInstance<T>();
            so.name = name;
            
            var serialized = new SerializedObject(so);
            serialized.FindProperty("upgradeName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("rarity").intValue = rarity;
            serialized.FindProperty("pieceFilter").enumValueIndex = (int)filter;
            serialized.FindProperty("upgradeType").enumValueIndex = (int)UpgradeType.Ability;
            serialized.FindProperty("upgradeId").stringValue = name;
            serialized.ApplyModifiedProperties();
            
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[UpgradeAutoGenerator] 생성: {path} ({typeof(T).Name})");
        }

        private void GenerateMovementUpgrades()
        {
            string dir = $"{SO_BASE_PATH}/Movement";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            CreateAssetsFromTypes<MovementUpgradeSO>("Movement");
        }

        private void CreateMovementDirectories()
        {
            string[] dirs = {
                $"{SO_BASE_PATH}/Movement",
                $"{SO_BASE_PATH}/Movement/Pawn",
                $"{SO_BASE_PATH}/Movement/Knight",
                $"{SO_BASE_PATH}/Movement/Rook",
                $"{SO_BASE_PATH}/Movement/Bishop",
                $"{SO_BASE_PATH}/Movement/Queen",
                $"{SO_BASE_PATH}/Movement/King"
            };
            
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }
        
        private void CreateSpecificMovement<T>(string subFolder, string name, string displayName, 
            string description, int rarity, PieceTypeFilter filter) where T : MovementUpgradeSO
        {
            string dir = $"{SO_BASE_PATH}/Movement/{subFolder}";
            string path = $"{dir}/{name}.asset";
            
            if (File.Exists(path)) return;
            
            var so = ScriptableObject.CreateInstance<T>();
            so.name = name;
            
            var serialized = new SerializedObject(so);
            serialized.FindProperty("upgradeName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("rarity").intValue = rarity;
            serialized.FindProperty("pieceFilter").enumValueIndex = (int)filter;
            serialized.FindProperty("upgradeType").enumValueIndex = (int)UpgradeType.Movement;
            serialized.FindProperty("upgradeId").stringValue = name;
            serialized.ApplyModifiedProperties();
            
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[UpgradeAutoGenerator] 생성: {path} ({typeof(T).Name})");
        }

        private void GenerateGlobalUpgrades()
        {
            string dir = $"{SO_BASE_PATH}/Global";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            CreateAssetsFromTypes<GlobalUpgradeSO>("Global");
        }

        private void GenerateStatUpgrades()
        {
            string dir = $"{SO_BASE_PATH}/Stat";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 등급별로 스탯 업그레이드 생성
            GenerateStatUpgradesForRarity(1); // Common
            GenerateStatUpgradesForRarity(2); // Uncommon
            GenerateStatUpgradesForRarity(3); // Rare
            GenerateStatUpgradesForRarity(4); // Epic
            GenerateStatUpgradesForRarity(5); // Legendary
        }

        private void GenerateStatUpgradesForRarity(int rarity)
        {
            string rarityName = GetRarityName(rarity);
            string dir = $"{SO_BASE_PATH}/Stat/{rarityName}";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int multiplier = GetMultiplierForRarity(rarity);

            // 각 스탯 타입별로 업그레이드 생성
            CreateStatUpgrade<AttackUpgradeSO>(dir, $"Attack_{rarityName}", $"{rarityName} 공격력 증가", $"공격력을 {multiplier}만큼 증가시킵니다", multiplier, rarity);
            CreateStatUpgrade<DefenseUpgradeSO>(dir, $"Defense_{rarityName}", $"{rarityName} 방어력 증가", $"방어력을 {multiplier}만큼 증가시킵니다", multiplier, rarity);
            CreateStatUpgrade<HealthUpgradeSO>(dir, $"Health_{rarityName}", $"{rarityName} 체력 증가", $"최대 체력을 {multiplier * 5}만큼 증가시킵니다", multiplier * 5, rarity);
            CreateStatUpgrade<ShieldUpgradeSO>(dir, $"Shield_{rarityName}", $"{rarityName} 보호막 증가", $"보호막을 {multiplier}만큼 증가시킵니다", multiplier, rarity);
            CreateStatUpgrade<RegenUpgradeSO>(dir, $"Regen_{rarityName}", $"{rarityName} 재생 증가", $"체력 재생을 {multiplier}만큼 증가시킵니다", multiplier, rarity);
        }

        private void CreateStatUpgrade<T>(string dir, string fileName, string displayName, string description, int value, int rarity) where T : StatUpgradeSO
        {
            string path = $"{dir}/{fileName}.asset";

            if (File.Exists(path)) return;

            var so = ScriptableObject.CreateInstance<T>();
            so.name = fileName;

            var serialized = new SerializedObject(so);
            serialized.FindProperty("upgradeName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("rarity").intValue = (int)rarity;
            serialized.FindProperty("pieceFilter").enumValueIndex = (int)PieceTypeFilter.Any;
            serialized.FindProperty("upgradeType").enumValueIndex = (int)UpgradeType.Stat;
            serialized.FindProperty("upgradeId").stringValue = fileName;

            // StatUpgradeSO의 flatBonus 설정
            serialized.FindProperty("flatBonus").intValue = value;

            serialized.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[UpgradeAutoGenerator] 생성: {path} ({typeof(T).Name})");
        }

        private int GetMultiplierForRarity(int rarity)
        {
            return rarity switch
            {
                1 => 1, // Common
                2 => 2, // Uncommon
                3 => 3, // Rare
                4 => 4, // Epic
                5 => 5, // Legendary
                _ => 1
            };
        }

        private string GetRarityName(int rarity)
        {
            return rarity switch
            {
                1 => "Common",
                2 => "Uncommon",
                3 => "Rare",
                4 => "Epic",
                5 => "Legendary",
                _ => "Common"
            };
        }

        private void RegisterAllUpgradesToPool()
        {
            if (upgradePool == null) return;

            var allUpgrades = new List<(UpgradeBaseSO upgrade, string path)>();
            string[] guids = AssetDatabase.FindAssets("t:UpgradeBaseSO", new[] { SO_BASE_PATH });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeBaseSO>(path);
                if (upgrade != null)
                {
                    allUpgrades.Add((upgrade, path));
                }
            }

            if (allUpgrades.Count == 0)
            {
                Debug.LogWarning("[UpgradeAutoGenerator] 등록할 업그레이드를 찾을 수 없습니다.");
                return;
            }

            var so = new SerializedObject(upgradePool);

            // 공통 풀
            var commonMovement = so.FindProperty("commonMovementUpgrades");
            var commonStat = so.FindProperty("commonStatUpgrades");
            var commonAbility = so.FindProperty("commonAbilityUpgrades");
            var globalPool = so.FindProperty("globalUpgrades");

            // 피스별 풀 (movement/ability/stat 하위 포함)
            var piecePools = new Dictionary<PieceTypeFilter, SerializedProperty>
            {
                { PieceTypeFilter.Pawn, so.FindProperty("pawnUpgrades") },
                { PieceTypeFilter.Knight, so.FindProperty("knightUpgrades") },
                { PieceTypeFilter.Bishop, so.FindProperty("bishopUpgrades") },
                { PieceTypeFilter.Rook, so.FindProperty("rookUpgrades") },
                { PieceTypeFilter.Queen, so.FindProperty("queenUpgrades") },
                { PieceTypeFilter.King, so.FindProperty("kingUpgrades") }
            };

            // 기존 null 항목 정리
            ClearNullEntries(commonMovement);
            ClearNullEntries(commonStat);
            ClearNullEntries(commonAbility);
            ClearNullEntries(globalPool);

            foreach (var pool in piecePools.Values)
            {
                if (pool == null) continue;
                ClearNullEntries(pool.FindPropertyRelative("movementUpgrades"));
                ClearNullEntries(pool.FindPropertyRelative("abilityUpgrades"));
                ClearNullEntries(pool.FindPropertyRelative("statUpgrades"));
            }

            foreach (var (upgrade, path) in allUpgrades)
            {
                var filterToUse = upgrade.PieceFilter;

                // Try to infer missing piece filter from folder or type name so assignments go to the right pool.
                if (filterToUse == PieceTypeFilter.Any)
                {
                    var inferred = InferPieceFilter(path, upgrade.GetType());
                    if (inferred.HasValue)
                    {
                        filterToUse = inferred.Value;
                        var serializedUpgrade = new SerializedObject(upgrade);
                        var filterProp = serializedUpgrade.FindProperty("pieceFilter");
                        if (filterProp != null)
                        {
                            filterProp.enumValueIndex = (int)filterToUse;
                            serializedUpgrade.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(upgrade);
                        }
                    }
                }

                SerializedProperty targetList = ResolveTargetListProperty(
                    upgrade,
                    filterToUse,
                    commonMovement,
                    commonStat,
                    commonAbility,
                    globalPool,
                    piecePools);

                if (targetList == null)
                {
                    Debug.LogWarning($"[UpgradeAutoGenerator] {upgrade.name}의 등록 위치를 결정할 수 없습니다. (Type:{upgrade.UpgradeType}, Filter:{upgrade.PieceFilter})");
                    continue;
                }

                AddIfNotExists(targetList, upgrade);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(upgradePool);

            Debug.Log($"[UpgradeAutoGenerator] {allUpgrades.Count}개 업그레이드를 Pool에 등록했습니다.");
        }

        private void AddMissingUpgradesToPool()
        {
            RegisterAllUpgradesToPool();
            EditorUtility.DisplayDialog("완료", "Pool에 누락된 업그레이드를 추가했습니다!", "확인");
        }

        private SerializedProperty ResolveTargetListProperty(
            UpgradeBaseSO upgrade,
            PieceTypeFilter filterToUse,
            SerializedProperty commonMovement,
            SerializedProperty commonStat,
            SerializedProperty commonAbility,
            SerializedProperty globalPool,
            Dictionary<PieceTypeFilter, SerializedProperty> piecePools)
        {
            if (upgrade == null) return null;

            if (upgrade.UpgradeType == UpgradeType.Global)
            {
                return globalPool;
            }

            if (filterToUse == PieceTypeFilter.Any)
            {
                return upgrade.UpgradeType switch
                {
                    UpgradeType.Movement => commonMovement,
                    UpgradeType.Stat => commonStat,
                    UpgradeType.Ability => commonAbility,
                    _ => null
                };
            }

            if (!piecePools.TryGetValue(filterToUse, out var piecePool) || piecePool == null)
            {
                return null;
            }

            string childProperty = upgrade.UpgradeType switch
            {
                UpgradeType.Movement => "movementUpgrades",
                UpgradeType.Stat => "statUpgrades",
                UpgradeType.Ability => "abilityUpgrades",
                _ => null
            };

            return string.IsNullOrEmpty(childProperty)
                ? null
                : piecePool.FindPropertyRelative(childProperty);
        }

        private PieceTypeFilter? InferPieceFilter(string assetPath, Type type)
        {
            string lowerPath = assetPath.ToLowerInvariant();
            string lowerName = type.Name.ToLowerInvariant();

            if (lowerPath.Contains("/pawn/") || lowerName.Contains("pawn"))
                return PieceTypeFilter.Pawn;
            if (lowerPath.Contains("/knight/") || lowerName.Contains("knight"))
                return PieceTypeFilter.Knight;
            if (lowerPath.Contains("/bishop/") || lowerName.Contains("bishop"))
                return PieceTypeFilter.Bishop;
            if (lowerPath.Contains("/rook/") || lowerName.Contains("rook"))
                return PieceTypeFilter.Rook;
            if (lowerPath.Contains("/queen/") || lowerName.Contains("queen"))
                return PieceTypeFilter.Queen;
            if (lowerPath.Contains("/king/") || lowerName.Contains("king"))
                return PieceTypeFilter.King;

            if (lowerPath.Contains("/common/"))
                return PieceTypeFilter.Any;

            return null;
        }

        private void CleanupPool()
        {
            if (upgradePool == null) return;

            var so = new SerializedObject(upgradePool);
            
            var pools = new[]
            {
                so.FindProperty("commonUpgradePool"),
                so.FindProperty("pawnSpecificPool"),
                so.FindProperty("rookSpecificPool"),
                so.FindProperty("knightSpecificPool"),
                so.FindProperty("bishopSpecificPool"),
                so.FindProperty("queenSpecificPool"),
                so.FindProperty("kingSpecificPool")
            };

            int removedCount = 0;
            foreach (var pool in pools)
            {
                if (pool != null)
                {
                    removedCount += ClearNullEntries(pool);
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(upgradePool);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("완료", $"Pool에서 {removedCount}개의 null 항목을 제거했습니다.", "확인");
        }

        private void AddIfNotExists(SerializedProperty arrayProperty, UpgradeBaseSO upgrade)
        {
            if (arrayProperty == null) return;

            // 이미 존재하는지 확인
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == upgrade)
                {
                    return; // 이미 존재함
                }
            }

            // 새 항목 추가
            arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            var newElement = arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
            newElement.objectReferenceValue = upgrade;
        }

        private int ClearNullEntries(SerializedProperty arrayProperty)
        {
            if (arrayProperty == null) return 0;

            int removedCount = 0;
            for (int i = arrayProperty.arraySize - 1; i >= 0; i--)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    arrayProperty.DeleteArrayElementAtIndex(i);
                    removedCount++;
                }
            }

            return removedCount;
        }

        /// <summary>
        /// Enumerate all concrete ScriptableObject types assignable to T across loaded assemblies.
        /// Excludes the base type T itself, only returns subclasses.
        /// </summary>
        private IEnumerable<Type> GetConcreteTypes<T>() where T : ScriptableObject
        {
            var baseType = typeof(T);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || t == baseType) continue;
                    if (baseType.IsAssignableFrom(t) && t.GetCustomAttribute<CreateAssetMenuAttribute>() != null)
                    {
                        yield return t;
                    }
                }
            }
        }

        /// <summary>
        /// Create assets for all discovered upgrade types in the given subfolder.
        /// Saves into piece-specific subfolders (Pawn/Knight/...) or Common/Global when applicable.
        /// </summary>
        private void CreateAssetsFromTypes<T>(string subFolder) where T : UpgradeBaseSO
        {
            foreach (var type in GetConcreteTypes<T>())
            {
                var attr = type.GetCustomAttribute<CreateAssetMenuAttribute>();
                string fileName = attr?.fileName;
                if (string.IsNullOrEmpty(fileName)) fileName = type.Name;

                // Temp instance to read defaults and infer folder/piece filter.
                var so = ScriptableObject.CreateInstance(type) as UpgradeBaseSO;
                if (so == null) continue;

                var serialized = new SerializedObject(so);
                var idProp = serialized.FindProperty("upgradeId");
                if (idProp != null) idProp.stringValue = type.Name;

                // Description is set by OnValidate in the SO script

                var filterProp = serialized.FindProperty("pieceFilter");
                PieceTypeFilter filter = filterProp != null
                    ? (PieceTypeFilter)filterProp.enumValueIndex
                    : PieceTypeFilter.Any;

                if ((filter == PieceTypeFilter.Any || filter == 0) && subFolder != "Global")
                {
                    var inferred = InferPieceFilter($"/{type.Name}/", type);
                    if (inferred.HasValue)
                    {
                        filter = inferred.Value;
                        if (filterProp != null)
                        {
                            filterProp.enumValueIndex = (int)filter;
                        }
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                string pieceFolder = ResolvePieceFolder(filter, subFolder);
                string dir = string.IsNullOrEmpty(pieceFolder)
                    ? $"{SO_BASE_PATH}/{subFolder}"
                    : $"{SO_BASE_PATH}/{subFolder}/{pieceFolder}";

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string path = $"{dir}/{fileName}.asset";
                if (File.Exists(path))
                {
                    UnityEngine.Object.DestroyImmediate(so);
                    continue;
                }

                AssetDatabase.CreateAsset(so, path);
                Debug.Log($"[UpgradeAutoGenerator] 생성: {path} ({type.Name})");

                // Call OnValidate to set default values
                var onValidateMethod = type.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (onValidateMethod != null)
                {
                    onValidateMethod.Invoke(so, null);
                    EditorUtility.SetDirty(so);
                }

                // Special handling for MovementUpgradeSO: auto-assign movementToAdd after asset creation
                if (so is MovementUpgradeSO movementUpgrade)
                {
                    PieceMovementSO movementSO = InferMovementSO(type.Name);
                    if (movementSO != null)
                    {
                        // Asset 저장 후 다시 로드
                        AssetDatabase.SaveAssets();
                        var loadedUpgrade = AssetDatabase.LoadAssetAtPath<MovementUpgradeSO>(path);
                        
                        if (loadedUpgrade != null)
                        {
                            loadedUpgrade.Initialize(movementSO);
                            EditorUtility.SetDirty(loadedUpgrade);
                            Debug.Log($"[UpgradeAutoGenerator] Auto-assigned {movementSO.name} to {type.Name}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateUpgradableMovementSOs()
        {
            var movementTypes = GetConcreteTypes<PieceMovementSO>();
            
            foreach (var type in movementTypes)
            {
                // Skip base movements, only create upgradable ones
                if (type.Namespace != "LevelUpChess.Pieces.Movements.UpgradableMovements")
                    continue;
                
                string assetName = GetMovementAssetName(type.Name);
                string assetPath = $"Assets/ScriptableObject/{assetName}.asset";
                
                if (AssetDatabase.LoadAssetAtPath<PieceMovementSO>(assetPath) != null)
                {
                    Debug.Log($"[UpgradeAutoGenerator] {assetName} already exists, skipping...");
                    continue;
                }
                
                var so = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(so, assetPath);
                Debug.Log($"[UpgradeAutoGenerator] Created MovementSO: {assetPath}");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private string GetMovementAssetName(string typeName)
        {
            // MovementBackstepMoveSO -> MovementBackstepMove
            string baseName = typeName.Replace("Movement", "").Replace("SO", ""); // BackstepMove
            return $"Movement{baseName}";
        }

        private PieceMovementSO InferMovementSO(string upgradeTypeName)
        {
            // First, try upgradable movements
            string[] upgradableKeys = { "BackstepMove", "BishopAttack", "KnightMove", "LimitedBishop", "DiagonalMove", "FrontAttack", "LargerAttackSpace", "Sideway", "TwoStepFront", "Camel", "Cross", "Dash", "Zebra", "KnightAttack", "ReflectAttack", "RookMove", "RookAttack", "BishopMove", "Bishop3", "Rook3" };
            foreach (string key in upgradableKeys)
            {
                if (upgradeTypeName.Contains(key)) // 특성을 찾아서 매치
                {
                    string typeName = $"Movement{key}SO"; // MovementBackstepMoveSO
                    string assetName = GetMovementAssetName(typeName); // MovementBackstepMove
                    string assetPath = $"Assets/ScriptableObject/{assetName}.asset";
                    PieceMovementSO movementSO = AssetDatabase.LoadAssetAtPath<PieceMovementSO>(assetPath);
                    if (movementSO != null)
                    {
                        return movementSO;
                    }
                }
            }
            
            // Then, try base movements
            string[] movementNames = { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" };
            foreach (string movementName in movementNames)
            {
                if (upgradeTypeName.Contains(movementName))
                {
                    string assetPath = $"Assets/ScriptableObject/{movementName}Movement.asset";
                    PieceMovementSO movementSO = AssetDatabase.LoadAssetAtPath<PieceMovementSO>(assetPath);
                    if (movementSO != null)
                    {
                        return movementSO;
                    }
                }
            }
            
            return null;
        }

        private string ResolvePieceFolder(PieceTypeFilter filter, string topFolder)
        {
            if (topFolder == "Global") return string.Empty;
            return filter switch
            {
                PieceTypeFilter.Any => "Common",
                PieceTypeFilter.Pawn => "Pawn",
                PieceTypeFilter.Rook => "Rook",
                PieceTypeFilter.Knight => "Knight",
                PieceTypeFilter.Bishop => "Bishop",
                PieceTypeFilter.Queen => "Queen",
                PieceTypeFilter.King => "King",
                _ => "Common"
            };
        }
    }
}
