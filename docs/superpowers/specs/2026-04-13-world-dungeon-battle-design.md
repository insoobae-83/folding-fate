# World System Design — Dungeon & Battle

## Overview

던전 탐험과 턴제 전투를 구현하는 World 시스템. 기존 Entity 스펙(`2026-04-06-entity-system-design.md`)의 CBD 구조를 기반으로, Dungeon과 Battle 피처를 추가한다.

포커 핸드 결과와 전투의 연결 방식은 아직 미정이며, 나중에 연결할 수 있는 확장 가능한 구조로 설계한다.

## Prerequisites

이 스펙은 기존 Entity 스펙의 다음 요소가 구현되어 있다고 가정한다:

- `Core`: `Entity`, `IEntityComponent`, `EntityStatType`
- `Features/Entity/Models`: `Stats`, `Health`, `Combat`
- `Features/Entity/Systems`: `EntityFactory`, `HealthSystem`, `StatsSystem`
- `Features/Entity/Structs`: `EntityStatModifier`
- `Features/Entity/Enums`: `ModifierSource`

## Requirements

### Dungeon

- 던전은 Dungeon → Floor → Room 계층 구조
- 디자이너가 SO로 던전 구성(층 수, 방 구성, 몬스터 풀)을 정의
- 런타임에 SO를 기반으로 DungeonModel → FloorModel → RoomModel 트리 생성
- 방 타입: Combat, Puzzle, Shop, Event, Boss
- 방 상태: Locked → Available → Active → Cleared
- 던전/층/방 진행 상태를 ReactiveProperty로 노출하여 UI 바인딩 가능

### Battle

- 턴제 N vs N 전투 (캐릭터 파티 vs 몬스터 그룹)
- 전투는 Room에서 발생 — Room이 직접 전투를 시작하지 않고, EventBus로 이벤트 발행
- 전투 전용 데이터(BattleModel, BattleAction)는 Battle 피처에, Entity 공통 데이터(Stats, Health, StatusEffect 등)는 Entity 피처에
- 전투 중에만 존재하는 상태는 Battle에, 전투 종료 후에도 유지되는 상태는 Entity에
- 포커 결과 연결 지점을 열어둠 (DamageSystem.Calculate, BattleAction 확장)

### 피처 간 의존성

- Battle → Entity (Entity의 컴포넌트를 읽고 수정)
- Dungeon → Entity (Room에 몬스터 Entity 배치)
- Battle ↔ Dungeon 직접 참조 없음 — R3 EventBus로 통신
- Poker → Battle 연결은 미정 — 나중에 추가

## Architecture

### Core Enums 추가

```csharp
namespace FoldingFate.Core
{
    public enum RoomType { Combat, Puzzle, Shop, Event, Boss }
    public enum RoomState { Locked, Available, Active, Cleared }
    public enum BattlePhase { Start, PlayerTurn, EnemyTurn, Victory, Defeat }
    public enum BattleActionType { Attack, Defend, Skill }
    public enum ActionResultType { Damage, Heal, Buff, Debuff, Miss }
}
```

### Dungeon 피처

#### Models

```csharp
using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Dungeon.Models
{
    public class DungeonModel : IDisposable
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<FloorModel> Floors { get; }
        public ReactiveProperty<int> CurrentFloorIndex { get; }

        public FloorModel CurrentFloor => Floors[CurrentFloorIndex.CurrentValue];

        public DungeonModel(string id, string displayName, IReadOnlyList<FloorModel> floors)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Floors = floors ?? throw new ArgumentNullException(nameof(floors));
            CurrentFloorIndex = new ReactiveProperty<int>(0);
        }

        public void Dispose()
        {
            CurrentFloorIndex?.Dispose();
        }
    }

    public class FloorModel : IDisposable
    {
        public int Index { get; }
        public IReadOnlyList<RoomModel> Rooms { get; }
        public ReactiveProperty<int> CurrentRoomIndex { get; }

        public RoomModel CurrentRoom => Rooms[CurrentRoomIndex.CurrentValue];
        public bool IsCleared
        {
            get
            {
                for (int i = 0; i < Rooms.Count; i++)
                {
                    if (Rooms[i].State.CurrentValue != RoomState.Cleared)
                        return false;
                }
                return true;
            }
        }

        public FloorModel(int index, IReadOnlyList<RoomModel> rooms)
        {
            Index = index;
            Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            CurrentRoomIndex = new ReactiveProperty<int>(0);
        }

        public void Dispose()
        {
            CurrentRoomIndex?.Dispose();
        }
    }

    public class RoomModel : IDisposable
    {
        public string Id { get; }
        public RoomType Type { get; }
        public ReactiveProperty<RoomState> State { get; }
        public IReadOnlyList<Entity> Entities { get; }

        public RoomModel(string id, RoomType type, IReadOnlyList<Entity> entities)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            State = new ReactiveProperty<RoomState>(RoomState.Locked);
            Entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        public void Dispose()
        {
            State?.Dispose();
        }
    }
}
```

