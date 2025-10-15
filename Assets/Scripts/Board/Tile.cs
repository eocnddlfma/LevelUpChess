using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Tile : MonoBehaviour
{
    public Vector2Int coordinate;
    [SerializeField] GameObject highlightObject;
    [SerializeField] GameObject baseColorObject;

    ChessPiece _occupyingPiece;

    public ChessPiece OccupyingPiece
    {
        get
        {
            return _occupyingPiece;
        }
        set
        {
            _occupyingPiece = value;
        }
    }

    void Awake()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
    }
    public void SetColor(Color color)
    {
        if (baseColorObject != null)
        {
            baseColorObject.GetComponent<SpriteRenderer>().color = color;
        }
    }

    public void ShowHighlight(bool show)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(show);
        }
    }

    void OnMouseDown()
    {
        Debug.Log("Tile clicked: " + coordinate);
    }
}
