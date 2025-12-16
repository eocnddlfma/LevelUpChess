using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using LevelUpChess.UI;
using LevelUpChess.Pieces;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 플레이어 경험치 바 UI를 자동으로 생성하는 에디터
    /// </summary>
    public class PlayerExpBarGenerator : EditorWindow
    {
        private string outputPath = "Assets/Prefabs/UI";
        private Team targetTeam = Team.White;

        [MenuItem("Tools/Chess/Generate Player Exp Bar")]
        public static void ShowWindow()
        {
            GetWindow<PlayerExpBarGenerator>("Player Exp Bar Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("플레이어 경험치 바 생성기", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "플레이어의 레벨과 경험치를 표시하는 UI를 생성합니다.\n" +
                "생성된 프리팹은 Canvas의 자식으로 추가하면 됩니다.",
                MessageType.Info);

            GUILayout.Space(20);

            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            targetTeam = (Team)EditorGUILayout.EnumPopup("Target Team", targetTeam);

            GUILayout.Space(20);

            if (GUILayout.Button("경험치 바 생성", GUILayout.Height(40)))
            {
                CreateExpBar();
            }
        }

        private void CreateExpBar()
        {
            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
                AssetDatabase.Refresh();
            }

            // 1. 루트 GameObject 생성 (PlayerExpBar)
            GameObject expBarRoot = new GameObject($"PlayerExpBar_{targetTeam}");
            RectTransform rootRect = expBarRoot.AddComponent<RectTransform>();
            
            // 앵커 설정 (화면 상단)
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(400, 80);
            
            // 위치 설정 (White는 위, Black은 아래)
            if (targetTeam == Team.White)
            {
                rootRect.anchoredPosition = new Vector2(0, -20);
            }
            else
            {
                rootRect.anchorMin = new Vector2(0.5f, 0f);
                rootRect.anchorMax = new Vector2(0.5f, 0f);
                rootRect.pivot = new Vector2(0.5f, 0f);
                rootRect.anchoredPosition = new Vector2(0, 20);
            }

            // VerticalLayoutGroup 추가
            VerticalLayoutGroup layoutGroup = expBarRoot.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            // 2. Level Text 생성
            GameObject levelTextObj = new GameObject("LevelText");
            levelTextObj.transform.SetParent(expBarRoot.transform, false);
            
            RectTransform levelTextRect = levelTextObj.AddComponent<RectTransform>();
            levelTextRect.sizeDelta = new Vector2(0, 25);
            
            TextMeshProUGUI levelText = levelTextObj.AddComponent<TextMeshProUGUI>();
            levelText.text = "Lv.1";
            levelText.fontSize = 20;
            levelText.fontStyle = FontStyles.Bold;
            levelText.color = Color.white;
            levelText.alignment = TextAlignmentOptions.Center;

            // 3. Exp Bar Background 생성
            GameObject expBarBgObj = new GameObject("ExpBarBackground");
            expBarBgObj.transform.SetParent(expBarRoot.transform, false);
            
            RectTransform expBarBgRect = expBarBgObj.AddComponent<RectTransform>();
            expBarBgRect.sizeDelta = new Vector2(0, 25);
            
            Image expBarBg = expBarBgObj.AddComponent<Image>();
            expBarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // 4. Exp Fill 생성 (Background의 자식)
            GameObject expFillObj = new GameObject("ExpFill");
            expFillObj.transform.SetParent(expBarBgObj.transform, false);
            
            RectTransform expFillRect = expFillObj.AddComponent<RectTransform>();
            expFillRect.anchorMin = new Vector2(0, 0);
            expFillRect.anchorMax = new Vector2(1, 1);
            expFillRect.offsetMin = Vector2.zero;
            expFillRect.offsetMax = Vector2.zero;
            
            Image expFillImage = expFillObj.AddComponent<Image>();
            expFillImage.color = Color.yellow;
            expFillImage.type = Image.Type.Filled;
            expFillImage.fillMethod = Image.FillMethod.Horizontal;
            expFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            expFillImage.fillAmount = 0f;

            // 5. Exp Text 생성 (Background의 자식)
            GameObject expTextObj = new GameObject("ExpText");
            expTextObj.transform.SetParent(expBarBgObj.transform, false);
            
            RectTransform expTextRect = expTextObj.AddComponent<RectTransform>();
            expTextRect.anchorMin = new Vector2(0, 0);
            expTextRect.anchorMax = new Vector2(1, 1);
            expTextRect.offsetMin = Vector2.zero;
            expTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI expText = expTextObj.AddComponent<TextMeshProUGUI>();
            expText.text = "0/10";
            expText.fontSize = 14;
            expText.color = Color.white;
            expText.alignment = TextAlignmentOptions.Center;

            // 6. PlayerExpBar 스크립트 추가
            PlayerExpBar expBarScript = expBarRoot.AddComponent<PlayerExpBar>();

            SerializedObject so = new SerializedObject(expBarScript);
            so.FindProperty("targetTeam").enumValueIndex = (int)targetTeam;
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("expText").objectReferenceValue = expText;
            so.FindProperty("expFillImage").objectReferenceValue = expFillImage;
            so.FindProperty("fillColor").colorValue = Color.yellow;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 7. 프리팹 저장
            string prefabPath = $"{outputPath}/PlayerExpBar_{targetTeam}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(expBarRoot, prefabPath);

            if (prefab != null)
            {
                Debug.Log($"[PlayerExpBarGenerator] ✓ Created PlayerExpBar prefab at: {prefabPath}");
                EditorUtility.DisplayDialog("성공", 
                    $"PlayerExpBar_{targetTeam} 프리팹이 생성되었습니다!\n\n경로: {prefabPath}\n\n" +
                    "사용법:\n" +
                    "1. Canvas에 프리팹을 자식으로 추가\n" +
                    "2. PlayerLevel 컴포넌트가 경험치 이벤트를 발생시키면 자동으로 업데이트됩니다", 
                    "확인");
                
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                Debug.LogError("[PlayerExpBarGenerator] Failed to create prefab!");
                EditorUtility.DisplayDialog("실패", "프리팹 생성에 실패했습니다.", "확인");
            }

            DestroyImmediate(expBarRoot);
        }
    }
}
