using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Managers;
using LevelUpChess.Interactables;
using LevelUpChess.UI;

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
    public class ChessPiece : Interactable, IClickable, ITooltipProvider
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
        public PieceMovementSO[] MovementStrategies => pieceDataSo?.MovementStrategies ?? new PieceMovementSO[0];
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
                _combat.Initialize(
                    this, 
                    pieceDataSo.MaxHealth, 
                    pieceDataSo.AttackPower,
                    pieceDataSo.Defense,
                    pieceDataSo.Shield,
                    pieceDataSo.HealthRegeneration,
                    pieceDataSo.LifeSteal
                );
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
                _combat.Initialize(
                    this, 
                    dataSo.MaxHealth, 
                    dataSo.AttackPower,
                    dataSo.Defense,
                    dataSo.Shield,
                    dataSo.HealthRegeneration,
                    dataSo.LifeSteal
                );
            }
        }
        
        public void ResetStats()
        {
            _hasMoved = false;
            if (_combat != null)
            {
                _combat.ResetStats();
            }
        }

        // ========== ITooltipProvider 구현 ==========
        
        public string GetTooltipContent()
        {
            StringBuilder sb = new StringBuilder();
            
            AppendTitle(sb);
            AppendBasicInfo(sb);
            AppendCombatStats(sb);
            AppendMovementInfo(sb);
            
            return sb.ToString();
        }
        
        public Team? GetTooltipTeam()
        {
            return _team;
        }
        
        private void AppendTitle(StringBuilder sb)
        {
            string pieceName = pieceDataSo != null ? pieceDataSo.DisplayName : PieceType.ToString();
            sb.AppendLine($"<size=18><b>{_team} {pieceName}</b></size>");
            sb.AppendLine();
        }
        
        private void AppendBasicInfo(StringBuilder sb)
        {
            if (_combat != null)
            {
                sb.AppendLine($"<b>레벨:</b> {_combat.Level}");
                sb.AppendLine($"<b>경험치:</b> {_combat.CurrentExp}/{_combat.ExpToNextLevel}");
                sb.AppendLine();
            }
        }
        
        private void AppendCombatStats(StringBuilder sb)
        {
            if (_combat != null)
            {
                sb.AppendLine($"<b>체력:</b> {_combat.CurrentHealth}/{_combat.MaxHealth}");
                sb.AppendLine($"<b>공격력:</b> {_combat.AttackPower}");
                
                if (_combat.Defense > 0)
                    sb.AppendLine($"<b>방어력:</b> {_combat.Defense}");
                
                if (_combat.Shield > 0)
                    sb.AppendLine($"<b>보호막:</b> {_combat.Shield}");
                
                if (_combat.HealthRegeneration > 0)
                    sb.AppendLine($"<b>체력 재생:</b> {_combat.HealthRegeneration}/턴");
                
                if (_combat.LifeSteal > 0)
                    sb.AppendLine($"<b>흡혈:</b> {(_combat.LifeSteal * 100):F0}%");
                
                sb.AppendLine();
            }
        }
        
        private void AppendMovementInfo(StringBuilder sb)
        {
            if (MovementStrategies == null || MovementStrategies.Length == 0)
                return;
            
            sb.AppendLine("<b>이동 패턴:</b>");
            foreach (var movement in MovementStrategies)
            {
                if (movement == null) continue;
                
                string moveTypeName = movement.MoveType switch
                {
                    MoveType.Normal => "[이동/공격]",
                    MoveType.MoveOnly => "[이동]",
                    MoveType.AttackOnly => "[공격]",
                    _ => ""
                };
                
                sb.AppendLine($"  {moveTypeName} <i>{movement.name}</i>");
            }
        }
    }
}
