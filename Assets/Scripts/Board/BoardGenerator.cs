using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif





public class BoardGenerator : MonoBehaviour
{
    public GameObject tilePrefab; 
    public int width = 8;
    public int height = 8;
    public float spacing = 1.1f; 
    public Color whiteColor = Color.white;
    public Color greenColor = Color.green;

    private Tile[,] _tiles;

    public void GenerateBoard()
    {
        Debug.Log("[BoardGenerator] Generating board...");
        
        // 기존 피스만 제거 (타일은 유지)
        ClearPieces();
        
        // 타일이 없으면 생성
        if (_tiles == null || _tiles.Length == 0)
        {
            _tiles = new Tile[width, height];
            GenerateTiles();
            InitializeBoardManager();
        }
        
        // 피스 배치
        ApplyDefaultSetup();
        
        Debug.Log("[BoardGenerator] Board generation completed");
    }

    /// <summary>
    /// 타일 생성 (별도 메서드로 분리)
    /// </summary>
    private void GenerateTiles()
    {
        Debug.Log("[BoardGenerator] Generating tiles...");
        
        // 부모 오브젝트의 중앙을 기준으로 보드 생성 (오프셋 미리 계산)
        Vector3 centerOffset = new Vector3((width - 1) * spacing / 2f, (height - 1) * spacing / 2f, 0f);

        // 타일 생성 (중앙 기준)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 중앙 기준 위치 계산
                Vector3 localPos = new Vector3(x * spacing, y * spacing, 0f) - centerOffset;
                
#if UNITY_EDITOR
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, transform);
                go.transform.localPosition = localPos;
#else
                GameObject go = Instantiate(tilePrefab, transform);
                go.transform.localPosition = localPos;
#endif
                
                go.name = "Tile_" + x + "_" + y;

                Tile tile = go.GetComponent<Tile>();
                if (tile == null)
                {
                    Debug.LogWarning("tilePrefab does not contain Tile component");
                    continue;
                }

                tile.coordinate = new Vector2Int(x, y);

                bool isWhite = ((x + y) % 2 == 0);
                tile.SetColor(isWhite ? whiteColor : greenColor);

                _tiles[x, y] = tile;
            }
        }
        
        Debug.Log("[BoardGenerator] Tiles generated successfully");
    }

    /// <summary>
    /// BoardManager 초기화
    /// </summary>
    private void InitializeBoardManager()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            
            BoardManager boardManager = FindFirstObjectByType<BoardManager>();
            if (boardManager == null)
            {
                GameObject managerGO = new GameObject("BoardManager");
                boardManager = managerGO.AddComponent<BoardManager>();
                Debug.Log("[BoardGenerator] Created new BoardManager in scene");
            }
            
            boardManager.Initialize(_tiles, width, height);
            UnityEditor.EditorUtility.SetDirty(boardManager);
            Debug.Log("[BoardGenerator] Saved tiles to BoardManager (Editor mode)");
        }
        else
