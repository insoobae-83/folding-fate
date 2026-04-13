# World System (Entity + Dungeon + Battle) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CBD 기반 Entity 모듈, Dungeon(Dungeon→Floor→Room) 탐험 구조, 3-Phase 턴제 전투 시스템을 구현한다.

**Architecture:** Entity는 Id+Type+DisplayName + Dictionary 컴포넌트 컨테이너로 CBD를 구현한다. Dungeon은 SO 설계도로부터 런타임 모델 트리를 생성한다. Battle은 Collect→Resolve→Apply 3-Phase 구조로 턴을 처리하며, ActionResult/TurnRecord로 리플레이 가능한 로그를 남긴다. 모든 모델은 Anemic Domain Model이고 로직은 System에 둔다.

**Tech Stack:** Unity 6 (C#), R3 (ReactiveProperty), VContainer (DI), NUnit (EditMode tests)

**Specs:**
- `docs/superpowers/specs/2026-04-06-entity-system-design.md` — Entity CBD 구조
- `docs/superpowers/specs/2026-04-13-world-dungeon-battle-design.md` — Dungeon + Battle

---

## File Map

### Core 레이어 (`Assets/Scripts/Core/`)

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Interfaces/IEntityComponent.cs` | 모든 엔티티 컴포넌트의 기반 인터페이스 |
| Create | `Entity/Entity.cs` | CBD 컨테이너 — Id, Type, DisplayName + 컴포넌트 Dictionary |
| Create | `Enums/EntityType.cs` | Character, Monster |
| Create | `Enums/EntityStatType.cs` | MaxHp, Attack, Defense, Speed 등 |
| Create | `Enums/RoomType.cs` | Combat, Puzzle, Shop, Event, Boss |
| Create | `Enums/RoomState.cs` | Locked, Available, Active, Cleared |
| Create | `Enums/BattlePhase.cs` | Start, PlayerTurn, EnemyTurn, Victory, Defeat |
| Create | `Enums/BattleActionType.cs` | Attack, Defend, Skill |
| Create | `Enums/ActionResultType.cs` | Damage, Heal, Buff, Debuff, Miss |

### Entity 피처 (`Assets/Scripts/Features/Entity/`)

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Enums/ModifierSource.cs` | Equipment, Buff, Debuff, CardEffect, Passive |
| Create | `Structs/EntityStatModifier.cs` | StatType + Value + Source + SourceId |
| Create | `Models/Stats.cs` | 엔티티 컴포넌트: BaseStats Dictionary + Modifiers List |
| Create | `Models/Health.cs` | 엔티티 컴포넌트: CurrentHp, MaxHp, IsAlive/IsDead |
| Create | `Models/Combat.cs` | 엔티티 컴포넌트: IsInCombat flag |
| Create | `Systems/StatsSystem.cs` | GetValue(base+modifiers), AddModifier, RemoveModifiers |
| Create | `Systems/HealthSystem.cs` | TakeDamage, Heal, SetMaxHp |
| Create | `Systems/EntityFactory.cs` | CreateCombatEntity, 편의 팩토리 |

### Dungeon 피처 (`Assets/Scripts/Features/Dungeon/`)

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Models/RoomModel.cs` | Id, RoomType, State(RP), Entities |
| Create | `Models/FloorModel.cs` | Index, Rooms, CurrentRoomIndex(RP), IsCleared |
| Create | `Models/DungeonModel.cs` | Id, DisplayName, Floors, CurrentFloorIndex(RP) |
| Create | `Systems/RoomSystem.cs` | Enter(→Active), Clear(→Cleared) |
| Create | `Systems/FloorSystem.cs` | MoveToNextRoom, MoveToNextFloor |
| Create | `Systems/DungeonSystem.cs` | Create(DungeonData→DungeonModel 트리) |

### Battle 피처 (`Assets/Scripts/Features/Battle/`)

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Models/BattleAction.cs` | Phase 1 — Actor, ActionType, Target |
| Create | `Models/ActionResult.cs` | Phase 2 — Source, ResultType, Target, Value |
| Create | `Models/TurnRecord.cs` | TurnNumber, Actions, Results |
| Create | `Models/BattleModel.cs` | Id, Allies, Enemies, Phase(RP), TurnCount(RP), TurnHistory |
| Create | `Systems/ResolveSystem.cs` | Phase 2 — BattleAction → ActionResult 변환 |
| Create | `Systems/ApplySystem.cs` | Phase 3 — ActionResult → Entity 상태 적용 |
| Create | `Systems/TurnSystem.cs` | StartTurn, ExecuteTurn(Resolve→Apply), EndTurn(승패 판정) |
| Create | `Systems/BattleSystem.cs` | StartBattle, EndBattle |
| Create | `Events/CombatStartEvent.cs` | Dungeon→Battle 이벤트 |
| Create | `Events/CombatEndEvent.cs` | Battle→Dungeon 이벤트 |

### Tests (`Assets/Tests/EditMode/`)

| Action | Path |
|--------|------|
| Create | `Entity/EntityTests.cs` |
| Create | `Entity/StatsSystemTests.cs` |
| Create | `Entity/HealthSystemTests.cs` |
| Create | `Entity/EntityFactoryTests.cs` |
| Create | `Dungeon/RoomModelTests.cs` |
| Create | `Dungeon/FloorModelTests.cs` |
| Create | `Dungeon/DungeonModelTests.cs` |
| Create | `Dungeon/RoomSystemTests.cs` |
| Create | `Dungeon/FloorSystemTests.cs` |
| Create | `Dungeon/DungeonSystemTests.cs` |
| Create | `Battle/BattleActionTests.cs` |
| Create | `Battle/ActionResultTests.cs` |
| Create | `Battle/TurnRecordTests.cs` |
| Create | `Battle/BattleModelTests.cs` |
| Create | `Battle/ResolveSystemTests.cs` |
| Create | `Battle/ApplySystemTests.cs` |
| Create | `Battle/TurnSystemTests.cs` |
| Create | `Battle/BattleSystemTests.cs` |

---

## Task 1: Core — Entity, IEntityComponent, Enums

Entity CBD의 기반. Core asmdef은 `noEngineReferences: true`이므로 순수 C#만 사용.

**Files:**
- Create: `Assets/Scripts/Core/Interfaces/IEntityComponent.cs`
- Create: `Assets/Scripts/Core/Entity/Entity.cs`
- Create: `Assets/Scripts/Core/Enums/EntityType.cs`
- Create: `Assets/Scripts/Core/Enums/EntityStatType.cs`
- Test: `Assets/Tests/EditMode/Entity/EntityTests.cs`

- [ ] **Step 1: Core Enums 생성**

```csharp
// Assets/Scripts/Core/Enums/EntityType.cs
namespace FoldingFate.Core
{
    public enum EntityType
    {
        Character,
        Monster
    }
}
```

```csharp
// Assets/Scripts/Core/Enums/EntityStatType.cs
namespace FoldingFate.Core
{
    public enum EntityStatType
    {
        MaxHp,
        Attack,
        Defense,
        Speed
    }
}
```

- [ ] **Step 2: IEntityComponent 인터페이스 생성**

```csharp
// Assets/Scripts/Core/Interfaces/IEntityComponent.cs
namespace FoldingFate.Core
{
    public interface IEntityComponent
    {
        Entity Owner { get; set; }
    }
}
```

- [ ] **Step 3: Entity 클래스 생성**

```csharp
// Assets/Scripts/Core/Entity/Entity.cs
using System;
using System.Collections.Generic;

namespace FoldingFate.Core
{
    public class Entity
    {
        public string Id { get; }
        public EntityType Type { get; }
        public string DisplayName { get; }
        private readonly Dictionary<Type, IEntityComponent> _components = new();

        public Entity(string id, EntityType type, string displayName)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Type = type;
        }

        public void Add<T>(T component) where T : class, IEntityComponent
        {
            _components[typeof(T)] = component;
            component.Owner = this;
        }

        public T Get<T>() where T : class, IEntityComponent
        {
            _components.TryGetValue(typeof(T), out var component);
            return component as T;
        }

        public bool Has<T>() where T : class, IEntityComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        public bool Remove<T>() where T : class, IEntityComponent
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                component.Owner = null;
                return _components.Remove(typeof(T));
            }
            return false;
        }
    }
}
```

- [ ] **Step 4: Entity 테스트 작성**

```csharp
// Assets/Tests/EditMode/Entity/EntityTests.cs
using NUnit.Framework;
using FoldingFate.Core;

namespace FoldingFate.Tests.EditMode.Entity
{
    // 테스트 전용 더미 컴포넌트
    public class DummyComponent : IEntityComponent
    {
        public Entity Owner { get; set; }
        public int Value { get; set; }
    }

    public class AnotherDummyComponent : IEntityComponent
    {
        public Entity Owner { get; set; }
    }

    [TestFixture]
    public class EntityTests
    {
        private Core.Entity _entity;

        [SetUp]
        public void SetUp()
        {
            _entity = new Core.Entity("test-id", EntityType.Character, "TestEntity");
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            Assert.AreEqual("test-id", _entity.Id);
            Assert.AreEqual(EntityType.Character, _entity.Type);
            Assert.AreEqual("TestEntity", _entity.DisplayName);
        }

        [Test]
        public void Add_ThenGet_ReturnsComponent()
        {
            var comp = new DummyComponent { Value = 42 };
            _entity.Add(comp);
            var result = _entity.Get<DummyComponent>();
            Assert.IsNotNull(result);
            Assert.AreEqual(42, result.Value);
        }

        [Test]
        public void Add_SetsOwner()
        {
            var comp = new DummyComponent();
            _entity.Add(comp);
            Assert.AreEqual(_entity, comp.Owner);
        }

        [Test]
        public void Has_ReturnsTrueAfterAdd()
        {
            _entity.Add(new DummyComponent());
            Assert.IsTrue(_entity.Has<DummyComponent>());
        }

        [Test]
        public void Has_ReturnsFalseBeforeAdd()
        {
            Assert.IsFalse(_entity.Has<DummyComponent>());
        }

        [Test]
        public void Get_ReturnsNullWhenNotAdded()
        {
            Assert.IsNull(_entity.Get<DummyComponent>());
        }

        [Test]
        public void Remove_ReturnsTrueAndClearsOwner()
        {
            var comp = new DummyComponent();
            _entity.Add(comp);
            var removed = _entity.Remove<DummyComponent>();
            Assert.IsTrue(removed);
            Assert.IsNull(comp.Owner);
            Assert.IsFalse(_entity.Has<DummyComponent>());
        }

        [Test]
        public void Remove_ReturnsFalseWhenNotPresent()
        {
            Assert.IsFalse(_entity.Remove<DummyComponent>());
        }

        [Test]
        public void Add_OverwritesPreviousComponent()
        {
            _entity.Add(new DummyComponent { Value = 1 });
            _entity.Add(new DummyComponent { Value = 2 });
            Assert.AreEqual(2, _entity.Get<DummyComponent>().Value);
        }

        [Test]
        public void MultipleComponentTypes_Independent()
        {
            _entity.Add(new DummyComponent());
            _entity.Add(new AnotherDummyComponent());
            Assert.IsTrue(_entity.Has<DummyComponent>());
            Assert.IsTrue(_entity.Has<AnotherDummyComponent>());
        }
    }
}
```

- [ ] **Step 5: Unity MCP로 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Entity.EntityTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Core/Enums/EntityType.cs Assets/Scripts/Core/Enums/EntityStatType.cs \
  Assets/Scripts/Core/Interfaces/IEntityComponent.cs Assets/Scripts/Core/Entity/Entity.cs \
  Assets/Tests/EditMode/Entity/EntityTests.cs
# .meta 파일도 함께 추가
git add Assets/Scripts/Core/Enums/EntityType.cs.meta Assets/Scripts/Core/Enums/EntityStatType.cs.meta \
  Assets/Scripts/Core/Interfaces/ Assets/Scripts/Core/Entity/ \
  Assets/Tests/EditMode/Entity/
git commit -m "feat(entity): add Entity CBD container with IEntityComponent interface"
```

---

## Task 2: Entity 컴포넌트 — Stats, Health, Combat

Entity에 조합할 컴포넌트 모델들. Anemic — 데이터와 파생 속성만.

**Files:**
- Create: `Assets/Scripts/Features/Entity/Enums/ModifierSource.cs`
- Create: `Assets/Scripts/Features/Entity/Structs/EntityStatModifier.cs`
- Create: `Assets/Scripts/Features/Entity/Models/Stats.cs`
- Create: `Assets/Scripts/Features/Entity/Models/Health.cs`
- Create: `Assets/Scripts/Features/Entity/Models/Combat.cs`
- Test: (Task 3에서 System과 함께 테스트)

- [ ] **Step 1: ModifierSource enum 생성**

```csharp
// Assets/Scripts/Features/Entity/Enums/ModifierSource.cs
namespace FoldingFate.Features.Entity.Enums
{
    public enum ModifierSource
    {
        Equipment,
        Buff,
        Debuff,
        CardEffect,
        Passive
    }
}
```

- [ ] **Step 2: EntityStatModifier struct 생성**

```csharp
// Assets/Scripts/Features/Entity/Structs/EntityStatModifier.cs
using System;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Enums;

namespace FoldingFate.Features.Entity.Structs
{
    [Serializable]
    public struct EntityStatModifier
    {
        public EntityStatType StatType;
        public float Value;
        public ModifierSource Source;
        public string SourceId;

        public EntityStatModifier(EntityStatType statType, float value, ModifierSource source, string sourceId)
        {
            StatType = statType;
            Value = value;
            Source = source;
            SourceId = sourceId;
        }
    }
}
```

- [ ] **Step 3: Stats 컴포넌트 생성**

```csharp
// Assets/Scripts/Features/Entity/Models/Stats.cs
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Structs;

namespace FoldingFate.Features.Entity.Models
{
    public class Stats : IEntityComponent
    {
        public Entity Owner { get; set; }
        public Dictionary<EntityStatType, float> BaseStats { get; } = new();
        public List<EntityStatModifier> Modifiers { get; } = new();
    }
}
```

- [ ] **Step 4: Health 컴포넌트 생성**

```csharp
// Assets/Scripts/Features/Entity/Models/Health.cs
using FoldingFate.Core;

namespace FoldingFate.Features.Entity.Models
{
    public class Health : IEntityComponent
    {
        public Entity Owner { get; set; }
        public float CurrentHp { get; set; }
        public float MaxHp { get; set; }
        public bool IsAlive => CurrentHp > 0;
        public bool IsDead => !IsAlive;

        public Health(float maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }
    }
}
```

- [ ] **Step 5: Combat 컴포넌트 생성**

```csharp
// Assets/Scripts/Features/Entity/Models/Combat.cs
using FoldingFate.Core;

namespace FoldingFate.Features.Entity.Models
{
    public class Combat : IEntityComponent
    {
        public Entity Owner { get; set; }
        public bool IsInCombat { get; set; }
    }
}
```

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Features/Entity/
git commit -m "feat(entity): add Stats, Health, Combat components and EntityStatModifier"
```

---

## Task 3: Entity Systems — StatsSystem, HealthSystem, EntityFactory + Tests

컴포넌트 데이터를 조작하는 System들과 전체 테스트.

**Files:**
- Create: `Assets/Scripts/Features/Entity/Systems/StatsSystem.cs`
- Create: `Assets/Scripts/Features/Entity/Systems/HealthSystem.cs`
- Create: `Assets/Scripts/Features/Entity/Systems/EntityFactory.cs`
- Test: `Assets/Tests/EditMode/Entity/StatsSystemTests.cs`
- Test: `Assets/Tests/EditMode/Entity/HealthSystemTests.cs`
- Test: `Assets/Tests/EditMode/Entity/EntityFactoryTests.cs`

- [ ] **Step 1: StatsSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Entity/StatsSystemTests.cs
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Enums;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Structs;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Entity
{
    [TestFixture]
    public class StatsSystemTests
    {
        private StatsSystem _system;
        private Stats _stats;

        [SetUp]
        public void SetUp()
        {
            _system = new StatsSystem();
            _stats = new Stats();
            _stats.BaseStats[EntityStatType.Attack] = 10f;
            _stats.BaseStats[EntityStatType.Defense] = 5f;
        }

        [Test]
        public void GetValue_ReturnsBaseValue()
        {
            Assert.AreEqual(10f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void GetValue_UnsetStat_ReturnsZero()
        {
            Assert.AreEqual(0f, _system.GetValue(_stats, EntityStatType.Speed));
        }

        [Test]
        public void GetValue_WithModifier_ReturnsBaseAndModifierSum()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-1"));
            Assert.AreEqual(13f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void GetValue_MultipleModifiers_SumsAll()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-1"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, -2f, ModifierSource.Debuff, "debuff-1"));
            Assert.AreEqual(11f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void RemoveModifiersBySourceId_RemovesOnlyMatching()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 5f, ModifierSource.Buff, "buff-1"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-2"));
            _system.RemoveModifiersBySourceId(_stats, "buff-1");
            Assert.AreEqual(13f, _system.GetValue(_stats, EntityStatType.Attack));
        }

        [Test]
        public void RemoveModifiersBySource_RemovesAllOfSource()
        {
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 5f, ModifierSource.Buff, "buff-1"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Attack, 3f, ModifierSource.Buff, "buff-2"));
            _system.AddModifier(_stats, new EntityStatModifier(
                EntityStatType.Defense, 2f, ModifierSource.Equipment, "equip-1"));
            _system.RemoveModifiersBySource(_stats, ModifierSource.Buff);
            Assert.AreEqual(10f, _system.GetValue(_stats, EntityStatType.Attack));
            Assert.AreEqual(7f, _system.GetValue(_stats, EntityStatType.Defense));
        }
    }
}
```

- [ ] **Step 2: StatsSystem 구현**

```csharp
// Assets/Scripts/Features/Entity/Systems/StatsSystem.cs
using FoldingFate.Core;
using FoldingFate.Features.Entity.Enums;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Structs;

namespace FoldingFate.Features.Entity.Systems
{
    public class StatsSystem
    {
        public float GetValue(Stats stats, EntityStatType type)
        {
            stats.BaseStats.TryGetValue(type, out var baseValue);
            float modifierSum = 0f;
            var modifiers = stats.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].StatType == type)
                {
                    modifierSum += modifiers[i].Value;
                }
            }
            return baseValue + modifierSum;
        }

        public void AddModifier(Stats stats, EntityStatModifier modifier)
        {
            stats.Modifiers.Add(modifier);
        }

        public void RemoveModifiersBySourceId(Stats stats, string sourceId)
        {
            stats.Modifiers.RemoveAll(m => m.SourceId == sourceId);
        }

        public void RemoveModifiersBySource(Stats stats, ModifierSource source)
        {
            stats.Modifiers.RemoveAll(m => m.Source == source);
        }
    }
}
```

- [ ] **Step 3: Unity MCP로 StatsSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Entity.StatsSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 4: HealthSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Entity/HealthSystemTests.cs
using NUnit.Framework;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Entity
{
    [TestFixture]
    public class HealthSystemTests
    {
        private HealthSystem _system;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _system = new HealthSystem();
            _health = new Health(100f);
        }

        [Test]
        public void Health_Constructor_SetsCurrentHpToMax()
        {
            Assert.AreEqual(100f, _health.CurrentHp);
            Assert.AreEqual(100f, _health.MaxHp);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            _system.TakeDamage(_health, 30f);
            Assert.AreEqual(70f, _health.CurrentHp);
        }

        [Test]
        public void TakeDamage_DoesNotGoBelowZero()
        {
            _system.TakeDamage(_health, 150f);
            Assert.AreEqual(0f, _health.CurrentHp);
            Assert.IsTrue(_health.IsDead);
        }

        [Test]
        public void Heal_IncreasesHp()
        {
            _system.TakeDamage(_health, 50f);
            _system.Heal(_health, 20f);
            Assert.AreEqual(70f, _health.CurrentHp);
        }

        [Test]
        public void Heal_DoesNotExceedMaxHp()
        {
            _system.TakeDamage(_health, 10f);
            _system.Heal(_health, 50f);
            Assert.AreEqual(100f, _health.CurrentHp);
        }

        [Test]
        public void SetMaxHp_ClampsCurrentHp()
        {
            _system.SetMaxHp(_health, 50f);
            Assert.AreEqual(50f, _health.MaxHp);
            Assert.AreEqual(50f, _health.CurrentHp);
        }

        [Test]
        public void SetMaxHp_DoesNotClampWhenCurrentIsLower()
        {
            _system.TakeDamage(_health, 80f);
            _system.SetMaxHp(_health, 50f);
            Assert.AreEqual(50f, _health.MaxHp);
            Assert.AreEqual(20f, _health.CurrentHp);
        }
    }
}
```