#### Data (ScriptableObject)

```csharp
using UnityEngine;

namespace FoldingFate.Features.Dungeon.Data
{
    [CreateAssetMenu(fileName = "NewDungeon", menuName = "FoldingFate/Dungeon/DungeonData")]
    public class DungeonData : ScriptableObject
    {
        public string DisplayName;
        public FloorData[] Floors;
    }

    [CreateAssetMenu(fileName = "NewFloor", menuName = "FoldingFate/Dungeon/FloorData")]
    public class FloorData : ScriptableObject
    {
        public int RoomCount;
        public RoomType[] RoomComposition;
        public MonsterData[] AvailableMonsters;
    }
}
```

`MonsterData`는 기존 Entity 스펙의 SO 확장. 아직 미구현이므로 Entity 구현 시 함께 정의.

#### Systems

```csharp
using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;
using FoldingFate.Features.Dungeon.Data;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Dungeon.Systems
{
    /// <summary>
    /// DungeonData(SO)를 기반으로 런타임 DungeonModel 트리를 생성한다.
    /// </summary>
    public class DungeonSystem
    {
        private readonly EntityFactory _entityFactory;

        public DungeonSystem(EntityFactory entityFactory)
        {
            _entityFactory = entityFactory;
        }

        public DungeonModel Create(DungeonData data)
        {
            var floors = new List<FloorModel>();
            for (int i = 0; i < data.Floors.Length; i++)
            {
                var floorData = data.Floors[i];
                var rooms = new List<RoomModel>();
                for (int j = 0; j < floorData.RoomCount; j++)
                {
                    var roomType = floorData.RoomComposition[j];
                    var entities = CreateRoomEntities(roomType, floorData);
                    var room = new RoomModel(
                        id: Guid.NewGuid().ToString(),
                        type: roomType,
                        entities: entities.AsReadOnly()
                    );
                    rooms.Add(room);
                }
                floors.Add(new FloorModel(i, rooms.AsReadOnly()));
            }
            return new DungeonModel(
                id: Guid.NewGuid().ToString(),
                displayName: data.DisplayName,
                floors: floors.AsReadOnly()
            );
        }

        private List<Entity> CreateRoomEntities(RoomType roomType, FloorData floorData)
        {
            // Combat/Boss 방에만 몬스터 배치, 나머지는 빈 목록
            // 구체적인 배치 로직은 구현 시 결정
            return new List<Entity>();
        }
    }

    /// <summary>
    /// 층/방 이동 진행을 처리한다.
    /// </summary>
    public class FloorSystem
    {
        public void MoveToNextRoom(FloorModel floor)
        {
            var next = floor.CurrentRoomIndex.CurrentValue + 1;
            if (next < floor.Rooms.Count)
            {
                floor.CurrentRoomIndex.Value = next;
            }
        }

        public void MoveToNextFloor(DungeonModel dungeon)
        {
            var next = dungeon.CurrentFloorIndex.CurrentValue + 1;
            if (next < dungeon.Floors.Count)
            {
                dungeon.CurrentFloorIndex.Value = next;
            }
        }
    }

    /// <summary>
    /// 방 진입/클리어를 처리하고, 방 타입에 따라 EventBus로 이벤트를 발행한다.
    /// </summary>
    public class RoomSystem
    {
        // R3 Subject로 이벤트 발행 (EventBus 구현 시 교체 가능)

        public void Enter(RoomModel room)
        {
            room.State.Value = RoomState.Active;
            // RoomType에 따라 이벤트 발행:
            // Combat/Boss → CombatStartEvent
            // Shop → ShopOpenEvent
            // etc.
        }

        public void Clear(RoomModel room)
        {
            room.State.Value = RoomState.Cleared;
        }
    }
}
```

