using System.Collections.Generic;
using UnityEngine;
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
    /// - 전투 로직은 PieceCombat 컴포넌트에서 처리
    /// - 애니메이션은 PieceAnimator 컴포넌트에서 처리
    /// - 이 클래스는 기물의 기본 상태와 이동을 담당
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ChessPiece : Interactable, IClickable
    {
        [Header("기물 설정")]
        [SerializeField] private PieceDataSO pieceDataSo;
        [SerializeField] private Team _team = Team.White;
        
        [SerializeField] private bool _hasMoved = false;
        
        [HideInInspector]
        [SerializeField] private Tile _currentTile;

        private SpriteRenderer _spriteRenderer;
        private PieceAnimator _animator;
        private PieceCombat _combat;
        private PieceUI _ui;

        // ========== Public 프로퍼티 ==========
        public PieceDataSO DataSo => pieceDataSo;
        public Team Team => _team;
        public PieceType PieceType => pieceDataSo?.PieceType ?? PieceType.Pawn;
        public int PieceValue => pieceDataSo?.PieceValue ?? 1;
        public float MoveDuration => pieceDataSo?.MoveDuration ?? 0.1f;
        public PieceMovement[] MovementStrategies => pieceDataSo?.MovementStrategies ?? new PieceMovement[0];
        public bool HasMoved { get => _hasMoved; set => _hasMoved = value; }
        public Tile CurrentTile => _currentTile;
        public bool IsMoved => _hasMoved;
        
        // 컴포넌트 참조
        public PieceCombat Combat => _combat;
        public PieceAnimator Animator => _animator;
        public PieceUI UI => _ui;
        
        // Combat 위임 프로퍼티 (편의용)
        public int MaxHealth => _combat?.MaxHealth ?? 1;
        public int CurrentHealth => _combat?.CurrentHealth ?? 1;
        public int AttackPower => _combat?.AttackPower ?? 1;
        public int Level => _combat?.Level ?? 1;
        public int CurrentExp => _combat?.CurrentExp ?? 0;
        public int ExpToNextLevel => _combat?.ExpToNextLevel ?? 1;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<PieceAnimator>();
            _combat = GetComponent<PieceCombat>();
            _ui = GetComponent<PieceUI>();
            
            // PieceAnimator가 없으면 추가
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<PieceAnimator>();
            }
            
            // PieceUI가 없으면 추가
            if (_ui == null)
            {
                _ui = gameObject.AddComponent<PieceUI>();
            }
            
            // PieceCombat이 없으면 추가
            if (_combat == null)
            {
                _combat = gameObject.AddComponent<PieceCombat>();
            }
            
            // Combat 초기화
            InitializeCombat();
        }
        
        private void InitializeCombat()
        {
            if (pieceDataSo != null && _combat != null)
            {
                _combat.Initialize(this, pieceDataSo.MaxHealth, pieceDataSo.AttackPower);
            }
            else if (_combat != null)
            {
                _combat.Initialize(this, 1, 1);
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
        /// 타일 정보만 업데이트 (위치 이동 없이, 공격 애니메이션 후 사용)
        /// </summary>
        public void SetCurrentTile(Tile tile)
        {
            UpdateTileInfo(tile);
        }
        
        /// <summary>
        /// 타일 정보 내부 설정 (위치/플래그 업데이트 없이)
        /// </summary>
        internal void SetCurrentTileInternal(Tile tile)
        {
            _currentTile = tile;
        }
        
        /// <summary>
        /// 타일 정보 업데이트 (이전 타일 정리, BoardManager 등록 포함)
        /// </summary>
        public void UpdateTileInfo(Tile newTile)
        {
            var previousTile = _currentTile;
            
            // 이전 타일에서 제거
            if (previousTile != null)
            {
                previousTile.OccupyingPiece = null;
            }
            
            _currentTile = newTile;
            
            if (newTile != null)
            {
                newTile.OccupyingPiece = this;
                
                var boardManager = ServiceLocator.Get<BoardManager>();
                if (boardManager != null)
                {
                    boardManager.RegisterPiece(this, newTile.coordinate);
                }
            }
            
            // 이동 플래그 업데이트
            if (previousTile != null && newTile != null && previousTile != newTile)
            {
                _hasMoved = true;
            }
        }

        /// <summary>
        /// 기물을 특정 타일로 애니메이션과 함께 이동
        /// </summary>
        public void MoveToTile(Tile tile, System.Action onComplete = null)
        {
            // 타일 정보 먼저 업데이트
            UpdateTileInfo(tile);
            
            if (_animator != null)
            {
                Vector3 targetPos = tile.transform.position;
                _animator.AnimateMoveTo(targetPos, MoveDuration, onComplete);
            }
            else
            {
                // Animator가 없으면 즉시 이동
                Vector3 targetPos = tile.transform.position;
                transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// 공격 실행 (PieceCombat에 위임)
        /// </summary>
        public void AttackPiece(Tile targetTile, ChessPiece target, System.Action onComplete)
        {
            if (_combat != null)
            {
                _combat.Attack(targetTile, target, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
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
            
            if (_combat != null)
            {
                _combat.Initialize(this, dataSo.MaxHealth, dataSo.AttackPower);
            }
        }
        
        /// <summary>
        /// 스탯 초기화 (리매치용)
        /// </summary>
        public void ResetStats()
        {
            _hasMoved = false;
            if (_combat != null)
            {
                _combat.ResetStats();
            }
        }
    }
}