- [ ] **Step 5: HealthSystem 구현**

```csharp
// Assets/Scripts/Features/Entity/Systems/HealthSystem.cs
using System;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Entity.Systems
{
    public class HealthSystem
    {
        public void TakeDamage(Health health, float amount)
        {
            health.CurrentHp = Math.Max(0, health.CurrentHp - amount);
        }

        public void Heal(Health health, float amount)
        {
            health.CurrentHp = Math.Min(health.MaxHp, health.CurrentHp + amount);
        }

        public void SetMaxHp(Health health, float value)
        {
            health.MaxHp = value;
            if (health.CurrentHp > health.MaxHp)
            {
                health.CurrentHp = health.MaxHp;
            }
        }
    }
}
```

- [ ] **Step 6: Unity MCP로 HealthSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Entity.HealthSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 7: EntityFactory 테스트 작성**

```csharp
// Assets/Tests/EditMode/Entity/EntityFactoryTests.cs
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Entity
{
    [TestFixture]
    public class EntityFactoryTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
        }

        [Test]
        public void CreateCombatEntity_HasStatsHealthCombat()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            Assert.AreEqual("hero", entity.Id);
            Assert.AreEqual(EntityType.Character, entity.Type);
            Assert.AreEqual("Hero", entity.DisplayName);
            Assert.IsTrue(entity.Has<Stats>());
            Assert.IsTrue(entity.Has<Health>());
            Assert.IsTrue(entity.Has<Combat>());
        }

        [Test]
        public void CreateCombatEntity_StatsInitialized()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            var stats = entity.Get<Stats>();
            Assert.AreEqual(10f, stats.BaseStats[EntityStatType.Attack]);
            Assert.AreEqual(5f, stats.BaseStats[EntityStatType.Defense]);
        }

        [Test]
        public void CreateCombatEntity_HealthInitialized()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            var health = entity.Get<Health>();
            Assert.AreEqual(100f, health.MaxHp);
            Assert.AreEqual(100f, health.CurrentHp);
        }

        [Test]
        public void CreateCombatEntity_CombatNotInCombat()
        {
            var entity = _factory.CreateCombatEntity("hero", EntityType.Character, "Hero", 100f, 10f, 5f);
            Assert.IsFalse(entity.Get<Combat>().IsInCombat);
        }

        [Test]
        public void CreateCombatEntity_Monster()
        {
            var entity = _factory.CreateCombatEntity("slime", EntityType.Monster, "Slime", 50f, 8f, 3f);
            Assert.AreEqual(EntityType.Monster, entity.Type);
            Assert.AreEqual("Slime", entity.DisplayName);
        }
    }
}
```

