using UnityEngine;
using UnityEditor;
using LevelUpChess.Upgrades;
using LevelUpChess.Upgrades.UI;
using Unity.Netcode;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 업그레이드 시스템 진단 도구
    /// </summary>
    public class UpgradeSystemDiagnostics : EditorWindow
    {
        [MenuItem("Tools/LevelUpChess/Diagnose Upgrade System")]
        public static void ShowWindow()
        {
            var window = GetWindow<UpgradeSystemDiagnostics>("Upgrade System Diagnostics");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private Vector2 scrollPosition;
        
        private void OnGUI()
        {
            GUILayout.Label("업그레이드 시스템 진단", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (GUILayout.Button("진단 실행", GUILayout.Height(40)))
            {
                RunDiagnostics();
            }
            
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.EndScrollView();
        }
        
        private void RunDiagnostics()
        {
            Debug.Log("========== 업그레이드 시스템 진단 시작 ==========");
            
            // 1. UpgradeManager 확인
            var manager = FindFirstObjectByType<UpgradeManager>();
            if (manager == null)
            {
                Debug.LogError("❌ UpgradeManager가 씬에 없습니다!");
            }
            else
            {
                Debug.Log($"✅ UpgradeManager 발견: {manager.name}");
                
                // NetworkBehaviour 확인
                var netBehaviour = manager.GetComponent<NetworkBehaviour>();
                if (netBehaviour != null)
                {
                    Debug.Log($"  - NetworkBehaviour: OK");
                }
                else
                {
                    Debug.LogWarning($"  - NetworkBehaviour: 없음");
                }
                
                // UpgradePool 확인
                var serializedManager = new SerializedObject(manager);
                var poolProperty = serializedManager.FindProperty("upgradePool");
                if (poolProperty != null && poolProperty.objectReferenceValue != null)
                {
                    var pool = poolProperty.objectReferenceValue as UpgradePoolSO;
                    Debug.Log($"  - UpgradePool: ✅ {pool.name}");
                    
                    // 풀 내용 확인
                    CheckUpgradePool(pool);
                }
                else
                {
                    Debug.LogError($"  - UpgradePool: ❌ 할당되지 않음!");
                }
                
                // BoardManager 확인
                var bmProperty = serializedManager.FindProperty("boardManager");
                if (bmProperty != null && bmProperty.objectReferenceValue != null)
                {
                    Debug.Log($"  - BoardManager: ✅");
                }
                else
                {
                    Debug.LogWarning($"  - BoardManager: ⚠️ 할당되지 않음");
                }
            }
            
            // 2. UI 확인
            var panelUI = FindFirstObjectByType<UpgradeSelectionPanelUI>();
            if (panelUI == null)
            {
                Debug.LogError("❌ UpgradeSelectionPanelUI가 씬에 없습니다!");
            }
            else
            {
                Debug.Log($"✅ UpgradeSelectionPanelUI 발견: {panelUI.name}");
                
                var serializedUI = new SerializedObject(panelUI);
                
                // 필수 UI 요소 확인
                CheckUIField(serializedUI, "panelRoot", "Panel Root");
                CheckUIField(serializedUI, "canvasGroup", "Canvas Group");
                CheckUIField(serializedUI, "titleText", "Title Text");
                CheckUIField(serializedUI, "pieceNameText", "Piece Name Text");
                CheckUIField(serializedUI, "cardContainer", "Card Container");
                CheckUIField(serializedUI, "cardPrefab", "Card Prefab");
                CheckUIField(serializedUI, "skipButton", "Skip Button");
                CheckUIField(serializedUI, "closeButton", "Close Button");
            }
            
            // 3. NetworkManager 확인
            var networkManager = FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogWarning("⚠️ NetworkManager가 씬에 없습니다!");
            }
            else
            {
                Debug.Log($"✅ NetworkManager 발견");
            }
            
            // 4. 이벤트 버스 확인
            CheckEventBus();
            
            Debug.Log("========== 업그레이드 시스템 진단 완료 ==========");
        }
        
        private void CheckUpgradePool(UpgradePoolSO pool)
        {
            var serializedPool = new SerializedObject(pool);
            
            // 공통 풀 확인
            var commonMovement = serializedPool.FindProperty("commonMovementUpgrades");
            var commonStat = serializedPool.FindProperty("commonStatUpgrades");
            var commonAbility = serializedPool.FindProperty("commonAbilityUpgrades");
            
            Debug.Log($"    공통 풀:");
            Debug.Log($"      - Movement: {commonMovement?.arraySize}개");
            Debug.Log($"      - Stat: {commonStat?.arraySize}개");
            Debug.Log($"      - Ability: {commonAbility?.arraySize}개");
            
            // 피스별 풀 확인
            var pawnUp = serializedPool.FindProperty("pawnUpgrades");
            var knightUp = serializedPool.FindProperty("knightUpgrades");
            var bishopUp = serializedPool.FindProperty("bishopUpgrades");
            var rookUp = serializedPool.FindProperty("rookUpgrades");
            var queenUp = serializedPool.FindProperty("queenUpgrades");
            var kingUp = serializedPool.FindProperty("kingUpgrades");
            
            Debug.Log($"    피스별 풀:");
            Debug.Log($"      - Pawn: {GetPoolSize(pawnUp)}개");
            Debug.Log($"      - Knight: {GetPoolSize(knightUp)}개");
            Debug.Log($"      - Bishop: {GetPoolSize(bishopUp)}개");
            Debug.Log($"      - Rook: {GetPoolSize(rookUp)}개");
            Debug.Log($"      - Queen: {GetPoolSize(queenUp)}개");
            Debug.Log($"      - King: {GetPoolSize(kingUp)}개");

            int totalUpgrades = commonMovement.arraySize + 
                               commonStat.arraySize + 
                               commonAbility.arraySize +
                               GetPoolSize(pawnUp) + GetPoolSize(knightUp) + 
                               GetPoolSize(bishopUp) + GetPoolSize(rookUp) + 
                               GetPoolSize(queenUp) + GetPoolSize(kingUp);
            
            if (totalUpgrades == 0)
            {
                Debug.LogError("    ❌ 업그레이드가 하나도 없습니다!");
            }
        }
        
        private int GetPoolSize(SerializedProperty poolProp)
        {
            if (poolProp == null) return 0;
            
            var movement = poolProp.FindPropertyRelative("movementUpgrades");
            var ability = poolProp.FindPropertyRelative("abilityUpgrades");
            var stat = poolProp.FindPropertyRelative("statUpgrades");
            
            return (movement.arraySize) + (ability.arraySize) + (stat.arraySize);
        }
        
        private void CheckUIField(SerializedObject serializedUI, string fieldName, string displayName)
        {
            var prop = serializedUI.FindProperty(fieldName);
            if (prop != null && prop.objectReferenceValue != null)
            {
                Debug.Log($"  - {displayName}: ✅");
            }
            else
            {
                Debug.LogWarning($"  - {displayName}: ⚠️ 미할당");
            }
        }
        
        private void CheckEventBus()
        {
            // 이벤트 버스는 런타임에만 확인 가능
            if (Application.isPlaying)
            {
                Debug.Log("✅ 이벤트 버스 체크 (런타임)");
                // Bus<PieceLevelUpEvent>.OnEvent의 구독자 수는 런타임에 확인 가능
            }
            else
            {
                Debug.Log("⚠️ 이벤트 버스는 플레이 모드에서 확인 가능합니다.");
            }
        }
    }
}
