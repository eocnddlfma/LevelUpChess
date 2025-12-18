using Unity.Netcode;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Events;
using LevelUpChess.Managers;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 임시 수정자 구조체
    /// </summary>
    public struct TemporaryModifier
    {
        public StatType Stat;
        public int Amount;
        public int TurnsLeft;
        
        public TemporaryModifier(StatType stat, int amount, int turns)
        {
            Stat = stat;
            Amount = amount;
            TurnsLeft = turns;
        }
    }

    /// <summary>
    /// 기물의 전투 관련 로직을 담당하는 컴포넌트
    /// - 체력, 공격력, 대미지, 사망 처리
    /// - 레벨, 경험치 시스템
    /// - 강화 시스템
    /// - 능력 시스템
    /// </summary>
    public class PieceCombat : NetworkBehaviour
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
        private int _maxShield = 0;
        private int _healthRegeneration = 0;
        private float _lifeSteal = 0f;
        
        // 스탯 보너스 (업그레이드로 인한 추가 스탯)
        private int _bonusMaxHealth = 0;
        private int _bonusAttackPower = 0;
        private int _bonusDefense = 0;
        private int _bonusShield = 0;
        private int _bonusHealthRegen = 0;
        private float _bonusLifeSteal = 0f;
        
        // 레벨 시스템
        private int _level = 1;
        private int _currentExp = 0;
        private int _pendingLevelUps = 0;
        
        // 능력 시스템
        private List<IAbility> _abilities = new List<IAbility>();
        private List<StatUpgradeSO> _statUpgrades = new List<StatUpgradeSO>();
        private List<LevelUpChess.Upgrades.Status.StatusEffect> _statusEffects = new List<LevelUpChess.Upgrades.Status.StatusEffect>();
        
        // 임시 수정자 (턴 기반)
        private List<TemporaryModifier> _temporaryModifiers = new List<TemporaryModifier>();
        
        // ========== Public 프로퍼티 ==========
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth + _bonusMaxHealth;
        public int AttackPower => _attackPower + _bonusAttackPower;
        public int Defense => _defense + _bonusDefense;
        public int Shield => _shield + _bonusShield;
        public int HealthRegeneration => _healthRegeneration + _bonusHealthRegen;
        public float LifeSteal => _lifeSteal + _bonusLifeSteal;
        public int Level { get => _level; set { _level = value; _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield); } }
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => _level; // 레벨만큼 경험치 필요
        public bool IsAlive => _currentHealth > 0;
        
        // 능력 관련 프로퍼티
        public IReadOnlyList<IAbility> Abilities => _abilities;
        public bool HasAbility(string abilityId) => _abilities.Any(a => a.AbilityId == abilityId);
        
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
            _maxShield = shield;
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
                UpdateTemporaryModifiers();
                
                // OnTurnEnd 능력 실행
                var upgradeManager = UpgradeManager.Instance;
                if (upgradeManager != null)
                {
                    var turnEndContext = upgradeManager.CreateAbilityContext(_piece, null);
                    upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnTurnEnd, turnEndContext);
                }
            }
            // Tick status effects when this piece's team's turn starts
            if (_piece != null && eventData.NewTeam == _piece.Team)
            {
                TickStatusEffects();
            }
        }

        private void TickStatusEffects()
        {
            if (_statusEffects == null || _statusEffects.Count == 0) return;
            for (int i = _statusEffects.Count - 1; i >= 0; i--)
            {
                var s = _statusEffects[i];
                try { s.OnTick(); } catch { }
                if (s.IsExpired)
                {
                    try { s.OnRemove(); } catch { }
                    _statusEffects.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 체력 재생 적용
        /// </summary>
        private void ApplyHealthRegeneration()
        {
            int totalRegen = HealthRegeneration;
            if (totalRegen > 0 && _currentHealth < _maxHealth)
            {
                Heal(totalRegen);
                Debug.Log($"[PieceCombat] {_piece.name} regenerated {totalRegen} HP at turn end");

                // 체력이 최대치가 되었으면 보호막 회복
                if (_currentHealth >= _maxHealth && _maxShield > 0)
                {
                    _shield = _maxShield;
                    Debug.Log($"[PieceCombat] {_piece.name} shield restored to {_shield} at full health");
                }
            }
        }
        
        // ========== 전투 시스템 ==========
        
        /// <summary>
        /// 대상 기물 공격 (애니메이션 + 대미지 처리 + 결과 처리)
        /// 흐름: 찌르기 애니메이션 → 대미지 처리 → 복귀 → 적이 죽었으면 이동
        /// </summary>
        public void PerformAttack(Tile targetTile, ChessPiece target, System.Action onComplete)
        {
            var upgradeManager = UpgradeManager.Instance;

            if (_animator == null)
            {
                // Animator가 없으면 즉시 처리
                bool died = target.Combat.TakeDamage(_attackPower, _piece);
                
                // OnKill 능력 실행
                if (died)
                {
                    if (upgradeManager != null)
                    {
                        var context = upgradeManager.CreateAbilityContext(_piece, target);
                        context.Target = target;
                        context.TargetDied = true;
                        context.FromTile = _piece.CurrentTile;
                        context.ToTile = targetTile;
                        context.Damage = _attackPower;
                        upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnKill, context);
                        
                        if (context.PreventMoveAfterKill)
                        {
                            Debug.Log($"[PieceCombat] PreventMoveAfterKill activated, staying in place");
                        }
                        else
                        {
                            _piece.UpdateTileInfo(targetTile);
                        }
                    }
                    else
                    {
                        _piece.UpdateTileInfo(targetTile);
                    }
                }
                
                onComplete?.Invoke();
                return;
            }
            
            Vector3 attackPos = targetTile.transform.position;
            bool targetDied = false;
            AbilityContext abilityContext = null;
            AbilityContext attackStartContext = null;
            
            // 공격 시작 시 능력 실행 (데미지 계산 등)
            if (upgradeManager != null)
            {
                attackStartContext = upgradeManager.CreateAbilityContext(_piece, target);
                attackStartContext.FromTile = _piece.CurrentTile;
                attackStartContext.ToTile = targetTile;
                attackStartContext.Damage = _attackPower;
                upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnAttackStart, attackStartContext);
                
                // 보너스 데미지 적용
                _attackPower += attackStartContext.BonusDamage;
            }
            
            // 공격 애니메이션 실행
            // onAttackHit: 찌르기가 닿았을 때 대미지 처리
            // onComplete: 찌르기 복귀 후 결과에 따라 이동
            _animator.AnimateAttack(attackPos, 
                onAttackHit: () =>
                {
                    // 대미지 처리
                    int appliedDamage = _attackPower;
                    targetDied = target.Combat.TakeDamage(_attackPower, _piece);
                    Debug.Log($"[PieceCombat] Attack hit! {target.name} {(targetDied ? "died" : $"survived with {target.Combat.CurrentHealth} HP")}");
                    
                    // Raise OnAttackHit event for attacker
                    _piece?.RaiseAttackHit(target, appliedDamage);

                    // OnAttackHit 능력 실행
                    if (upgradeManager != null)
                    {
                        var attackContext = upgradeManager.CreateAbilityContext(_piece, target);
                        attackContext.Target = target;
                        attackContext.Damage = appliedDamage;
                        upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnAttackHit, attackContext);
                    }
                    
                    // 보너스 데미지 제거
                    if (attackStartContext != null)
                    {
                        _attackPower -= attackStartContext.BonusDamage;
                    }

                    // 공격 성공 이벤트 발송
                    Bus<AttackSuccessEvent>.Raise(new AttackSuccessEvent
                    {
                        Attacker = _piece,
                        Target = target,
                        DamageDealt = appliedDamage,
                        TargetDied = targetDied
                    });

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
                    
                    // OnKill 능력 실행 (적이 죽었을 때)
                    if (targetDied)
                    {
                        var upgradeManager = UpgradeManager.Instance;
                        if (upgradeManager != null)
                        {
                            abilityContext = upgradeManager.CreateAbilityContext(_piece, target);
                            abilityContext.Target = target;
                            abilityContext.TargetDied = true;
                            abilityContext.FromTile = _piece.CurrentTile;
                            abilityContext.ToTile = targetTile;
                            abilityContext.Damage = _attackPower;
                            upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnKill, abilityContext);
                        }
                    }
                },
                onComplete: () =>
                {
                    if (targetDied && (abilityContext == null || !abilityContext.PreventMoveAfterKill))
                    {
                        // 적이 죽었고 이동 방지 능력이 없으면 해당 위치로 이동
                        Debug.Log($"[PieceCombat] Moving to killed enemy's position");
                        DOVirtual.DelayedCall(0.15f, () =>
                        {
                            _animator.AnimateMoveToTarget(attackPos, () =>
                            {
                                _piece.UpdateTileInfo(targetTile);
                                
                                // OnAfterMove 능력 실행
                                var upgradeManager = UpgradeManager.Instance;
                                if (upgradeManager != null)
                                {
                                    var moveContext = upgradeManager.CreateAbilityContext(_piece, null);
                                    moveContext.FromTile = _piece.CurrentTile; // 이동 전 타일 (targetTile이 이동 후)
                                    moveContext.ToTile = targetTile;
                                    upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnAfterMove, moveContext);
                                }
                                
                                if (_pendingLevelUps > 0)
                                {
                                    for (int i = 0; i < _pendingLevelUps; i++)
                                    {
                                        Debug.Log($"[PieceCombat] Enqueuing PieceLevelUpEvent for {_piece.name} to level {_level - _pendingLevelUps + i + 1}");
                                        EventQueue.Instance.Enqueue(new PieceLevelUpEvent
                                        {
                                            Piece = _piece,
                                            NewLevel = _level - _pendingLevelUps + i + 1,
                                            AttackIncrease = 0, // 이미 증가됨
                                            HealthIncrease = 0  // 이미 증가됨
                                        });
                                    }
                                    _pendingLevelUps = 0;
                                }
                                onComplete?.Invoke();
                            });
                        });
                    }
                    else
                    {
                        // 적이 살아있거나 이동 방지 능력이 발동했으면 제자리
                        if (targetDied && abilityContext != null && abilityContext.PreventMoveAfterKill)
                        {
                            Debug.Log($"[PieceCombat] PreventMoveAfterKill activated, staying in place");
                        }
                        onComplete?.Invoke();
                    }
                });
        }
        
        /// <summary>
        /// 대미지를 받음. 죽으면 true 반환
        /// </summary>
        public bool TakeDamage(int amount, ChessPiece attacker = null, bool handleDeath = true)
        {
            // If the piece is currently immortal, ignore damage
            if (_piece != null && (_piece.IsImmortal))
            {
                Debug.Log($"[PieceCombat] {_piece.name} is currently invincible. Damage ignored.");
                return false;
            }

            int actualDamage = 0;

            // 1. 보호막 확인 - 보호막이 있으면 먼저 보호막으로 데미지 흡수
            int totalShield = _shield + _bonusShield;
            if (totalShield > 0)
            {
                // 보호막이 있을 때는 방어력 효과 2배 적용
                actualDamage = Mathf.Max(1, amount - (_defense * 2)); // 최소 1 대미지

                // 보호막으로 데미지 흡수
                int shieldDamage = Mathf.Min(actualDamage, totalShield);
                _shield -= shieldDamage;
                actualDamage -= shieldDamage;

                Debug.Log($"[PieceCombat] Shield absorbed {shieldDamage} damage. Shield remaining: {_shield}");

                // 보호막으로 모두 흡수했으면 체력 데미지 없음
                if (actualDamage <= 0)
                {
                    _ui?.ShowDamageEffect(0); // 데미지 효과는 0으로 표시
                    _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield);
                    return false;
                }

                // 남은 데미지는 체력에 적용
                _currentHealth -= actualDamage;
                _currentHealth = Mathf.Max(0, _currentHealth);

                _ui?.UpdateHealth(_currentHealth, _maxHealth);
                _ui?.ShowDamageEffect(actualDamage);
                _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield);
            }
            else
            {
                // 2. 보호막이 없으면 일반 방어력 적용
                actualDamage = Mathf.Max(1, amount - _defense); // 최소 1 대미지
                
                // 3. 체력 감소
                _currentHealth -= actualDamage;
                _currentHealth = Mathf.Max(0, _currentHealth);

                _ui?.UpdateHealth(_currentHealth, _maxHealth);
                _ui?.ShowDamageEffect(actualDamage);
            }

            // OnHit 능력 실행 (피해 후, 사망 전)
            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                var hitContext = upgradeManager.CreateAbilityContext(_piece, null);
                hitContext.Damage = actualDamage;
                hitContext.Attacker = attacker;
                upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnHit, hitContext);

                // BonusDamage 적용 (음수면 데미지 감소)
                actualDamage += hitContext.BonusDamage;
                actualDamage = Mathf.Max(1, actualDamage); // 최소 1 데미지
            }
            
            if (_currentHealth <= 0 && handleDeath)
            {
                bool prevented = _piece != null && _piece.TryPreventDeath();

                if (prevented)
                {
                    _currentHealth = 1;
                    _ui?.UpdateHealth(_currentHealth, _maxHealth);
                    _ui?.ShowHealEffect(1);
                    return false;
                }

                Die(attacker);
                return true;
            }

            // raise OnDamageTaken event on victim
            _piece?.RaiseDamageTaken(attacker, actualDamage);
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
        public void Die(ChessPiece killer = null)
        {
            // OnDeath 능력 실행 (사망 전)
            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                var deathContext = upgradeManager.CreateAbilityContext(_piece, null);
                deathContext.Target = killer; // 처치자
                upgradeManager.ExecuteAbilities(_piece, AbilityTrigger.OnDeath, deathContext);
            }
            
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
            _ui?.UpdateAll(_currentHealth, _maxHealth, _attackPower, _level, Shield);
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
            _ui?.UpdateAll(_currentHealth, _maxHealth, _attackPower, _level, Shield);
        }
        
        /// <summary>
        /// 공격력 설정
        /// </summary>
        public void SetAttackPower(int attack)
        {
            _attackPower = attack;
            _ui?.UpdateAttackPower(_attackPower);
        }

        public int Attack => AttackPower; // Stats.Attack compatibility

        public void ApplyStatusEffect(LevelUpChess.Upgrades.Status.StatusEffect effect)
        {
            if (effect == null) return;
            effect.SetOwner(_piece);
            _statusEffects.Add(effect);
            try { effect.OnApply(); } catch { }
        }

        public void RemoveStatusEffect(LevelUpChess.Upgrades.Status.StatusEffect effect)
        {
            if (effect == null) return;
            try { effect.OnRemove(); } catch { }
            _statusEffects.Remove(effect);
        }

        /// <summary>
        /// 게임 내 임시 보정(미니몬스터) 등으로 사용되는 간단한 stat modifier
        /// </summary>
        public void AddModifier(StatType stat, int amount)
        {
            switch (stat)
            {
                case StatType.Attack:
                case StatType.AttackPower:
                    _bonusAttackPower += amount;
                    break;
                case StatType.MaxHealth:
                case StatType.Health:
                    _bonusMaxHealth += amount;
                    _currentHealth += amount;
                    break;
                case StatType.Defense:
                    _bonusDefense += amount;
                    break;
                case StatType.Shield:
                    _bonusShield += amount;
                    break;
                case StatType.HealthRegeneration:
                    _bonusHealthRegen += amount;
                    break;
                default:
                    Debug.LogWarning($"[PieceCombat] AddModifier: unknown stat {stat}");
                    break;
            }
            if (Application.isPlaying)
            {
                _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield);
            }
        }

        public void RemoveModifier(StatType stat, int amount)
        {
            switch (stat)
            {
                case StatType.Attack:
                case StatType.AttackPower:
                    _bonusAttackPower -= amount;
                    break;
                case StatType.MaxHealth:
                case StatType.Health:
                    _bonusMaxHealth -= amount;
                    if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;
                    break;
                case StatType.Defense:
                    _bonusDefense -= amount;
                    break;
                case StatType.Shield:
                    _bonusShield -= amount;
                    break;
                case StatType.HealthRegeneration:
                    _bonusHealthRegen -= amount;
                    break;
                default:
                    Debug.LogWarning($"[PieceCombat] RemoveModifier: unknown stat {stat}");
                    break;
            }
            if (Application.isPlaying)
            {
                _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield);
            }
        }

        /// <summary>
        /// 임시 수정자 추가 (턴 기반)
        /// </summary>
        public void AddTemporaryModifier(StatType stat, int amount, int turns)
        {
            var modifier = new TemporaryModifier(stat, amount, turns);
            _temporaryModifiers.Add(modifier);
            AddModifier(stat, amount);
            Debug.Log($"[PieceCombat] {_piece.name} temporary modifier added: {stat} +{amount} for {turns} turns");
        }

        /// <summary>
        /// 임시 수정자 제거
        /// </summary>
        private void RemoveTemporaryModifier(TemporaryModifier modifier)
        {
            _temporaryModifiers.Remove(modifier);
            RemoveModifier(modifier.Stat, modifier.Amount);
        }

        /// <summary>
        /// 턴 종료 시 임시 수정자 업데이트
        /// </summary>
        private void UpdateTemporaryModifiers()
        {
            for (int i = _temporaryModifiers.Count - 1; i >= 0; i--)
            {
                var mod = _temporaryModifiers[i];
                mod.TurnsLeft--;
                _temporaryModifiers[i] = mod;
                
                if (mod.TurnsLeft <= 0)
                {
                    RemoveTemporaryModifier(mod);
                    Debug.Log($"[PieceCombat] {_piece.name} temporary modifier expired: {mod.Stat} +{mod.Amount}");
                }
            }
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
            int levelUps = 0;
            while (_currentExp >= ExpToNextLevel)
            {
                LevelUp();
                levelUps++;
            }
            
            if (levelUps > 0)
            {
                Debug.Log($"[PieceCombat] {_piece.name} leveled up from {previousLevel} to {_level}! Remaining exp: {_currentExp}/{ExpToNextLevel}");
                _pendingLevelUps += levelUps;
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
            
            _ui?.UpdateAll(_currentHealth, _maxHealth, _attackPower, _level, Shield);
            _ui?.OnLevelUp(_level, _currentExp, ExpToNextLevel);
            
            Debug.Log($"[PieceCombat] {_piece.name} is now level {_level}! HP: {_maxHealth}, ATK: {_attackPower}");
            
            // 레벨업 이벤트는 이동 후에 발생
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
                
                // 보너스 스탯 초기화
                _bonusMaxHealth = 0;
                _bonusAttackPower = 0;
                _bonusDefense = 0;
                _bonusShield = 0;
                _bonusHealthRegen = 0;
                _bonusLifeSteal = 0f;
                
                // 능력 및 업그레이드 초기화
                _abilities.Clear();
                _statUpgrades.Clear();
                
                _ui?.InitializeStatusUI(_maxHealth, _attackPower, _level, _currentExp, ExpToNextLevel);
            }
        }
        
        // ========== 능력 시스템 ==========
        
        /// <summary>
        /// 능력 추가
        /// </summary>
        public void AddAbility(IAbility ability)
        {
            if (ability == null) return;
            
            // 중복 체크
            if (_abilities.Any(a => a.AbilityId == ability.AbilityId))
            {
                Debug.Log($"[PieceCombat] {_piece.name} already has ability {ability.AbilityName}");
                return;
            }
            
            _abilities.Add(ability);
            ability.OnApply(_piece);
            Debug.Log($"[PieceCombat] {_piece.name} gained ability: {ability.AbilityName}");
        }
        
        /// <summary>
        /// 능력 제거
        /// </summary>
        public void RemoveAbility(string abilityId)
        {
            var ability = _abilities.Find(a => a.AbilityId == abilityId);
            if (ability != null)
            {
                ability.OnRemove(_piece);
                _abilities.Remove(ability);
                Debug.Log($"[PieceCombat] {_piece.name} lost ability: {ability.AbilityName}");
            }
        }
        
        /// <summary>
        /// 특정 트리거의 능력들 실행
        /// </summary>
        public AbilityContext TriggerAbilities(AbilityTrigger trigger, AbilityContext context = null)
        {
            if (context == null)
            {
                context = new AbilityContext(_piece);
            }
            
            foreach (var ability in _abilities.Where(a => a.Trigger == trigger))
            {
                ability.Execute(_piece, context);
                if (context.CancelAction)
                {
                    Debug.Log($"[PieceCombat] Ability {ability.AbilityName} cancelled the action");
                    break;
                }
            }
            
            return context;
        }
        
        /// <summary>
        /// 능력 ID 목록 반환 (네트워크 동기화용)
        /// </summary>
        public List<string> GetAbilityIds()
        {
            return _abilities.Select(a => a.AbilityId).ToList();
        }
        
        // ========== 스탯 업그레이드 시스템 ==========
        
        /// <summary>
        /// 스탯 업그레이드 적용
        /// </summary>
        public void ApplyStatUpgrade(StatUpgradeSO upgrade)
        {
            if (upgrade == null) return;
            
            _statUpgrades.Add(upgrade);
            RecalculateBonusStats();
            
            // 최대 체력이 올랐으면 현재 체력도 증가
            if (upgrade.StatType == StatType.MaxHealth)
            {
                _currentHealth += upgrade.FlatBonus;
            }
            
            _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield);
            Debug.Log($"[PieceCombat] {_piece.name} stat upgrade applied: {upgrade.UpgradeName}");
        }
        
        /// <summary>
        /// 스탯 업그레이드 제거
        /// </summary>
        public void RemoveStatUpgrade(StatUpgradeSO upgrade)
        {
            if (upgrade == null) return;
            
            if (_statUpgrades.Remove(upgrade))
            {
                RecalculateBonusStats();
                _ui?.UpdateAll(_currentHealth, MaxHealth, AttackPower, _level, Shield);
            }
        }
        
        /// <summary>
        /// 보너스 스탯 재계산
        /// </summary>
        private void RecalculateBonusStats()
        {
            // 초기화
            _bonusMaxHealth = 0;
            _bonusAttackPower = 0;
            _bonusDefense = 0;
            _bonusShield = 0;
            _bonusHealthRegen = 0;
            _bonusLifeSteal = 0f;
            
            // 고정 보너스 적용
            foreach (var upgrade in _statUpgrades)
            {
                switch (upgrade.StatType)
                {
                    case StatType.MaxHealth:
                        _bonusMaxHealth += upgrade.FlatBonus;
                        break;
                    case StatType.Attack:
                    case StatType.AttackPower:
                        _bonusAttackPower += upgrade.FlatBonus;
                        break;
                    case StatType.Defense:
                        _bonusDefense += upgrade.FlatBonus;
                        break;
                    case StatType.Shield:
                        _bonusShield += upgrade.FlatBonus;
                        break;
                    case StatType.HealthRegeneration:
                        _bonusHealthRegen += upgrade.FlatBonus;
                        break;
                    case StatType.LifeSteal:
                        _bonusLifeSteal += upgrade.FlatBonus / 100f; // 정수를 % 로 변환
                        break;
                }
            }
            
            // 퍼센트 보너스 적용
            foreach (var upgrade in _statUpgrades)
            {
                if (upgrade.PercentBonus == 0) continue;
                
                switch (upgrade.StatType)
                {
                    case StatType.MaxHealth:
                        _bonusMaxHealth += Mathf.RoundToInt(_maxHealth * upgrade.PercentBonus);
                        break;
                    case StatType.AttackPower:
                        _bonusAttackPower += Mathf.RoundToInt(_attackPower * upgrade.PercentBonus);
                        break;
                    case StatType.Defense:
                        _bonusDefense += Mathf.RoundToInt(_defense * upgrade.PercentBonus);
                        break;
                }
            }
        }
    }
}