- [ ] **Step 8: EntityFactory 구현**

```csharp
// Assets/Scripts/Features/Entity/Systems/EntityFactory.cs
using FoldingFate.Core;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Entity.Systems
{
    public class EntityFactory
    {
        public Core.Entity CreateCombatEntity(string id, EntityType type, string displayName,
            float maxHp, float attack, float defense)
        {
            var entity = new Core.Entity(id, type, displayName);

            var stats = new Stats();
            stats.BaseStats[EntityStatType.Attack] = attack;
            stats.BaseStats[EntityStatType.Defense] = defense;
            entity.Add(stats);

            entity.Add(new Health(maxHp));
            entity.Add(new Combat());

            return entity;
        }
    }
}
```

- [ ] **Step 9: Unity MCP로 EntityFactory 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Entity.EntityFactoryTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 10: 전체 Entity 테스트 실행 확인**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Entity`)
Expected: EntityTests, StatsSystemTests, HealthSystemTests, EntityFactoryTests 모두 PASS

- [ ] **Step 11: 커밋**

```bash
git add Assets/Scripts/Features/Entity/Systems/ Assets/Tests/EditMode/Entity/
git commit -m "feat(entity): add StatsSystem, HealthSystem, EntityFactory with tests"
```

---

## Task 4: Dungeon Models — RoomModel, FloorModel, DungeonModel

Dungeon 계층 구조의 순수 C# 모델. ReactiveProperty 사용.

**Files:**
- Create: `Assets/Scripts/Core/Enums/RoomType.cs`
- Create: `Assets/Scripts/Core/Enums/RoomState.cs`
- Create: `Assets/Scripts/Features/Dungeon/Models/RoomModel.cs`
- Create: `Assets/Scripts/Features/Dungeon/Models/FloorModel.cs`
- Create: `Assets/Scripts/Features/Dungeon/Models/DungeonModel.cs`
- Test: `Assets/Tests/EditMode/Dungeon/RoomModelTests.cs`
- Test: `Assets/Tests/EditMode/Dungeon/FloorModelTests.cs`
- Test: `Assets/Tests/EditMode/Dungeon/DungeonModelTests.cs`

- [ ] **Step 1: Core Enums 생성**

```csharp
// Assets/Scripts/Core/Enums/RoomType.cs
namespace FoldingFate.Core
{
    public enum RoomType
    {
        Combat,
        Puzzle,
        Shop,
        Event,
        Boss
    }
}
```

```csharp
// Assets/Scripts/Core/Enums/RoomState.cs
namespace FoldingFate.Core
{
    public enum RoomState
    {
        Locked,
        Available,
        Active,
        Cleared
    }
}
```

- [ ] **Step 2: RoomModel 테스트 작성**

```csharp
// Assets/Tests/EditMode/Dungeon/RoomModelTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class RoomModelTests
    {
        [Test]
        public void Constructor_DefaultStateLocked()
        {
            var room = new RoomModel("room-1", RoomType.Combat, new List<Entity>().AsReadOnly());
            Assert.AreEqual(RoomState.Locked, room.State.CurrentValue);
        }

        [Test]
        public void Constructor_SetsTypeAndId()
        {
            var room = new RoomModel("room-1", RoomType.Shop, new List<Entity>().AsReadOnly());
            Assert.AreEqual("room-1", room.Id);
            Assert.AreEqual(RoomType.Shop, room.Type);
        }

        [Test]
        public void Entities_AccessibleAfterCreation()
        {
            var entity = new Entity("e1", EntityType.Monster, "Slime");
            var room = new RoomModel("room-1", RoomType.Combat, new List<Entity> { entity }.AsReadOnly());
            Assert.AreEqual(1, room.Entities.Count);
            Assert.AreEqual("Slime", room.Entities[0].DisplayName);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var room = new RoomModel("room-1", RoomType.Combat, new List<Entity>().AsReadOnly());
            Assert.DoesNotThrow(() => room.Dispose());
        }
    }
}
```

- [ ] **Step 3: RoomModel 구현**

```csharp
// Assets/Scripts/Features/Dungeon/Models/RoomModel.cs
using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Dungeon.Models
{
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

- [ ] **Step 4: Unity MCP로 RoomModel 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon.RoomModelTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 5: FloorModel 테스트 작성**

```csharp
// Assets/Tests/EditMode/Dungeon/FloorModelTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class FloorModelTests
    {
        private static RoomModel MakeRoom(string id, RoomType type) =>
            new RoomModel(id, type, new List<Entity>().AsReadOnly());

        [Test]
        public void Constructor_DefaultCurrentRoomIndexZero()
        {
            var rooms = new List<RoomModel> { MakeRoom("r1", RoomType.Combat) }.AsReadOnly();
            var floor = new FloorModel(0, rooms);
            Assert.AreEqual(0, floor.CurrentRoomIndex.CurrentValue);
        }

        [Test]
        public void CurrentRoom_ReturnsCorrectRoom()
        {
            var r1 = MakeRoom("r1", RoomType.Combat);
            var r2 = MakeRoom("r2", RoomType.Shop);
            var floor = new FloorModel(0, new List<RoomModel> { r1, r2 }.AsReadOnly());
            Assert.AreEqual(r1, floor.CurrentRoom);
        }

        [Test]
        public void IsCleared_FalseWhenNotAllCleared()
        {
            var r1 = MakeRoom("r1", RoomType.Combat);
            var r2 = MakeRoom("r2", RoomType.Combat);
            var floor = new FloorModel(0, new List<RoomModel> { r1, r2 }.AsReadOnly());
            r1.State.Value = RoomState.Cleared;
            Assert.IsFalse(floor.IsCleared);
        }

        [Test]
        public void IsCleared_TrueWhenAllCleared()
        {
            var r1 = MakeRoom("r1", RoomType.Combat);
            var r2 = MakeRoom("r2", RoomType.Shop);
            var floor = new FloorModel(0, new List<RoomModel> { r1, r2 }.AsReadOnly());
            r1.State.Value = RoomState.Cleared;
            r2.State.Value = RoomState.Cleared;
            Assert.IsTrue(floor.IsCleared);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var floor = new FloorModel(0, new List<RoomModel> { MakeRoom("r1", RoomType.Combat) }.AsReadOnly());
            Assert.DoesNotThrow(() => floor.Dispose());
        }
    }
}
```

- [ ] **Step 6: FloorModel 구현**

```csharp
// Assets/Scripts/Features/Dungeon/Models/FloorModel.cs
using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;

namespace FoldingFate.Features.Dungeon.Models
{
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
}
```

- [ ] **Step 7: Unity MCP로 FloorModel 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon.FloorModelTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 8: DungeonModel 테스트 작성**

```csharp
// Assets/Tests/EditMode/Dungeon/DungeonModelTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class DungeonModelTests
    {
        private static RoomModel MakeRoom(string id) =>
            new RoomModel(id, RoomType.Combat, new List<Entity>().AsReadOnly());

        private static FloorModel MakeFloor(int index, int roomCount)
        {
            var rooms = new List<RoomModel>();
            for (int i = 0; i < roomCount; i++)
                rooms.Add(MakeRoom($"f{index}-r{i}"));
            return new FloorModel(index, rooms.AsReadOnly());
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            var floors = new List<FloorModel> { MakeFloor(0, 2), MakeFloor(1, 3) }.AsReadOnly();
            var dungeon = new DungeonModel("d1", "TestDungeon", floors);
            Assert.AreEqual("d1", dungeon.Id);
            Assert.AreEqual("TestDungeon", dungeon.DisplayName);
            Assert.AreEqual(2, dungeon.Floors.Count);
        }

        [Test]
        public void Constructor_DefaultFloorIndexZero()
        {
            var floors = new List<FloorModel> { MakeFloor(0, 1) }.AsReadOnly();
            var dungeon = new DungeonModel("d1", "Test", floors);
            Assert.AreEqual(0, dungeon.CurrentFloorIndex.CurrentValue);
        }

        [Test]
        public void CurrentFloor_ReturnsCorrectFloor()
        {
            var f0 = MakeFloor(0, 1);
            var f1 = MakeFloor(1, 2);
            var dungeon = new DungeonModel("d1", "Test", new List<FloorModel> { f0, f1 }.AsReadOnly());
            Assert.AreEqual(f0, dungeon.CurrentFloor);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var dungeon = new DungeonModel("d1", "Test", new List<FloorModel> { MakeFloor(0, 1) }.AsReadOnly());
            Assert.DoesNotThrow(() => dungeon.Dispose());
        }
    }
}
```

- [ ] **Step 9: DungeonModel 구현**

```csharp
// Assets/Scripts/Features/Dungeon/Models/DungeonModel.cs
using System;
using System.Collections.Generic;
using R3;

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
}
```

- [ ] **Step 10: Unity MCP로 Dungeon Model 테스트 전체 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon`)
Expected: RoomModelTests, FloorModelTests, DungeonModelTests 모두 PASS

- [ ] **Step 11: 커밋**

```bash
git add Assets/Scripts/Core/Enums/RoomType.cs Assets/Scripts/Core/Enums/RoomState.cs \
  Assets/Scripts/Features/Dungeon/ Assets/Tests/EditMode/Dungeon/
git commit -m "feat(dungeon): add RoomModel, FloorModel, DungeonModel with reactive state"
```

---

## Task 5: Dungeon Systems — RoomSystem, FloorSystem, DungeonSystem

Dungeon 모델을 조작하는 System들.

**Files:**
- Create: `Assets/Scripts/Features/Dungeon/Systems/RoomSystem.cs`
- Create: `Assets/Scripts/Features/Dungeon/Systems/FloorSystem.cs`
- Create: `Assets/Scripts/Features/Dungeon/Systems/DungeonSystem.cs`
- Test: `Assets/Tests/EditMode/Dungeon/RoomSystemTests.cs`
- Test: `Assets/Tests/EditMode/Dungeon/FloorSystemTests.cs`
- Test: `Assets/Tests/EditMode/Dungeon/DungeonSystemTests.cs`

- [ ] **Step 1: RoomSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Dungeon/RoomSystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;
using FoldingFate.Features.Dungeon.Systems;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class RoomSystemTests
    {
        private RoomSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new RoomSystem();
        }

        [Test]
        public void Enter_SetsStateToActive()
        {
            var room = new RoomModel("r1", RoomType.Combat, new List<Entity>().AsReadOnly());
            _system.Enter(room);
            Assert.AreEqual(RoomState.Active, room.State.CurrentValue);
        }

        [Test]
        public void Clear_SetsStateToCleared()
        {
            var room = new RoomModel("r1", RoomType.Combat, new List<Entity>().AsReadOnly());
            _system.Enter(room);
            _system.Clear(room);
            Assert.AreEqual(RoomState.Cleared, room.State.CurrentValue);
        }
    }
}
```

- [ ] **Step 2: RoomSystem 구현**

```csharp
// Assets/Scripts/Features/Dungeon/Systems/RoomSystem.cs
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Features.Dungeon.Systems
{
    public class RoomSystem
    {
        public void Enter(RoomModel room)
        {
            room.State.Value = RoomState.Active;
        }

        public void Clear(RoomModel room)
        {
            room.State.Value = RoomState.Cleared;
        }
    }
}
```

- [ ] **Step 3: Unity MCP로 RoomSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon.RoomSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 4: FloorSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Dungeon/FloorSystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;
using FoldingFate.Features.Dungeon.Systems;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class FloorSystemTests
    {
        private FloorSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new FloorSystem();
        }

        private static RoomModel MakeRoom(string id) =>
            new RoomModel(id, RoomType.Combat, new List<Entity>().AsReadOnly());

        private static FloorModel MakeFloor(int roomCount)
        {
            var rooms = new List<RoomModel>();
            for (int i = 0; i < roomCount; i++)
                rooms.Add(MakeRoom($"r{i}"));
            return new FloorModel(0, rooms.AsReadOnly());
        }

        [Test]
        public void MoveToNextRoom_IncrementsIndex()
        {
            var floor = MakeFloor(3);
            _system.MoveToNextRoom(floor);
            Assert.AreEqual(1, floor.CurrentRoomIndex.CurrentValue);
        }

        [Test]
        public void MoveToNextRoom_AtLastRoom_DoesNotIncrement()
        {
            var floor = MakeFloor(2);
            _system.MoveToNextRoom(floor);
            _system.MoveToNextRoom(floor);
            Assert.AreEqual(1, floor.CurrentRoomIndex.CurrentValue);
        }

        [Test]
        public void MoveToNextFloor_IncrementsIndex()
        {
            var f0 = new FloorModel(0, new List<RoomModel> { MakeRoom("r0") }.AsReadOnly());
            var f1 = new FloorModel(1, new List<RoomModel> { MakeRoom("r1") }.AsReadOnly());
            var dungeon = new DungeonModel("d1", "Test", new List<FloorModel> { f0, f1 }.AsReadOnly());
            _system.MoveToNextFloor(dungeon);
            Assert.AreEqual(1, dungeon.CurrentFloorIndex.CurrentValue);
        }

        [Test]
        public void MoveToNextFloor_AtLastFloor_DoesNotIncrement()
        {
            var f0 = new FloorModel(0, new List<RoomModel> { MakeRoom("r0") }.AsReadOnly());
            var dungeon = new DungeonModel("d1", "Test", new List<FloorModel> { f0 }.AsReadOnly());
            _system.MoveToNextFloor(dungeon);
            Assert.AreEqual(0, dungeon.CurrentFloorIndex.CurrentValue);
        }
    }
}
```

- [ ] **Step 5: FloorSystem 구현**

```csharp
// Assets/Scripts/Features/Dungeon/Systems/FloorSystem.cs
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Features.Dungeon.Systems
{
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
}
```

- [ ] **Step 6: Unity MCP로 FloorSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon.FloorSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 7: DungeonSystem 테스트 작성**

DungeonSystem.Create()는 DungeonData(SO)를 받지만, SO는 EditMode에서 직접 생성이 번거로우므로 SO 없이 테스트 가능한 구조로 진행. EntityFactory를 주입받아 몬스터 생성.

```csharp
// Assets/Tests/EditMode/Dungeon/DungeonSystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;
using FoldingFate.Features.Dungeon.Systems;

