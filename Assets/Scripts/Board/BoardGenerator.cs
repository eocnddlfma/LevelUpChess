using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Events;
using LevelUpChess.Pieces;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelUpChess.Board
{
    /// <summary>
    /// 보드 생성 전용 클래스
    /// - 타일 생성 및 초기 기물 배치 담당
    /// - BoardManager와 직접 참조 없이 이벤트로 통신
    /// </summary>
    public class BoardGenerator : MonoBehaviour
    {
        [Header("Board Configuration")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float spacing = 1.1f;

        [Header("Visual Settings")]
        [SerializeField] private Color whiteColor = new Color(0.93f, 0.93f, 0.82f); // 밝은 베이지
        [SerializeField] private Color darkColor = new Color(0.45f, 0.32f, 0.22f);  // 어두운 갈색

        private Tile[,] _tiles;
        private bool _isInitialized = false;

        private void OnEnable()
        {
            Bus<BoardGenerationRequestedEvent>.OnEvent += OnBoardGenerationRequested;
            Bus<BoardClearRequestedEvent>.OnEvent += OnBoardClearRequested;
        }

        private void OnDisable()
        {
            Bus<BoardGenerationRequestedEvent>.OnEvent -= OnBoardGenerationRequested;
            Bus<BoardClearRequestedEvent>.OnEvent -= OnBoardClearRequested;
        }

        private void OnBoardGenerationRequested(BoardGenerationRequestedEvent evt)
        {
            GenerateBoard();
        }

        private void OnBoardClearRequested(BoardClearRequestedEvent evt)
        {
            ClearBoard();
        }

        /// <summary>
        /// 보드 생성 및 이벤트 발행
        /// </summary>
        public void GenerateBoard()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[BoardGenerator] Board already initialized. Use ClearBoard() first if you want to regenerate.");
                return;
            }

            Debug.Log("[BoardGenerator] ========== Generating Board ==========");
            
            if (!ValidateSetup())
                return;

            ClearPieces();
            _tiles = new Tile[width, height];

            GenerateTiles();

            // 이벤트로 BoardManager에 타일 데이터 전달
            PublishBoardGeneratedEvent();

            ApplyDefaultSetup();

            _isInitialized = true;
            Debug.Log("[BoardGenerator] Board generation completed successfully");
        }

        private bool ValidateSetup()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("[BoardGenerator] tilePrefab is not assigned in inspector!");
                return false;
            }

            if (tilePrefab.GetComponent<Tile>() == null)
            {
                Debug.LogError("[BoardGenerator] tilePrefab does not have Tile component!");
                return false;
            }

            if (width <= 0 || height <= 0)
            {
                Debug.LogError($"[BoardGenerator] Invalid board dimensions: {width}x{height}");
                return false;
            }

            return true;
        }

        private void GenerateTiles()
        {
            Debug.Log($"[BoardGenerator] Generating {width}x{height} tiles...");
            
            Vector3 centerOffset = new Vector3((width - 1) * spacing / 2f, (height - 1) * spacing / 2f, 0f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 localPos = new Vector3(x * spacing, y * spacing, 0f) - centerOffset;
                    
#if UNITY_EDITOR
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, transform);
                    go.transform.localPosition = localPos;
#else
                    GameObject go = Instantiate(tilePrefab, transform);
                    go.transform.localPosition = localPos;
#endif
                    
                    go.name = $"Tile_{x}_{y}";

                    Tile tile = go.GetComponent<Tile>();
                    if (tile == null)
                    {
                        Debug.LogError($"[BoardGenerator] Tile component missing on tilePrefab at {x},{y}");
                        return;
                    }

                    tile.coordinate = new Vector2Int(x, y);
                    bool isWhite = (x + y) % 2 == 0;
                    tile.SetColor(isWhite ? whiteColor : darkColor);

                    _tiles[x, y] = tile;
                }
            }
            
            Debug.Log("[BoardGenerator] Tiles generated successfully");
        }

        /// <summary>
        /// 이벤트로 BoardManager에 타일 데이터 전달
        /// </summary>
        private void PublishBoardGeneratedEvent()
        {
#if UNITY_EDITOR
            // Editor 모드에서는 직접 BoardManager 찾아서 초기화 (이벤트 시스템이 동작하지 않을 수 있음)
            if (!Application.isPlaying)
            {
                BoardManager editorBoardManager = FindFirstObjectByType<BoardManager>();
                if (editorBoardManager == null)
                {
                    GameObject managerGO = new GameObject("BoardManager");
                    editorBoardManager = managerGO.AddComponent<BoardManager>();
                    Debug.Log("[BoardGenerator] Created new BoardManager in scene");
                }
                
                editorBoardManager.InitializeWithTiles(_tiles, width, height);
                EditorUtility.SetDirty(editorBoardManager);
                return;
            }
#endif

            // 런타임: 이벤트로 전달
            Bus<BoardGeneratedEvent>.Raise(new BoardGeneratedEvent
            {
                Tiles = _tiles,
                Width = width,
                Height = height
            });
            
            Debug.Log("[BoardGenerator] BoardGeneratedEvent raised");
        }
        
        /// <summary>
        /// 보드 전체 초기화 (타일 포함)
        /// </summary>
        public void ClearBoard()
        {
            Debug.Log("[BoardGenerator] Clearing entire board...");
            
            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            foreach (var tile in allTiles)
            {
                DestroyObject(tile.gameObject);
            }
            
            ClearPieces();
            
            _tiles = null;
            _isInitialized = false;
            
            Debug.Log("[BoardGenerator] Board cleared");
        }

        public Tile GetTileAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) 
                return null;
            return _tiles?[x, y];
        }

        /// <summary>
        /// 기본 체스 초기 배치 적용
        /// </summary>
        public void ApplyDefaultSetup()
        {
            if (_tiles == null) 
            {
                Debug.LogError("[BoardGenerator] _tiles is null in ApplyDefaultSetup!");
                return;
            }

            Debug.Log("[BoardGenerator] Applying default chess setup...");
            
            // 폰 배치
            for (int x = 0; x < width; x++)
            {
                PieceFactory.Create(PieceType.Pawn, Team.White, _tiles[x, 1], transform);
                PieceFactory.Create(PieceType.Pawn, Team.Black, _tiles[x, 6], transform);
            }

            // 루크 배치
            PieceFactory.Create(PieceType.Rook, Team.White, _tiles[0, 0], transform);
            PieceFactory.Create(PieceType.Rook, Team.White, _tiles[7, 0], transform);
            PieceFactory.Create(PieceType.Rook, Team.Black, _tiles[0, 7], transform);
            PieceFactory.Create(PieceType.Rook, Team.Black, _tiles[7, 7], transform);

            // 나이트 배치
            PieceFactory.Create(PieceType.Knight, Team.White, _tiles[1, 0], transform);
            PieceFactory.Create(PieceType.Knight, Team.White, _tiles[6, 0], transform);
            PieceFactory.Create(PieceType.Knight, Team.Black, _tiles[1, 7], transform);
            PieceFactory.Create(PieceType.Knight, Team.Black, _tiles[6, 7], transform);

            // 비숍 배치
            PieceFactory.Create(PieceType.Bishop, Team.White, _tiles[2, 0], transform);
            PieceFactory.Create(PieceType.Bishop, Team.White, _tiles[5, 0], transform);
            PieceFactory.Create(PieceType.Bishop, Team.Black, _tiles[2, 7], transform);
            PieceFactory.Create(PieceType.Bishop, Team.Black, _tiles[5, 7], transform);

            // 퀸과 킹 배치
            PieceFactory.Create(PieceType.Queen, Team.White, _tiles[3, 0], transform);
            PieceFactory.Create(PieceType.King, Team.White, _tiles[4, 0], transform);
            PieceFactory.Create(PieceType.Queen, Team.Black, _tiles[3, 7], transform);
            PieceFactory.Create(PieceType.King, Team.Black, _tiles[4, 7], transform);

            Debug.Log("[BoardGenerator] Default setup completed");
        }

        private void ClearPieces()
        {
            if (_tiles != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Tile t = _tiles[x, y];
                        if (t?.OccupyingPiece != null)
                        {
                            DestroyObject(t.OccupyingPiece.gameObject);
                            t.OccupyingPiece = null;
                        }
                    }
                }
            }

            ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            
            foreach (var piece in allPieces)
            {
                DestroyObject(piece.gameObject);
            }
            
            if (allPieces.Length > 0)
            {
                Debug.Log($"[BoardGenerator] Cleared {allPieces.Length} pieces");
            }
        }

        private void DestroyObject(GameObject go)
        {
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }
    }
}
