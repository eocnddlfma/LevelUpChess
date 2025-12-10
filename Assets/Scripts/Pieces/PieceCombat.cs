using UnityEngine;
using DG.Tweening;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Events;
using LevelUpChess.Managers;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 기물의 전투 관련 로직을 담당하는 컴포넌트
    /// - 체력, 공격력, 대미지, 사망 처리
    /// - 레벨, 경험치 시스템
    /// - 강화 시스템
    /// </summary>
    public class PieceCombat : MonoBehaviour
    {
        private ChessPiece _piece;
        private PieceAnimator _animator;
        private PieceUI _ui;
        
        // 전투 스탯
        private int _currentHealth;
        private int _maxHealth;
        private int _attackPower;
        private int _defense = 0;
        private int _shield = 0;
        private int _healthRegeneration = 0;
        private float _lifeSteal = 0f;
        
        // 레벨 시스템
        private int _level = 1;
        private int _currentExp = 0;
        
        // ========== Public 프로퍼티 ==========
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int AttackPower => _attackPower;
        public int Defense => _defense;
        public int Shield => _shield;
        public int HealthRegeneration => _healthRegeneration;
        public float LifeSteal => _lifeSteal;
        public int Level => _level;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => _level; // 레벨만큼 경험치 필요
        public bool IsAlive => _currentHealth > 0;
        
        /// <summary>
        /// 총 누적 경험치 (레벨업에 사용된 경험치 + 현재 보유 경험치)
        /// 레벨 1→2: 1exp, 2→3: 2exp, ... (n-1)→n: (n-1)exp
        /// 합계: 1+2+...+(level-1) = (level-1)*level/2
        /// </summary>
        public int TotalAccumulatedExp => ((_level - 1) * _level / 2) + _currentExp;
        
        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(ChessPiece ownerPiece, int maxHp, int attackPower, int defense = 0, int shield = 0, int healthRegen = 0, float lifeSteal = 0f)
        {
            _piece = ownerPiece;
            _animator = GetComponent<PieceAnimator>();
            _ui = GetComponent<PieceUI>();
            
            _maxHealth = maxHp;
            _currentHealth = maxHp;
            _attackPower = attackPower;
            _defense = defense;
            _shield = shield;
            _healthRegeneration = healthRegen;
            _lifeSteal = lifeSteal;
            _level = 1;
            _currentExp = 0;
            
            // UI 초기화
            if (_ui != null)
            {
                _ui.Initialize(ownerPiece);
                _ui.InitializeStatusUI(_maxHealth, _attackPower, _level, _currentExp, ExpToNextLevel);
            }
        }
        
        private void OnEnable()
        {
            Bus<TurnChangedEvent>.OnEvent += OnTurnChanged;
        }
        
        private void OnDisable()
        {
            Bus<TurnChangedEvent>.OnEvent -= OnTurnChanged;
        }
        
        /// <summary>
        /// 턴 변경 시 호출 - 내 턴이 끝났을 때 체력 재생
        /// </summary>
        private void OnTurnChanged(TurnChangedEvent eventData)
        {
            // 상대 팀으로 턴이 넣어갔다는 것은 내 턴이 끝났다는 의미
            if (_piece != null && eventData.NewTeam != _piece.Team)
            {
                ApplyHealthRegeneration();
            }
        }
        
        /// <summary>
        /// 체력 재생 적용
        /// </summary>
        private void ApplyHealthRegeneration()
        {
            if (_healthRegeneration > 0 && _currentHealth < _maxHealth)
            {
                Heal(_healthRegeneration);
                Debug.Log($"[PieceCombat] {_piece.name} regenerated {_healthRegeneration} HP at turn end");
            }
        }
        
        // ========== 전투 시스템 ==========
        
        /// <summary>
        /// 대상 기물 공격 (애니메이션 + 대미지 처리 + 결과 처리)
        /// 흐름: 찌르기 애니메이션 → 대미지 처리 → 복귀 → 적이 죽었으면 이동
        /// </summary>
        public void Attack(Tile targetTile, ChessPiece target, System.Action onComplete)
        {
            if (_animator == null)
            {
                // Animator가 없으면 즉시 처리
                bool died = target.Combat.TakeDamage(_attackPower, _piece);
                if (died)
                {
                    _piece.UpdateTileInfo(targetTile);
                }
                onComplete?.Invoke();
                return;
            }
            
            Vector3 attackPos = targetTile.transform.position;
            bool targetDied = false;
            
            // 공격 애니메이션 실행
            // onAttackHit: 찌르기가 닿았을 때 대미지 처리
            // onComplete: 찌르기 복귀 후 결과에 따라 이동
            _animator.AnimateAttack(attackPos, 
                onAttackHit: () =>
                {
                    // 대미지 처리
                    targetDied = target.Combat.TakeDamage(_attackPower, _piece);
                    Debug.Log($"[PieceCombat] Attack hit! {target.name} {(targetDied ? "died" : $"survived with {target.Combat.CurrentHealth} HP")}");
                    
                    // 흡혈 처리
                    if (_lifeSteal > 0 && !targetDied)
                    {
                        int healAmount = Mathf.CeilToInt(_attackPower * _lifeSteal);
                        if (healAmount > 0)
                        {
                            Heal(healAmount);
                            Debug.Log($"[PieceCombat] Life steal: healed {healAmount} HP");
                        }
                    }
                },
                onComplete: () =>
                {
                    if (targetDied)
                    {
                        // 적이 죽었으면 잠시 대기 후 해당 위치로 이동
                        Debug.Log($"[PieceCombat] Moving to killed enemy's position");
                        DOVirtual.DelayedCall(0.15f, () =>
                        {
                            _animator.AnimateMoveToTarget(attackPos, () =>
                            {
                                _piece.UpdateTileInfo(targetTile);
                                onComplete?.Invoke();
                            });
                        });
                    }
                    else
                    {
                        // 적이 살아있으면 제자리 (이미 복귀된 상태)
                        onComplete?.Invoke();
                    }
                });
        }
        
        /// <summary>
        /// 대미지를 받음. 죽으면 true 반환
        /// </summary>
        public bool TakeDamage(int amount, ChessPiece attacker = null)
        {
            // 1. 방어력 적용
            int actualDamage = Mathf.Max(1, amount - _defense); // 최소 1 대미지
            
            // 2. 보호막 먼저 소모
            if (_shield > 0)
            {
                if (_shield >= actualDamage)
                {
                    _shield -= actualDamage;
                    Debug.Log($"[PieceCombat] Shield absorbed {actualDamage} damage. Shield remaining: {_shield}");
                    _ui?.ShowDamageEffect(actualDamage);
                    return false;
                }
                else
                {
                    actualDamage -= _shield;
                    Debug.Log($"[PieceCombat] Shield absorbed {_shield} damage. {actualDamage} damage to health.");
                    _shield = 0;
                }
            }
            
            // 3. 체력 감소
            _currentHealth -= actualDamage;
            _currentHealth = Mathf.Max(0, _currentHealth);
            
            _ui?.UpdateHealth(_currentHealth, _maxHealth);
            _ui?.ShowDamageEffect(actualDamage);
            
            if (_currentHealth <= 0)
            {
                Die(attacker);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(int amount)
        {
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            _ui?.UpdateHealth(_currentHealth, _maxHealth);
            _ui?.ShowHealEffect(amount);
            Debug.Log($"[PieceCombat] {_piece.name} healed by {amount}. Current health: {_currentHealth}/{_maxHealth}");
        }
        
        /// <summary>
        /// 기물 사망 처리
        /// </summary>
        private void Die(ChessPiece killer = null)
        {
            // 처치자에게 경험치 부여 (사망 전에 계산)
            int expReward = CalculateExpReward();
            
            // 사망 이벤트 발생
            Bus<PieceDeathEvent>.Raise(new PieceDeathEvent
            {
                DeadPiece = _piece,
                Killer = killer,
                DeadPieceTeam = _piece.Team,
                DeadPieceType = _piece.PieceType,
                PieceValue = _piece.PieceValue
            });
            
            // 처치자에게 경험치 부여 (기물 점수 + 상대의 누적 경험치)
            if (killer != null && killer.Combat != null)
            {
                killer.Combat.GainExperience(expReward);
                Debug.Log($"[PieceCombat] {killer.name} gained {expReward} exp from killing {_piece.name} (PieceValue:{_piece.PieceValue} + AccumulatedExp:{TotalAccumulatedExp})");
            }
            
            // 애니메이션 정리
            if (_animator != null)
            {
                _animator.StopAnimation();
            }

            // 타일에서 제거
            if (_piece.CurrentTile != null)
            {
                _piece.CurrentTile.OccupyingPiece = null;
            }

            // King 사망 시 게임 오버
            if (_piece.PieceType == PieceType.King)
            {
                Team winnerTeam = _piece.Team == Team.White ? Team.Black : Team.White;
                
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
        
        // ========== 강화 시스템 ==========
        
        /// <summary>
        /// 공격력 증가
        /// </summary>
        public void IncreaseAttackPower(int amount)
        {
            _attackPower += amount;
            _ui?.UpdateAttackPower(_attackPower);
            Debug.Log($"[PieceCombat] {_piece.name} attack power increased by {amount}. New value: {_attackPower}");
        }
        
        /// <summary>
        /// 최대 체력 증가
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            _maxHealth += amount;
            _currentHealth += amount; // 현재 체력도 함께 증가
            _ui?.UpdateAll(_currentHealth, _maxHealth, _attackPower, _level);
            Debug.Log($"[PieceCombat] {_piece.name} max health increased by {amount}. New value: {_maxHealth}");
        }
        
        /// <summary>
        /// 체력 직접 설정
        /// </summary>
        public void SetHealth(int health)
        {
            _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
            _ui?.UpdateHealth(_currentHealth, _maxHealth);
        }
        
        /// <summary>
        /// 최대 체력 설정
        /// </summary>
        public void SetMaxHealth(int newMaxHealth)
        {
            _maxHealth = newMaxHealth;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            _ui?.UpdateAll(_currentHealth, _maxHealth, _attackPower, _level);
        }
        
        /// <summary>
        /// 공격력 설정
        /// </summary>
        public void SetAttackPower(int attack)
        {
            _attackPower = attack;
            _ui?.UpdateAttackPower(_attackPower);
        }
        
        // ========== 레벨 시스템 ==========
        
        /// <summary>
        /// 처치 시 획득할 경험치 계산
        /// 기물 점수 + 상대의 누적 경험치 (레벨업에 사용된 + 현재 보유)
        /// </summary>
        private int CalculateExpReward()
        {
            return _piece.PieceValue + TotalAccumulatedExp;
        }
        
        /// <summary>
        /// 경험치 획득
        /// </summary>
        public void GainExperience(int amount)
        {
            int previousExp = _currentExp;
            int previousLevel = _level;
            
            _currentExp += amount;
            Debug.Log($"[PieceCombat] {_piece.name} gained {amount} exp. Total: {_currentExp}/{ExpToNextLevel} (Level {_level})");
            
            // 레벨업 체크 - 경험치가 충분한 동안 계속 레벨업
            while (_currentExp >= ExpToNextLevel)
            {
                LevelUp();
            }
            
            if (_level > previousLevel)
            {
                Debug.Log($"[PieceCombat] {_piece.name} leveled up from {previousLevel} to {_level}! Remaining exp: {_currentExp}/{ExpToNextLevel}");
            }
            else
            {
                // 레벨업이 없었다면 경험치 바만 업데이트
                _ui?.UpdateExperience(_currentExp, ExpToNextLevel);
            }
        }
        
        /// <summary>
        /// 레벨업 처리
        /// </summary>
        private void LevelUp()
        {
            int expNeeded = ExpToNextLevel; // 현재 레벨에서 필요한 경험치 저장
            _currentExp -= expNeeded;       // 남은 경험치 이월
            _level++;
            
            Debug.Log($"[PieceCombat] {_piece.name} LevelUp! Used {expNeeded} exp, remaining: {_currentExp}, next level needs: {ExpToNextLevel}");
            
            var dataSo = _piece.DataSo;
            int healthIncrease = dataSo?.HealthPerLevel ?? 1;
            int attackIncrease = dataSo?.AttackPerLevel ?? 1;
            
            _maxHealth += healthIncrease;
            _currentHealth += healthIncrease; // 레벨업 시 체력도 회복
            _attackPower += attackIncrease;
            
            _ui?.UpdateAll(_currentHealth, _maxHealth, _attackPower, _level);
            _ui?.OnLevelUp(_level, _currentExp, ExpToNextLevel);
            
            Debug.Log($"[PieceCombat] {_piece.name} is now level {_level}! HP: {_maxHealth}, ATK: {_attackPower}");
            
            // 레벨업 이벤트 발생
            Bus<PieceLevelUpEvent>.Raise(new PieceLevelUpEvent
            {
                Piece = _piece,
                NewLevel = _level,
                AttackIncrease = attackIncrease,
                HealthIncrease = healthIncrease
            });
        }
        
        /// <summary>
        /// 스탯 초기화 (리매치용)
        /// </summary>
        public void ResetStats()
        {
            var dataSo = _piece.DataSo;
            if (dataSo != null)
            {
                _maxHealth = dataSo.MaxHealth;
                _currentHealth = dataSo.MaxHealth;
                _attackPower = dataSo.AttackPower;
                _defense = 0;
                _shield = 0;
                _healthRegeneration = 0;
                _lifeSteal = 0f;
                _level = 1;
                _currentExp = 0;
                _ui?.InitializeStatusUI(_maxHealth, _attackPower, _level, _currentExp, ExpToNextLevel);
            }
        }
    }
}