namespace FoldingFate.Tests.EditMode.Dungeon
{
    [TestFixture]
    public class DungeonSystemTests
    {
        private DungeonSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new DungeonSystem();
        }

        [Test]
        public void CreateFromConfig_ReturnsCorrectStructure()
        {
            // Floor 0: Combat, Shop  / Floor 1: Combat, Boss
            var floorConfigs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Shop }),
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Boss })
            };

            var dungeon = _system.Create("TestDungeon", floorConfigs);

            Assert.AreEqual("TestDungeon", dungeon.DisplayName);
            Assert.AreEqual(2, dungeon.Floors.Count);
            Assert.AreEqual(2, dungeon.Floors[0].Rooms.Count);
            Assert.AreEqual(2, dungeon.Floors[1].Rooms.Count);
        }

        [Test]
        public void CreateFromConfig_RoomTypesMatchConfig()
        {
            var floorConfigs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Shop, RoomType.Boss })
            };

            var dungeon = _system.Create("Test", floorConfigs);

            Assert.AreEqual(RoomType.Combat, dungeon.Floors[0].Rooms[0].Type);
            Assert.AreEqual(RoomType.Shop, dungeon.Floors[0].Rooms[1].Type);
            Assert.AreEqual(RoomType.Boss, dungeon.Floors[0].Rooms[2].Type);
        }

        [Test]
        public void CreateFromConfig_FloorIndicesCorrect()
        {
            var floorConfigs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat }),
                new DungeonSystem.FloorConfig(new[] { RoomType.Boss })
            };

            var dungeon = _system.Create("Test", floorConfigs);

            Assert.AreEqual(0, dungeon.Floors[0].Index);
            Assert.AreEqual(1, dungeon.Floors[1].Index);
        }

        [Test]
        public void CreateFromConfig_RoomsHaveUniqueIds()
        {
            var floorConfigs = new List<DungeonSystem.FloorConfig>
            {
                new DungeonSystem.FloorConfig(new[] { RoomType.Combat, RoomType.Combat })
            };

            var dungeon = _system.Create("Test", floorConfigs);

            Assert.AreNotEqual(dungeon.Floors[0].Rooms[0].Id, dungeon.Floors[0].Rooms[1].Id);
        }
    }
}
```

- [ ] **Step 8: DungeonSystem 구현**

SO 기반 Create는 나중에 DungeonData SO가 정의되면 추가. 현재는 순수 C# FloorConfig로 테스트 가능한 Create 메서드를 제공.

```csharp
// Assets/Scripts/Features/Dungeon/Systems/DungeonSystem.cs
using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Dungeon.Models;

