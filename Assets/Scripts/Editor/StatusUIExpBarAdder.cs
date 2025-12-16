using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using LevelUpChess.UI;

/// <summary>
/// StatusUI 프리팹에 경험치 바를 추가하는 에디터 도구
/// </summary>
public class StatusUIExpBarAdder : Editor
{
    [MenuItem("Tools/LevelUpChess/Add Experience Bar to StatusUI Prefab")]
    public static void AddExpBarToStatusUI()
    {
        // StatusUI 프리팹 로드
        string prefabPath = "Assets/Prefabs/UI/StatusUI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("오류", "StatusUI 프리팹을 찾을 수 없습니다: " + prefabPath, "확인");
            return;
        }
        
        // 프리팹 인스턴스 생성
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        
        try
        {
            // StatusUI 컴포넌트 찾기
            StatusUI statusUI = instance.GetComponent<StatusUI>();
            if (statusUI == null)
            {
                EditorUtility.DisplayDialog("오류", "StatusUI 컴포넌트를 찾을 수 없습니다.", "확인");
                DestroyImmediate(instance);
                return;
            }
            
            // HealthContainer 찾기 (경험치 바를 체력바 아래에 배치)
            Transform healthContainer = instance.transform.Find("HealthContainer");
            if (healthContainer == null)
            {
                EditorUtility.DisplayDialog("오류", "HealthContainer를 찾을 수 없습니다.", "확인");
                DestroyImmediate(instance);
                return;
            }
            
            // 이미 ExpBar가 있는지 확인
            Transform existingExpBar = instance.transform.Find("ExpContainer");
            if (existingExpBar != null)
            {
                EditorUtility.DisplayDialog("알림", "경험치 바가 이미 존재합니다.", "확인");
                DestroyImmediate(instance);
                return;
            }
            
            // ExpContainer 생성 (경험치 바 컨테이너)
            GameObject expContainer = new GameObject("ExpContainer");
            RectTransform expContainerRect = expContainer.AddComponent<RectTransform>();
            expContainer.transform.SetParent(instance.transform, false);
            
            // HealthContainer 아래에 위치 (체력바 아래)
            expContainerRect.anchorMin = new Vector2(0, 0);
            expContainerRect.anchorMax = new Vector2(1, 0);
            expContainerRect.pivot = new Vector2(0.5f, 1f);
            expContainerRect.anchoredPosition = new Vector2(0, -0.08f); // 체력바 아래
            expContainerRect.sizeDelta = new Vector2(0, 0.04f); // 체력바보다 얇게
            
            // 경험치 바 배경
            GameObject expBackground = new GameObject("ExpBackground");
            RectTransform expBgRect = expBackground.AddComponent<RectTransform>();
            expBackground.AddComponent<CanvasRenderer>();
            Image expBgImage = expBackground.AddComponent<Image>();
            expBackground.transform.SetParent(expContainer.transform, false);
            
            expBgRect.anchorMin = Vector2.zero;
            expBgRect.anchorMax = Vector2.one;
            expBgRect.sizeDelta = Vector2.zero;
            expBgRect.anchoredPosition = Vector2.zero;
            expBgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            
            // 경험치 바 Fill
            GameObject expFill = new GameObject("ExpFill");
            RectTransform expFillRect = expFill.AddComponent<RectTransform>();
            expFill.AddComponent<CanvasRenderer>();
            Image expFillImage = expFill.AddComponent<Image>();
            expFill.transform.SetParent(expContainer.transform, false);
            
            expFillRect.anchorMin = Vector2.zero;
            expFillRect.anchorMax = Vector2.one;
            expFillRect.sizeDelta = Vector2.zero;
            expFillRect.anchoredPosition = Vector2.zero;
            expFillImage.type = Image.Type.Filled;
            expFillImage.fillMethod = Image.FillMethod.Horizontal;
            expFillImage.fillOrigin = 0;
            expFillImage.fillAmount = 0f;
            expFillImage.color = new Color(0.3f, 0.7f, 1f, 1f); // 하늘색
            
            // SerializedObject를 사용하여 private 필드 연결
            SerializedObject serializedStatusUI = new SerializedObject(statusUI);
            
            SerializedProperty expBackgroundProp = serializedStatusUI.FindProperty("expBackgroundImage");
            SerializedProperty expFillProp = serializedStatusUI.FindProperty("expFillImage");
            
            if (expBackgroundProp != null && expFillProp != null)
            {
                expBackgroundProp.objectReferenceValue = expBgImage;
                expFillProp.objectReferenceValue = expFillImage;
                serializedStatusUI.ApplyModifiedProperties();
                Debug.Log("경험치 바 UI 참조가 성공적으로 연결되었습니다.");
            }
            else
            {
                Debug.LogWarning("expBackgroundImage 또는 expFillImage 필드를 찾을 수 없습니다. 스크립트가 업데이트되었는지 확인하세요.");
            }
            
            // 프리팹에 변경사항 저장
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            
            EditorUtility.DisplayDialog("완료", "경험치 바가 StatusUI 프리팹에 추가되었습니다!", "확인");
        }
        finally
        {
            DestroyImmediate(instance);
        }
        
        AssetDatabase.Refresh();
    }
}
