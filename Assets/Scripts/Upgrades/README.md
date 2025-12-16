# LevelUpChess 업그레이드 시스템 기술 문서

## 개요

뱀파이어 서바이버 스타일의 업그레이드 시스템으로, 체스 기물이 레벨업할 때 3가지 선택지 중 하나를 선택하여 강화할 수 있습니다.

**핵심 특징:**
- **공통 업그레이드**: 모든 피스에 적용 가능한 범용 강화
- **피스별 전용 업그레이드**: 특정 피스에만 적용되는 고유 강화
- **가중치 기반 뽑기**: 희귀도, 타입, 공통/전용 비율에 따른 확률 조정

## 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│                        UpgradeManager                           │
│  (네트워크 동기화, 업그레이드 선택/적용 관리)                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        UpgradePoolSO                            │
│  (공통/피스별 풀 관리, 가중치 기반 뽑기 시스템)                    │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ 공통 풀     │ │ 전역 풀     │ │ 피스별 풀   │               │
│  │ Movement    │ │ Global      │ │ Pawn        │               │
│  │ Stat        │ │ Upgrades    │ │ Knight      │               │
│  │ Ability     │ │             │ │ Bishop      │               │
│  │             │ │             │ │ Rook/Queen  │               │
│  │             │ │             │ │ King        │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
└─────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
    ┌──────────┐       ┌──────────┐       ┌──────────┐
    │ Movement │       │   Stat   │       │ Ability  │
    │ Upgrades │       │ Upgrades │       │ Upgrades │
    └──────────┘       └──────────┘       └──────────┘
          │                   │                   │
          ▼                   ▼                   ▼
    ┌──────────┐       ┌──────────┐       ┌──────────┐
    │ChessPiece│       │PieceCombat│      │PieceCombat│
    │_dynamics │       │ BonusStats│      │ _abilities│
    └──────────┘       └──────────┘       └──────────┘
```

---

## 뽑기 풀 시스템

### 풀 구조

```
UpgradePoolSO
├── 공통 업그레이드 (Common Pool)
│   ├── commonMovementUpgrades[]  - 모든 피스 적용 가능한 행마법
│   ├── commonStatUpgrades[]      - 모든 피스 적용 가능한 스탯 강화
│   └── commonAbilityUpgrades[]   - 모든 피스 적용 가능한 능력
│
├── 전역 업그레이드 (Global Pool)
│   └── globalUpgrades[]          - 팀 전체에 영향
│
└── 피스별 전용 업그레이드 (Piece-Specific Pools)
    ├── pawnUpgrades
    │   ├── movementUpgrades[]    - 폰 전용 행마법 (백스탭 등)
    │   ├── abilityUpgrades[]     - 폰 전용 능력
    │   └── statUpgrades[]        - 폰 전용 스탯 강화
    ├── knightUpgrades
    ├── bishopUpgrades
    ├── rookUpgrades              - 룩 전용 (비숍 어택 등)
    ├── queenUpgrades             - 퀸 전용 (나이트 무브 등)
    └── kingUpgrades              - 킹 전용 (제한된 비숍 등)
```

### 가중치 시스템

```csharp
UpgradeWeightSettings
├── 타입별 가중치
│   ├── movementWeight = 1.0   // 행마법 등장 확률
│   ├── statWeight = 2.0       // 스탯 강화 등장 확률 (2배)
│   └── abilityWeight = 1.5    // 능력 등장 확률
│
├── 희귀도별 가중치
│   ├── commonWeight = 50      // Common 등장 확률
│   ├── uncommonWeight = 30    // Uncommon 등장 확률
│   ├── rareWeight = 15        // Rare 등장 확률
│   ├── epicWeight = 4         // Epic 등장 확률
│   └── legendaryWeight = 1    // Legendary 등장 확률
│
└── 공통/전용 비율
    └── commonPoolChance = 0.5  // 50% 공통, 50% 전용
```

### 뽑기 로직

```csharp
// 가중치 계산
weight = rarityWeight × typeWeight × poolChance

// 예시: Uncommon Stat 공통 업그레이드
// weight = 30 × 2.0 × 0.5 = 30

