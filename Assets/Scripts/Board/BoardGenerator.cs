using UnityEngine;

/// <summary>
/// 간단한 8x8 보드를 에디터/런타임에서 생성합니다.
/// 사용법: 빈 GameObject에 붙이고 `tilePrefab`에 타일 프리팹을 넣어 실행하면 자동 생성됩니다.
/// </summary>
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
        // 기존 보드가 있으면 삭제
        ClearBoard();
        _tiles = new Tile[width, height];

        // reset local position so placement is deterministic
        transform.localPosition = Vector3.zero;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 localPos = new Vector3(x * spacing, y * spacing, 0f);
                GameObject go = Instantiate(tilePrefab, transform);
                go.name = "Tile_" + x + "_" + y;
                go.transform.localPosition = localPos;

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

        // center board locally
        Vector3 centerOffset = new Vector3((width - 1) * spacing / 2f, (height - 1) * spacing / 2f, 0f);
        transform.localPosition = -centerOffset;
    }

    /// <summary>
    /// 에디터나 런타임에서 호출 가능한 보드 제거 유틸.
    /// </summary>
    public void ClearBoard()
    {
        // DestroyImmediate는 에디터에서 즉시 제거에 사용되고,
        // 런타임에서는 일반 Destroy를 사용합니다.
        if (!Application.isPlaying)
        {
            // 수동으로 자식들을 지움
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }
        else
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                Destroy(child.gameObject);
            }
        }
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return _tiles[x, y];
    }
}