### Battle 피처

#### 3-Phase 턴 구조: Collect → Resolve → Apply

모든 전투 행동은 3단계로 처리된다:

1. **Collect** — 양측(플레이어 + 적 AI)의 `BattleAction`을 수집
2. **Resolve** — 수집된 Action을 기반으로 `ActionResult`를 생성 (데미지 계산, 효과 판정). Entity 상태는 아직 변경하지 않음
3. **Apply** — `ActionResult`를 Entity에 실제 적용 (HP 감소, 상태이상 부여 등). 결과 로그 보존

이 분리를 통해:
- 리플레이: ActionResult 로그만 재생하면 전투 재현 가능
- 되돌리기: Apply 전에 취소/수정 가능
- 동시 행동 해결: N vs N에서 양쪽 동시 사망 등 순서 문제를 Resolve 단계에서 일괄 처리
- UI 연출 분리: 결과 데이터를 기반으로 연출을 순차 재생

#### Models

```csharp
using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Models
{
    public class BattleModel : IDisposable
    {
        public string Id { get; }
        public IReadOnlyList<Entity> Allies { get; }
        public IReadOnlyList<Entity> Enemies { get; }
        public ReactiveProperty<BattlePhase> Phase { get; }
        public ReactiveProperty<int> TurnCount { get; }
        public List<TurnRecord> TurnHistory { get; } = new();

        public BattleModel(
            string id,
            IReadOnlyList<Entity> allies,
            IReadOnlyList<Entity> enemies)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Allies = allies ?? throw new ArgumentNullException(nameof(allies));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            Phase = new ReactiveProperty<BattlePhase>(BattlePhase.Start);
            TurnCount = new ReactiveProperty<int>(0);
        }

        public void Dispose()
        {
            Phase?.Dispose();
            TurnCount?.Dispose();
        }
    }

    /// <summary>
    /// Phase 1 — Collect: 플레이어/AI가 선택한 행동 의도
    /// </summary>
    public class BattleAction
    {
        public Entity Actor { get; }
        public BattleActionType ActionType { get; }
        public Entity Target { get; }

        public BattleAction(Entity actor, BattleActionType actionType, Entity target)
        {
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            ActionType = actionType;
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }
    }

    /// <summary>
    /// Phase 2 — Resolve: Action으로부터 계산된 결과. Entity 상태는 아직 미변경.
    /// </summary>
    public class ActionResult
    {
        public BattleAction Source { get; }
        public ActionResultType ResultType { get; }    // Damage, Heal, Buff, Miss 등
        public Entity Target { get; }
        public int Value { get; }                      // 데미지량, 힐량 등

        public ActionResult(BattleAction source, ActionResultType resultType, Entity target, int value)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ResultType = resultType;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Value = value;
        }
    }

    /// <summary>
    /// 한 턴의 전체 기록 — 리플레이/로그 용도
    /// </summary>
    public class TurnRecord
    {
        public int TurnNumber { get; }
        public IReadOnlyList<BattleAction> Actions { get; }
        public IReadOnlyList<ActionResult> Results { get; }

        public TurnRecord(int turnNumber, IReadOnlyList<BattleAction> actions, IReadOnlyList<ActionResult> results)
        {
            TurnNumber = turnNumber;
            Actions = actions;
            Results = results;
        }
    }
}
```

#### Events (R3 EventBus)

```csharp
using System.Collections.Generic;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Events
{
    /// <summary>
    /// Dungeon → Battle: 전투방 진입 시 발행
    /// </summary>
    public record CombatStartEvent(
        IReadOnlyList<Entity> Allies,
        IReadOnlyList<Entity> Enemies
    );

    /// <summary>
    /// Battle → Dungeon: 전투 종료 시 발행
    /// </summary>
    public record CombatEndEvent(
        string BattleId,
        bool IsVictory
    );
}
```

#### Systems

