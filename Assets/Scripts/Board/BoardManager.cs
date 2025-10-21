using UnityEngine;
using System.Collections.Generic;







public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    
    [SerializeField] private Tile[] _serializedTiles;
    [SerializeField] private int _width;
    [SerializeField] private int _height;

    private Tile[,] _tiles;
    
    
    private Dictionary<ChessPiece, Vector2Int> _piecePositions = new Dictionary<ChessPiece, Vector2Int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        
        if (_serializedTiles != null && _serializedTiles.Length > 0)
        {
            RestoreTilesFrom1DArray();
        }
    }

    
    
    
    private void RestoreTilesFrom1DArray()
    {
        if (_width <= 0 || _height <= 0)
        {
            Debug.LogError("[BoardManager] Invalid board dimensions!");
            return;
        }

        _tiles = new Tile[_width, _height];
        
        for (int i = 0; i < _serializedTiles.Length; i++)
        {
            if (_serializedTiles[i] != null)
            {
                int x = i % _width;
                int y = i / _width;
                _tiles[x, y] = _serializedTiles[i];
            }
        }
        
        Debug.Log($"[BoardManager] Restored {_serializedTiles.Length} tiles from serialized data ({_width}x{_height})");
    }

    
    
    
    public void Initialize(Tile[,] tiles, int width, int height)
    {
        _tiles = tiles;
        _width = width;
        _height = height;
        
        
        SaveTilesTo1DArray();
        
        Debug.Log($"[BoardManager] Initialized with tiles {width}x{height}");
    }

    
    
    
    public void SaveTilesTo1DArray()
    {
        if (_tiles == null) return;
        
        _serializedTiles = new Tile[_width * _height];
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int index = y * _width + x;
                _serializedTiles[index] = _tiles[x, y];
            }
        }
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[BoardManager] Saved {_serializedTiles.Length} tiles to serialized array");
    }

    
    
    
    public Tile GetTileAt(int x, int y)
    {
        if (_tiles == null)
        {
            Debug.LogError("[BoardManager] _tiles is null!");
            return null;
        }

        if (x < 0 || y < 0 || x >= _width || y >= _height)
        {
            Debug.LogWarning($"[BoardManager] Coordinates out of bounds: ({x}, {y}) for size {_width}x{_height}");
            return null;
        }

        return _tiles[x, y];
    }

    
    
    
    public Tile GetTileAt(Vector2Int coord)
    {
        return GetTileAt(coord.x, coord.y);
    }

    
    
    
    public void RegisterPiece(ChessPiece piece, Vector2Int position)
    {
        _piecePositions[piece] = position;
    }

    
    
    
    public void MovePiece(ChessPiece piece, Vector2Int fromPos, Vector2Int toPos)
    {
        if (_piecePositions.ContainsKey(piece))
        {
            _piecePositions[piece] = toPos;
        }
        else
        {
            Debug.LogWarning($"말 {piece.name}이(가) 위치 추적에 없습니다");
            _piecePositions[piece] = toPos;
        }
    }

    
    
    
    public void UnregisterPiece(ChessPiece piece)
    {
        _piecePositions.Remove(piece);
    }

    
    
    
    public Vector2Int GetPiecePosition(ChessPiece piece)
    {
        if (_piecePositions.TryGetValue(piece, out var pos))
        {
            return pos;
        }
        return Vector2Int.one * -1; 
    }

    
    
    
    public ChessPiece GetPieceAt(Vector2Int coord)
    {
        Tile tile = GetTileAt(coord);
        return tile != null ? tile.OccupyingPiece : null;
    }

    
    
    
    public List<ChessPiece> GetPiecesByTeam(Team team)
    {
        List<ChessPiece> pieces = new List<ChessPiece>();
        foreach (var kvp in _piecePositions)
        {
            if (kvp.Key != null && kvp.Key.team == team)
            {
                pieces.Add(kvp.Key);
            }
        }
        return pieces;
    }

    
    
    
    public int Width => _width;
    public int Height => _height;

    
    
    
    public Tile[,] Tiles => _tiles;
}
