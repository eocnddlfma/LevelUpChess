using UnityEngine;
using LevelUpChess.Board;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 체스 기물 생성 팩토리
    /// - 기물 프리팹 로드 및 인스턴스화 담당
    /// - Editor/Runtime 환경에 따른 프리팹 로드 처리
    /// </summary>
    public static class PieceFactory
    {
        private const string EDITOR_PREFAB_PATH = "Assets/Prefabs/Pieces/{0}_{1}.prefab";
        private const string RUNTIME_PREFAB_PATH = "Prefabs/Pieces/{0}_{1}";

        /// <summary>
        /// 지정된 타일에 기물 생성
        /// </summary>
        /// <param name="pieceType">기물 타입</param>
        /// <param name="team">팀</param>
        /// <param name="tile">배치할 타일</param>
        /// <param name="parent">부모 Transform (선택)</param>
        /// <returns>생성된 ChessPiece, 실패 시 null</returns>
        public static ChessPiece Create(PieceType pieceType, Team team, Tile tile, Transform parent = null)
        {
            if (tile == null)
            {
                Debug.LogError("[PieceFactory] Cannot create piece: tile is null");
                return null;
            }

            GameObject prefab = LoadPrefab(pieceType, team);
            if (prefab == null)
                return null;

            GameObject instance;
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // 에디터 모드: PrefabUtility 사용하여 프리팹 연결 유지
                instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.position = tile.transform.position;
                instance.transform.rotation = Quaternion.identity;
            }
            else
#endif
            {
                // 런타임: Instantiate 사용
                instance = Object.Instantiate(prefab, tile.transform.position, Quaternion.identity, parent);
            }
            
            instance.name = $"{team}_{pieceType}";

            ChessPiece piece = instance.GetComponent<ChessPiece>();
            if (piece == null)
            {
                Debug.LogError($"[PieceFactory] ChessPiece component missing on {team}_{pieceType}");
                DestroyObject(instance);
                return null;
            }

            // Collider 검증 및 추가
            if (instance.GetComponent<Collider2D>() == null)
            {
                Debug.LogWarning($"[PieceFactory] Collider2D missing on {team}_{pieceType}. Adding BoxCollider2D...");
                instance.AddComponent<BoxCollider2D>();
            }

            piece.PlaceOnTile(tile);
            
            Debug.Log($"[PieceFactory] ✓ Created {team} {pieceType} at {tile.coordinate}");
            return piece;
        }

        public static ChessPiece Create(PieceType pieceType, Team team, int x, int y, Transform parent = null)
        {
            var boardManager = Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogError("[PieceFactory] BoardManager not found");
                return null;
            }

            Tile tile = boardManager.GetTileAt(x, y);
            if (tile == null)
            {
                Debug.LogError($"[PieceFactory] Tile not found at ({x}, {y})");
                return null;
            }

            return Create(pieceType, team, tile, parent);
        }

        private static GameObject LoadPrefab(PieceType pieceType, Team team)
        {
            GameObject prefab = null;

#if UNITY_EDITOR
            // 에디터에서는 항상 AssetDatabase로 로드 (플레이 모드 포함)
            string editorPath = string.Format(EDITOR_PREFAB_PATH, team, pieceType);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorPath);
            if (prefab != null)
            {
                return prefab;
            }
            Debug.LogWarning($"[PieceFactory] Prefab not found at editor path: {editorPath}, trying Resources...");
#endif

            // 빌드된 게임에서는 Resources로 로드
            string resourcePath = string.Format(RUNTIME_PREFAB_PATH, team, pieceType);
            prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[PieceFactory] Cannot load prefab: {resourcePath}");
                Debug.LogError("[PieceFactory] Ensure prefabs are in Assets/Resources/Prefabs/Pieces/");
            }

            return prefab;
        }

        private static void DestroyObject(GameObject go)
        {
            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }
    }
}
