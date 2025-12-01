#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using LevelUpChess.Pieces;
using LevelUpChess.UI;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 체력바 프리팹을 체스 기물 프리팹에 자동으로 추가하는 에디터 도구
    /// </summary>
    public class HealthBarSetupEditor : EditorWindow
    {
        private const string PIECE_PREFAB_FOLDER = "Assets/Prefabs/Pieces";
        private const string RESOURCES_PREFAB_FOLDER = "Assets/Resources/Prefabs/Pieces";
        private const string HEALTHBAR_PREFAB_PATH = "Assets/Prefabs/UI/HealthBar.prefab";
        
        private GameObject healthBarPrefab;
        private Vector3 healthBarOffset = new Vector3(0, 0.6f, 0);
        private bool overwriteExisting = false;

        [MenuItem("Chess/Setup Health Bars")]
        public static void ShowWindow()
        {
            GetWindow<HealthBarSetupEditor>("Health Bar Setup");
        }
        
        private void OnEnable()
        {
            // 체력바 프리팹 자동 로드
            healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HEALTHBAR_PREFAB_PATH);
        }

        private void OnGUI()
        {
            GUILayout.Label("체력바 설정 도구", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이 도구는 체력바 프리팹을 모든 체스 기물 프리팹에 추가합니다.\n" +
                "체력바 프리팹을 수정하면 모든 기물에 자동 반영됩니다.", 
                MessageType.Info);

            GUILayout.Space(15);
            
            // 프리팹 설정
            GUILayout.Label("프리팹 설정", EditorStyles.boldLabel);
            healthBarPrefab = (GameObject)EditorGUILayout.ObjectField("체력바 프리팹", healthBarPrefab, typeof(GameObject), false);
            
            if (healthBarPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "체력바 프리팹이 없습니다!\n'체력바 프리팹 생성' 버튼을 눌러 생성하세요.", 
                    MessageType.Warning);
                
                if (GUILayout.Button("체력바 프리팹 생성", GUILayout.Height(35)))
                {
                    CreateHealthBarPrefab();
                }
            }
            
            GUILayout.Space(10);
            healthBarOffset = EditorGUILayout.Vector3Field("위치 오프셋", healthBarOffset);
            
            GUILayout.Space(10);
            overwriteExisting = EditorGUILayout.Toggle("기존 체력바 덮어쓰기", overwriteExisting);

            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(healthBarPrefab == null);
            
            if (GUILayout.Button("모든 기물 프리팹에 체력바 추가", GUILayout.Height(40)))
            {
                AddHealthBarsToPrefabs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("선택된 프리팹에만 체력바 추가", GUILayout.Height(30)))
            {
                AddHealthBarToSelectedPrefab();
            }
            
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("모든 기물에서 체력바 제거", GUILayout.Height(30)))
            {
                RemoveHealthBarsFromPrefabs();
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                $"기물 프리팹 위치:\n{PIECE_PREFAB_FOLDER}\n{RESOURCES_PREFAB_FOLDER}\n\n" +
                $"체력바 프리팹 위치:\n{HEALTHBAR_PREFAB_PATH}", 
                MessageType.None);
        }

        private void CreateHealthBarPrefab()
        {
            // 폴더 생성
            string folderPath = "Assets/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // 체력바 프리팹 생성
            GameObject healthBarObj = CreateHealthBarGameObject();
            
            // 프리팹으로 저장
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(healthBarObj, HEALTHBAR_PREFAB_PATH);
            DestroyImmediate(healthBarObj);
            
            healthBarPrefab = prefab;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 생성된 프리팹 선택
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            EditorUtility.DisplayDialog("완료", 
                $"체력바 프리팹이 생성되었습니다!\n\n경로: {HEALTHBAR_PREFAB_PATH}\n\n" +
                "프리팹을 열어 원하는대로 수정하세요.\n" +
                "(크기, 색상, 폰트, 아이콘 등)", "확인");
        }

        private GameObject CreateHealthBarGameObject()
        {
            // Canvas 오브젝트 생성
            GameObject canvasObj = new GameObject("HealthBar");

            // Canvas 컴포넌트 설정
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            // Canvas Scaler
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // RectTransform 설정
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(0.8f, 0.1f);

            // 배경 이미지 생성
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.localScale = Vector3.one;
            
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // 트레일 바 생성 (흰색)
            GameObject trailObj = new GameObject("Trail");
            trailObj.transform.SetParent(canvasObj.transform);
            RectTransform trailRect = trailObj.AddComponent<RectTransform>();
            trailRect.anchorMin = Vector2.zero;
            trailRect.anchorMax = Vector2.one;
            trailRect.sizeDelta = Vector2.zero;
            trailRect.anchoredPosition = Vector2.zero;
            trailRect.localScale = Vector3.one;
            
            UnityEngine.UI.Image trailImage = trailObj.AddComponent<UnityEngine.UI.Image>();
            trailImage.color = Color.white;
            trailImage.type = UnityEngine.UI.Image.Type.Filled;
            trailImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            trailImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            trailImage.fillAmount = 1f;

            // 체력바 Fill 이미지 생성
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.localScale = Vector3.one;
            
            UnityEngine.UI.Image fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = Color.green;
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            // 체력 수치 텍스트 생성
            GameObject healthTextObj = new GameObject("HealthText");
            healthTextObj.transform.SetParent(canvasObj.transform);
            RectTransform healthTextRect = healthTextObj.AddComponent<RectTransform>();
            healthTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            healthTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            healthTextRect.sizeDelta = new Vector2(0.8f, 0.1f);
            healthTextRect.anchoredPosition = Vector2.zero;
            healthTextRect.localScale = Vector3.one;
            
            TMPro.TextMeshProUGUI healthText = healthTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            healthText.text = "10/10";
            healthText.fontSize = 0.3f;
            healthText.color = Color.white;
            healthText.alignment = TMPro.TextAlignmentOptions.Center;
            healthText.enableAutoSizing = true;
            healthText.fontSizeMin = 0.05f;
            healthText.fontSizeMax = 0.3f;

            // 공격력 컨테이너 생성
            GameObject attackContainer = new GameObject("AttackContainer");
            attackContainer.transform.SetParent(canvasObj.transform);
            RectTransform containerRect = attackContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(0f, 0f);
            containerRect.pivot = new Vector2(0f, 1f);
            containerRect.sizeDelta = new Vector2(0.4f, 0.12f);
            containerRect.anchoredPosition = new Vector2(0, -0.01f);
            containerRect.localScale = Vector3.one;

            // 공격력 아이콘 이미지
            GameObject attackIconObj = new GameObject("AttackIcon");
            attackIconObj.transform.SetParent(attackContainer.transform);
            RectTransform iconRect = attackIconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(0.1f, 0.1f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.localScale = Vector3.one;
            
            UnityEngine.UI.Image attackIcon = attackIconObj.AddComponent<UnityEngine.UI.Image>();
            // 아이콘 스프라이트 로드 시도
            Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ExternalAssets/Sword icon.png");
            if (swordSprite != null)
            {
                attackIcon.sprite = swordSprite;
            }
            attackIcon.preserveAspect = true;

            // 공격력 수치 텍스트
            GameObject attackTextObj = new GameObject("AttackText");
            attackTextObj.transform.SetParent(attackContainer.transform);
            RectTransform attackTextRect = attackTextObj.AddComponent<RectTransform>();
            attackTextRect.anchorMin = new Vector2(0f, 0.5f);
            attackTextRect.anchorMax = new Vector2(0f, 0.5f);
            attackTextRect.pivot = new Vector2(0f, 0.5f);
            attackTextRect.sizeDelta = new Vector2(0.24f, 0.12f);
            attackTextRect.anchoredPosition = new Vector2(0.12f, 0);
            attackTextRect.localScale = Vector3.one;
            
            TMPro.TextMeshProUGUI attackText = attackTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            attackText.text = "1";
            attackText.fontSize = 0.24f;
            attackText.color = Color.yellow;
            attackText.alignment = TMPro.TextAlignmentOptions.Left;
            attackText.enableAutoSizing = true;
            attackText.fontSizeMin = 0.05f;
            attackText.fontSizeMax = 0.24f;

            // HealthBarUI 컴포넌트 추가
            HealthBarUI healthBarUI = canvasObj.AddComponent<HealthBarUI>();
            
            // SerializedObject를 사용하여 private 필드 설정
            SerializedObject so = new SerializedObject(healthBarUI);
            so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("fillImage").objectReferenceValue = fillImage;
            so.FindProperty("trailImage").objectReferenceValue = trailImage;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("attackIcon").objectReferenceValue = attackIcon;
            so.FindProperty("attackText").objectReferenceValue = attackText;
            so.FindProperty("fullHealthColor").colorValue = Color.green;
            so.FindProperty("lowHealthColor").colorValue = Color.red;
            so.FindProperty("backgroundColor").colorValue = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            so.FindProperty("trailColor").colorValue = Color.white;
            so.FindProperty("healthTextColor").colorValue = Color.white;
            so.FindProperty("attackTextColor").colorValue = Color.yellow;
            so.FindProperty("trailDelay").floatValue = 0.3f;
            so.FindProperty("trailSpeed").floatValue = 2f;
            so.FindProperty("showHealthNumbers").boolValue = true;
            so.FindProperty("showAttackPower").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            return canvasObj;
        }

        private void AddHealthBarsToPrefabs()
        {
            if (healthBarPrefab == null)
            {
                EditorUtility.DisplayDialog("오류", "체력바 프리팹을 먼저 설정해주세요.", "확인");
                return;
            }

            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;

            string[] prefabFolders = { PIECE_PREFAB_FOLDER, RESOURCES_PREFAB_FOLDER };

            foreach (string folder in prefabFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"[HealthBarSetup] Folder not found: {folder}");
                    continue;
                }

                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

                foreach (string guid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    
                    if (prefab == null) continue;

                    // ChessPiece 컴포넌트가 있는지 확인
                    ChessPiece piece = prefab.GetComponent<ChessPiece>();
                    if (piece == null) continue;

                    // 이미 체력바가 있는지 확인
                    HealthBarUI existingHealthBar = prefab.GetComponentInChildren<HealthBarUI>();
                    if (existingHealthBar != null && !overwriteExisting)
                    {
                        Debug.Log($"[HealthBarSetup] Skipping {prefab.name} - already has health bar");
                        skipCount++;
                        continue;
                    }

                    // 프리팹 수정
                    try
                    {
                        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                        {
                            GameObject root = editScope.prefabContentsRoot;
                            
                            // 기존 체력바 제거 (덮어쓰기 모드일 경우)
                            if (overwriteExisting)
                            {
                                HealthBarUI oldHealthBar = root.GetComponentInChildren<HealthBarUI>();
                                if (oldHealthBar != null)
                                {
                                    DestroyImmediate(oldHealthBar.gameObject);
                                }
                            }
                            
                            // 체력바 프리팹 인스턴스화
                            GameObject healthBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, root.transform);
                            healthBarInstance.transform.localPosition = healthBarOffset;
                            healthBarInstance.transform.localRotation = Quaternion.identity;
                            healthBarInstance.transform.localScale = Vector3.one;
                            
                            Debug.Log($"[HealthBarSetup] Added health bar to {prefab.name}");
                            successCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[HealthBarSetup] Failed to modify {prefab.name}: {e.Message}");
                        failCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("완료", 
                $"체력바 추가 완료!\n성공: {successCount}개\n스킵: {skipCount}개\n실패: {failCount}개", "확인");
            
            Debug.Log($"[HealthBarSetup] Complete! Success: {successCount}, Skipped: {skipCount}, Failed: {failCount}");
        }

        private void AddHealthBarToSelectedPrefab()
        {
            if (healthBarPrefab == null)
            {
                EditorUtility.DisplayDialog("오류", "체력바 프리팹을 먼저 설정해주세요.", "확인");
                return;
            }

            GameObject selected = Selection.activeGameObject;
            
            if (selected == null)
            {
                EditorUtility.DisplayDialog("오류", "프리팹을 선택해주세요.", "확인");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected);
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog("오류", "유효한 프리팹이 아닙니다.", "확인");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            ChessPiece piece = prefab.GetComponent<ChessPiece>();
            
            if (piece == null)
            {
                EditorUtility.DisplayDialog("오류", "ChessPiece 컴포넌트가 없는 프리팹입니다.", "확인");
                return;
            }

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                GameObject root = editScope.prefabContentsRoot;
                
                // 기존 체력바 제거
                HealthBarUI oldHealthBar = root.GetComponentInChildren<HealthBarUI>();
                if (oldHealthBar != null)
                {
                    DestroyImmediate(oldHealthBar.gameObject);
                }
                
                // 체력바 프리팹 인스턴스화
                GameObject healthBarInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, root.transform);
                healthBarInstance.transform.localPosition = healthBarOffset;
                healthBarInstance.transform.localRotation = Quaternion.identity;
                healthBarInstance.transform.localScale = Vector3.one;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("완료", $"{selected.name}에 체력바가 추가되었습니다.", "확인");
        }

        private void RemoveHealthBarsFromPrefabs()
        {
            if (!EditorUtility.DisplayDialog("확인", 
                "정말로 모든 기물에서 체력바를 제거하시겠습니까?", "예", "아니오"))
            {
                return;
            }

            int removeCount = 0;
            string[] prefabFolders = { PIECE_PREFAB_FOLDER, RESOURCES_PREFAB_FOLDER };

            foreach (string folder in prefabFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

                foreach (string guid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    
                    if (prefab == null) continue;

                    HealthBarUI existingHealthBar = prefab.GetComponentInChildren<HealthBarUI>();
                    if (existingHealthBar == null) continue;

                    using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                    {
                        HealthBarUI healthBar = editScope.prefabContentsRoot.GetComponentInChildren<HealthBarUI>();
                        if (healthBar != null)
                        {
                            DestroyImmediate(healthBar.gameObject);
                            removeCount++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("완료", $"체력바 {removeCount}개가 제거되었습니다.", "확인");
        }
    }
}
#endif
