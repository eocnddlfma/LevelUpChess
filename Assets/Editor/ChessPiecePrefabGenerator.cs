using UnityEngine;
using UnityEditor;
using LevelUpChess.Pieces;

/// <summary>
/// 체스 피스 프리팹을 자동으로 생성하고 설정합니다
/// </summary>
public class ChessPiecePrefabGenerator : EditorWindow
{
    private Team selectedTeam = Team.White;
    private PieceType selectedPieceType = PieceType.Pawn;
    private string outputPath = "Assets/Prefabs";
    private string pieceDataPath = "Assets/ScriptableObject/PieceData";

    // Movement Strategy SO들
    private PawnMovement pawnMovement;
    private RookMovement rookMovement;
    private KnightMovement knightMovement;
    private BishopMovement bishopMovement;
    private KingMovement kingMovement;

    [MenuItem("Tools/Chess/Generate Piece Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<ChessPiecePrefabGenerator>("Piece Prefab Generator");
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

        EditorGUILayout.Space();

        // Movement Strategy SO 선택
        GUILayout.Label("Movement Strategies", EditorStyles.boldLabel);
        pawnMovement = (PawnMovement)EditorGUILayout.ObjectField("Pawn Movement", pawnMovement, typeof(PawnMovement), false);
        rookMovement = (RookMovement)EditorGUILayout.ObjectField("Rook Movement", rookMovement, typeof(RookMovement), false);
        knightMovement = (KnightMovement)EditorGUILayout.ObjectField("Knight Movement", knightMovement, typeof(KnightMovement), false);
        bishopMovement = (BishopMovement)EditorGUILayout.ObjectField("Bishop Movement", bishopMovement, typeof(BishopMovement), false);
        kingMovement = (KingMovement)EditorGUILayout.ObjectField("King Movement", kingMovement, typeof(KingMovement), false);

        EditorGUILayout.Space();

        // 출력 경로
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

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

            // 4. SpriteRenderer 추가
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();

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
                SerializedProperty pieceDataProp = serializedPiece.FindProperty("pieceData");
                if (pieceDataProp != null)
                {
                    pieceDataProp.objectReferenceValue = pieceDataSo;
                }
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

            // 8. 프리팹 저장
            string prefabPath = $"{outputPath}/{team}_{pieceType}.prefab";
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);

            Debug.Log($"✓ Created prefab: {prefabPath}");
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
