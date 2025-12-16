using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using LevelUpChess.UI;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// TooltipUI 프리팹을 자동으로 생성하는 에디터
    /// </summary>
    public class TooltipUIGenerator : EditorWindow
    {
        private string outputPath = "Assets/Prefabs/UI";

        [MenuItem("Tools/Chess/Generate Tooltip UI")]
        public static void ShowWindow()
        {
            GetWindow<TooltipUIGenerator>("Tooltip UI Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("툴팁 UI 생성기", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "마우스를 따라다니는 툴팁 UI 프리팹을 생성합니다.\n" +
                "생성된 프리팹은 Canvas의 자식으로 추가하면 됩니다.",
                MessageType.Info);

            GUILayout.Space(20);

            outputPath = EditorGUILayout.TextField("Output Path", outputPath);

            GUILayout.Space(20);

            if (GUILayout.Button("툴팁 UI 생성", GUILayout.Height(40)))
            {
                CreateTooltipUI();
            }
        }

        private void CreateTooltipUI()
        {
            if (!System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.CreateDirectory(outputPath);
                AssetDatabase.Refresh();
            }

            // 1. 루트 GameObject 생성
            GameObject tooltipRoot = new GameObject("TooltipUI");
            RectTransform rootRect = tooltipRoot.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0, 0);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;

            // 2. TooltipPanel 생성
            GameObject panelObj = new GameObject("TooltipPanel");
            panelObj.transform.SetParent(tooltipRoot.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(300, 200);
            panelRect.pivot = new Vector2(0, 1);

            // 배경 이미지
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // CanvasGroup
            CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // ContentSizeFitter
            ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = panelObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(15, 15, 15, 15);
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            // 3. TooltipText 생성
            GameObject textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(panelObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(270, 0);

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Tooltip Text";
            tmpText.fontSize = 14;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.TopLeft;
            tmpText.textWrappingMode = TextWrappingModes.Normal;
            tmpText.richText = true;

            // LayoutElement
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 270;
            textLayout.flexibleHeight = 1;

            // 4. TooltipUI 스크립트 추가
            TooltipUI tooltipScript = tooltipRoot.AddComponent<TooltipUI>();

            SerializedObject so = new SerializedObject(tooltipScript);
            so.FindProperty("tooltipPanel").objectReferenceValue = panelRect;
            so.FindProperty("tooltipText").objectReferenceValue = tmpText;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("offset").vector2Value = new Vector2(20f, 20f);
            so.FindProperty("fadeSpeed").floatValue = 10f;
            so.FindProperty("screenPadding").vector2Value = new Vector2(10f, 10f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // 5. 프리팹 저장
            string prefabPath = $"{outputPath}/TooltipUI.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tooltipRoot, prefabPath);

            if (prefab != null)
            {
                Debug.Log($"[TooltipUIGenerator] ✓ Created TooltipUI prefab at: {prefabPath}");
                EditorUtility.DisplayDialog("성공", 
                    $"TooltipUI 프리팹이 생성되었습니다!\n\n경로: {prefabPath}\n\n" +
                    "사용법:\n" +
                    "1. Canvas에 프리팹을 자식으로 추가\n" +
                    "2. InputManager가 MouseHoverBeganEvent를 발생시키면 자동으로 표시됩니다", 
                    "확인");
                
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                Debug.LogError("[TooltipUIGenerator] Failed to create prefab!");
                EditorUtility.DisplayDialog("실패", "프리팹 생성에 실패했습니다.", "확인");
            }

            DestroyImmediate(tooltipRoot);
        }
    }
}