// 예시: Epic Movement 전용 업그레이드  
// weight = 4 × 1.0 × 0.5 = 2
```

### 사용 방법

```csharp
// 가중치 기반 뽑기 (추천)
var results = upgradePool.DrawUpgrades(piece, count: 3, excludeIds, maxRarity);

foreach (var result in results)
{
    Debug.Log($"{result.Upgrade.UpgradeName}");
    Debug.Log($"공통 풀: {result.IsFromCommonPool}");
    Debug.Log($"가중치: {result.DrawWeight}");
}

// 피스별 풀 직접 접근
var pawnPool = upgradePool.GetPiecePool(PieceType.Pawn);
var pawnUpgrades = pawnPool.GetAllUpgrades();

// 공통 업그레이드만 가져오기
var commonUpgrades = upgradePool.GetAllCommonUpgrades();
```

---

## 디렉토리 구조

```
Assets/Scripts/Upgrades/
├── Core/
│   ├── UpgradeEnums.cs          # 열거형 정의
│   ├── AbilityContext.cs        # 능력 실행 컨텍스트
│   ├── IAbility.cs              # 능력 인터페이스
│   ├── UpgradeBaseSO.cs         # 업그레이드 기본 클래스
│   └── AbilityBaseSO.cs         # 능력 업그레이드 기본 클래스
├── Movement/
│   ├── MovementUpgradeSO.cs     # 행마법 업그레이드 기본
│   ├── PawnBackstepUpgradeSO.cs # 폰 백스탭
│   ├── RookBishopAttackUpgradeSO.cs
│   ├── QueenKnightMoveUpgradeSO.cs
│   └── KingLimitedBishopUpgradeSO.cs
├── Stat/
│   └── StatUpgradeSO.cs         # 스탯 업그레이드
├── Abilities/
│   ├── HitAndRunAbilitySO.cs    # 히트앤런
│   ├── AutoMoveAbilitySO.cs     # 자동 이동
│   ├── ShoulderBashAbilitySO.cs # 어깨빵
│   ├── MothersProtectionAbilitySO.cs # 어머니의 보호
│   ├── VampirismAbilitySO.cs    # 흡혈
│   └── BerserkerAbilitySO.cs    # 광전사
├── Global/
│   ├── GlobalUpgradeSO.cs       # 전역 업그레이드 기본
│   ├── GambitUpgradeSO.cs       # 갬빗
│   ├── MachoismUpgradeSO.cs     # 마쵸이즘
│   ├── SolidarityUpgradeSO.cs   # 연대의 힘
│   └── KingsPrestigeUpgradeSO.cs # 왕의 위엄
├── UI/
│   ├── UpgradeCardUI.cs         # 업그레이드 카드
│   └── UpgradeSelectionPanelUI.cs # 선택 패널
├── UpgradePoolSO.cs             # 업그레이드 풀 (공통/피스별)
├── UpgradePoolData.cs           # 풀 데이터 구조체
├── UpgradeManager.cs            # 매니저
└── README.md                    # 이 문서
```

---

## Core 클래스

### UpgradeEnums.cs

```csharp
// 업그레이드 종류
public enum UpgradeType
{
    Movement,   // 행마법 추가
    Stat,       // 스탯 증가
    Ability,    // 특수 능력
    Global      // 팀 전체 영향
}

// 적용 대상
public enum UpgradeTarget
{
    Piece,      // 특정 기물
    Player,     // 플레이어 (미래 확장)
    AllPieces   // 모든 기물 (전역)
}

// 기물 타입 필터
public enum PieceTypeFilter
{
    All, Pawn, Knight, Bishop, Rook, Queen, King
}

