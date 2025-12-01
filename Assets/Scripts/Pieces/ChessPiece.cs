using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Managers;
using LevelUpChess.Interactables;

namespace LevelUpChess.Pieces
{
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

    /// <summary>
    /// 체스 기물 컴포넌트
    /// - 기물 데이터는 PieceData ScriptableObject에서 관리
    /// - 체력 로직은 PieceHealth 컴포넌트에서 처리 (선택적)
    /// - 이 클래스는 기물의 상태와 이동을 담당
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ChessPiece : Interactable, IClickable
    {
        [Header("기물 설정")]
        [SerializeField] private PieceDataSO pieceDataSo;
        [SerializeField] private Team _team = Team.White;
        
        [Header("런타임 상태 (자동 설정)")]
        [SerializeField] private int _currentHealth;
        [SerializeField] private bool _hasMoved = false;
        
        [HideInInspector]
        [SerializeField] private Tile _currentTile;

        private SpriteRenderer _spriteRenderer;
        private Tween _currentMoveTween;
        private PieceHealth _health;

        // ========== Public 프로퍼티 ==========
        public PieceDataSO DataSo => pieceDataSo;
        public Team Team => _team;
        public PieceType PieceType => pieceDataSo?.PieceType ?? PieceType.Pawn;
        public int MaxHealth => pieceDataSo?.MaxHealth ?? 1;
        public int CurrentHealth => _currentHealth;
        public int AttackPower => pieceDataSo?.AttackPower ?? 1;
        public float MoveDuration => pieceDataSo?.MoveDuration ?? 0.1f;
        public PieceMovement[] MovementStrategies => pieceDataSo?.MovementStrategies ?? new PieceMovement[0];
        public bool HasMoved { get => _hasMoved; set => _hasMoved = value; }
        public Tile CurrentTile => _currentTile;
        public bool IsMoved => _hasMoved;
        public PieceHealth Health => _health;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<PieceHealth>();
            
            // PieceData가 있으면 초기화
            if (pieceDataSo != null)
            {
                _currentHealth = pieceDataSo.MaxHealth;
            }
            
            // Health 컴포넌트 초기화
            if (_health != null)
            {
                _health.Initialize(this, pieceDataSo?.MaxHealth ?? 1);
            }
        }

        /// <summary>
        /// 기물을 특정 타일에 즉시 배치
        /// </summary>
        public void PlaceOnTile(Tile tile)
        {
            var previous = _currentTile;
            if (_currentTile != null)
            {
                _currentTile.occupyingPiece = null;
            }

            _currentTile = tile;
            if (tile != null)
            {
                tile.OccupyingPiece = this;
                Vector3 target = tile.transform.position;
                transform.position = new Vector3(target.x, target.y, target.z);

                var boardManager = ServiceLocator.Get<BoardManager>();
                if (boardManager != null)
                {
                    boardManager.RegisterPiece(this, tile.coordinate);
                }
            }

            if (previous != null && tile != null && previous != tile)
                _hasMoved = true;
        }

        /// <summary>
        /// 기물을 특정 타일로 애니메이션과 함께 이동
        /// </summary>
        public Tween MoveToTile(Tile tile, float duration = -1f)
        {
            if (duration < 0)
                duration = pieceDataSo?.MoveDuration ?? 0.1f;

            // 이전 트윈이 있으면 정리
            if (_currentMoveTween != null && _currentMoveTween.IsActive())
            {
                _currentMoveTween.Kill();
            }

            var previous = _currentTile;
            if (_currentTile != null)
            {
                _currentTile.occupyingPiece = null;
            }

            _currentTile = tile;
            if (tile != null)
            {
                tile.OccupyingPiece = this;

                var boardManager = ServiceLocator.Get<BoardManager>();
                if (boardManager != null)
                {
                    boardManager.RegisterPiece(this, tile.coordinate);
                }
            }

            Vector3 targetPos = tile.transform.position;
            targetPos.z = transform.position.z;

            if (previous != null && tile != null && previous != tile)
                _hasMoved = true;

            _currentMoveTween = transform.DOMove(targetPos, duration).SetEase(Ease.InOutQuad);
            return _currentMoveTween;
        }

        /// <summary>
        /// 대미지를 받음 (PieceHealth가 있으면 위임, 없으면 직접 처리)
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_health != null)
            {
                _health.TakeDamage(amount);
            }
            else
            {
                _currentHealth -= amount;
                if (_currentHealth <= 0)
                {
                    Die();
                }
            }
        }

        /// <summary>
        /// 기물 사망 처리
        /// </summary>
        public void Die()
        {
            // 진행 중인 트윈 정리
            if (_currentMoveTween != null && _currentMoveTween.IsActive())
            {
                _currentMoveTween.Kill();
            }

            if (_currentTile != null)
            {
                _currentTile.OccupyingPiece = null;
            }

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager != null)
            {
                boardManager.UnregisterPiece(this);
            }

            // King 사망 시 게임 오버
            if (PieceType == PieceType.King)
            {
                Team winnerTeam = _team == Team.White ? Team.Black : Team.White;
                
                var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
                if (networkGameManager != null)
                {
                    networkGameManager.SetGameOverServerRpc(winnerTeam);
                }
                else
                {
                    Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = winnerTeam });
                }
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 현재 피스의 이동 가능한 위치를 반환
        /// </summary>
        public List<Move> GetAvailableMoves()
        {
            var allMoves = new List<Move>();
            
            Debug.Log($"[ChessPiece.GetAvailableMoves] {name} - CurrentTile: {(_currentTile != null ? _currentTile.coordinate.ToString() : "NULL")}, " +
                      $"PieceDataSo: {(pieceDataSo != null ? pieceDataSo.name : "NULL")}");
            
            var strategies = pieceDataSo?.MovementStrategies;

            if (strategies == null || strategies.Length == 0)
            {
                Debug.LogWarning($"[ChessPiece.GetAvailableMoves] {name} - No movement strategies!");
                return allMoves;
            }
            
            Debug.Log($"[ChessPiece.GetAvailableMoves] {name} - Strategies count: {strategies.Length}");

            foreach (var strategy in strategies)
            {
                if (strategy == null) 
                {
                    Debug.LogWarning($"[ChessPiece.GetAvailableMoves] {name} - Strategy is null!");
                    continue;
                }
                var moves = strategy.GetAvailableMoves(this);
                Debug.Log($"[ChessPiece.GetAvailableMoves] {name} - {strategy.GetType().Name} returned {moves.Count} moves");
                allMoves.AddRange(moves);
            }

            return allMoves;
        }
        
        public void SetPieceData(PieceDataSO dataSo, Team team)
        {
            pieceDataSo = dataSo;
            _team = team;
            _currentHealth = dataSo.MaxHealth;
            
            if (_health != null)
            {
                _health.Initialize(this, dataSo.MaxHealth);
            }
        }
    }
}
