using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Core;
using LevelUpChess.Events;
using LevelUpChess.Pieces;

namespace LevelUpChess.Board
{
    public class BoardManager : MonoBehaviour
    {
        [SerializeField] private Tile[] _serializedTiles;
        [SerializeField] private int _width;
        [SerializeField] private int _height;

        private Tile[,] _tiles;

        private void Awake()
        {
            if (ServiceLocator.Has<BoardManager>())
            {
                Destroy(gameObject);
                return;
            }

            ServiceLocator.Register(this);
            
            if (_serializedTiles != null && _serializedTiles.Length > 0)
            {
                RestoreTilesFrom1DArray();
            }
        }

        private void OnEnable()
        {
            Bus<BoardGeneratedEvent>.OnEvent += OnBoardGenerated;
        }

        private void OnDisable()
        {
            Bus<BoardGeneratedEvent>.OnEvent -= OnBoardGenerated;
        }

        /// <summary>
        /// BoardGenerator로부터 보드 생성 완료 이벤트 수신
        /// </summary>
        private void OnBoardGenerated(BoardGeneratedEvent evt)
        {
            InitializeWithTiles(evt.Tiles, evt.Width, evt.Height);
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

        // ========== 공개 보드 생성 메서드 ==========

        /// <summary>
        /// 새 보드 생성 요청 (이벤트 발행)
        /// </summary>
        public void RequestNewBoard()
        {
            Debug.Log("[BoardManager] Requesting new board generation...");
            Bus<BoardGenerationRequestedEvent>.Raise(new BoardGenerationRequestedEvent());
        }

        /// <summary>
        /// 보드 초기화 (이벤트 또는 Editor에서 호출)
        /// </summary>
        public void InitializeWithTiles(Tile[,] tiles, int width, int height)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            
            SaveTilesTo1DArray();
            
            Debug.Log($"[BoardManager] Initialized with tiles {width}x{height}");
        }

    // ========== 공개 쿼리 메서드 ==========

    /// <summary>
    /// 특정 좌표의 타일 반환
    /// </summary>
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

    /// <summary>
    /// 특정 팀의 모든 피스 반환
    /// </summary>
    public Tile GetTileAt(Vector2Int coord)
    {
        return GetTileAt(coord.x, coord.y);
    }

    // ========== 공개 위치 조회 메서드 ==========

    /// <summary>
    /// 특정 좌표의 피스 반환 (없으면 null)
    /// </summary>
    public ChessPiece GetPieceAt(Vector2Int coord)
    {
        Tile tile = GetTileAt(coord);
        return tile?.OccupyingPiece;
    }

    /// <summary>
    /// 특정 팀의 모든 피스 반환
    /// </summary>
    public List<ChessPiece> GetPiecesByTeam(Team team)
    {
        List<ChessPiece> pieces = new List<ChessPiece>();
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var piece = _tiles[x, y]?.OccupyingPiece;
                if (piece != null && piece.Team == team)
                {
                    pieces.Add(piece);
                }
            }
        }
        
        return pieces;
    }

    // ========== 공개 프로퍼티 ==========

    public int Width => _width;
    public int Height => _height;
    public Tile[,] Tiles => _tiles;

    // ========== 내부 직렬화 헬퍼 ==========

    /// <summary>
    /// 2D 배열을 1D 배열로 변환 (Editor 저장용)
    /// </summary>
    private void SaveTilesTo1DArray()
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

    private void OnDestroy()
    {
        if (ServiceLocator.Get<BoardManager>() == this)
            ServiceLocator.Unregister<BoardManager>();
    }
    }
}
