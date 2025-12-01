using UnityEngine;
using LevelUpChess.Events;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Interactables;

namespace LevelUpChess.Managers
{
    public class InputManager : MonoBehaviour
    {
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
            Debug.Log($"[InputManager] Awake - Camera: {(_camera != null ? _camera.name : "null")}");
        }

        private void Start()
        {
            // Awake에서 카메라를 못 찾은 경우 Start에서 다시 시도
            if (_camera == null)
            {
                _camera = Camera.main;
                Debug.Log($"[InputManager] Start - Camera: {(_camera != null ? _camera.name : "null")}");
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleMouseClick();
        }

        private void HandleMouseClick()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogWarning("[InputManager] No main camera found");
                    return;
                }
            }
            
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f; // 2D에서는 z=0 설정 필요
            
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            Debug.Log($"[InputManager] Click at {worldPos}, hits: {hits.Length}");

            // 디버그: 모든 hit된 오브젝트 출력
            foreach (var hit in hits)
            {
                Debug.Log($"[InputManager] Hit: {hit.collider.gameObject.name}, " +
                          $"HasChessPiece: {hit.collider.GetComponent<ChessPiece>() != null}, " +
                          $"HasTile: {hit.collider.GetComponent<Tile>() != null}");
            }

            foreach (var hit in hits)
            {
                ChessPiece piece = hit.collider.GetComponent<ChessPiece>();
                if (piece != null)
                {
                    Bus<ClickableSelectedEvent>.Raise(new ClickableSelectedEvent { Clickable = piece });
                    return;
                }
            }

            foreach (var hit in hits)
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    Bus<ClickableSelectedEvent>.Raise(new ClickableSelectedEvent { Clickable = tile });
                    return;
                }
            }
        }
    }
}
