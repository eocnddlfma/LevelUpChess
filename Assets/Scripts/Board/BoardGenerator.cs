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
        
        
        ClearBoard();
        _tiles = new Tile[width, height];

        
        transform.localPosition = Vector3.zero;

        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 localPos = new Vector3(x * spacing, y * spacing, 0f);
                
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

        
        Vector3 centerOffset = new Vector3((width - 1) * spacing / 2f, (height - 1) * spacing / 2f, 0f);
        transform.localPosition = -centerOffset;

        
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

        
        ApplyDefaultSetup();
        
        Debug.Log("[BoardGenerator] Board generation completed");
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
        if (_tiles == null) return;
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

    void SpawnPieceOnTile(PieceType pieceType, Team team, int x, int y)
    {
        Tile tile = GetTileAt(x, y);
        if (tile == null) 
        {
            Debug.LogError($"Tile not found at {x}, {y}");
            return;
        }

        
        string prefabPath = $"Assets/Prefabs/Pieces/{team}_{pieceType}.prefab";
        
        GameObject prefab = null;
        
#if UNITY_EDITOR
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[EDITOR] Cannot load prefab from: {prefabPath}");
        }
#else
        
        string resourcePath = prefabPath.Replace("Assets/Resources/", "").Replace(".prefab", "");
        prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"[RUNTIME] Cannot load prefab from: {resourcePath}");
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
            Debug.LogError($"ChessPiece component missing on {prefabPath}");
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
            Debug.LogError($"Collider2D missing on {prefabPath}! Adding BoxCollider2D...");
            collider = go.AddComponent<BoxCollider2D>();
        }
        
        Debug.Log($"✓ Spawned {team} {pieceType} at ({x}, {y}) - Has Collider: {collider != null}");

        
        piece.PlaceOnTile(tile);
    }
}
