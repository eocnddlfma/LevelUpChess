using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    White,
    Black
}

public enum PieceType
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}

[RequireComponent(typeof(Collider2D))]
public class ChessPiece : Interactable
{
    public Team team = Team.White;
    public PieceType pieceType = PieceType.Pawn;
    public PieceMovement[] movementStrategies = new PieceMovement[0];
    
    public int maxHealth = 1;
    public int currentHealth = 1;
    public int attackPower = 1;

    // true if this piece has moved at least once (used for pawn initial two-square move, castling, etc.)
    public bool HasMoved = false;

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
            currentTile.occupyingPiece = null;
        }

        currentTile = tile;
        if (tile != null)
        {
            tile.OccupyingPiece = this;
            Vector3 target = tile.transform.position;
            transform.position = new Vector3(target.x, target.y, target.z);

            // BoardManager에 말 위치 등록
            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.RegisterPiece(this, tile.coordinate);
            }
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

    public void Die()
    {
        if (currentTile != null)
        {
            currentTile.OccupyingPiece = null;
        }

        // BoardManager에서 말 등록 해제
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.UnregisterPiece(this);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 현재 피스의 이동 가능한 위치를 반환합니다
    /// 모든 movement strategy의 이동을 조합합니다
    /// </summary>
    public List<Move> GetAvailableMoves()
    {
        var allMoves = new List<Move>();

        if (movementStrategies == null || movementStrategies.Length == 0)
            return allMoves;

        // 모든 movement strategy의 이동을 수집
        foreach (var strategy in movementStrategies)
        {
            if (strategy == null) continue;
            var moves = strategy.GetAvailableMoves(this);
            allMoves.AddRange(moves);
        }

        return allMoves;
    }
}
