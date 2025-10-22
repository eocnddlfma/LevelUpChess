using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Events;

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
    public float moveDuration = 0.1f;

    public bool HasMoved = false;

    [HideInInspector]
    public Tile currentTile;

    private SpriteRenderer _spriteRenderer;
    private Tween _currentMoveTween;  // 진행 중인 이동 트윈 추적

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

            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.RegisterPiece(this, tile.coordinate);
            }
        }

        if (previous != null && tile != null && previous != tile)
            HasMoved = true;
    }

    public Tween MoveToTile(Tile tile, float duration = -1f)
    {
        if (duration < 0)
            duration = moveDuration;

        // 이전 트윈이 있으면 정리
        if (_currentMoveTween != null && _currentMoveTween.IsActive())
        {
            _currentMoveTween.Kill();
        }

        var previous = currentTile;
        if (currentTile != null)
        {
            currentTile.occupyingPiece = null;
        }

        currentTile = tile;
        if (tile != null)
        {
            tile.OccupyingPiece = this;

            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.RegisterPiece(this, tile.coordinate);
            }
        }

        Vector3 targetPos = tile.transform.position;
        targetPos.z = transform.position.z;

        if (previous != null && tile != null && previous != tile)
            HasMoved = true;

        _currentMoveTween = transform.DOMove(targetPos, duration).SetEase(Ease.InOutQuad);
        return _currentMoveTween;
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
        // 진행 중인 트윈 정리
        if (_currentMoveTween != null && _currentMoveTween.IsActive())
        {
            _currentMoveTween.Kill();
        }

        if (currentTile != null)
        {
            currentTile.OccupyingPiece = null;
        }

        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.UnregisterPiece(this);
        }

        if (pieceType == PieceType.King)
        {
            Team winnerTeam = team == Team.White ? Team.Black : Team.White;
            Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = winnerTeam });
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