namespace FoldingFate.Features.Dungeon.Systems
{
    public class DungeonSystem
    {
        public class FloorConfig
        {
            public RoomType[] RoomTypes { get; }

            public FloorConfig(RoomType[] roomTypes)
            {
                RoomTypes = roomTypes ?? throw new ArgumentNullException(nameof(roomTypes));
            }
        }

        public DungeonModel Create(string displayName, IReadOnlyList<FloorConfig> floorConfigs)
        {
            var floors = new List<FloorModel>();
            for (int i = 0; i < floorConfigs.Count; i++)
            {
                var config = floorConfigs[i];
                var rooms = new List<RoomModel>();
                for (int j = 0; j < config.RoomTypes.Length; j++)
                {
                    var room = new RoomModel(
                        id: Guid.NewGuid().ToString(),
                        type: config.RoomTypes[j],
                        entities: new List<Entity>().AsReadOnly()
                    );
                    rooms.Add(room);
                }
                floors.Add(new FloorModel(i, rooms.AsReadOnly()));
            }
            return new DungeonModel(
                id: Guid.NewGuid().ToString(),
                displayName: displayName,
                floors: floors.AsReadOnly()
            );
        }
    }
}
```

- [ ] **Step 9: Unity MCP로 DungeonSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon.DungeonSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 10: 전체 Dungeon 테스트 실행 확인**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Dungeon`)
Expected: 모든 Dungeon 테스트 PASS

- [ ] **Step 11: 커밋**

