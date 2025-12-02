using UnityEngine;
using UnityEditor;
using LevelUpChess.Pieces;
using LevelUpChess.UI;
using Pieces.Movements;

/// <summary>
/// 체스 피스 프리팹을 자동으로 생성하고 설정합니다
/// </summary>
public class ChessPiecePrefabGenerator : EditorWindow
{
    private Team selectedTeam = Team.White;
    private PieceType selectedPieceType = PieceType.Pawn;
    private string outputPath = "Assets/Prefabs/Pieces";
    private string resourcesOutputPath = "Assets/Resources/Prefabs/Pieces";
    private string pieceDataPath = "Assets/ScriptableObject/PieceData";
    private string movementPath = "Assets/ScriptableObject/Movements";
    private string statusUIPrefabPath = "Assets/Prefabs/UI/StatusUI.prefab";
    private Vector3 statusUIOffset = new Vector3(0, 0.6f, 0);
    private bool copyToResources = true;

    // Movement Strategy SO들 (자동 로드됨)
    private PawnMovement pawnMovement;
    private RookMovement rookMovement;
    private KnightMovement knightMovement;
    private BishopMovement bishopMovement;
    private KingMovement kingMovement;
    
    // StatusUI 프리팹
    private GameObject statusUIPrefab;

    [MenuItem("Tools/Chess/Generate Piece Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<ChessPiecePrefabGenerator>("Piece Prefab Generator");
    }
    
    [MenuItem("Tools/Chess/Fix All Piece Prefabs")]
    public static void FixAllPiecePrefabs()
    {
        string[] paths = new[]
        {
            "Assets/Prefabs/Pieces",
            "Assets/Resources/Prefabs/Pieces"
        };
        
        string pieceDataBasePath = "Assets/ScriptableObject/PieceData";
        int fixedCount = 0;
        
        foreach (string basePath in paths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { basePath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null) continue;
                
                ChessPiece piece = prefab.GetComponent<ChessPiece>();
                SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                
                if (piece == null) continue;
                
                bool needsFix = false;
                
                // 프리팹 인스턴스 생성
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                ChessPiece instancePiece = instance.GetComponent<ChessPiece>();
                SpriteRenderer instanceSr = instance.GetComponent<SpriteRenderer>();
                
                // 1. Sorting Order 수정
                if (instanceSr != null && instanceSr.sortingOrder != 5)
                {
                    instanceSr.sortingOrder = 5;
                    needsFix = true;
                    Debug.Log($"  ✓ Fixed sorting order for: {path}");
                }
                
                // 2. 누락된 컴포넌트 추가
                if (instance.GetComponent<PieceCombat>() == null)
                {
                    instance.AddComponent<PieceCombat>();
                    needsFix = true;
                    Debug.Log($"  ✓ Added PieceCombat to: {path}");
                }
                
                if (instance.GetComponent<PieceAnimator>() == null)
                {
                    instance.AddComponent<PieceAnimator>();
                    needsFix = true;
                    Debug.Log($"  ✓ Added PieceAnimator to: {path}");
                }
                
                if (instance.GetComponent<PieceUI>() == null)
                {
                    instance.AddComponent<PieceUI>();
                    needsFix = true;
                    Debug.Log($"  ✓ Added PieceUI to: {path}");
                }
                
                // 3. PieceDataSO 수정 - 프리팹 이름에서 PieceType 추출
                SerializedObject serializedPiece = new SerializedObject(instance.GetComponent<ChessPiece>());
                SerializedProperty pieceDataProp = serializedPiece.FindProperty("pieceDataSo");
                
                if (pieceDataProp != null && pieceDataProp.objectReferenceValue == null)
                {
                    // 프리팹 이름에서 PieceType 추출 (예: "White_Pawn" -> "Pawn")
                    string prefabName = prefab.name;
                    string[] parts = prefabName.Split('_');
                    if (parts.Length >= 2)
                    {
                        string pieceTypeName = parts[1];
                        string dataPath = $"{pieceDataBasePath}/{pieceTypeName}Data.asset";
                        PieceDataSO dataSo = AssetDatabase.LoadAssetAtPath<PieceDataSO>(dataPath);
                        
                        if (dataSo != null)
                        {
                            pieceDataProp.objectReferenceValue = dataSo;
                            serializedPiece.ApplyModifiedPropertiesWithoutUndo();
                            needsFix = true;
                            Debug.Log($"  ✓ Set PieceDataSO for {prefabName}: {dataSo.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"  ⚠ PieceDataSO not found: {dataPath}");
                        }
                    }
                }
                
                // 변경사항 저장
                if (needsFix)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    fixedCount++;
                }
                
                Object.DestroyImmediate(instance);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[FixAllPiecePrefabs] Fixed {fixedCount} prefabs");
        EditorUtility.DisplayDialog("완료", $"{fixedCount}개의 프리팹을 수정했습니다.\n(SortingOrder, Components, PieceDataSO)", "OK");
    }
    
    private void OnEnable()
    {
        // Movement Strategies 자동 로드
        LoadMovementStrategies();
        
        // StatusUI 프리팹 자동 로드
        statusUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(statusUIPrefabPath);
    }
    
    private void LoadMovementStrategies()
    {
        // 모든 Movement SO 검색
        string[] guids = AssetDatabase.FindAssets("t:PieceMovement", new[] { movementPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PieceMovement movement = AssetDatabase.LoadAssetAtPath<PieceMovement>(path);
            
            if (movement is PawnMovement pawn) pawnMovement = pawn;
            else if (movement is RookMovement rook) rookMovement = rook;
            else if (movement is KnightMovement knight) knightMovement = knight;
            else if (movement is BishopMovement bishop) bishopMovement = bishop;
            else if (movement is KingMovement king) kingMovement = king;
        }
        
        // 못 찾았으면 전체 프로젝트에서 검색
        if (pawnMovement == null || rookMovement == null || knightMovement == null || 
            bishopMovement == null || kingMovement == null)
        {
            guids = AssetDatabase.FindAssets("t:PieceMovement");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PieceMovement movement = AssetDatabase.LoadAssetAtPath<PieceMovement>(path);
                
                if (movement is PawnMovement pawn && pawnMovement == null) pawnMovement = pawn;
                else if (movement is RookMovement rook && rookMovement == null) rookMovement = rook;
                else if (movement is KnightMovement knight && knightMovement == null) knightMovement = knight;
                else if (movement is BishopMovement bishop && bishopMovement == null) bishopMovement = bishop;
                else if (movement is KingMovement king && kingMovement == null) kingMovement = king;
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("체스 피스 프리팹 생성기", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 팀 선택
        selectedTeam = (Team)EditorGUILayout.EnumPopup("Team", selectedTeam);

        // 기물 선택
        selectedPieceType = (PieceType)EditorGUILayout.EnumPopup("Piece Type", selectedPieceType);

        EditorGUILayout.Space();

        // PieceData 경로
        pieceDataPath = EditorGUILayout.TextField("PieceData Path", pieceDataPath);
        
        // Movement 경로
        movementPath = EditorGUILayout.TextField("Movement Path", movementPath);

        EditorGUILayout.Space();

        // Movement Strategy SO 표시 (자동 로드됨)
        GUILayout.Label("Movement Strategies (자동 로드)", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Pawn Movement", pawnMovement, typeof(PawnMovement), false);
        EditorGUILayout.ObjectField("Rook Movement", rookMovement, typeof(RookMovement), false);
        EditorGUILayout.ObjectField("Knight Movement", knightMovement, typeof(KnightMovement), false);
        EditorGUILayout.ObjectField("Bishop Movement", bishopMovement, typeof(BishopMovement), false);
        EditorGUILayout.ObjectField("King Movement", kingMovement, typeof(KingMovement), false);
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("Movement 다시 검색"))
        {
            LoadMovementStrategies();
        }

        EditorGUILayout.Space();
        
        // StatusUI 설정
        GUILayout.Label("StatusUI 설정", EditorStyles.boldLabel);
        statusUIPrefab = (GameObject)EditorGUILayout.ObjectField("StatusUI 프리팹", statusUIPrefab, typeof(GameObject), false);
        statusUIOffset = EditorGUILayout.Vector3Field("StatusUI 오프셋", statusUIOffset);

        EditorGUILayout.Space();

        // 출력 경로
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        resourcesOutputPath = EditorGUILayout.TextField("Resources Path", resourcesOutputPath);
        copyToResources = EditorGUILayout.Toggle("Resources에도 복사", copyToResources);

        EditorGUILayout.Space();

        // 생성 버튼
        if (GUILayout.Button("Generate Single Prefab", GUILayout.Height(40)))
        {
            GenerateSinglePrefab();
        }

        EditorGUILayout.Space();

        // 모든 피스 생성 버튼
        if (GUILayout.Button("Generate All Prefabs", GUILayout.Height(40)))
        {
            GenerateAllPrefabs();
        }

        EditorGUILayout.HelpBox(
            "프리팹 자동 생성 도구입니다.\n" +
            "- Movement Strategies가 자동으로 검색됩니다.\n" +
            "- StatusUI가 자동으로 추가됩니다.\n" +
            "- Single Prefab: 선택한 팀/기물만 생성\n" +
            "- All Prefabs: 모든 팀/기물 조합 생성 (White/Black × 6 피스)",
            MessageType.Info
        );
    }

    private void GenerateSinglePrefab()
    {
        CreatePiecePrefab(selectedTeam, selectedPieceType);
    }

    private void GenerateAllPrefabs()
    {
        foreach (Team team in System.Enum.GetValues(typeof(Team)))
        {
            foreach (PieceType pieceType in System.Enum.GetValues(typeof(PieceType)))
            {
                CreatePiecePrefab(team, pieceType);
            }
        }
    }

    private void CreatePiecePrefab(Team team, PieceType pieceType)
    {
        // 1. 빈 GameObject 생성
        GameObject go = new GameObject($"{team}_{pieceType}");

        try
        {
            // 2. Collider2D 먼저 추가 (ChessPiece의 RequireComponent 만족)
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1);

            // 3. ChessPiece 컴포넌트 추가
            ChessPiece piece = go.AddComponent<ChessPiece>();
            
            // 3-1. PieceCombat 컴포넌트 추가
            go.AddComponent<PieceCombat>();
            
            // 3-2. PieceAnimator 컴포넌트 추가
            go.AddComponent<PieceAnimator>();
            
            // 3-3. PieceUI 컴포넌트 추가
            go.AddComponent<PieceUI>();

            // 4. SpriteRenderer 추가
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5; // 타일 위에 그려지도록 설정

            // 5. SerializedObject를 사용하여 private 필드 설정
            SerializedObject serializedPiece = new SerializedObject(piece);
            
            // Team 설정
            SerializedProperty teamProp = serializedPiece.FindProperty("_team");
            if (teamProp != null)
            {
                teamProp.enumValueIndex = (int)team;
            }

            // 6. PieceData 로드 또는 생성
            PieceDataSO pieceDataSo = LoadOrCreatePieceData(pieceType);
            if (pieceDataSo != null)
            {
                SerializedProperty pieceDataProp = serializedPiece.FindProperty("pieceDataSo");
                if (pieceDataProp != null)
                {
                    pieceDataProp.objectReferenceValue = pieceDataSo;
                    Debug.Log($"✓ Set PieceDataSO for {team}_{pieceType}: {pieceDataSo.name}");
                }
                else
                {
                    Debug.LogError($"✗ Cannot find 'pieceDataSo' property on ChessPiece!");
                }
            }
            else
            {
                Debug.LogError($"✗ PieceDataSO is null for {pieceType}!");
            }

            serializedPiece.ApplyModifiedPropertiesWithoutUndo();

            // 7. Sprite 로드 및 설정
            Sprite sprite = LoadSprite(team, pieceType);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                Debug.Log($"ℹ Sprite not found for {team}_{pieceType}. You can add sprites manually later.");
            }
            
            // 8. StatusUI 프리팹 추가
            if (statusUIPrefab != null)
            {
                GameObject statusUIInstance = (GameObject)PrefabUtility.InstantiatePrefab(statusUIPrefab, go.transform);
                statusUIInstance.transform.localPosition = statusUIOffset;
                statusUIInstance.transform.localRotation = Quaternion.identity;
                statusUIInstance.transform.localScale = Vector3.one;
                Debug.Log($"✓ Added StatusUI to {team}_{pieceType}");
            }
            else
            {
                Debug.LogWarning($"⚠ StatusUI prefab not found. StatusUI will not be added to {team}_{pieceType}");
            }

            // 9. 프리팹 저장
            string prefabPath = $"{outputPath}/{team}_{pieceType}.prefab";
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"✓ Created prefab: {prefabPath}");
            
            // 10. Resources 폴더에도 복사
            if (copyToResources && savedPrefab != null)
            {
                string resourcesPrefabPath = $"{resourcesOutputPath}/{team}_{pieceType}.prefab";
                string resourcesDirectory = System.IO.Path.GetDirectoryName(resourcesPrefabPath);
                if (!System.IO.Directory.Exists(resourcesDirectory))
                {
                    System.IO.Directory.CreateDirectory(resourcesDirectory);
                }
                
                AssetDatabase.CopyAsset(prefabPath, resourcesPrefabPath);
                Debug.Log($"✓ Copied to Resources: {resourcesPrefabPath}");
            }
            
            DestroyImmediate(go);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create prefab for {team}_{pieceType}: {e.Message}");
            DestroyImmediate(go);
        }
    }

    private PieceDataSO LoadOrCreatePieceData(PieceType pieceType)
    {
        // 기존 PieceData 찾기
        string dataPath = $"{pieceDataPath}/{pieceType}Data.asset";
        PieceDataSO existingDataSo = AssetDatabase.LoadAssetAtPath<PieceDataSO>(dataPath);
        
        if (existingDataSo != null)
        {
            Debug.Log($"✓ Loaded existing PieceData: {dataPath}");
            return existingDataSo;
        }

        // PieceData가 없으면 새로 생성
        string directory = System.IO.Path.GetDirectoryName(dataPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        PieceDataSO newDataSo = ScriptableObject.CreateInstance<PieceDataSO>();
        
        // SerializedObject로 PieceData 필드 설정
        SerializedObject serializedData = new SerializedObject(newDataSo);
        
        SerializedProperty pieceTypeProp = serializedData.FindProperty("pieceType");
        if (pieceTypeProp != null)
        {
            pieceTypeProp.enumValueIndex = (int)pieceType;
        }

        // Movement Strategies 설정
        SerializedProperty strategiesProp = serializedData.FindProperty("movementStrategies");
        if (strategiesProp != null)
        {
            PieceMovement[] strategies = GetMovementStrategies(pieceType);
            strategiesProp.arraySize = strategies.Length;
            for (int i = 0; i < strategies.Length; i++)
            {
                strategiesProp.GetArrayElementAtIndex(i).objectReferenceValue = strategies[i];
            }
        }

        // 기본 스탯 설정
        SerializedProperty maxHealthProp = serializedData.FindProperty("maxHealth");
        if (maxHealthProp != null)
        {
            maxHealthProp.intValue = GetDefaultHealth(pieceType);
        }

        SerializedProperty attackPowerProp = serializedData.FindProperty("attackPower");
        if (attackPowerProp != null)
        {
            attackPowerProp.intValue = GetDefaultAttackPower(pieceType);
        }

        serializedData.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(newDataSo, dataPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✓ Created new PieceData: {dataPath}");
        return newDataSo;
    }

    private int GetDefaultHealth(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => 10,
            PieceType.Rook => 50,
            PieceType.Knight => 30,
            PieceType.Bishop => 30,
            PieceType.Queen => 90,
            PieceType.King => 100,
            _ => 10
        };
    }

    private int GetDefaultAttackPower(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => 10,
            PieceType.Rook => 50,
            PieceType.Knight => 30,
            PieceType.Bishop => 30,
            PieceType.Queen => 90,
            PieceType.King => 100,
            _ => 10
        };
    }

    private Sprite LoadSprite(Team team, PieceType pieceType)
    {
        // 소문자로 변환 (파일이 소문자이므로)
        string teamLower = team.ToString().ToLower();
        string pieceLower = pieceType.ToString().ToLower();
        string spriteName = $"{teamLower}_{pieceLower}";

        // 여러 경로에서 Sprite 찾기 시도
        string[] possiblePaths = new string[]
        {
            $"Assets/ExternalAssets/Pixel_Art_Chess_DevilsWorkshop_V04/chess/{spriteName}",
            $"Assets/Art/Sprites/{spriteName}",
            $"Assets/Sprites/{spriteName}",
            $"Assets/Images/{spriteName}",
            $"Assets/Art/{spriteName}",
            $"Sprites/{spriteName}",
            // CamelCase도 시도
            $"Assets/ExternalAssets/Pixel_Art_Chess_DevilsWorkshop_V04/chess/{team}_{pieceType}",
            $"Assets/Art/Sprites/{team}_{pieceType}",
        };

        foreach (string path in possiblePaths)
        {
            // .png 확장자로 시도
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}.png");
            if (sprite != null) 
            {
                Debug.Log($"✓ Loaded sprite from: {path}.png");
                return sprite;
            }

            // 확장자 없이 시도
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}");
            if (sprite != null) 
            {
                Debug.Log($"✓ Loaded sprite from: {path}");
                return sprite;
            }
        }

        // 여전히 못 찾으면 파일명 검색
        string[] guids = AssetDatabase.FindAssets(spriteName);
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (assetPath.ToLower().Contains("chess") && assetPath.EndsWith(".png"))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    Debug.Log($"✓ Loaded sprite from: {assetPath}");
                    return sprite;
                }
            }
        }

        return null;
    }

    private PieceMovement[] GetMovementStrategies(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => pawnMovement != null ? new PieceMovement[] { pawnMovement } : new PieceMovement[0],
            PieceType.Rook => rookMovement != null ? new PieceMovement[] { rookMovement } : new PieceMovement[0],
            PieceType.Knight => knightMovement != null ? new PieceMovement[] { knightMovement } : new PieceMovement[0],
            PieceType.Bishop => bishopMovement != null ? new PieceMovement[] { bishopMovement } : new PieceMovement[0],
            PieceType.Queen => (rookMovement != null && bishopMovement != null) 
                ? new PieceMovement[] { rookMovement, bishopMovement } 
                : new PieceMovement[0],
            PieceType.King => kingMovement != null ? new PieceMovement[] { kingMovement } : new PieceMovement[0],
            _ => new PieceMovement[0]
        };
    }
}
