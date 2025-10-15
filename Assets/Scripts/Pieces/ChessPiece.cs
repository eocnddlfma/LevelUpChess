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
    public int maxHealth = 10;
    public int currentHealth = 10;
    public int attackPower = 3;

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
        if (currentTile != null)
        {
            currentTile.OccupyingPiece = null;
        }

        currentTile = tile;
        if (tile != null)
        {
            tile.OccupyingPiece = this;
            // 2D 공간에서는 tile의 position.x,y를 사용하고 약간 위로 띄우려면 z를 변경하지 않음
            Vector3 target = tile.transform.position;
            // 만약 SpriteRenderer가 있으면 sorting과 Y offset을 고려해서 위치 보정
            if (_spriteRenderer != null)
            {
                transform.position = new Vector3(target.x, target.y, target.z);
            }
            else
            {
                transform.position = new Vector3(target.x, target.y, target.z);
            }
        }
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
