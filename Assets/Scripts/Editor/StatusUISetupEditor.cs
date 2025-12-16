#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using LevelUpChess.Pieces;
using LevelUpChess.UI;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// StatusUI 프리팹을 체스 기물 프리팹에 자동으로 추가하는 에디터 도구
    /// </summary>
    public class StatusUISetupEditor : EditorWindow
    {
        private const string PIECE_PREFAB_FOLDER = "Assets/Prefabs/Pieces";
        private const string RESOURCES_PREFAB_FOLDER = "Assets/Resources/Prefabs/Pieces";
        private const string STATUSUI_PREFAB_PATH = "Assets/Prefabs/UI/StatusUI.prefab";
        
        private GameObject statusUIPrefab;
        private Vector3 statusUIOffset = new Vector3(0, 0.6f, 0);
        private bool overwriteExisting = false;

        [MenuItem("Chess/Setup Status UI")]
        public static void ShowWindow()
        {
            GetWindow<StatusUISetupEditor>("Status UI Setup");
        }
        
        private void OnEnable()
        {
            // StatusUI 프리팹 자동 로드
            statusUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(STATUSUI_PREFAB_PATH);
        }

        private void OnGUI()
        {
            GUILayout.Label("상태 UI 설정 도구", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이 도구는 StatusUI 프리팹을 모든 체스 기물 프리팹에 추가합니다.\n" +
                "StatusUI 프리팹을 수정하면 모든 기물에 자동 반영됩니다.", 
                MessageType.Info);

            GUILayout.Space(15);
            
            // 프리팹 설정
            GUILayout.Label("프리팹 설정", EditorStyles.boldLabel);
            statusUIPrefab = (GameObject)EditorGUILayout.ObjectField("StatusUI 프리팹", statusUIPrefab, typeof(GameObject), false);
            
            if (statusUIPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "StatusUI 프리팹이 없습니다!\n'StatusUI 프리팹 생성' 버튼을 눌러 생성하세요.", 
                    MessageType.Warning);
                
                if (GUILayout.Button("StatusUI 프리팹 생성", GUILayout.Height(35)))
                {
                    CreateStatusUIPrefab();
                }
            }
            
            GUILayout.Space(10);
            statusUIOffset = EditorGUILayout.Vector3Field("위치 오프셋", statusUIOffset);
            
            GUILayout.Space(10);
            overwriteExisting = EditorGUILayout.Toggle("기존 StatusUI 덮어쓰기", overwriteExisting);

            GUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(statusUIPrefab == null);
            
            if (GUILayout.Button("모든 기물 프리팹에 StatusUI 추가", GUILayout.Height(40)))
            {
                AddStatusUIToPrefabs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("선택된 프리팹에만 StatusUI 추가", GUILayout.Height(30)))
            {
                AddStatusUIToSelectedPrefab();
            }
            
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("모든 기물에서 StatusUI 제거", GUILayout.Height(30)))
            {
                RemoveStatusUIFromPrefabs();
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                $"기물 프리팹 위치:\n{PIECE_PREFAB_FOLDER}\n{RESOURCES_PREFAB_FOLDER}\n\n" +
                $"StatusUI 프리팹 위치:\n{STATUSUI_PREFAB_PATH}", 
                MessageType.None);
        }

        private void CreateStatusUIPrefab()
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

            // StatusUI 프리팹 생성
            GameObject statusUIObj = CreateStatusUIGameObject();
            
            // 프리팹으로 저장
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(statusUIObj, STATUSUI_PREFAB_PATH);
            DestroyImmediate(statusUIObj);
            
            statusUIPrefab = prefab;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 생성된 프리팹 선택
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            EditorUtility.DisplayDialog("완료", 
                $"StatusUI 프리팹이 생성되었습니다!\n\n경로: {STATUSUI_PREFAB_PATH}\n\n" +
                "프리팹을 열어 원하는대로 수정하세요.\n" +
                "(크기, 색상, 폰트, 아이콘 등)", "확인");
        }

        private GameObject CreateStatusUIGameObject()
        {
            // Canvas 오브젝트 생성
            GameObject canvasObj = new GameObject("StatusUI");

            // Canvas 컴포넌트 설정
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            // Canvas Scaler
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // RectTransform 설정
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1f, 0.12f);

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

            // 레벨 컨테이너 생성 (오른쪽 하단)
            GameObject levelContainer = new GameObject("LevelContainer");
            levelContainer.transform.SetParent(canvasObj.transform);
            RectTransform levelContainerRect = levelContainer.AddComponent<RectTransform>();
            levelContainerRect.anchorMin = new Vector2(1f, 0f);
            levelContainerRect.anchorMax = new Vector2(1f, 0f);
            levelContainerRect.pivot = new Vector2(1f, 1f);
            levelContainerRect.sizeDelta = new Vector2(0.35f, 0.12f);
            levelContainerRect.anchoredPosition = new Vector2(0, -0.01f);
            levelContainerRect.localScale = Vector3.one;

            // 레벨 텍스트
            GameObject levelTextObj = new GameObject("LevelText");
            levelTextObj.transform.SetParent(levelContainer.transform);
            RectTransform levelTextRect = levelTextObj.AddComponent<RectTransform>();
            levelTextRect.anchorMin = new Vector2(0f, 0f);
            levelTextRect.anchorMax = new Vector2(1f, 1f);
            levelTextRect.sizeDelta = Vector2.zero;
            levelTextRect.anchoredPosition = Vector2.zero;
            levelTextRect.localScale = Vector3.one;
            
            TMPro.TextMeshProUGUI levelText = levelTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            levelText.text = "Lv.1";
            levelText.fontSize = 0.2f;
            levelText.color = Color.cyan;
            levelText.alignment = TMPro.TextAlignmentOptions.Right;
            levelText.enableAutoSizing = true;
            levelText.fontSizeMin = 0.05f;
            levelText.fontSizeMax = 0.2f;

            // StatusUI 컴포넌트 추가
            StatusUI statusUI = canvasObj.AddComponent<StatusUI>();
            
            // SerializedObject를 사용하여 private 필드 설정
            SerializedObject so = new SerializedObject(statusUI);
            so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("fillImage").objectReferenceValue = fillImage;
            so.FindProperty("trailImage").objectReferenceValue = trailImage;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("attackIcon").objectReferenceValue = attackIcon;
            so.FindProperty("attackText").objectReferenceValue = attackText;
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("fullHealthColor").colorValue = Color.green;
            so.FindProperty("lowHealthColor").colorValue = Color.red;
            so.FindProperty("backgroundColor").colorValue = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            so.FindProperty("trailColor").colorValue = Color.white;
            so.FindProperty("healthTextColor").colorValue = Color.white;
            so.FindProperty("attackTextColor").colorValue = Color.yellow;
            so.FindProperty("levelTextColor").colorValue = Color.cyan;
            so.FindProperty("trailDelay").floatValue = 0.3f;
            so.FindProperty("trailSpeed").floatValue = 2f;
            so.FindProperty("showHealthNumbers").boolValue = true;
            so.FindProperty("showAttackPower").boolValue = true;
            so.FindProperty("showLevel").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            return canvasObj;
        }

        private void AddStatusUIToPrefabs()
        {
            if (statusUIPrefab == null)
            {
                EditorUtility.DisplayDialog("오류", "StatusUI 프리팹을 먼저 설정해주세요.", "확인");
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
                    Debug.LogWarning($"[StatusUISetup] Folder not found: {folder}");
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

                    // 이미 StatusUI가 있는지 확인
                    StatusUI existingStatusUI = prefab.GetComponentInChildren<StatusUI>();
                    if (existingStatusUI != null && !overwriteExisting)
                    {
                        Debug.Log($"[StatusUISetup] Skipping {prefab.name} - already has StatusUI");
                        skipCount++;
                        continue;
                    }

                    // 프리팹 수정
                    try
                    {
                        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                        {
                            GameObject root = editScope.prefabContentsRoot;
                            
                            // 기존 StatusUI 제거 (덮어쓰기 모드일 경우)
                            if (overwriteExisting)
                            {
                                StatusUI oldStatusUI = root.GetComponentInChildren<StatusUI>();
                                if (oldStatusUI != null)
                                {
                                    DestroyImmediate(oldStatusUI.gameObject);
                                }
                            }
                            
                            // StatusUI 프리팹 인스턴스화
                            GameObject statusUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(statusUIPrefab, root.transform);
                            statusUIInstance.transform.localPosition = statusUIOffset;
                            statusUIInstance.transform.localRotation = Quaternion.identity;
                            statusUIInstance.transform.localScale = Vector3.one;
                            
                            Debug.Log($"[StatusUISetup] Added StatusUI to {prefab.name}");
                            successCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[StatusUISetup] Failed to modify {prefab.name}: {e.Message}");
                        failCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("완료", 
                $"StatusUI 추가 완료!\n성공: {successCount}개\n스킵: {skipCount}개\n실패: {failCount}개", "확인");
            
            Debug.Log($"[StatusUISetup] Complete! Success: {successCount}, Skipped: {skipCount}, Failed: {failCount}");
        }

        private void AddStatusUIToSelectedPrefab()
        {
            if (statusUIPrefab == null)
            {
                EditorUtility.DisplayDialog("오류", "StatusUI 프리팹을 먼저 설정해주세요.", "확인");
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
                
                // 기존 StatusUI 제거
                StatusUI oldStatusUI = root.GetComponentInChildren<StatusUI>();
                if (oldStatusUI != null)
                {
                    DestroyImmediate(oldStatusUI.gameObject);
                }
                
                // StatusUI 프리팹 인스턴스화
                GameObject statusUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(statusUIPrefab, root.transform);
                statusUIInstance.transform.localPosition = statusUIOffset;
                statusUIInstance.transform.localRotation = Quaternion.identity;
                statusUIInstance.transform.localScale = Vector3.one;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("완료", $"{selected.name}에 StatusUI가 추가되었습니다.", "확인");
        }

        private void RemoveStatusUIFromPrefabs()
        {
            if (!EditorUtility.DisplayDialog("확인", 
                "정말로 모든 기물에서 StatusUI를 제거하시겠습니까?", "예", "아니오"))
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

                    StatusUI existingStatusUI = prefab.GetComponentInChildren<StatusUI>();
                    if (existingStatusUI == null) continue;

                    using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                    {
                        StatusUI statusUI = editScope.prefabContentsRoot.GetComponentInChildren<StatusUI>();
                        if (statusUI != null)
                        {
                            DestroyImmediate(statusUI.gameObject);
                            removeCount++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("완료", $"StatusUI {removeCount}개가 제거되었습니다.", "확인");
        }
    }
}
#endif