// 능력 발동 시점
public enum AbilityTrigger
{
    OnAttackStart,    // 공격 시작
    OnAttackEnd,      // 공격 종료
    OnKill,           // 처치 시
    OnDamaged,        // 피해 받을 때
    OnBeforeMove,     // 이동 전
    OnAfterMove,      // 이동 후
    OnTurnStart,      // 턴 시작
    OnTurnEnd,        // 턴 종료
    OnDeath,          // 사망 시
    Passive           // 상시 적용
}
```

### AbilityContext.cs

능력 실행 시 필요한 모든 정보를 담는 컨텍스트 객체:

```csharp
public class AbilityContext
{
    public PieceCombat Owner;           // 능력 소유자
    public PieceCombat Target;          // 대상
    public Tile FromTile;               // 시작 타일
    public Tile ToTile;                 // 도착 타일
    public int Damage;                  // 피해량 (수정 가능)
    public bool TargetDied;             // 대상 사망 여부
    public bool CancelAction;           // 행동 취소 플래그
    public bool PreventMoveAfterKill;   // 처치 후 이동 방지
    public float DamageMultiplier;      // 데미지 배율
    public int DamageBonus;             // 추가 데미지
    public Dictionary<string, object> AdditionalData; // 추가 데이터
}
```

### IAbility.cs

모든 능력이 구현해야 할 인터페이스:

```csharp
public interface IAbility
{
    string AbilityId { get; }
    string AbilityName { get; }
    string Description { get; }
    AbilityTrigger Trigger { get; }
    
    void OnApply(PieceCombat combat);   // 능력 적용 시
    void OnRemove(PieceCombat combat);  // 능력 제거 시
    void Execute(AbilityContext context); // 능력 실행
}
```

---

## 업그레이드 타입별 상세

### 1. Movement Upgrade (행마법)

새로운 이동 방식을 기물에 추가합니다.

**MoveType 시스템:**
| MoveType | 설명 |
|----------|------|
| `Normal` | 이동 + 공격 모두 가능 |
| `MoveOnly` | 이동만 가능 (공격 불가) |
| `AttackOnly` | 공격만 가능 (이동 불가) |

**피스별 전용 행마법:**

| 기물 | 업그레이드 | 효과 | MoveType |
|------|-----------|------|----------|
| 폰 | 백스탭 | 한 칸 뒤로 이동 | MoveOnly |
| 룩 | 비숍 어택 | 대각선 공격 | AttackOnly |
| 퀸 | 나이트 무브 | L자 이동 | MoveOnly |
| 킹 | 비숍 발걸음 | 대각선 3칸 | Normal |

**사용 예시:**
```csharp
// MovementUpgradeSO 생성
[CreateAssetMenu(fileName = "NewMovement", menuName = "LevelUpChess/Upgrades/Movement/Custom")]
public class CustomMovementUpgradeSO : MovementUpgradeSO
{
    // movementToAdd 필드에 PieceMovementSO 할당
}
```

### 2. Stat Upgrade (스탯)

기물의 능력치를 증가시킵니다.

**지원 스탯:**
```csharp
public enum StatType
{
    MaxHealth,          // 최대 체력
    AttackPower,        // 공격력
    Defense,            // 방어력
    Shield,             // 보호막
    HealthRegeneration, // 체력 재생
    LifeSteal           // 흡혈
}
```

**적용 방식:**
- `flatBonus`: 고정 수치 증가 (+10)
- `percentBonus`: 비율 증가 (+20%)

**사용 예시:**
```csharp
// ScriptableObject 생성 후 Inspector에서 설정:
// - statType: AttackPower
// - flatBonus: 5
// - percentBonus: 0.1 (10%)
```

### 3. Ability Upgrade (특수 능력)

기물에 특수 능력을 부여합니다.

**구현된 능력:**

| 능력 | 트리거 | 효과 |
|------|--------|------|
| 히트앤런 | OnKill | 처치 후 원래 위치로 복귀 |
| 자동 전진 | OnTurnEnd | 자동으로 전진/공격 |
| 어깨빵 | OnAfterMove | 이동 후 인접 적에게 데미지 |
| 어머니의 보호 | OnDamaged | 치명적 피해 3회 방어 |
| 흡혈 | OnAttackEnd | 피해량의 일부 회복 |
| 광전사 | OnAttackStart | 낮은 체력 = 높은 공격력 |

**능력 구현 예시:**
```csharp
[CreateAssetMenu(fileName = "CustomAbility", menuName = "LevelUpChess/Upgrades/Abilities/Custom")]
public class CustomAbilitySO : AbilityBaseSO
{
    public override string AbilityId => "ability_custom";
    public override string AbilityName => "커스텀 능력";
    public override string Description => "설명";