```bash
git add Assets/Scripts/Features/Dungeon/Systems/ Assets/Tests/EditMode/Dungeon/
git commit -m "feat(dungeon): add RoomSystem, FloorSystem, DungeonSystem with tests"
```

---

## Task 6: Battle Models — BattleAction, ActionResult, TurnRecord, BattleModel

3-Phase 전투의 데이터 모델. Anemic — 데이터와 파생 속성만.

**Files:**
- Create: `Assets/Scripts/Core/Enums/BattlePhase.cs`
- Create: `Assets/Scripts/Core/Enums/BattleActionType.cs`
- Create: `Assets/Scripts/Core/Enums/ActionResultType.cs`
- Create: `Assets/Scripts/Features/Battle/Models/BattleAction.cs`
- Create: `Assets/Scripts/Features/Battle/Models/ActionResult.cs`
- Create: `Assets/Scripts/Features/Battle/Models/TurnRecord.cs`
- Create: `Assets/Scripts/Features/Battle/Models/BattleModel.cs`
- Create: `Assets/Scripts/Features/Battle/Events/CombatStartEvent.cs`
- Create: `Assets/Scripts/Features/Battle/Events/CombatEndEvent.cs`
- Test: `Assets/Tests/EditMode/Battle/BattleModelTests.cs`

- [ ] **Step 1: Core Enums 생성**

```csharp
// Assets/Scripts/Core/Enums/BattlePhase.cs
namespace FoldingFate.Core
{
    public enum BattlePhase
    {
        Start,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }
}
```

```csharp
// Assets/Scripts/Core/Enums/BattleActionType.cs
namespace FoldingFate.Core
{
    public enum BattleActionType
    {
        Attack,
        Defend,
        Skill
    }
}
```

```csharp
// Assets/Scripts/Core/Enums/ActionResultType.cs
namespace FoldingFate.Core
{
    public enum ActionResultType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Miss
    }
}
```

- [ ] **Step 2: BattleAction 모델 생성**

```csharp
// Assets/Scripts/Features/Battle/Models/BattleAction.cs
using System;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Models
{
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
}
```

- [ ] **Step 3: ActionResult 모델 생성**

```csharp
// Assets/Scripts/Features/Battle/Models/ActionResult.cs
using System;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Models
{
    public class ActionResult
    {
        public BattleAction Source { get; }
        public ActionResultType ResultType { get; }
        public Entity Target { get; }
        public int Value { get; }

        public ActionResult(BattleAction source, ActionResultType resultType, Entity target, int value)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ResultType = resultType;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Value = value;
        }
    }
}
```

- [ ] **Step 4: TurnRecord 모델 생성**

```csharp
// Assets/Scripts/Features/Battle/Models/TurnRecord.cs
using System.Collections.Generic;

namespace FoldingFate.Features.Battle.Models
{
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

- [ ] **Step 5: BattleModel 생성**

```csharp
// Assets/Scripts/Features/Battle/Models/BattleModel.cs
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
}
```

- [ ] **Step 6: Events 생성**

```csharp
// Assets/Scripts/Features/Battle/Events/CombatStartEvent.cs
using System.Collections.Generic;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Events
{
    public record CombatStartEvent(
        IReadOnlyList<Entity> Allies,
        IReadOnlyList<Entity> Enemies
    );
}
```

```csharp
// Assets/Scripts/Features/Battle/Events/CombatEndEvent.cs
namespace FoldingFate.Features.Battle.Events
{
    public record CombatEndEvent(
        string BattleId,
        bool IsVictory
    );
}
```

- [ ] **Step 7: BattleModel 테스트 작성**

```csharp
// Assets/Tests/EditMode/Battle/BattleModelTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class BattleModelTests
    {
        private static Entity MakeEntity(string id, EntityType type, string name) =>
            new Entity(id, type, name);

        [Test]
        public void Constructor_DefaultPhaseStart()
        {
            var allies = new List<Entity> { MakeEntity("a1", EntityType.Character, "Hero") }.AsReadOnly();
            var enemies = new List<Entity> { MakeEntity("e1", EntityType.Monster, "Slime") }.AsReadOnly();
            var battle = new BattleModel("b1", allies, enemies);
            Assert.AreEqual(BattlePhase.Start, battle.Phase.CurrentValue);
        }

        [Test]
        public void Constructor_DefaultTurnCountZero()
        {
            var allies = new List<Entity> { MakeEntity("a1", EntityType.Character, "Hero") }.AsReadOnly();
            var enemies = new List<Entity> { MakeEntity("e1", EntityType.Monster, "Slime") }.AsReadOnly();
            var battle = new BattleModel("b1", allies, enemies);
            Assert.AreEqual(0, battle.TurnCount.CurrentValue);
        }

        [Test]
        public void Constructor_TurnHistoryEmpty()
        {
            var allies = new List<Entity> { MakeEntity("a1", EntityType.Character, "Hero") }.AsReadOnly();
            var enemies = new List<Entity> { MakeEntity("e1", EntityType.Monster, "Slime") }.AsReadOnly();
            var battle = new BattleModel("b1", allies, enemies);
            Assert.AreEqual(0, battle.TurnHistory.Count);
        }

        [Test]
        public void Allies_Enemies_Accessible()
        {
            var allies = new List<Entity> { MakeEntity("a1", EntityType.Character, "Hero") }.AsReadOnly();
            var enemies = new List<Entity>
            {
                MakeEntity("e1", EntityType.Monster, "Slime"),
                MakeEntity("e2", EntityType.Monster, "Goblin")
            }.AsReadOnly();
            var battle = new BattleModel("b1", allies, enemies);
            Assert.AreEqual(1, battle.Allies.Count);
            Assert.AreEqual(2, battle.Enemies.Count);
        }

        [Test]
        public void TurnRecord_PreservesData()
        {
            var actor = MakeEntity("a1", EntityType.Character, "Hero");
            var target = MakeEntity("e1", EntityType.Monster, "Slime");
            var action = new BattleAction(actor, BattleActionType.Attack, target);
            var result = new ActionResult(action, ActionResultType.Damage, target, 10);
            var record = new TurnRecord(1,
                new List<BattleAction> { action }.AsReadOnly(),
                new List<ActionResult> { result }.AsReadOnly());

            Assert.AreEqual(1, record.TurnNumber);
            Assert.AreEqual(1, record.Actions.Count);
            Assert.AreEqual(1, record.Results.Count);
            Assert.AreEqual(10, record.Results[0].Value);
            Assert.AreEqual(ActionResultType.Damage, record.Results[0].ResultType);
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var allies = new List<Entity> { MakeEntity("a1", EntityType.Character, "Hero") }.AsReadOnly();
            var enemies = new List<Entity> { MakeEntity("e1", EntityType.Monster, "Slime") }.AsReadOnly();
            var battle = new BattleModel("b1", allies, enemies);
            Assert.DoesNotThrow(() => battle.Dispose());
        }
    }
}
```

- [ ] **Step 8: Unity MCP로 Battle Model 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Battle.BattleModelTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 9: 커밋**

```bash
git add Assets/Scripts/Core/Enums/BattlePhase.cs Assets/Scripts/Core/Enums/BattleActionType.cs \
  Assets/Scripts/Core/Enums/ActionResultType.cs \
  Assets/Scripts/Features/Battle/ Assets/Tests/EditMode/Battle/
