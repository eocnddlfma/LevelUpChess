using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Tile : MonoBehaviour
{
    public Vector2Int coordinate;
    public GameObject highlightObject;
    public GameObject baseColorObject;

    // 한 개의 피스만 가질 수 있음
    private ChessPiece _occupyingPiece;

    public ChessPiece OccupyingPiece
    {
        get => _occupyingPiece;
        set => _occupyingPiece = value;
    }

    private SpriteRenderer _spriteRenderer;
    private Renderer _renderer;

    private void Awake()
    {
        // baseColorObject로부터 SpriteRenderer 또는 일반 Renderer를 가져옵니다.
        if (baseColorObject != null)
        {
            _spriteRenderer = baseColorObject.GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _renderer = baseColorObject.GetComponent<Renderer>();
        }
        else
        {
            // baseColorObject가 없으면 타일 자체의 렌더러를 시도
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _renderer = GetComponent<Renderer>();
        }

        // highlightObject가 없으면 그냥 무시. 있으면 기본으로 비활성화
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    public void SetColor(Color color)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
            return;
        }

        if (_renderer != null)
        {
            _renderer.material.color = color;
            return;
        }

        Debug.LogWarning($"Tile '{name}': No renderer found to set color. Assign baseColorObject with a SpriteRenderer or Renderer.");
    }

    public void ShowHighlight(bool show)
    {
        if (highlightObject != null)
            highlightObject.SetActive(show);
    }

    private void OnMouseDown()
    {
        // 디버그: 어떤 타일인지 확인
        Debug.Log($"Tile clicked: {coordinate}");
    }
}