```csharp
using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Battle.Systems
{
    /// <summary>
    /// 전투 생성/종료를 관리한다.
    /// </summary>
    public class BattleSystem
    {
        public BattleModel StartBattle(
            IReadOnlyList<Entity> allies,
            IReadOnlyList<Entity> enemies)
        {
            // 모든 참여 Entity의 Combat.IsInCombat = true
            return new BattleModel(
                id: Guid.NewGuid().ToString(),
                allies: allies,
                enemies: enemies
            );
        }

        public void EndBattle(BattleModel battle)
        {
            // 모든 참여 Entity의 Combat.IsInCombat = false
            // CombatEndEvent 발행
            battle.Dispose();
        }
    }

    /// <summary>
    /// 턴 진행을 3-Phase로 관리한다.
    /// </summary>
    public class TurnSystem
    {
        private readonly ResolveSystem _resolveSystem;
        private readonly ApplySystem _applySystem;

        public TurnSystem(ResolveSystem resolveSystem, ApplySystem applySystem)
        {
            _resolveSystem = resolveSystem;
            _applySystem = applySystem;
        }

        public void StartTurn(BattleModel battle)
        {
            battle.TurnCount.Value++;
            battle.Phase.Value = BattlePhase.PlayerTurn;
        }

        /// <summary>
        /// Phase 1 완료 후 호출. 수집된 Action들을 Resolve → Apply 순으로 처리한다.
        /// </summary>
        public void ExecuteTurn(BattleModel battle, IReadOnlyList<BattleAction> actions)
        {
            // Phase 2 — Resolve
            var results = _resolveSystem.Resolve(actions);

            // Phase 3 — Apply
            _applySystem.Apply(results);

            // 턴 기록 저장
            battle.TurnHistory.Add(new TurnRecord(
                battle.TurnCount.CurrentValue,
                actions,
                results
            ));
        }

        public void EndTurn(BattleModel battle)
        {
            // 승패 판정
            // 모든 적 사망 → Phase = Victory
            // 모든 아군 사망 → Phase = Defeat
            // 아니면 다음 턴 (StartTurn 호출)
        }
    }

    /// <summary>
    /// Phase 2 — Resolve: BattleAction → ActionResult 변환.
    /// Entity 상태를 변경하지 않고 결과만 계산한다.
    /// 포커 결과 연결 시 이 클래스를 확장한다.
    /// </summary>
    public class ResolveSystem
    {
        private readonly StatsSystem _statsSystem;

        public ResolveSystem(StatsSystem statsSystem)
        {
            _statsSystem = statsSystem;
        }

        public IReadOnlyList<ActionResult> Resolve(IReadOnlyList<BattleAction> actions)
        {
            var results = new List<ActionResult>();
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var result = ResolveAction(action);
                results.Add(result);
            }
            return results.AsReadOnly();
        }

        private ActionResult ResolveAction(BattleAction action)
        {
            switch (action.ActionType)
            {
                case BattleActionType.Attack:
                    var attackerStats = action.Actor.Get<Stats>();
                    var targetStats = action.Target.Get<Stats>();
                    var attack = _statsSystem.GetValue(attackerStats, EntityStatType.Attack);
                    var defense = _statsSystem.GetValue(targetStats, EntityStatType.Defense);
                    var damage = Math.Max(0, (int)(attack - defense));
                    return new ActionResult(action, ActionResultType.Damage, action.Target, damage);

                case BattleActionType.Defend:
                    // 방어 버프 결과 생성
                    return new ActionResult(action, ActionResultType.Buff, action.Actor, 0);

                default:
                    return new ActionResult(action, ActionResultType.Miss, action.Target, 0);
            }
        }
    }

    /// <summary>
    /// Phase 3 — Apply: ActionResult를 Entity에 실제 적용한다.
    /// </summary>
    public class ApplySystem
    {
        private readonly HealthSystem _healthSystem;

        public ApplySystem(HealthSystem healthSystem)
        {
            _healthSystem = healthSystem;
        }

        public void Apply(IReadOnlyList<ActionResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                ApplyResult(results[i]);
            }
        }

        private void ApplyResult(ActionResult result)
        {
            switch (result.ResultType)
            {
                case ActionResultType.Damage:
                    var health = result.Target.Get<Health>();
                    _healthSystem.TakeDamage(health, result.Value);
                    break;

                case ActionResultType.Heal:
                    var healTarget = result.Target.Get<Health>();
                    _healthSystem.Heal(healTarget, result.Value);
                    break;

                // Buff, Debuff, Miss 등 추가 처리
            }
        }
    }
}
```