git commit -m "feat(battle): add BattleModel, BattleAction, ActionResult, TurnRecord, Events"
```

---

## Task 7: Battle Systems — ResolveSystem, ApplySystem

3-Phase의 Phase 2(Resolve)와 Phase 3(Apply).

**Files:**
- Create: `Assets/Scripts/Features/Battle/Systems/ResolveSystem.cs`
- Create: `Assets/Scripts/Features/Battle/Systems/ApplySystem.cs`
- Test: `Assets/Tests/EditMode/Battle/ResolveSystemTests.cs`
- Test: `Assets/Tests/EditMode/Battle/ApplySystemTests.cs`

- [ ] **Step 1: ResolveSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Battle/ResolveSystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class ResolveSystemTests
    {
        private ResolveSystem _system;
        private EntityFactory _entityFactory;

        [SetUp]
        public void SetUp()
        {
            _system = new ResolveSystem(new StatsSystem());
            _entityFactory = new EntityFactory();
        }

        [Test]
        public void Resolve_Attack_ReturnsDamageResult()
        {
            var attacker = _entityFactory.CreateCombatEntity("a", EntityType.Character, "Hero", 100f, 15f, 5f);
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Monster, "Slime", 50f, 8f, 3f);
            var action = new BattleAction(attacker, BattleActionType.Attack, target);

            var results = _system.Resolve(new List<BattleAction> { action }.AsReadOnly());

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(ActionResultType.Damage, results[0].ResultType);
            Assert.AreEqual(12, results[0].Value); // 15 attack - 3 defense = 12
        }

        [Test]
        public void Resolve_Attack_DefenseHigherThanAttack_ZeroDamage()
        {
            var attacker = _entityFactory.CreateCombatEntity("a", EntityType.Character, "Hero", 100f, 3f, 5f);
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Monster, "Tank", 50f, 8f, 10f);
            var action = new BattleAction(attacker, BattleActionType.Attack, target);

            var results = _system.Resolve(new List<BattleAction> { action }.AsReadOnly());

            Assert.AreEqual(0, results[0].Value);
        }

        [Test]
        public void Resolve_Defend_ReturnsBuffResult()
        {
            var actor = _entityFactory.CreateCombatEntity("a", EntityType.Character, "Hero", 100f, 10f, 5f);
            var action = new BattleAction(actor, BattleActionType.Defend, actor);

            var results = _system.Resolve(new List<BattleAction> { action }.AsReadOnly());

            Assert.AreEqual(ActionResultType.Buff, results[0].ResultType);
        }

        [Test]
        public void Resolve_DoesNotModifyEntityState()
        {
            var attacker = _entityFactory.CreateCombatEntity("a", EntityType.Character, "Hero", 100f, 15f, 5f);
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Monster, "Slime", 50f, 8f, 3f);
            var action = new BattleAction(attacker, BattleActionType.Attack, target);

            _system.Resolve(new List<BattleAction> { action }.AsReadOnly());

            Assert.AreEqual(50f, target.Get<Health>().CurrentHp); // HP unchanged
        }

        [Test]
        public void Resolve_MultipleActions_ReturnsSameCount()
        {
            var a1 = _entityFactory.CreateCombatEntity("a1", EntityType.Character, "Hero", 100f, 10f, 5f);
            var a2 = _entityFactory.CreateCombatEntity("a2", EntityType.Character, "Mage", 80f, 12f, 3f);
            var t1 = _entityFactory.CreateCombatEntity("t1", EntityType.Monster, "Slime", 50f, 8f, 3f);

            var actions = new List<BattleAction>
            {
                new BattleAction(a1, BattleActionType.Attack, t1),
                new BattleAction(a2, BattleActionType.Attack, t1)
            }.AsReadOnly();

            var results = _system.Resolve(actions);
            Assert.AreEqual(2, results.Count);
        }
    }
}
```

- [ ] **Step 2: ResolveSystem 구현**

```csharp
// Assets/Scripts/Features/Battle/Systems/ResolveSystem.cs
using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Battle.Systems
{
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
                results.Add(ResolveAction(actions[i]));
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
                    return new ActionResult(action, ActionResultType.Buff, action.Actor, 0);

                default:
                    return new ActionResult(action, ActionResultType.Miss, action.Target, 0);
            }
        }
    }
}
```

- [ ] **Step 3: Unity MCP로 ResolveSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Battle.ResolveSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 4: ApplySystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Battle/ApplySystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class ApplySystemTests
    {
        private ApplySystem _system;
        private EntityFactory _entityFactory;

        [SetUp]
        public void SetUp()
        {
            _system = new ApplySystem(new HealthSystem());
            _entityFactory = new EntityFactory();
        }

        private BattleAction MakeAttackAction(Entity actor, Entity target) =>
            new BattleAction(actor, BattleActionType.Attack, target);

        [Test]
        public void Apply_Damage_ReducesTargetHp()
        {
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Monster, "Slime", 50f, 8f, 3f);
            var action = MakeAttackAction(
                _entityFactory.CreateCombatEntity("a", EntityType.Character, "Hero", 100f, 10f, 5f),
                target);
            var result = new ActionResult(action, ActionResultType.Damage, target, 12);

            _system.Apply(new List<ActionResult> { result }.AsReadOnly());

            Assert.AreEqual(38f, target.Get<Health>().CurrentHp);
        }

        [Test]
        public void Apply_Heal_IncreasesTargetHp()
        {
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Character, "Hero", 100f, 10f, 5f);
            var health = target.Get<Health>();
            health.CurrentHp = 60f;

            var action = new BattleAction(target, BattleActionType.Skill, target);
            var result = new ActionResult(action, ActionResultType.Heal, target, 20);

            _system.Apply(new List<ActionResult> { result }.AsReadOnly());

            Assert.AreEqual(80f, health.CurrentHp);
        }

        [Test]
        public void Apply_Heal_DoesNotExceedMaxHp()
        {
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Character, "Hero", 100f, 10f, 5f);
            var health = target.Get<Health>();
            health.CurrentHp = 95f;

            var action = new BattleAction(target, BattleActionType.Skill, target);
            var result = new ActionResult(action, ActionResultType.Heal, target, 20);

            _system.Apply(new List<ActionResult> { result }.AsReadOnly());

            Assert.AreEqual(100f, health.CurrentHp);
        }

        [Test]
        public void Apply_MultipleResults_AppliesSequentially()
        {
            var target = _entityFactory.CreateCombatEntity("t", EntityType.Monster, "Slime", 50f, 8f, 3f);
            var attacker = _entityFactory.CreateCombatEntity("a", EntityType.Character, "Hero", 100f, 10f, 5f);
            var action = MakeAttackAction(attacker, target);

            var results = new List<ActionResult>
            {
                new ActionResult(action, ActionResultType.Damage, target, 10),
                new ActionResult(action, ActionResultType.Damage, target, 15)
            }.AsReadOnly();

            _system.Apply(results);

            Assert.AreEqual(25f, target.Get<Health>().CurrentHp); // 50 - 10 - 15 = 25
        }
    }
}
```

- [ ] **Step 5: ApplySystem 구현**

```csharp
// Assets/Scripts/Features/Battle/Systems/ApplySystem.cs
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Features.Battle.Systems
{
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
            }
        }
    }
}
```

- [ ] **Step 6: Unity MCP로 ApplySystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Battle.ApplySystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 7: 커밋**

```bash
git add Assets/Scripts/Features/Battle/Systems/ResolveSystem.cs \
  Assets/Scripts/Features/Battle/Systems/ApplySystem.cs \
  Assets/Tests/EditMode/Battle/ResolveSystemTests.cs \
  Assets/Tests/EditMode/Battle/ApplySystemTests.cs
