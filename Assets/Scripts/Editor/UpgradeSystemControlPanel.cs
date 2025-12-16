using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using LevelUpChess.Upgrades;
using LevelUpChess.Upgrades.UI;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 업그레이드 자산 생성 + 기본 프리팹/씬 배치 + 간단 진단을 한 번에 처리하는 컨트롤 패널.
    /// </summary>
    public class UpgradeSystemControlPanel : EditorWindow
    {
        private const string SO_BASE_PATH = "Assets/ScriptableObject/Upgrades";
        private const string PREFAB_PATH = "Assets/Prefabs/UI/Upgrades";
        private const string KOREAN_FONT_PATH = "Assets/TextMesh Pro/Pretendard-Medium SDF";
        private const string POOL_PATH = SO_BASE_PATH + "/MainUpgradePool.asset";

        private bool generateAbilities = true;
        private bool generateMovements = true;
        private bool generateStats = true;
        private bool generateGlobals = true;
        private bool createUpgradePool = true;
        private bool createUIPrefabs = true;
        private bool setupSceneObjects = true;
        private Canvas targetCanvas;
        private TMP_FontAsset koreanFont;

        [MenuItem("Tools/LevelUpChess/Upgrade System Control Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<UpgradeSystemControlPanel>("Upgrade System Control");
            window.minSize = new Vector2(420, 520);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("업그레이드 시스템 통합 패널", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label("자산 생성 (리플렉션 기반)", EditorStyles.boldLabel);
            generateAbilities = EditorGUILayout.Toggle("능력(Ability)", generateAbilities);
            generateMovements = EditorGUILayout.Toggle("행마법(Movement)", generateMovements);
            generateStats = EditorGUILayout.Toggle("스탯(Stat)", generateStats);
            generateGlobals = EditorGUILayout.Toggle("단체/글로벌(Global)", generateGlobals);
            createUpgradePool = EditorGUILayout.Toggle("UpgradePool SO 생성", createUpgradePool);

            EditorGUILayout.Space();
            GUILayout.Label("프리팹/씬 배치", EditorStyles.boldLabel);
            createUIPrefabs = EditorGUILayout.Toggle("UI 프리팹 생성", createUIPrefabs);
            setupSceneObjects = EditorGUILayout.Toggle("씬에 배치 (UpgradeManager/UI)", setupSceneObjects);
            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);

            EditorGUILayout.Space();
            if (GUILayout.Button("원클릭 실행", GUILayout.Height(40)))
            {
                RunOneClick();
            }

            if (GUILayout.Button("간단 진단 로그 출력", GUILayout.Height(30)))
            {
                RunDiagnostics();
            }
        }

        private void RunOneClick()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Upgrade System", "생성 중...", 0f);
                CreateDirectories();
                koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KOREAN_FONT_PATH);

                if (generateAbilities) CreateAssetsFromTypes<AbilityBaseSO>("Ability");
                if (generateMovements) CreateAssetsFromTypes<MovementUpgradeSO>("Movement");
                if (generateStats) CreateAssetsFromTypes<StatUpgradeSO>("Stat");
                if (generateGlobals) CreateAssetsFromTypes<GlobalUpgradeSO>("Global");

                UpgradePoolSO pool = null;
                if (createUpgradePool)
                {
                    pool = EnsureUpgradePool();
                }
                if (pool != null)
                {
                    RegisterAllUpgradesToPool(pool);
                }

                GameObject managerPrefab = null;
                GameObject panelPrefab = null;
                if (createUIPrefabs || setupSceneObjects)
                {
                    managerPrefab = CreateUpgradeManagerPrefab(pool);
                }

                if (createUIPrefabs)
                {
                    var card = CreateUpgradeCardPrefab();
                    panelPrefab = CreateUpgradeSelectionPanelPrefab(card);
                }

                if (setupSceneObjects)
                {
                    SetupSceneObjects(managerPrefab, panelPrefab);
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("완료", "업그레이드 시스템 원클릭 설정이 완료되었습니다.", "확인");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("오류", $"원클릭 실행 중 오류:\n{e.Message}", "확인");
                Debug.LogError($"[UpgradeSystemControl] Error: {e}");
            }
        }

        private void RunDiagnostics()
        {
            Debug.Log("==== Upgrade System Diagnostics ====");
            var manager = FindFirstObjectByType<UpgradeManager>();
            Debug.Log(manager ? $"UpgradeManager: {manager.name}" : "UpgradeManager not found");

            var networkManager = FindFirstObjectByType<NetworkManager>();
            Debug.Log(networkManager ? "NetworkManager: OK" : "NetworkManager not found");

            var panelUI = FindFirstObjectByType<UpgradeSelectionPanelUI>();
            Debug.Log(panelUI ? $"UpgradeSelectionPanelUI: {panelUI.name}" : "UpgradeSelectionPanelUI not found");

            var pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(POOL_PATH);
            Debug.Log(pool ? $"UpgradePool: {pool.name}" : "UpgradePool not found");
            Debug.Log("====================================");
        }

        private void CreateDirectories()
        {
            if (!Directory.Exists(SO_BASE_PATH)) Directory.CreateDirectory(SO_BASE_PATH);
            if (!Directory.Exists($"{SO_BASE_PATH}/Ability")) Directory.CreateDirectory($"{SO_BASE_PATH}/Ability");
            if (!Directory.Exists($"{SO_BASE_PATH}/Movement")) Directory.CreateDirectory($"{SO_BASE_PATH}/Movement");
            if (!Directory.Exists($"{SO_BASE_PATH}/Stat")) Directory.CreateDirectory($"{SO_BASE_PATH}/Stat");
            if (!Directory.Exists($"{SO_BASE_PATH}/Global")) Directory.CreateDirectory($"{SO_BASE_PATH}/Global");
            if (!Directory.Exists(PREFAB_PATH)) Directory.CreateDirectory(PREFAB_PATH);
        }

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
                    if (t == null || t.IsAbstract) continue;
                    if (baseType.IsAssignableFrom(t)) yield return t;
                }
            }
        }

        private void CreateAssetsFromTypes<T>(string subFolder) where T : UpgradeBaseSO
        {
            foreach (var type in GetConcreteTypes<T>())
            {
                var attr = type.GetCustomAttribute<CreateAssetMenuAttribute>();
                string fileName = attr?.fileName;
                if (string.IsNullOrEmpty(fileName)) fileName = type.Name;

                var so = ScriptableObject.CreateInstance(type) as UpgradeBaseSO;
                if (so == null) continue;

                var serialized = new SerializedObject(so);
                var idProp = serialized.FindProperty("upgradeId");
                if (idProp != null) idProp.stringValue = type.Name;

                var nameProp = serialized.FindProperty("upgradeName");
                if (nameProp != null && string.IsNullOrEmpty(nameProp.stringValue))
                    nameProp.stringValue = fileName;

                if (attr != null && !string.IsNullOrEmpty(attr.menuName))
                {
                    var descProp = serialized.FindProperty("description");
                    if (descProp != null && string.IsNullOrEmpty(descProp.stringValue))
                        descProp.stringValue = attr.menuName;
                }

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
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string path = $"{dir}/{fileName}.asset";
                if (File.Exists(path))
                {
                    UnityEngine.Object.DestroyImmediate(so);
                    continue;
                }

                AssetDatabase.CreateAsset(so, path);
                Debug.Log($"[UpgradeSystemControl] 생성: {path} ({type.Name})");
            }
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

        private UpgradePoolSO EnsureUpgradePool()
        {
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(POOL_PATH);
            if (pool != null) return pool;

            pool = ScriptableObject.CreateInstance<UpgradePoolSO>();
            AssetDatabase.CreateAsset(pool, POOL_PATH);
            Debug.Log($"[UpgradeSystemControl] UpgradePool 생성: {POOL_PATH}");
            return pool;
        }

        private GameObject CreateUpgradeManagerPrefab(UpgradePoolSO pool)
        {
            string path = $"{PREFAB_PATH}/UpgradeManager.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var obj = new GameObject("UpgradeManager");
            obj.AddComponent<NetworkObject>();
            var mgr = obj.AddComponent<UpgradeManager>();

            if (pool != null)
            {
                var serializedManager = new SerializedObject(mgr);
                var poolProperty = serializedManager.FindProperty("upgradePool");
                if (poolProperty != null)
                {
                    poolProperty.objectReferenceValue = pool;
                    serializedManager.ApplyModifiedProperties();
                }
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
            Debug.Log($"[UpgradeSystemControl] UpgradeManager prefab 생성: {path}");
            return prefab;
        }

        private GameObject CreateUpgradeCardPrefab()
        {
            string path = $"{PREFAB_PATH}/UpgradeCard.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject card = new GameObject("UpgradeCard");
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(300, 400);
            var cardUI = card.AddComponent<UpgradeCardUI>();

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(card.transform);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            GameObject border = new GameObject("RarityBorder");
            border.transform.SetParent(card.transform);
            var borderImage = border.AddComponent<Image>();
            borderImage.color = Color.white;

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(card.transform);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0, -80);
            iconRect.sizeDelta = new Vector2(100, 100);
            var iconImage = icon.AddComponent<Image>();

            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(card.transform);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Upgrade Name";
            nameText.fontSize = 24;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            if (koreanFont != null) nameText.font = koreanFont;

            GameObject descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(card.transform);
            var descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "Upgrade description";
            descText.fontSize = 16;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.textWrappingMode = TextWrappingModes.Normal;
            if (koreanFont != null) descText.font = koreanFont;

            var cardUIType = typeof(UpgradeCardUI);
            cardUIType.GetField("backgroundImage", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cardUI, bgImage);
            cardUIType.GetField("iconImage", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cardUI, iconImage);
            cardUIType.GetField("rarityBorder", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cardUI, borderImage);
            cardUIType.GetField("nameText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cardUI, nameText);
            cardUIType.GetField("descriptionText", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cardUI, descText);

            PrefabUtility.SaveAsPrefabAsset(card, path);
            UnityEngine.Object.DestroyImmediate(card);
            Debug.Log($"[UpgradeSystemControl] UpgradeCard prefab 생성: {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private GameObject CreateUpgradeSelectionPanelPrefab(GameObject cardPrefab)
        {
            string path = $"{PREFAB_PATH}/UpgradeSelectionPanel.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject panel = new GameObject("UpgradeSelectionPanel");
            var canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            panel.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            panel.AddComponent<GraphicRaycaster>();

            var panelUI = panel.AddComponent<UpgradeSelectionPanelUI>();

            GameObject content = new GameObject("ContentPanel");
            content.transform.SetParent(panel.transform);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(1000, 600);
            var contentGroup = content.AddComponent<CanvasGroup>();

            var panelUIType = typeof(UpgradeSelectionPanelUI);
            panelUIType.GetField("panelRoot", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(panelUI, content);
            panelUIType.GetField("canvasGroup", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(panelUI, contentGroup);
            panelUIType.GetField("cardPrefab", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(panelUI, cardPrefab?.GetComponent<UpgradeCardUI>());

            PrefabUtility.SaveAsPrefabAsset(panel, path);
            UnityEngine.Object.DestroyImmediate(panel);
            Debug.Log($"[UpgradeSystemControl] UpgradeSelectionPanel prefab 생성: {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private void SetupSceneObjects(GameObject managerPrefab, GameObject panelPrefab)
        {
            // UpgradeManager 배치
            var manager = FindFirstObjectByType<UpgradeManager>();
            if (manager == null && managerPrefab != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(managerPrefab) as GameObject;
                manager = instance?.GetComponent<UpgradeManager>();
                Debug.Log("[UpgradeSystemControl] UpgradeManager 배치 완료");
            }

            // Canvas 찾기
            Canvas canvas = targetCanvas;
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            // UI 패널 배치
            if (panelPrefab != null && canvas != null)
            {
                var panelInstance = PrefabUtility.InstantiatePrefab(panelPrefab) as GameObject;
                if (panelInstance != null)
                {
                    panelInstance.transform.SetParent(canvas.transform, false);
                    var panelUI = panelInstance.GetComponent<UpgradeSelectionPanelUI>();
                    if (panelUI != null && manager != null)
                    {
                        var serializedManager = new SerializedObject(manager);
                        var uiProperty = serializedManager.FindProperty("upgradeSelectionUI");
                        if (uiProperty != null)
                        {
                            uiProperty.objectReferenceValue = panelUI;
                            serializedManager.ApplyModifiedProperties();
                        }
                    }
                }
            }
        }

        private void RegisterAllUpgradesToPool(UpgradePoolSO pool)
        {
            if (pool == null) return;

            var upgrades = new List<(UpgradeBaseSO upgrade, string path)>();
            string[] guids = AssetDatabase.FindAssets("t:UpgradeBaseSO", new[] { SO_BASE_PATH });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var up = AssetDatabase.LoadAssetAtPath<UpgradeBaseSO>(path);
                if (up != null) upgrades.Add((up, path));
            }

            var so = new SerializedObject(pool);
            var commonMovement = so.FindProperty("commonMovementUpgrades");
            var commonStat = so.FindProperty("commonStatUpgrades");
            var commonAbility = so.FindProperty("commonAbilityUpgrades");
            var globalPool = so.FindProperty("globalUpgrades");

            var piecePools = new Dictionary<PieceTypeFilter, SerializedProperty>
            {
                { PieceTypeFilter.Pawn, so.FindProperty("pawnUpgrades") },
                { PieceTypeFilter.Knight, so.FindProperty("knightUpgrades") },
                { PieceTypeFilter.Bishop, so.FindProperty("bishopUpgrades") },
                { PieceTypeFilter.Rook, so.FindProperty("rookUpgrades") },
                { PieceTypeFilter.Queen, so.FindProperty("queenUpgrades") },
                { PieceTypeFilter.King, so.FindProperty("kingUpgrades") }
            };

            foreach (var (up, path) in upgrades)
            {
                var filterToUse = up.PieceFilter;

                if (filterToUse == PieceTypeFilter.Any)
                {
                    var inferred = InferPieceFilter(path, up.GetType());
                    if (inferred.HasValue)
                    {
                        filterToUse = inferred.Value;
                        var serializedUpgrade = new SerializedObject(up);
                        var filterProp = serializedUpgrade.FindProperty("pieceFilter");
                        if (filterProp != null)
                        {
                            filterProp.enumValueIndex = (int)filterToUse;
                            serializedUpgrade.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(up);
                        }
                    }
                }

                SerializedProperty targetList = null;
                if (up.UpgradeType == UpgradeType.Global)
                {
                    targetList = globalPool;
                }
                else if (filterToUse == PieceTypeFilter.Any)
                {
                    targetList = up.UpgradeType switch
                    {
                        UpgradeType.Movement => commonMovement,
                        UpgradeType.Stat => commonStat,
                        UpgradeType.Ability => commonAbility,
                        _ => null
                    };
                }
                else if (piecePools.TryGetValue(filterToUse, out var poolProp) && poolProp != null)
                {
                    string child = up.UpgradeType switch
                    {
                        UpgradeType.Movement => "movementUpgrades",
                        UpgradeType.Stat => "statUpgrades",
                        UpgradeType.Ability => "abilityUpgrades",
                        _ => null
                    };
                    if (!string.IsNullOrEmpty(child))
                    {
                        targetList = poolProp.FindPropertyRelative(child);
                    }
                }

                if (targetList != null)
                {
                    AddIfNotExists(targetList, up);
                }
            }

            so.ApplyModifiedProperties();
            Debug.Log("[UpgradeSystemControl] UpgradePool 등록 완료");
        }

        private void AddIfNotExists(SerializedProperty arrayProperty, UpgradeBaseSO upgrade)
        {
            if (arrayProperty == null || upgrade == null) return;
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == upgrade) return;
            }
            arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            var newElement = arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
            newElement.objectReferenceValue = upgrade;
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
    }
}