### VContainer 구성

```csharp
using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Dungeon
{
    public class DungeonInstaller : LifetimeScope
    {
        [SerializeField] private DungeonData _dungeonData;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_dungeonData);
            builder.Register<DungeonSystem>(Lifetime.Singleton);
            builder.Register<FloorSystem>(Lifetime.Singleton);
            builder.Register<RoomSystem>(Lifetime.Singleton);
        }
    }
}

namespace FoldingFate.Features.Battle
{
    public class BattleInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleSystem>(Lifetime.Singleton);
            builder.Register<TurnSystem>(Lifetime.Singleton);
            builder.Register<ResolveSystem>(Lifetime.Singleton);
            builder.Register<ApplySystem>(Lifetime.Singleton);
        }
    }
}
```

### 게임 플레이 흐름

```
1. 던전 입장
   DungeonSystem.Create(dungeonData) → DungeonModel 트리 생성

2. 방 이동
   FloorSystem.MoveToNextRoom() → RoomSystem.Enter()

3. 전투방 진입 (RoomType == Combat or Boss)
   RoomSystem.Enter() → EventBus → CombatStartEvent 발행

4. 전투 시작
   BattleSystem가 CombatStartEvent 구독
   → BattleModel 생성 (allies + room의 monster entities)

5. 턴 루프 (3-Phase)
   TurnSystem.StartTurn() → Phase = PlayerTurn
   
   Phase 1 — Collect:
     플레이어가 BattleAction 선택 + 적 AI가 BattleAction 결정
     → 양측 Action 목록 수집
   
   Phase 2 — Resolve:
     ResolveSystem.Resolve(actions) → ActionResult 목록 생성
     (Entity 상태 아직 미변경, 데미지/효과만 계산)
   
   Phase 3 — Apply:
     ApplySystem.Apply(results) → Entity에 실제 적용
     (HP 감소, 상태이상 부여 등)
   
   턴 기록 저장 → TurnRecord(actions, results) → BattleModel.TurnHistory
   TurnSystem.EndTurn() → 승패 판정 또는 다음 턴
   (매 턴 StatusEffectSystem.TickTurn() 호출)

6. 전투 종료
   BattleSystem.EndBattle() → EventBus → CombatEndEvent 발행

7. 방 클리어
   RoomSystem이 CombatEndEvent 구독 → room.State = Cleared

8. 진행
   FloorSystem.MoveToNextRoom() → 다음 방
   모든 방 클리어 → FloorSystem.MoveToNextFloor() → 다음 층
   모든 층 클리어 → 던전 클리어
```

## Folder Structure

```
Assets/Scripts/
  Core/
    Enums/
      RoomType.cs
      RoomState.cs
      BattlePhase.cs
      BattleActionType.cs
      ActionResultType.cs

  Features/
    Dungeon/
      Models/
        DungeonModel.cs
        FloorModel.cs
        RoomModel.cs
      Systems/
        DungeonSystem.cs
        FloorSystem.cs
        RoomSystem.cs
      Data/
        DungeonData.cs
        FloorData.cs
      DungeonInstaller.cs
    Battle/
      Models/
        BattleModel.cs
        BattleAction.cs
        ActionResult.cs
        TurnRecord.cs
      Systems/
        BattleSystem.cs
        TurnSystem.cs
        ResolveSystem.cs
        ApplySystem.cs
      Events/
        CombatStartEvent.cs
        CombatEndEvent.cs
      BattleInstaller.cs

Assets/Tests/
  EditMode/
    Dungeon/
      DungeonSystemTests.cs
      FloorSystemTests.cs
      RoomSystemTests.cs
      DungeonModelTests.cs
      FloorModelTests.cs
      RoomModelTests.cs
    Battle/
      BattleSystemTests.cs
      TurnSystemTests.cs
      ResolveSystemTests.cs
      ApplySystemTests.cs
      BattleModelTests.cs
      TurnRecordTests.cs
```