git commit -m "feat(battle): add ResolveSystem and ApplySystem for 3-phase turn processing"
```

---

## Task 8: Battle Systems — TurnSystem, BattleSystem

턴 진행 오케스트레이션(Collect→Resolve→Apply)과 전투 생명주기.

**Files:**
- Create: `Assets/Scripts/Features/Battle/Systems/TurnSystem.cs`
- Create: `Assets/Scripts/Features/Battle/Systems/BattleSystem.cs`
- Test: `Assets/Tests/EditMode/Battle/TurnSystemTests.cs`
- Test: `Assets/Tests/EditMode/Battle/BattleSystemTests.cs`

- [ ] **Step 1: TurnSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Battle/TurnSystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class TurnSystemTests
    {
        private TurnSystem _turnSystem;
        private EntityFactory _entityFactory;

        [SetUp]
        public void SetUp()
        {
            var statsSystem = new StatsSystem();
            var healthSystem = new HealthSystem();
            _turnSystem = new TurnSystem(
                new ResolveSystem(statsSystem),
                new ApplySystem(healthSystem));
            _entityFactory = new EntityFactory();
        }

        private BattleModel MakeBattle()
        {
            var allies = new List<Entity>
            {
                _entityFactory.CreateCombatEntity("a1", EntityType.Character, "Hero", 100f, 15f, 5f)
            }.AsReadOnly();
            var enemies = new List<Entity>
            {
                _entityFactory.CreateCombatEntity("e1", EntityType.Monster, "Slime", 50f, 8f, 3f)
            }.AsReadOnly();
            return new BattleModel("b1", allies, enemies);
        }

        [Test]
        public void StartTurn_IncrementsTurnCount()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);
            Assert.AreEqual(1, battle.TurnCount.CurrentValue);
        }

        [Test]
        public void StartTurn_SetsPhaseToPlayerTurn()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);
            Assert.AreEqual(BattlePhase.PlayerTurn, battle.Phase.CurrentValue);
        }

        [Test]
        public void ExecuteTurn_AppliesDamageAndRecordsTurn()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);

            var action = new BattleAction(battle.Allies[0], BattleActionType.Attack, battle.Enemies[0]);
            _turnSystem.ExecuteTurn(battle, new List<BattleAction> { action }.AsReadOnly());

            // Damage applied: 15 attack - 3 defense = 12
            Assert.AreEqual(38f, battle.Enemies[0].Get<Health>().CurrentHp);
            // Turn recorded
            Assert.AreEqual(1, battle.TurnHistory.Count);
            Assert.AreEqual(1, battle.TurnHistory[0].TurnNumber);
        }

        [Test]
        public void EndTurn_AllEnemiesDead_Victory()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);

            // Kill all enemies
            battle.Enemies[0].Get<Health>().CurrentHp = 0;
            _turnSystem.EndTurn(battle);

            Assert.AreEqual(BattlePhase.Victory, battle.Phase.CurrentValue);
        }

        [Test]
        public void EndTurn_AllAlliesDead_Defeat()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);

            // Kill all allies
            battle.Allies[0].Get<Health>().CurrentHp = 0;
            _turnSystem.EndTurn(battle);

            Assert.AreEqual(BattlePhase.Defeat, battle.Phase.CurrentValue);
        }

        [Test]
        public void EndTurn_BothAlive_PlayerTurnToEnemyTurn()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);
            battle.Phase.Value = BattlePhase.PlayerTurn;
            _turnSystem.EndTurn(battle);
            Assert.AreEqual(BattlePhase.EnemyTurn, battle.Phase.CurrentValue);
        }

        [Test]
        public void EndTurn_BothAlive_EnemyTurnToPlayerTurn()
        {
            var battle = MakeBattle();
            _turnSystem.StartTurn(battle);
            battle.Phase.Value = BattlePhase.EnemyTurn;
            _turnSystem.EndTurn(battle);
            Assert.AreEqual(BattlePhase.PlayerTurn, battle.Phase.CurrentValue);
        }
    }
}
```

- [ ] **Step 2: TurnSystem 구현**

```csharp
// Assets/Scripts/Features/Battle/Systems/TurnSystem.cs
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Systems
{
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

        public void ExecuteTurn(BattleModel battle, IReadOnlyList<BattleAction> actions)
        {
            var results = _resolveSystem.Resolve(actions);
            _applySystem.Apply(results);
            battle.TurnHistory.Add(new TurnRecord(
                battle.TurnCount.CurrentValue,
                actions,
                results));
        }

        public void EndTurn(BattleModel battle)
        {
            if (AllDead(battle.Enemies))
            {
                battle.Phase.Value = BattlePhase.Victory;
                return;
            }

            if (AllDead(battle.Allies))
            {
                battle.Phase.Value = BattlePhase.Defeat;
                return;
            }

            battle.Phase.Value = battle.Phase.CurrentValue == BattlePhase.PlayerTurn
                ? BattlePhase.EnemyTurn
                : BattlePhase.PlayerTurn;
        }

        private static bool AllDead(IReadOnlyList<Entity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var health = entities[i].Get<Health>();
                if (health != null && health.IsAlive)
                    return false;
            }
            return true;
        }
    }
}
```

- [ ] **Step 3: Unity MCP로 TurnSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Battle.TurnSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 4: BattleSystem 테스트 작성**

```csharp
// Assets/Tests/EditMode/Battle/BattleSystemTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;

namespace FoldingFate.Tests.EditMode.Battle
{
    [TestFixture]
    public class BattleSystemTests
    {
        private BattleSystem _system;
        private EntityFactory _entityFactory;

        [SetUp]
        public void SetUp()
        {
            _system = new BattleSystem();
            _entityFactory = new EntityFactory();
        }

        [Test]
        public void StartBattle_CreatesBattleModel()
        {
            var allies = new List<Entity>
            {
                _entityFactory.CreateCombatEntity("a1", EntityType.Character, "Hero", 100f, 10f, 5f)
            }.AsReadOnly();
            var enemies = new List<Entity>
            {
                _entityFactory.CreateCombatEntity("e1", EntityType.Monster, "Slime", 50f, 8f, 3f)
            }.AsReadOnly();

            var battle = _system.StartBattle(allies, enemies);

            Assert.IsNotNull(battle);
            Assert.AreEqual(1, battle.Allies.Count);
            Assert.AreEqual(1, battle.Enemies.Count);
        }

        [Test]
        public void StartBattle_SetsIsInCombatTrue()
        {
            var ally = _entityFactory.CreateCombatEntity("a1", EntityType.Character, "Hero", 100f, 10f, 5f);
            var enemy = _entityFactory.CreateCombatEntity("e1", EntityType.Monster, "Slime", 50f, 8f, 3f);

            _system.StartBattle(
                new List<Entity> { ally }.AsReadOnly(),
                new List<Entity> { enemy }.AsReadOnly());

            Assert.IsTrue(ally.Get<Combat>().IsInCombat);
            Assert.IsTrue(enemy.Get<Combat>().IsInCombat);
        }

        [Test]
        public void EndBattle_SetsIsInCombatFalse()
        {
            var ally = _entityFactory.CreateCombatEntity("a1", EntityType.Character, "Hero", 100f, 10f, 5f);
            var enemy = _entityFactory.CreateCombatEntity("e1", EntityType.Monster, "Slime", 50f, 8f, 3f);

            var battle = _system.StartBattle(
                new List<Entity> { ally }.AsReadOnly(),
                new List<Entity> { enemy }.AsReadOnly());

            _system.EndBattle(battle);

            Assert.IsFalse(ally.Get<Combat>().IsInCombat);
            Assert.IsFalse(enemy.Get<Combat>().IsInCombat);
        }
    }
}
```

- [ ] **Step 5: BattleSystem 구현**

```csharp
// Assets/Scripts/Features/Battle/Systems/BattleSystem.cs
using System;
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Systems
{
    public class BattleSystem
    {
        public BattleModel StartBattle(
            IReadOnlyList<Entity> allies,
            IReadOnlyList<Entity> enemies)
        {
            SetCombatState(allies, true);
            SetCombatState(enemies, true);

            return new BattleModel(
                id: Guid.NewGuid().ToString(),
                allies: allies,
                enemies: enemies);
        }

        public void EndBattle(BattleModel battle)
        {
            SetCombatState(battle.Allies, false);
            SetCombatState(battle.Enemies, false);
            battle.Dispose();
        }

        private static void SetCombatState(IReadOnlyList<Entity> entities, bool isInCombat)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var combat = entities[i].Get<Combat>();
                if (combat != null)
                {
                    combat.IsInCombat = isInCombat;
                }
            }
        }
    }
}
```

- [ ] **Step 6: Unity MCP로 BattleSystem 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Battle.BattleSystemTests`)
Expected: 모든 테스트 PASS

- [ ] **Step 7: 전체 Battle 테스트 실행 확인**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Battle`)
Expected: 모든 Battle 테스트 PASS

- [ ] **Step 8: 커밋**

```bash
git add Assets/Scripts/Features/Battle/Systems/TurnSystem.cs \
  Assets/Scripts/Features/Battle/Systems/BattleSystem.cs \
  Assets/Tests/EditMode/Battle/TurnSystemTests.cs \
  Assets/Tests/EditMode/Battle/BattleSystemTests.cs
git commit -m "feat(battle): add TurnSystem (3-phase orchestration) and BattleSystem"
```

---

## Task 9: 전체 테스트 실행 + 기존 테스트 회귀 확인

모든 테스트가 통과하는지 확인하고, 기존 Card/Poker 테스트에 회귀가 없는지 검증.

- [ ] **Step 1: 전체 EditMode 테스트 실행**

Run: `run_tests` (EditMode)
Expected: Entity, Dungeon, Battle, Card, Poker 테스트 모두 PASS

- [ ] **Step 2: 실패 시 원인 분석 및 수정**

실패하는 테스트가 있으면 원인을 파악하고 수정. 수정 후 전체 테스트 재실행.

- [ ] **Step 3: 최종 커밋 (필요 시)**

수정 사항이 있는 경우에만 커밋:
```bash
git add -A
git commit -m "fix: resolve test regressions after world system implementation"
```