    public override void OnApply(PieceCombat combat)
    {
        // 능력 적용 시 초기화
    }

    public override void Execute(AbilityContext context)
    {
        // 트리거 발동 시 실행되는 로직
    }
}
```

### 4. Global Upgrade (전역 강화)

팀 전체에 영향을 미치는 업그레이드입니다.

**구현된 전역 강화:**

| 강화 | 효과 | 부작용 |
|------|------|--------|
| 갬빗 | 생존 폰 2배 보상 | 폰 50% 희생 |
| 마쵸이즘 | 공격력 2배 | 후진 불가 |
| 연대의 힘 | 인접 아군당 버프 | 없음 |
| 왕의 위엄 | 킹 주변 오라 | 없음 |

---

## PieceCombat 연동

### 능력 시스템

```csharp
// 능력 추가
combat.AddAbility(ability);

// 능력 제거
combat.RemoveAbility(abilityId);

// 특정 트리거의 능력 실행
combat.TriggerAbilities(AbilityTrigger.OnKill, context);

// 적용된 능력 ID 목록
var ids = combat.GetAbilityIds();
```

### 스탯 업그레이드 시스템

```csharp
// 스탯 업그레이드 적용
combat.ApplyStatUpgrade(statUpgrade);

// 스탯 업그레이드 제거
combat.RemoveStatUpgrade(upgradeId);

// 보너스 스탯은 자동으로 기본 스탯에 합산됨
int totalAttack = combat.AttackPower; // base + bonus
```

### 보너스 스탯

```csharp
// PieceCombat 내부 구조
private int _bonusMaxHealth;
private int _bonusAttackPower;
private int _bonusDefense;
private int _bonusShield;
private int _bonusHealthRegen;
private float _bonusLifeSteal;

// 프로퍼티에서 자동 합산
public int AttackPower => _baseAttackPower + _bonusAttackPower;
```

---

## ChessPiece 연동

### 동적 이동 전략

```csharp
// 행마법 추가
piece.AddMovementStrategy(movementSO);

// 행마법 제거
piece.RemoveMovementStrategy(movementSO);

// 모든 이동 전략 (기본 + 동적)
var allMoves = piece.GetAllMovementStrategies();

// 동적 이동만 초기화
piece.ClearDynamicMovements();
```

---

## UpgradeManager 사용법

### 초기화

1. 씬에 `UpgradeManager` 컴포넌트가 있는 GameObject 배치
2. `upgradePool` 필드에 `UpgradePoolSO` 할당
3. `boardManager` 참조 설정

### 이벤트 구독

```csharp
// 업그레이드 선택지 표시 시
UpgradeManager.Instance.OnUpgradeSelectionAvailable += (upgrades, piece) => {
    // UI 표시
};

// 업그레이드 적용 완료 시
UpgradeManager.Instance.OnUpgradeApplied += (upgrade, piece) => {
    // 효과 표시
};
```

### 업그레이드 선택

```csharp
// 플레이어가 선택 (UI에서 호출)
UpgradeManager.Instance.SelectUpgrade(selectedIndex);

// 선택 취소
UpgradeManager.Instance.CancelSelection();
```

### 전역 업그레이드 적용

```csharp
// 게임 시작 시 또는 특수 이벤트
UpgradeManager.Instance.ApplyGlobalUpgrade(teamId, globalUpgrade, teamPieces);
```

---

## 네트워크 동기화

### 흐름

```
1. 기물 레벨업 (PieceLevelUpEvent)
        ↓
2. 서버: 적용 가능한 업그레이드 필터링
        ↓
3. 서버: 3개 랜덤 선택
        ↓
4. 서버 → 클라이언트: 선택지 인덱스 전송 (ClientRpc)
        ↓
5. 클라이언트: UI 표시
        ↓
6. 클라이언트 → 서버: 선택 전송 (ServerRpc)
        ↓
