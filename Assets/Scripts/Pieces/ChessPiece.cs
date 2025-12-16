using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using LevelUpChess.Events;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Managers;
using LevelUpChess.Interactables;
using LevelUpChess.UI;
using LevelUpChess.Upgrades;

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
    public partial class ChessPiece : Interactable, IClickable, ITooltipProvider
    {
        [Header("기물 설정")]
        [SerializeField] private PieceDataSO pieceDataSo;
        [SerializeField] private Team _team = Team.White;
        
        [SerializeField] private bool _hasMoved = false;
        [SerializeField] private bool _isDisabled = false;
        
        [HideInInspector]
        [SerializeField] private Tile _currentTile;

        private SpriteRenderer _spriteRenderer;
        private PieceAnimator _animator;
        private PieceCombat _combat;
        private PieceUI _ui;

        // ========== Public 프로퍼티 ==========
        public PieceDataSO DataSo { get => pieceDataSo; set => pieceDataSo = value; }
        public Team Team { get => _team; set => _team = value; }
        public PieceType PieceType => pieceDataSo?.PieceType ?? PieceType.Pawn;
        public int PieceValue => pieceDataSo?.PieceValue ?? 1;
        public float MoveDuration => pieceDataSo?.MoveDuration ?? 0.1f;
        public bool HasMoved { get => _hasMoved; set => _hasMoved = value; }
        public Tile CurrentTile => _currentTile;
        public bool IsMoved => _hasMoved;
        public bool HasMovedThisTurn { get; set; } = false;
        public bool IsDisabled { get => _isDisabled; set => _isDisabled = value; }
        
        // 동적 이동 전략 (기본 + 업그레이드)
        private List<PieceMovementSO> _dynamicMovements = new List<PieceMovementSO>();
        public IReadOnlyList<PieceMovementSO> MovementStrategies => GetAllMovementStrategies();
        
        // 컴포넌트 참조
        public PieceCombat Combat => _combat;
        // Alias for compatibility with existing code
        public PieceCombat Stats => _combat;
        public int BaseAttack => pieceDataSo?.AttackPower ?? 1;
        public PieceAnimator Animator => _animator;
        public PieceUI UI => _ui;
        
        // Combat 위임 프로퍼티 (편의용)
        public int MaxHealth => _combat?.MaxHealth ?? 1;
        public int CurrentHealth => _combat?.CurrentHealth ?? 1;
        public int AttackPower => _combat?.AttackPower ?? 1;
        public int Level { get => _combat?.Level ?? 1; set { if (_combat != null) _combat.Level = value; } }
        public int CurrentExp => _combat?.CurrentExp ?? 0;
        public int ExpToNextLevel => _combat?.ExpToNextLevel ?? 1;
        public bool IsAlive => _combat?.IsAlive ?? false;

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
            
            // 턴 변경 시 HasMovedThisTurn 리셋
            Bus<TurnChangedEvent>.OnEvent += OnTurnChanged;
        }
        
        private void OnDestroy()
        {
            Bus<TurnChangedEvent>.OnEvent -= OnTurnChanged;
        }
        
        private void OnTurnChanged(TurnChangedEvent evt)
        {
            if (evt.NewTeam == Team)
            {
                HasMovedThisTurn = false;
            }
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
        
        // ========== 이동 전략 관리 ==========
        
        /// <summary>
        /// 모든 이동 전략 반환 (기본 + 동적 추가)
        /// </summary>
        private List<PieceMovementSO> GetAllMovementStrategies()
        {
            var all = new List<PieceMovementSO>();
            
            // 기본 이동 전략
            if (pieceDataSo?.MovementStrategies != null)
            {
                all.AddRange(pieceDataSo.MovementStrategies);
            }
            
            // 동적 추가된 이동 전략
            all.AddRange(_dynamicMovements);
            
            return all;
        }
        
        /// <summary>
        /// 이동 전략 추가
        /// </summary>
        public void AddMovementStrategy(PieceMovementSO movement, bool replaceExisting = false)
        {
            if (movement == null) return;
            
            // 이미 있는지 확인
            if (_dynamicMovements.Contains(movement))
            {
                Debug.Log($"[ChessPiece] {name} already has movement {movement.name}");
                return;
            }
            
            if (replaceExisting)
            {
                _dynamicMovements.Clear();
            }
            
            _dynamicMovements.Add(movement);
            Debug.Log($"[ChessPiece] {name} gained new movement: {movement.name}");
        }
        
        /// <summary>
        /// 이동 전략 제거
        /// </summary>
        public void RemoveMovementStrategy(PieceMovementSO movement)
        {
            if (movement == null) return;
            
            if (_dynamicMovements.Remove(movement))
            {
                Debug.Log($"[ChessPiece] {name} lost movement: {movement.name}");
            }
        }
        
        /// <summary>
        /// 모든 동적 이동 전략 제거
        /// </summary>
        public void ClearDynamicMovements()
        {
            _dynamicMovements.Clear();
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
            if (IsDisabled)
            {
                Debug.Log($"[ChessPiece] {name} is disabled, cannot attack!");
                onComplete?.Invoke();
                return;
            }

            if (!CanAttackTarget(target))
            {
                Debug.Log($"[ChessPiece] {name} cannot attack {target.name} due to taunt!");
                onComplete?.Invoke();
                return;
            }

            if (_combat != null)
            {
                _combat.PerformAttack(targetTile, target, onComplete);
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
            if (IsDisabled || IsFrozen || IsOverloaded)
            {
                return new List<Move>();
            }

            var allMoves = new List<Move>();
            
            Debug.Log($"[ChessPiece.GetAvailableMoves] {name} - CurrentTile: {(_currentTile != null ? _currentTile.coordinate.ToString() : "NULL")}, " +
                      $"PieceDataSo: {(pieceDataSo != null ? pieceDataSo.name : "NULL")}");
            
            // 기본 이동 전략
            var strategies = pieceDataSo?.MovementStrategies;
            if (strategies != null)
            {
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
            }
            
            // 동적 이동 전략 (업그레이드로 추가된 것들)
            foreach (var strategy in _dynamicMovements)
            {
                if (strategy == null) 
                {
                    Debug.LogWarning($"[ChessPiece.GetAvailableMoves] {name} - Dynamic strategy is null!");
                    continue;
                }
                var moves = strategy.GetAvailableMoves(this);
                Debug.Log($"[ChessPiece.GetAvailableMoves] {name} - Dynamic {strategy.GetType().Name} returned {moves.Count} moves");
                allMoves.AddRange(moves);
            }

            return allMoves;
        }
        
        /// <summary>
        /// 공격 가능한 타일 목록 반환
        /// </summary>
        public List<Tile> GetAttackableTiles()
        {
            var moves = GetAvailableMoves();
            var attackableTiles = new List<Tile>();
            
            foreach (var move in moves)
            {
                if (!move.isCapture) continue;

                var tile = ServiceLocator.Get<BoardManager>()?.GetTileAt(move.to);
                if (tile != null) attackableTiles.Add(tile);
            }
            
            return attackableTiles;
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

        // --- Convenience wrappers for abilities and upgrades ---
        public void Heal(int amount) => _combat?.Heal(amount);
        public void SetHealth(int health) => _combat?.SetHealth(health);
        public void SetMaxHealth(int max) => _combat?.SetMaxHealth(max);
        public void SetAttackPower(int attack) => _combat?.SetAttackPower(attack);
        public void GainExperience(int amount) => _combat?.GainExperience(amount);
        public bool TakeDamage(int amount, ChessPiece attacker = null) => _combat?.TakeDamage(amount, attacker) ?? false;
        public void ForceKill() { if (_combat != null) _combat.TakeDamage(_combat.CurrentHealth, null); }

        public void ApplyStatusEffect(LevelUpChess.Upgrades.Status.StatusEffect effect)
        {
            _combat?.ApplyStatusEffect(effect);
        }

        // --- Flags and simple properties used by abilities ---
        public bool CanAttackAllies { get; set; } = false;
        public bool CanJumpOverPawns { get; set; } = false;
        public bool IsImmortal { get; set; } = false;
        private int _invincibilityTurnsLeft = 0;

        // --- Events for upgrade/ability subscriptions ---
        public event System.Action<ChessPiece, ChessPiece, int> OnAttackHit;
        public event System.Action<ChessPiece, ChessPiece, int> OnDamageTaken;
        // Func<ChessPiece,bool> returns true if death is prevented
        public event System.Func<ChessPiece, bool> OnBeforeDeath;

        internal void RaiseAttackHit(ChessPiece target, int damage)
        {
            try { OnAttackHit?.Invoke(this, target, damage); } catch { }
        }

        internal void RaiseDamageTaken(ChessPiece attacker, int damage)
        {
            try { OnDamageTaken?.Invoke(this, attacker, damage); } catch { }
        }

        internal bool TryPreventDeath()
        {
            if (OnBeforeDeath == null) return false;

            foreach (var d in OnBeforeDeath.GetInvocationList())
            {
                try
                {
                    var func = (System.Func<ChessPiece, bool>)d;
                    if (func(this))
                    {
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        public void GrantInvincibility(int turns)
        {
            if (turns <= 0) return;
            _invincibilityTurnsLeft = Mathf.Max(_invincibilityTurnsLeft, turns);
            IsImmortal = true;
            Bus<TurnChangedEvent>.OnEvent += HandleInvincibilityTurn;
        }

        private void HandleInvincibilityTurn(TurnChangedEvent ev)
        {
            if (_invincibilityTurnsLeft <= 0) return;
            _invincibilityTurnsLeft--;
            if (_invincibilityTurnsLeft <= 0)
            {
                IsImmortal = false;
                Bus<TurnChangedEvent>.OnEvent -= HandleInvincibilityTurn;
            }
        }

        // ========== ITooltipProvider 구현 ==========
        
        public string GetTooltipContent()
        {
            StringBuilder sb = new StringBuilder();
            
            AppendTitle(sb);
            AppendBasicInfo(sb);
            AppendCombatStats(sb);
            AppendUpgradeInfo(sb);
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
        
        private void AppendUpgradeInfo(StringBuilder sb)
        {
            var upgradeManager = LevelUpChess.Upgrades.UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                var appliedUpgrades = upgradeManager.GetAppliedUpgradesForPiece(this);
                if (appliedUpgrades.Count > 0)
                {
                    sb.AppendLine("<b>적용된 업그레이드:</b>");
                    foreach (var upgrade in appliedUpgrades)
                    {
                        sb.AppendLine($"  • {upgrade.UpgradeName}");
                    }
                    sb.AppendLine();
                }
            }
        }
        
        private void AppendMovementInfo(StringBuilder sb)
        {
            if (MovementStrategies == null || MovementStrategies.Count == 0)
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
