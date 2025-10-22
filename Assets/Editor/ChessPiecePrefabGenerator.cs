using UnityEngine;
using UnityEditor;

/// <summary>
/// 체스 피스 프리팹을 자동으로 생성하고 설정합니다
/// </summary>
public class ChessPiecePrefabGenerator : EditorWindow
{
    private Team selectedTeam = Team.White;
    private PieceType selectedPieceType = PieceType.Pawn;
    private string outputPath = "Assets/Prefabs";

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

            // 5. Team과 PieceType 설정
            piece.team = team;
            piece.pieceType = pieceType;

            // 6. Sprite 로드 및 설정
            Sprite sprite = LoadSprite(team, pieceType);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                Debug.Log($"ℹ Sprite not found for {team}_{pieceType}. You can add sprites manually later.");
            }

            // 7. Movement Strategy 배열 설정
            PieceMovement[] strategies = GetMovementStrategies(pieceType);
            if (strategies != null && strategies.Length > 0 && strategies[0] != null)
            {
                piece.movementStrategies = strategies;
            }
            else
            {
                Debug.LogWarning($"Movement strategies not found for {pieceType}. Please assign them in the window.");
                piece.movementStrategies = new PieceMovement[0];
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