7. 서버: 업그레이드 적용
        ↓
8. 서버 → 모든 클라이언트: 적용 알림 (ClientRpc)
```

### 주요 RPC

```csharp
// 서버 → 특정 클라이언트: 선택지 표시
[ClientRpc]
void ShowUpgradeSelectionClientRpc(ulong pieceNetworkId, int[] upgradeIndices, ClientRpcParams params)

// 클라이언트 → 서버: 선택 전송
[ServerRpc(RequireOwnership = false)]
void SelectUpgradeServerRpc(ulong pieceNetworkId, int upgradeIndex, ServerRpcParams params)

// 서버 → 모든 클라이언트: 적용 알림
[ClientRpc]
void NotifyUpgradeAppliedClientRpc(ulong pieceNetworkId, int upgradeIndex)
```

---

## UI 시스템

### UpgradeCardUI

개별 업그레이드 카드를 표시합니다.

```csharp
// 카드 설정
card.Setup(upgrade, index);

// 선택 이벤트
card.OnCardSelected += (index) => { /* 처리 */ };
```

### UpgradeSelectionPanelUI

3개의 카드를 표시하는 패널입니다.

```csharp
// 자동으로 UpgradeManager 이벤트 구독
// OnUpgradeSelectionAvailable → Show()
// OnUpgradeApplied → Hide()
```

---

## 새 업그레이드 추가 가이드

### 1. 스탯 업그레이드 추가

```
1. Project 창에서 우클릭
2. Create > LevelUpChess > Upgrades > Stat Upgrade
3. Inspector에서 설정:
   - upgradeId: "stat_custom_xxx"
   - upgradeName: "표시 이름"
   - statType: 원하는 스탯
   - flatBonus / percentBonus 설정
4. UpgradePoolSO의 statUpgrades에 추가
```

### 2. 행마법 업그레이드 추가

```
1. 새 PieceMovementSO 생성 (필요시)
   - Create > Chess > Piece Movement > Upgradable > [타입]
   - moveType 설정 (Normal/MoveOnly/AttackOnly)

2. MovementUpgradeSO 생성
   - Create > LevelUpChess > Upgrades > Movement Upgrade
   - movementToAdd에 위에서 만든 Movement 할당

3. UpgradePoolSO의 movementUpgrades에 추가
```

### 3. 능력 업그레이드 추가

```
1. 새 AbilityBaseSO 상속 클래스 작성
2. AbilityId, AbilityName, Description 구현
3. OnApply, OnRemove, Execute 구현
4. CreateAssetMenu 어트리뷰트 추가
5. ScriptableObject 생성 후 UpgradePoolSO에 추가
```

### 4. 전역 업그레이드 추가

```
1. GlobalUpgradeSO 상속 클래스 작성
2. ApplyToTeam, RemoveFromTeam 오버라이드
3. CreateAssetMenu 어트리뷰트 추가
4. ScriptableObject 생성 후 UpgradePoolSO에 추가
```

---

## 희귀도 시스템

```csharp
// 희귀도 등급 (0-4)
0: Common (일반) - 회색
1: Uncommon (고급) - 초록
2: Rare (희귀) - 파랑
3: Epic (영웅) - 보라
4: Legendary (전설) - 금색

// 레벨에 따른 희귀도 해금
maxRarity = baseMaxRarity + (level - 1) * rarityIncreasePerLevel
```

---

## 성능 고려사항

1. **ScriptableObject 캐싱**: 모든 업그레이드는 SO로 에셋화되어 메모리 효율적
2. **인덱스 기반 네트워크 전송**: 전체 데이터 대신 풀 인덱스만 전송
3. **이벤트 기반 시스템**: 폴링 없이 필요할 때만 처리
4. **풀링된 UI**: 카드 UI는 미리 생성 후 재사용

---

## 향후 확장 계획

- [ ] 업그레이드 조합 시너지
- [ ] 업그레이드 레벨업 (같은 업그레이드 중첩)
- [ ] 업그레이드 상점 시스템
- [ ] 업그레이드 해금 조건
- [ ] 업그레이드 프리셋 저장/로드