#endif
        {
            
            if (BoardManager.Instance != null)
            {
                Debug.Log("[BoardGenerator] Initializing BoardManager with tiles");
                BoardManager.Instance.Initialize(_tiles, width, height);
            }
            else
            {
                Debug.LogError("[BoardGenerator] BoardManager.Instance is null!");
            }
        }
    }
    
    public void ClearBoard()
    {
        
        ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        
        foreach (var piece in allPieces)
        {
            if (Application.isPlaying)
            {
                Destroy(piece.gameObject);
            }
            else
            {
                DestroyImmediate(piece.gameObject);
            }
        }

        
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        
        foreach (var tile in allTiles)
        {
            if (Application.isPlaying)
            {
                Destroy(tile.gameObject);
            }
            else
            {
                DestroyImmediate(tile.gameObject);
            }
        }

        Debug.Log($"[BoardGenerator] Cleared {allPieces.Length} pieces and {allTiles.Length} tiles from board");
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return _tiles[x, y];
    }

    
    public void ApplyDefaultSetup()
    {
        if (_tiles == null) 
        {
            Debug.LogError("_tiles is null in ApplyDefaultSetup!");
            return;
        }

        Debug.Log("ApplyDefaultSetup started");
        ClearPieces();

        
        for (int x = 0; x < width; x++)
        {
            SpawnPieceOnTile(PieceType.Pawn, Team.White, x, 1);
            SpawnPieceOnTile(PieceType.Pawn, Team.Black, x, 6);
        }

        
        SpawnPieceOnTile(PieceType.Rook, Team.White, 0, 0);
        SpawnPieceOnTile(PieceType.Rook, Team.White, 7, 0);
        SpawnPieceOnTile(PieceType.Rook, Team.Black, 0, 7);
        SpawnPieceOnTile(PieceType.Rook, Team.Black, 7, 7);

        
        SpawnPieceOnTile(PieceType.Knight, Team.White, 1, 0);
        SpawnPieceOnTile(PieceType.Knight, Team.White, 6, 0);
        SpawnPieceOnTile(PieceType.Knight, Team.Black, 1, 7);
        SpawnPieceOnTile(PieceType.Knight, Team.Black, 6, 7);

        
        SpawnPieceOnTile(PieceType.Bishop, Team.White, 2, 0);
        SpawnPieceOnTile(PieceType.Bishop, Team.White, 5, 0);
        SpawnPieceOnTile(PieceType.Bishop, Team.Black, 2, 7);
        SpawnPieceOnTile(PieceType.Bishop, Team.Black, 5, 7);

        
        SpawnPieceOnTile(PieceType.Queen, Team.White, 3, 0);
        SpawnPieceOnTile(PieceType.King, Team.White, 4, 0);
        SpawnPieceOnTile(PieceType.Queen, Team.Black, 3, 7);
        SpawnPieceOnTile(PieceType.King, Team.Black, 4, 7);

        Debug.Log("ApplyDefaultSetup finished");
    }

    void ClearPieces()
    {
        // 방법 1: Tile의 참조로 제거
        if (_tiles != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile t = _tiles[x, y];
                    if (t == null) continue;
                    if (t.OccupyingPiece != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(t.OccupyingPiece.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(t.OccupyingPiece.gameObject);
                        }

                        t.OccupyingPiece = null;
                    }
                }
            }
        }

        // 방법 2: 씬의 모든 ChessPiece 제거 (이전 로드의 고아 피스도 제거)
        ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        Debug.Log($"[BoardGenerator] Found {allPieces.Length} orphaned pieces, clearing them...");
        
        foreach (var piece in allPieces)
        {
            if (Application.isPlaying)
            {
                Destroy(piece.gameObject);
            }
            else
            {
                DestroyImmediate(piece.gameObject);
            }
        }
    }

    void SpawnPieceOnTile(PieceType pieceType, Team team, int x, int y)
    {
        Tile tile = GetTileAt(x, y);
        if (tile == null) 
        {
            Debug.LogError($"Tile not found at {x}, {y}");
            return;
        }

        GameObject prefab = null;
        
#if UNITY_EDITOR
        string editorPrefabPath = $"Assets/Prefabs/Pieces/{team}_{pieceType}.prefab";
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[EDITOR] Cannot load prefab from: {editorPrefabPath}");
        }
#else
        // 빌드본: Resources 폴더에서 로드
        string resourcePath = $"Prefabs/Pieces/{team}_{pieceType}";
        prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"[RUNTIME] Cannot load prefab from Resources: {resourcePath}");
            Debug.LogError($"[RUNTIME] Make sure prefabs are in Assets/Resources/Prefabs/Pieces/ folder");
        }
#endif

        if (prefab == null)
        {
            return;
        }

        
        GameObject go = Instantiate(prefab, tile.transform.position, Quaternion.identity, transform);
        go.name = $"{team}_{pieceType}";

        ChessPiece piece = go.GetComponent<ChessPiece>();
        if (piece == null)
        {
            Debug.LogError($"ChessPiece component missing on {team}_{pieceType}");
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
            return;
        }

        
        Collider2D collider = go.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError($"Collider2D missing on {team}_{pieceType}! Adding BoxCollider2D...");
            collider = go.AddComponent<BoxCollider2D>();
        }
        
        Debug.Log($"✓ Spawned {team} {pieceType} at ({x}, {y}) - Has Collider: {collider != null}");

        
        piece.PlaceOnTile(tile);
    }
}
