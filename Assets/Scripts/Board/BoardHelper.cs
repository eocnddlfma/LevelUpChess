using System.Collections.Generic;
using UnityEngine;

public class BoardHelper : MonoBehaviour
{
    private static BoardHelper _instance;
    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();

    public static BoardHelper Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        CacheTiles();
    }

    private void CacheTiles()
    {
        _tiles.Clear();
        var all = FindObjectsOfType<Tile>();
        foreach (var t in all)
        {
            _tiles[t.coordinate] = t;
        }
    }

    public static Tile GetTileAt(Vector2Int coord)
    {
        if (_instance == null) return null;
        _instance._tiles.TryGetValue(coord, out var t);
        return t;
    }
}
