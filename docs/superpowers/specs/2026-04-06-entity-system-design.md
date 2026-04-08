# Entity System Design

## Overview

월드 엔티티 데이터 모델 시스템. CBD(Component-Based Design) 기반으로, Entity는 빈 껍데기이고 능력(IEntityComponent)을 조합하여 캐릭터, 몬스터, 보스, NPC, 오브젝트, 이펙트 영역 등을 구성한다.

## Requirements

- 상호작용이 있는 모든 월드 객체를 엔티티로 취급 (렌더링만 하는 배경 객체 제외)
- 엔티티 공통: Id
- CBD 컴포지션 방식: Entity는 빈 컨테이너, 능력은 IEntityComponent 구현체로 조합
- 컴포넌트는 자신이 속한 Entity(Owner)를 알고 있음
- Anemic Domain Model: 컴포넌트는 데이터와 파생 속성(읽기 전용 계산값)만 소유. 생성자에서 구조적 무결성(필수 초기값 설정)은 처리하되, 상태 변경 로직은 두지 않는다. 모든 처리 로직은 System에 둔다
- 스탯: Base + Modifiers(출처 추적, 해제 가능) = Current. 카드 시스템의 StatType과는 별도 체계
- 이번 구현 범위: 순수 C# 데이터 모델만 (MonoBehaviour, 전투 시스템, 카드 연결은 이후)

## Architecture: CBD 컴포지션 (Dictionary 기반)

### Core 레이어

Entity와 IEntityComponent는 다른 피처에서도 사용되므로 Core에 배치.

#### IEntityComponent

```csharp
namespace FoldingFate.Core
{
    public interface IEntityComponent
    {
        Entity Owner { get; set; }
    }
}
```

모든 엔티티 컴포넌트의 기반 인터페이스. Owner는 Entity.Add 시 자동 설정된다.

#### Entity

```csharp
using System;
using System.Collections.Generic;

namespace FoldingFate.Core
{
    public class Entity
    {
        public string Id { get; }
        private readonly Dictionary<Type, IEntityComponent> _components = new();

        public Entity(string id)
        {
            Id = id;
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

### Core Enums

```csharp
namespace FoldingFate.Core
{
    public enum EntityStatType
    {
        MaxHp,
        Attack,
        Defense,
        Speed,
        Mana,
        CriticalRate,
        EvasionRate
    }
}
```

자유 확장 가능. 아이템/카드 효과 등으로 새로운 스탯이 필요하면 값 추가.

### Features/Entity 레이어

엔티티 전용 컴포넌트 구현체와 관련 타입.

#### ModifierSource (Enum)

```csharp
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

#### EntityStatModifier (Struct)

```csharp
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

### 컴포넌트 구현체 (Features/Entity/Models)

컴포넌트는 Anemic Domain Model 원칙을 따른다: 데이터와 파생 속성만 소유하고, 상태 변경 로직은 두지 않는다. 다른 컴포넌트를 직접 참조하지 않는다.

#### Stats

```csharp
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

#### Health

```csharp
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

#### Combat

```csharp
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

#### Dialogue

```csharp
using FoldingFate.Core;

namespace FoldingFate.Features.Entity.Models
{
    public class Dialogue : IEntityComponent
    {
        public Entity Owner { get; set; }

        public string DialogueId { get; }

        public Dialogue(string dialogueId)
        {
            DialogueId = dialogueId;
        }
    }
}
```

#### Interactable

```csharp
using FoldingFate.Core;

namespace FoldingFate.Features.Entity.Models
{
    public class Interactable : IEntityComponent
    {
        public Entity Owner { get; set; }

        public string InteractionType { get; }
        public bool IsInteractable { get; set; }

        public Interactable(string interactionType)
        {
            InteractionType = interactionType;
            IsInteractable = true;
        }
    }
}
```

### 엔티티 조합 예시

| 엔티티 | 컴포넌트 조합 |
|---|---|
| 플레이어 | Stats + Health + Combat |
| 몬스터 | Stats + Health + Combat |
| 보스 | Stats + Health + Combat |
| NPC | Dialogue (+ 필요시 Stats + Health) |
| 상자/문 | Interactable |
| 힐 영역 | Interactable + Stats (효과 수치용) |