## Assembly Definition References

기존 asmdef 사용 (CLAUDE.md 기준):

- `FoldingFate.Core`: 새 enum 추가 (RoomType, RoomState, BattlePhase, BattleActionType, ActionResultType)
- `FoldingFate.Features`: Dungeon, Battle 피처 추가. Entity 피처 참조.
- `FoldingFate.Tests.EditMode`: 테스트 추가

Features가 단일 asmdef이므로 Battle → Entity, Dungeon → Entity 참조는 같은 어셈블리 내에서 자연스럽게 해결된다. Battle ↔ Dungeon 간 직접 참조는 코드 리뷰로 방지하고, EventBus(Infrastructure)를 통해서만 통신한다.

## Test Strategy

EditMode 단위 테스트 (순수 C# 모델 + 시스템).

### DungeonModel Tests
- 생성 시 CurrentFloorIndex == 0
- CurrentFloor가 올바른 FloorModel 반환
- Dispose 시 ReactiveProperty 정리

### FloorModel Tests
- 생성 시 CurrentRoomIndex == 0
- CurrentRoom이 올바른 RoomModel 반환
- IsCleared: 모든 방 Cleared일 때 true, 아니면 false

### RoomModel Tests
- 생성 시 State == Locked
- Entities 목록 접근 가능

### DungeonSystem Tests
- Create: DungeonData 기반으로 올바른 계층 구조 생성
- 층 수, 방 수, 방 타입이 DungeonData와 일치

### FloorSystem Tests
- MoveToNextRoom: CurrentRoomIndex 증가
- MoveToNextRoom: 마지막 방에서 호출 시 인덱스 유지
- MoveToNextFloor: CurrentFloorIndex 증가
- MoveToNextFloor: 마지막 층에서 호출 시 인덱스 유지

### RoomSystem Tests
- Enter: State → Active
- Clear: State → Cleared

### BattleModel Tests
- 생성 시 Phase == Start, TurnCount == 0
- Allies, Enemies 목록 접근 가능
- Dispose 시 ReactiveProperty 정리

### BattleSystem Tests
- StartBattle: BattleModel 생성, 참여 Entity의 Combat.IsInCombat == true
- EndBattle: 참여 Entity의 Combat.IsInCombat == false

### TurnSystem Tests
- StartTurn: TurnCount 증가, Phase → PlayerTurn
- ExecuteTurn: actions → Resolve → Apply 순서 호출, TurnRecord가 TurnHistory에 추가
- EndTurn: 모든 적 사망 시 Phase → Victory
- EndTurn: 모든 아군 사망 시 Phase → Defeat
- EndTurn: 양쪽 생존 시 Phase 전환 (PlayerTurn → EnemyTurn, EnemyTurn → PlayerTurn)

### ResolveSystem Tests
- Resolve(Attack): Attack - Defense = Damage ActionResult 반환
- Resolve(Attack): Defense > Attack일 때 Value == 0
- Resolve(Defend): Buff ActionResult 반환
- Resolve: Entity 상태가 변경되지 않음 (HP 등 그대로)
- Resolve: 여러 Action 입력 시 같은 수의 ActionResult 반환

### ApplySystem Tests
- Apply(Damage): target의 Health.CurrentHp 감소
- Apply(Heal): target의 Health.CurrentHp 증가 (MaxHp 초과 안 함)
- Apply: 여러 ActionResult 순차 적용

### TurnRecord Tests
- TurnRecord가 Actions, Results를 올바르게 보존
- BattleModel.TurnHistory에 턴별 기록 누적

## Open Decisions

- [ ] 포커 핸드 결과 → 전투 연결 방식 (ResolveSystem 확장 또는 BattleAction에 포커 데이터 주입)
- [ ] 적 AI 행동 결정 로직
- [ ] MonsterData ScriptableObject 상세 필드 (Entity 스펙과 함께 구체화)
- [ ] 방 랜덤 배치 / 맵 생성 알고리즘
- [ ] 전투/던전 UI (ViewModel + View)
- [ ] StatusEffect 턴 틱 처리의 구체적 타이밍 (턴 시작 vs 턴 종료)
- [ ] EventBus 구현 (Infrastructure/EventBus — R3 Subject 기반)
