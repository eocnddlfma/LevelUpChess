using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Tile : Clickable
{
    public Vector2Int coordinate;
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private GameObject baseColorObject;
    [SerializeField] private GameObject moveableIndicator;
    [SerializeField] private GameObject attackableIndicator;
    public ChessPiece occupyingPiece;

    public ChessPiece OccupyingPiece
    {
        get { return occupyingPiece; }
        set { occupyingPiece = value; }
    }

    void Awake()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }

        if (moveableIndicator != null)
        {
            moveableIndicator.SetActive(false);
        }

        if (attackableIndicator != null)
        {
            attackableIndicator.SetActive(false);
        }
    }
    public void SetColor(Color color)
    {
        if (baseColorObject != null)
        {
            baseColorObject.GetComponent<SpriteRenderer>().color = color;
        }
    }

    public void SetHighlight(bool show)
    {
        Debug.Log($"[Tile.SetHighlight] Coordinate: {coordinate}, Show: {show}, highlightObject: {highlightObject}");
        if (highlightObject != null)
        {
            highlightObject.SetActive(show);
        }
        else
        {
            Debug.LogWarning($"[Tile] highlightObject is null on tile {coordinate}");
        }
    }

    public void SetMoveable(bool show)
    {
        if (moveableIndicator != null)
        {
            moveableIndicator.SetActive(show);
        }
    }

    public void SetAttackable(bool show)
    {
        if (attackableIndicator != null)
        {
            attackableIndicator.SetActive(show);
        }
    }

    public void ClearIndicators()
    {
        SetHighlight(false);
        SetMoveable(false);
        SetAttackable(false);
    }
}