### Systems (Features/Entity/Systems)

컴포넌트 데이터를 읽고 쓰는 모든 처리 로직은 System에 둔다.

#### HealthSystem

```csharp
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

#### StatsSystem

```csharp
using System.Collections.Generic;
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

#### EntityFactory

```csharp
namespace FoldingFate.Features.Entity.Systems
{
    public class EntityFactory
    {
        public Entity CreateCombatEntity(string id, float maxHp);
        public Entity CreateNpc(string id, string dialogueId);
        public Entity CreateInteractable(string id, string interactionType);
    }
}
```

편의 메서드로 자주 쓰는 조합을 제공. 직접 Entity + Add로 커스텀 조합도 가능.

## Folder Structure

```
Assets/Scripts/
  Core/
    Enums/
      EntityStatType.cs
    Interfaces/
      IEntityComponent.cs
    Entity/
      Entity.cs

  Features/
    Entity/
      Models/
        Stats.cs
        Health.cs
        Combat.cs
        Dialogue.cs
        Interactable.cs
      Enums/
        ModifierSource.cs
      Structs/
        EntityStatModifier.cs
      Systems/
        EntityFactory.cs
        HealthSystem.cs
        StatsSystem.cs

Assets/Tests/
  EditMode/
    Entity/
      EntityTests.cs
      StatsSystemTests.cs
      HealthSystemTests.cs
```

## Assembly Definition References

기존 asmdef 사용:
- `FoldingFate.Core`: Entity, IEntityComponent, EntityStatType 추가
- `FoldingFate.Features`: 컴포넌트 구현체, EntityFactory 추가
- `FoldingFate.Tests.EditMode`: 테스트 추가

Core asmdef에 `noEngineReferences: true` 유지 — Entity와 IEntityComponent는 순수 C#.

## Test Strategy

EditMode 단위 테스트 (순수 C# 모델).

### Entity Tests
- Add로 컴포넌트 추가 후 Get으로 조회
- Add 시 Owner 자동 설정
- Has로 존재 여부 확인
- Remove로 제거 후 Has == false
- Remove 시 Owner가 null로 해제
- 존재하지 않는 컴포넌트 Get 시 null 반환

### StatsSystem Tests
- BaseStats 설정 후 GetValue = Base 값 반환
- Modifier 추가 후 GetValue = Base + Modifier 합산
- 같은 StatType Modifier 여러 개 합산
- RemoveModifiersBySourceId로 특정 출처 수정자 제거
- RemoveModifiersBySource로 출처 종류별 일괄 제거
- 없는 StatType 조회 시 0 반환

### HealthSystem Tests
- TakeDamage: CurrentHp 감소
- TakeDamage: CurrentHp가 0 이하로 내려가지 않음
- Heal: CurrentHp 증가
- Heal: MaxHp 초과하지 않음
- SetMaxHp: CurrentHp가 새 MaxHp 초과 시 조정

### Health 컴포넌트 Tests
- 생성 시 CurrentHp == MaxHp
- IsAlive/IsDead 파생 속성 검증

### Combat 컴포넌트 Tests
- IsInCombat 기본값 false
- 직접 값 설정 후 상태 확인

### Interactable 컴포넌트 Tests
- 생성 시 기본 IsInteractable == true
- InteractionType 설정 확인

### EntityFactory Tests
- CreateCombatEntity: Stats + Health + Combat 조합 확인
- CreateNpc: Dialogue 컴포넌트 확인
- CreateInteractable: Interactable 컴포넌트 확인

## Open Decisions

- [ ] 카드 효과 → 엔티티 스탯 적용 연결 (카드 시스템 연동)
- [ ] MonoBehaviour 컴포넌트 (월드 배치, 렌더링, 물리)
- [ ] 전투 시스템 (System 레이어 — 데미지 계산, 턴 진행 등)
- [ ] NPC의 HP 보유 여부 (전투 가능 NPC)
- [ ] 엔티티 설정 데이터용 ScriptableObject (EntityData 등)
