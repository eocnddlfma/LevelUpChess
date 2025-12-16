using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using LevelUpChess.Upgrades;
using LevelUpChess.Upgrades.UI;
using System.IO;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 업그레이드 시스템 자동 세팅 마법사
    /// Tools > LevelUpChess > Setup Upgrade System
    /// </summary>
    public class UpgradeSystemSetupWizard : EditorWindow
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/Upgrades";
        private const string SO_PATH = "Assets/ScriptableObject/Upgrades";
        private const string KOREAN_FONT_PATH = "Assets/TextMesh Pro/Pretendard-Medium SDF";
        
        private bool createUpgradePool = true;
        private bool createUIPrefabs = true;
        private bool setupSceneObjects = true;
        private bool createSampleUpgrades = true;
        
        private GameObject upgradeManagerPrefab;
        private Canvas targetCanvas;
        private TMP_FontAsset koreanFont;
        
        [MenuItem("Tools/LevelUpChess/Setup Upgrade System")]
        public static void ShowWindow()
        {
            var window = GetWindow<UpgradeSystemSetupWizard>("Upgrade System Setup");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("업그레이드 시스템 자동 세팅", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "이 마법사는 업그레이드 시스템의 프리팹, ScriptableObject, 씬 오브젝트를 자동으로 생성합니다.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 옵션 선택
            GUILayout.Label("생성할 항목:", EditorStyles.boldLabel);
            createUpgradePool = EditorGUILayout.Toggle("UpgradePool SO 생성", createUpgradePool);
            createSampleUpgrades = EditorGUILayout.Toggle("샘플 업그레이드 SO 생성", createSampleUpgrades);
            createUIPrefabs = EditorGUILayout.Toggle("UI 프리팹 생성", createUIPrefabs);
            setupSceneObjects = EditorGUILayout.Toggle("씬 오브젝트 세팅", setupSceneObjects);
            
            EditorGUILayout.Space();
            
            if (setupSceneObjects)
            {
                GUILayout.Label("씬 설정:", EditorStyles.boldLabel);
                targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);
                
                if (targetCanvas == null)
                {
                    EditorGUILayout.HelpBox("씬에 Canvas를 배치하거나 자동 생성됩니다.", MessageType.Warning);
                }
            }
            
            EditorGUILayout.Space();
            
            // 실행 버튼
            GUI.enabled = createUpgradePool || createUIPrefabs || setupSceneObjects || createSampleUpgrades;
            if (GUILayout.Button("자동 세팅 시작", GUILayout.Height(40)))
            {
                SetupUpgradeSystem();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("기존 설정 초기화 (주의!)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("경고", 
                    "모든 업그레이드 시스템 에셋을 삭제합니다. 계속하시겠습니까?", 
                    "삭제", "취소"))
                {
                    CleanupUpgradeSystem();
                }
            }
        }
        
        private void SetupUpgradeSystem()
        {
            try
            {
                EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "폴더 생성 중...", 0f);
                CreateDirectories();
                
                // Load Korean font
                koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KOREAN_FONT_PATH);
                if (koreanFont == null)
                {
                    Debug.LogWarning($"[UpgradeSystemSetup] Korean font not found at {KOREAN_FONT_PATH}. Using default font.");
                }
                
                UpgradePoolSO upgradePool = null;
                
                if (createUpgradePool)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "UpgradePool 생성 중...", 0.2f);
                    upgradePool = CreateUpgradePool();
                }
                
                if (createSampleUpgrades)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "샘플 업그레이드 생성 중...", 0.3f);
                    CreateSampleUpgrades(upgradePool);
                }
                
                GameObject cardPrefab = null;
                GameObject panelPrefab = null;
                
                GameObject managerPrefab = null;
                
                if (createUIPrefabs || setupSceneObjects)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "UpgradeManager 프리팹 생성 중...", 0.4f);
                    managerPrefab = CreateUpgradeManagerPrefab(upgradePool);
                }
                
                if (createUIPrefabs)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "UI 프리팹 생성 중...", 0.5f);
                    cardPrefab = CreateUpgradeCardPrefab();
                    
                    EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "UI 패널 생성 중...", 0.7f);
                    panelPrefab = CreateUpgradeSelectionPanelPrefab(cardPrefab);
                }
                
                if (setupSceneObjects)
                {
                    EditorUtility.DisplayProgressBar("업그레이드 시스템 세팅", "씬 오브젝트 세팅 중...", 0.9f);
                    SetupSceneObjects(managerPrefab, panelPrefab);
                }
                
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("완료", 
                    "업그레이드 시스템 세팅이 완료되었습니다!\n\n생성된 위치:\n" +
                    $"- Prefabs: {PREFAB_PATH}\n" +
                    $"- ScriptableObjects: {SO_PATH}\n\n" +
                    "⚠️ 중요: UpgradeManager.prefab을 NetworkManager의 Prefabs List에 추가해야 합니다!", 
                    "확인");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("오류", $"세팅 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[UpgradeSystemSetup] Error: {e}");
            }
        }
        
        private void CreateDirectories()
        {
            if (!Directory.Exists(PREFAB_PATH))
                Directory.CreateDirectory(PREFAB_PATH);
            
            if (!Directory.Exists(SO_PATH))
            {
                Directory.CreateDirectory(SO_PATH);
                Directory.CreateDirectory($"{SO_PATH}/Movement");
                Directory.CreateDirectory($"{SO_PATH}/Stat");
                Directory.CreateDirectory($"{SO_PATH}/Ability");
                Directory.CreateDirectory($"{SO_PATH}/Global");
            }
        }
        
        private UpgradePoolSO CreateUpgradePool()
        {
            string path = $"{SO_PATH}/MainUpgradePool.asset";
            
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(path);
            if (pool != null)
            {
                Debug.Log("[UpgradeSystemSetup] UpgradePool already exists. Skipping...");
                return pool;
            }
            
            pool = ScriptableObject.CreateInstance<UpgradePoolSO>();
            AssetDatabase.CreateAsset(pool, path);
            
            Debug.Log($"[UpgradeSystemSetup] Created UpgradePool at {path}");
            return pool;
        }
        
        private void CreateSampleUpgrades(UpgradePoolSO pool)
        {
            // 샘플 스탯 업그레이드들은 이미 존재하는 SO들을 사용
            // 여기서는 풀에 자동으로 할당하는 것만 구현
            Debug.Log("[UpgradeSystemSetup] Sample upgrades should be created manually or loaded from existing assets.");
        }

        private GameObject CreateUpgradeManagerPrefab(UpgradePoolSO pool)
        {
            string path = $"{PREFAB_PATH}/UpgradeManager.prefab";
            
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Debug.Log("[UpgradeSystemSetup] UpgradeManager prefab already exists. Skipping...");
                return existing;
            }
            
            // 프리팹 폴더가 없으면 생성
            if (!Directory.Exists(PREFAB_PATH))
            {
                Directory.CreateDirectory(PREFAB_PATH);
            }
            
            // Create GameObject
            GameObject managerObj = new GameObject("UpgradeManager");
            
            // Add NetworkObject component FIRST (required for NetworkBehaviour)
            var netObj = managerObj.AddComponent<Unity.Netcode.NetworkObject>();
            
            // Add UpgradeManager component
            var manager = managerObj.AddComponent<UpgradeManager>();
            
            // Assign UpgradePool if available
            if (pool != null)
            {
                var serializedManager = new SerializedObject(manager);
                var poolProperty = serializedManager.FindProperty("upgradePool");
                if (poolProperty != null)
                {
                    poolProperty.objectReferenceValue = pool;
                    serializedManager.ApplyModifiedProperties();
                }
            }
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(managerObj, path);
            DestroyImmediate(managerObj);
            
            Debug.Log($"[UpgradeSystemSetup] Created UpgradeManager prefab at {path}");
            Debug.Log("⚠️ 이 프리팹을 NetworkManager의 Prefabs List에 추가해야 합니다!");
            
            return prefab;
        }
        
        private GameObject CreateUpgradeCardPrefab()
        {
            string path = $"{PREFAB_PATH}/UpgradeCard.prefab";
            
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Debug.Log("[UpgradeSystemSetup] UpgradeCard prefab already exists.");
                return existing;
            }
            
            // 카드 루트
            GameObject card = new GameObject("UpgradeCard");
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(300, 400);
            
            var cardUI = card.AddComponent<UpgradeCardUI>();
            
            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(card.transform);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            
            // Rarity Border
            GameObject border = new GameObject("RarityBorder");
            border.transform.SetParent(card.transform);
            var borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            var borderImage = border.AddComponent<Image>();
            borderImage.color = Color.white;
            borderImage.type = Image.Type.Sliced;
            
            // Icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(card.transform);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0, -80);
            iconRect.sizeDelta = new Vector2(100, 100);
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // Name Text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(card.transform);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.6f);
            nameRect.anchorMax = new Vector2(1, 0.8f);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-10, 0);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Upgrade Name";
            nameText.fontSize = 24;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            if (koreanFont != null) nameText.font = koreanFont;
            
            // Description Text
            GameObject descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(card.transform);
            var descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.2f);
            descRect.anchorMax = new Vector2(1, 0.6f);
            descRect.sizeDelta = Vector2.zero;
            descRect.offsetMin = new Vector2(15, 0);
            descRect.offsetMax = new Vector2(-15, 0);
            var descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "Upgrade description";
            descText.fontSize = 16;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.textWrappingMode = TextWrappingModes.Normal;
            if (koreanFont != null) descText.font = koreanFont;
            
            // Type Text
            GameObject typeObj = new GameObject("TypeText");
            typeObj.transform.SetParent(card.transform);
            var typeRect = typeObj.AddComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0, 0);
            typeRect.anchorMax = new Vector2(1, 0.15f);
            typeRect.sizeDelta = Vector2.zero;
            typeRect.offsetMin = new Vector2(10, 10);
            typeRect.offsetMax = new Vector2(-10, 0);
            var typeText = typeObj.AddComponent<TextMeshProUGUI>();
            typeText.text = "[Type]";
            typeText.fontSize = 14;
            typeText.alignment = TextAlignmentOptions.Center;
            typeText.color = new Color(0.7f, 0.7f, 0.7f);
            if (koreanFont != null) typeText.font = koreanFont;
            
            // Select Button
            GameObject btnObj = new GameObject("SelectButton");
            btnObj.transform.SetParent(card.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.sizeDelta = Vector2.zero;
            var button = btnObj.AddComponent<Button>();
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(1, 1, 1, 0.01f); // 거의 투명하게
            
            // Assign fields using reflection or direct assignment
            var cardUIType = typeof(UpgradeCardUI);
            var bgField = cardUIType.GetField("backgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconField = cardUIType.GetField("iconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var borderField = cardUIType.GetField("rarityBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameField = cardUIType.GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descField = cardUIType.GetField("descriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField = cardUIType.GetField("typeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var btnField = cardUIType.GetField("selectButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bgField?.SetValue(cardUI, bgImage);
            iconField?.SetValue(cardUI, iconImage);
            borderField?.SetValue(cardUI, borderImage);
            nameField?.SetValue(cardUI, nameText);
            descField?.SetValue(cardUI, descText);
            typeField?.SetValue(cardUI, typeText);
            btnField?.SetValue(cardUI, button);
            
            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(card, path);
            DestroyImmediate(card);
            
            Debug.Log($"[UpgradeSystemSetup] Created UpgradeCard prefab at {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        
        private GameObject CreateUpgradeSelectionPanelPrefab(GameObject cardPrefab)
        {
            string path = $"{PREFAB_PATH}/UpgradeSelectionPanel.prefab";
            
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Debug.Log("[UpgradeSystemSetup] UpgradeSelectionPanel prefab already exists.");
                return existing;
            }
            
            // 캔버스 루트
            GameObject panel = new GameObject("UpgradeSelectionPanel");
            var canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            panel.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            panel.AddComponent<GraphicRaycaster>();
            
            var panelUI = panel.AddComponent<UpgradeSelectionPanelUI>();
            
            // Background overlay
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(panel.transform);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);
            
            // Content Panel
            GameObject content = new GameObject("ContentPanel");
            content.transform.SetParent(panel.transform);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(1000, 600);
            var contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            var contentGroup = content.AddComponent<CanvasGroup>();
            
            // Title Text
            GameObject title = new GameObject("TitleText");
            title.transform.SetParent(content.transform);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = Vector2.zero;
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "LEVEL UP!";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.yellow;
            if (koreanFont != null) titleText.font = koreanFont;
            
            // Piece Name Text
            GameObject pieceName = new GameObject("PieceNameText");
            pieceName.transform.SetParent(content.transform);
            var pieceRect = pieceName.AddComponent<RectTransform>();
            pieceRect.anchorMin = new Vector2(0, 0.75f);
            pieceRect.anchorMax = new Vector2(1, 0.85f);
            pieceRect.sizeDelta = Vector2.zero;
            var pieceText = pieceName.AddComponent<TextMeshProUGUI>();
            pieceText.text = "Piece Name";
            pieceText.fontSize = 32;
            pieceText.alignment = TextAlignmentOptions.Center;
            if (koreanFont != null) pieceText.font = koreanFont;
            
            // Card Container
            GameObject cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(content.transform);
            var containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.15f);
            containerRect.anchorMax = new Vector2(1, 0.75f);
            containerRect.sizeDelta = Vector2.zero;
            containerRect.offsetMin = new Vector2(50, 0);
            containerRect.offsetMax = new Vector2(-50, 0);
            var layout = cardContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            // Skip Button
            GameObject skipBtn = new GameObject("SkipButton");
            skipBtn.transform.SetParent(content.transform);
            var skipRect = skipBtn.AddComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.1f, 0.05f);
            skipRect.anchorMax = new Vector2(0.3f, 0.12f);
            skipRect.sizeDelta = Vector2.zero;
            var skipButton = skipBtn.AddComponent<Button>();
            var skipImage = skipBtn.AddComponent<Image>();
            skipImage.color = new Color(0.3f, 0.3f, 0.3f);
            GameObject skipText = new GameObject("Text");
            skipText.transform.SetParent(skipBtn.transform);
            var skipTextRect = skipText.AddComponent<RectTransform>();
            skipTextRect.anchorMin = Vector2.zero;
            skipTextRect.anchorMax = Vector2.one;
            skipTextRect.sizeDelta = Vector2.zero;
            var skipTMP = skipText.AddComponent<TextMeshProUGUI>();
            skipTMP.text = "Skip";
            skipTMP.fontSize = 18;
            skipTMP.alignment = TextAlignmentOptions.Center;
            if (koreanFont != null) skipTMP.font = koreanFont;
            
            // Close Button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(content.transform);
            var closeRect = closeBtn.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.7f, 0.05f);
            closeRect.anchorMax = new Vector2(0.9f, 0.12f);
            closeRect.sizeDelta = Vector2.zero;
            var closeButton = closeBtn.AddComponent<Button>();
            var closeImage = closeBtn.AddComponent<Image>();
            closeImage.color = new Color(0.5f, 0.2f, 0.2f);
            GameObject closeText = new GameObject("Text");
            closeText.transform.SetParent(closeBtn.transform);
            var closeTextRect = closeText.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;
            var closeTMP = closeText.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "Close";
            closeTMP.fontSize = 18;
            closeTMP.alignment = TextAlignmentOptions.Center;
            if (koreanFont != null) closeTMP.font = koreanFont;
            
            // Assign fields using reflection
            var panelUIType = typeof(UpgradeSelectionPanelUI);
            panelUIType.GetField("panelRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, content);
            panelUIType.GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, contentGroup);
            panelUIType.GetField("titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, titleText);
            panelUIType.GetField("pieceNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, pieceText);
            panelUIType.GetField("cardContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, cardContainer.transform);
            panelUIType.GetField("skipButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, skipButton);
            panelUIType.GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, closeButton);
            panelUIType.GetField("cardPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panelUI, cardPrefab?.GetComponent<UpgradeCardUI>());
            
            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(panel, path);
            DestroyImmediate(panel);
            
            Debug.Log($"[UpgradeSystemSetup] Created UpgradeSelectionPanel prefab at {path}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        
        private void SetupSceneObjects(GameObject managerPrefab, GameObject panelPrefab)
        {
            // Find existing UpgradeManager in scene
            var manager = FindFirstObjectByType<UpgradeManager>();
            if (manager == null && managerPrefab != null)
            {
                // Instantiate the prefab in scene
                GameObject managerInstance = PrefabUtility.InstantiatePrefab(managerPrefab) as GameObject;
                if (managerInstance != null)
                {
                    manager = managerInstance.GetComponent<UpgradeManager>();
                    Debug.Log("[UpgradeSystemSetup] Instantiated UpgradeManager prefab in scene");
                }
            }
            
            if (manager == null)
            {
                Debug.LogError("[UpgradeSystemSetup] Failed to create or find UpgradeManager");
                return;
            }
            
            Debug.Log("[UpgradeSystemSetup] UpgradeManager found or created successfully");
            Debug.Log("⚠️ UpgradeManager 프리팹을 NetworkManager의 Prefabs List에 추가하는 것을 잊지 마세요!");
            
            // Find or create Canvas
            Canvas canvas = targetCanvas;
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            // Instantiate panel prefab
            if (panelPrefab != null)
            {
                try
                {
                    var panelInstance = PrefabUtility.InstantiatePrefab(panelPrefab) as GameObject;
                    if (panelInstance != null)
                    {
                        if (canvas != null)
                        {
                            panelInstance.transform.SetParent(canvas.transform, false);
                        }
                        
                        var panelUI = panelInstance.GetComponent<UpgradeSelectionPanelUI>();
                        if (panelUI != null)
                        {
                            // Assign to manager
                            try
                            {
                                var serializedManager = new SerializedObject(manager);
                                var uiProperty = serializedManager.FindProperty("upgradeSelectionUI");
                                if (uiProperty != null)
                                {
                                    uiProperty.objectReferenceValue = panelUI;
                                    serializedManager.ApplyModifiedProperties();
                                    Debug.Log("[UpgradeSystemSetup] Assigned UI Panel to UpgradeManager");
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[UpgradeSystemSetup] Failed to assign UI Panel: {e.Message}");
                            }
                        }
                        
                        Debug.Log("[UpgradeSystemSetup] Instantiated UpgradeSelectionPanel in scene");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UpgradeSystemSetup] Failed to instantiate panel: {e.Message}");
                }
            }
            
            // Find and assign BoardManager
            try
            {
                var boardManager = FindFirstObjectByType<LevelUpChess.Board.BoardManager>();
                if (boardManager != null)
                {
                    var serializedManager = new SerializedObject(manager);
                    var bmProperty = serializedManager.FindProperty("boardManager");
                    if (bmProperty != null)
                    {
                        bmProperty.objectReferenceValue = boardManager;
                        serializedManager.ApplyModifiedProperties();
                        Debug.Log("[UpgradeSystemSetup] Assigned BoardManager to UpgradeManager");
                    }
                }
                else
                {
                    Debug.LogWarning("[UpgradeSystemSetup] BoardManager not found in scene. Please assign manually.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UpgradeSystemSetup] Failed to assign BoardManager: {e.Message}");
            }
        }
        
        private void CleanupUpgradeSystem()
        {
            try
            {
                if (Directory.Exists(PREFAB_PATH))
                {
                    Directory.Delete(PREFAB_PATH, true);
                    File.Delete(PREFAB_PATH + ".meta");
                }
                
                if (Directory.Exists(SO_PATH))
                {
                    Directory.Delete(SO_PATH, true);
                    File.Delete(SO_PATH + ".meta");
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("완료", "업그레이드 시스템 에셋이 삭제되었습니다.", "확인");
                Debug.Log("[UpgradeSystemSetup] Cleanup completed");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"삭제 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[UpgradeSystemSetup] Cleanup error: {e}");
            }
        }
    }
}
