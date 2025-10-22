using UnityEngine;
using Events;

public class InputManager : MonoBehaviour
{
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleMouseClick();
    }

    private void HandleMouseClick()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

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
