#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using LevelUpChess.Pieces;
using Pieces.Movements;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// PieceDataSO 자동 생성 및 프리팹 연결 에디터
    /// </summary>
    public class PieceDataSetupEditor : EditorWindow
    {
        private const string PIECE_DATA_FOLDER = "Assets/ScriptableObject/PieceData";
        private const string PREFAB_FOLDER = "Assets/Prefabs/Pieces";
        private const string RESOURCES_PREFAB_FOLDER = "Assets/Resources/Prefabs/Pieces";
        private const string UPGRADE_MOVEMENTS_FOLDER = "Assets/ScriptableObject/Movements/UpgradeMovements";

        [MenuItem("Chess/Setup Piece Data")]
        public static void ShowWindow()
        {
            GetWindow<PieceDataSetupEditor>("Piece Data Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("체스 기물 데이터 설정", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이 도구는 각 체스 기물에 대한 PieceDataSO를 생성하고,\n" +
                "해당 프리팹에 자동으로 연결합니다.", 
                MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("1. PieceDataSO 생성", GUILayout.Height(40)))
            {
                CreateAllPieceData();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("2. 프리팹에 PieceDataSO 연결", GUILayout.Height(40)))
            {
                AssignPieceDataToPrefabs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("3. Upgrade Movement 생성", GUILayout.Height(40)))
            {
                CreateAllUpgradeMovements();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("전체 실행 (생성 + 연결)", GUILayout.Height(50)))
            {
                CreateAllUpgradeMovements();
                CreateAllPieceData();
                AssignPieceDataToPrefabs();
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                $"PieceData 저장 위치: {PIECE_DATA_FOLDER}\n" +
                $"Upgrade Movements 저장 위치: {UPGRADE_MOVEMENTS_FOLDER}\n" +
                $"프리팹 위치: {PREFAB_FOLDER}, {RESOURCES_PREFAB_FOLDER}", 
                MessageType.None);
        }

        private void CreateAllPieceData()
        {
            // 폴더 생성
            if (!AssetDatabase.IsValidFolder(PIECE_DATA_FOLDER))
            {
                string parent = Path.GetDirectoryName(PIECE_DATA_FOLDER).Replace("\\", "/");
                string folderName = Path.GetFileName(PIECE_DATA_FOLDER);
                AssetDatabase.CreateFolder(parent, folderName);
                Debug.Log($"[PieceDataSetup] Created folder: {PIECE_DATA_FOLDER}");
            }

            // 각 기물 타입별로 PieceDataSO 생성
            CreatePieceData(PieceType.Pawn, "폰", 1, 1, "MovementPawnSO");
            CreatePieceData(PieceType.Rook, "룩", 5, 5, "MovementRookSO");
            CreatePieceData(PieceType.Knight, "나이트", 3, 3, "MovementKnightSO");
            CreatePieceData(PieceType.Bishop, "비숍", 3, 3, "MovementBishopSO");
            CreatePieceData(PieceType.Queen, "퀸", 9, 9, "MovementRookSO", "MovementBishopSO");
            CreatePieceData(PieceType.King, "킹", 100, 1, "MovementKingSO");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PieceDataSetup] All PieceDataSO created successfully!");
        }

        private void CreatePieceData(PieceType pieceType, string displayName, int health, int attack, params string[] movementNames)
        {
            string assetPath = $"{PIECE_DATA_FOLDER}/{pieceType}Data.asset";

            // 이미 존재하면 스킵
            if (AssetDatabase.LoadAssetAtPath<PieceDataSO>(assetPath) != null)
            {
                Debug.Log($"[PieceDataSetup] {pieceType}Data already exists, skipping...");
                return;
            }

            PieceDataSO data = ScriptableObject.CreateInstance<PieceDataSO>();

            // SerializedObject를 사용하여 private 필드 설정
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("pieceType").enumValueIndex = (int)pieceType;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("maxHealth").intValue = health;
            so.FindProperty("attackPower").intValue = attack;
            so.FindProperty("moveDuration").floatValue = 0.15f;

            // Movement Strategies 설정
            SerializedProperty strategiesProperty = so.FindProperty("movementStrategies");
            strategiesProperty.arraySize = movementNames.Length;

            for (int i = 0; i < movementNames.Length; i++)
            {
                string movementPath = $"{UPGRADE_MOVEMENTS_FOLDER}/{pieceType}/{movementNames[i]}.asset";
                PieceMovementSO movementSo = AssetDatabase.LoadAssetAtPath<PieceMovementSO>(movementPath);
                
                if (movementSo != null)
                {
                    strategiesProperty.GetArrayElementAtIndex(i).objectReferenceValue = movementSo;
                    Debug.Log($"[PieceDataSetup] Linked {movementNames[i]} to {pieceType}");
                }
                else
                {
                    Debug.LogWarning($"[PieceDataSetup] Movement not found: {movementPath}");
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(data, assetPath);
            Debug.Log($"[PieceDataSetup] Created: {assetPath}");
        }

        private void AssignPieceDataToPrefabs()
        {
            int successCount = 0;
            int failCount = 0;

            // 두 폴더 모두 처리
            string[] prefabFolders = { PREFAB_FOLDER, RESOURCES_PREFAB_FOLDER };

            foreach (string folder in prefabFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"[PieceDataSetup] Folder not found: {folder}");
                    continue;
                }

                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

                foreach (string guid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    
                    if (prefab == null) continue;

                    ChessPiece piece = prefab.GetComponent<ChessPiece>();
                    if (piece == null) continue;

                    // 프리팹 이름에서 기물 타입과 팀 추출 (예: "White_Pawn" -> Team.White, PieceType.Pawn)
                    PieceType? pieceType = GetPieceTypeFromName(prefab.name);
                    Team? team = GetTeamFromName(prefab.name);
                    
                    if (pieceType == null)
                    {
                        Debug.LogWarning($"[PieceDataSetup] Cannot determine piece type from: {prefab.name}");
                        failCount++;
                        continue;
                    }

                    // PieceDataSO 로드
                    string dataPath = $"{PIECE_DATA_FOLDER}/{pieceType}Data.asset";
                    PieceDataSO pieceData = AssetDatabase.LoadAssetAtPath<PieceDataSO>(dataPath);

                    if (pieceData == null)
                    {
                        Debug.LogWarning($"[PieceDataSetup] PieceData not found: {dataPath}");
                        failCount++;
                        continue;
                    }

                    // 프리팹 수정
                    using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                    {
                        ChessPiece editPiece = editScope.prefabContentsRoot.GetComponent<ChessPiece>();
                        if (editPiece != null)
                        {
                            SerializedObject so = new SerializedObject(editPiece);
                            so.FindProperty("pieceDataSo").objectReferenceValue = pieceData;
                            
                            // 팀 설정 (프리팹 이름에서 추출)
                            if (team.HasValue)
                            {
                                so.FindProperty("_team").enumValueIndex = (int)team.Value;
                                Debug.Log($"[PieceDataSetup] Set team to {team.Value} for {prefab.name}");
                            }
                            
                            so.ApplyModifiedPropertiesWithoutUndo();
                            
                            Debug.Log($"[PieceDataSetup] Assigned {pieceData.name} to {prefab.name}");
                            successCount++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[PieceDataSetup] Prefab assignment complete! Success: {successCount}, Failed: {failCount}");
            EditorUtility.DisplayDialog("완료", 
                $"프리팹 연결 완료!\n성공: {successCount}개\n실패: {failCount}개", "확인");
        }

        private Team? GetTeamFromName(string prefabName)
        {
            string lowerName = prefabName.ToLower();
            
            if (lowerName.StartsWith("white")) return Team.White;
            if (lowerName.StartsWith("black")) return Team.Black;
            
            return null;
        }

        private void CreateAllUpgradeMovements()
        {
            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObject/Movements"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObject", "Movements");
            }
            if (!AssetDatabase.IsValidFolder(UPGRADE_MOVEMENTS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObject/Movements", "UpgradeMovements");
            }

            // 각 기물 타입별로 Upgrade Movement 생성
            CreateUpgradeMovement(PieceType.Pawn, "MovementPawnSO");
            CreateUpgradeMovement(PieceType.Rook, "MovementRookSO");
            CreateUpgradeMovement(PieceType.Knight, "MovementKnightSO");
            CreateUpgradeMovement(PieceType.Bishop, "MovementBishopSO");
            CreateUpgradeMovement(PieceType.Queen, "MovementRookSO", "MovementBishopSO");
            CreateUpgradeMovement(PieceType.King, "MovementKingSO");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PieceDataSetup] All Upgrade Movements created successfully!");
        }

        private void CreateUpgradeMovement(PieceType pieceType, params string[] movementNames)
        {
            string folder = $"{UPGRADE_MOVEMENTS_FOLDER}/{pieceType}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(UPGRADE_MOVEMENTS_FOLDER, pieceType.ToString());
            }

            foreach (string movementName in movementNames)
            {
                string assetPath = $"{folder}/{movementName}.asset";
                if (AssetDatabase.LoadAssetAtPath<PieceMovementSO>(assetPath) != null)
                {
                    Debug.Log($"[PieceDataSetup] {movementName} already exists, skipping...");
                    continue;
                }

                PieceMovementSO movement = CreateMovementInstance(movementName);
                if (movement != null)
                {
                    AssetDatabase.CreateAsset(movement, assetPath);
                    Debug.Log($"[PieceDataSetup] Created: {assetPath}");
                }
            }
        }

        private PieceMovementSO CreateMovementInstance(string movementName)
        {
            switch (movementName)
            {
                case "MovementPawnSO":
                    return ScriptableObject.CreateInstance<MovementPawnSO>();
                case "MovementRookSO":
                    return ScriptableObject.CreateInstance<MovementRookSO>();
                case "MovementKnightSO":
                    return ScriptableObject.CreateInstance<MovementKnightSO>();
                case "MovementBishopSO":
                    return ScriptableObject.CreateInstance<MovementBishopSO>();
                case "MovementKingSO":
                    return ScriptableObject.CreateInstance<MovementKingSO>();
                default:
                    Debug.LogWarning($"[PieceDataSetup] Unknown movement: {movementName}");
                    return null;
            }
        }

        private PieceType? GetPieceTypeFromName(string prefabName)
        {
            string lowerName = prefabName.ToLower();

            if (lowerName.Contains("pawn")) return PieceType.Pawn;
            if (lowerName.Contains("rook")) return PieceType.Rook;
            if (lowerName.Contains("knight")) return PieceType.Knight;
            if (lowerName.Contains("bishop")) return PieceType.Bishop;
            if (lowerName.Contains("queen")) return PieceType.Queen;
            if (lowerName.Contains("king")) return PieceType.King;

            return null;
        }
    }
}
#endif
