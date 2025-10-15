using UnityEngine;

public enum Team
{
    White,
    Black
}

[RequireComponent(typeof(Collider2D))]
public class ChessPiece : MonoBehaviour
{
    public Team team = Team.White;
    public int maxHealth = 1;
    public int currentHealth = 1;
    public int attackPower = 1;

    // true if this piece has moved at least once (used for pawn initial two-square move, castling, etc.)
    public bool HasMoved = false;

    // 이동/공격 가능한 좌표는 게임로직에서 계산하여 Tile.ShowHighlight로 표시할 예정

    [HideInInspector]
    public Tile currentTile;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlaceOnTile(Tile tile)
    {
        var previous = currentTile;
        if (currentTile != null)
        {
            currentTile.OccupyingPiece = null;
        }

        currentTile = tile;
        if (tile != null)
        {
            tile.OccupyingPiece = this;
            Vector3 target = tile.transform.position;
            transform.position = new Vector3(target.x, target.y, target.z);
        }

        // If we moved from a previous tile to a new tile, mark as moved.
        if (previous != null && tile != null && previous != tile)
            HasMoved = true;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (currentTile != null)
            currentTile.OccupyingPiece = null;

        Destroy(gameObject);
    }
}
