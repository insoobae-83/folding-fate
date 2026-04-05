# Card System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 트럼프 카드 기반의 BaseCard + CardVariant 2단 데이터 모델 시스템 구현

**Architecture:** Core 레이어에 공용 enum/struct, Features/Card에 도메인 모델(순수 C#) + ScriptableObject(불변 설정 데이터) + CardFactory(변환 System). TDD로 순수 C# 모델을 먼저 테스트하고 구현한다.

**Tech Stack:** Unity 6, C#, NUnit (Unity Test Framework), VContainer

**Spec:** `docs/superpowers/specs/2026-04-02-card-system-design.md`

---

## File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Create | `Assets/Scripts/Core/FoldingFate.Core.asmdef` | Core assembly definition |
| Create | `Assets/Scripts/Core/Enums/Suit.cs` | 문양 enum |
| Create | `Assets/Scripts/Core/Enums/Rank.cs` | 랭크 enum |
| Create | `Assets/Scripts/Core/Enums/Element.cs` | 속성 enum |
| Create | `Assets/Scripts/Core/Enums/StatType.cs` | 스탯 종류 enum |
| Create | `Assets/Scripts/Core/Enums/CardCategory.cs` | 카드 분류 enum |
| Create | `Assets/Scripts/Core/Structs/StatModifier.cs` | 스탯 수정자 struct |
| Create | `Assets/Scripts/Features/FoldingFate.Features.asmdef` | Features assembly definition |
| Create | `Assets/Scripts/Features/Card/Models/BaseCard.cs` | 기본 카드 도메인 모델 |
| Create | `Assets/Scripts/Features/Card/Models/CardVariant.cs` | 카드 변형 도메인 모델 |
| Create | `Assets/Scripts/Features/Card/Data/BaseCardData.cs` | 기본 카드 ScriptableObject |
| Create | `Assets/Scripts/Features/Card/Data/CardVariantData.cs` | 카드 변형 ScriptableObject |
| Create | `Assets/Scripts/Features/Card/Systems/CardFactory.cs` | SO → 도메인 모델 변환 |
| Create | `Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef` | EditMode 테스트 assembly definition |
| Create | `Assets/Tests/EditMode/Card/BaseCardTests.cs` | BaseCard 단위 테스트 |
| Create | `Assets/Tests/EditMode/Card/CardVariantTests.cs` | CardVariant 단위 테스트 |
| Create | `Assets/Tests/EditMode/Card/CardFactoryTests.cs` | CardFactory 단위 테스트 |

---

## Task 1: Assembly Definitions 생성

프로젝트의 첫 asmdef 파일들을 만든다. 이후 모든 코드가 이 어셈블리 안에 들어간다.

**Files:**
- Create: `Assets/Scripts/Core/FoldingFate.Core.asmdef`
- Create: `Assets/Scripts/Features/FoldingFate.Features.asmdef`
- Create: `Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef`

- [ ] **Step 1: Core asmdef 생성**

```json
// Assets/Scripts/Core/FoldingFate.Core.asmdef
{
    "name": "FoldingFate.Core",
    "rootNamespace": "FoldingFate.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

Note: `noEngineReferences: true` — Core는 순수 C#이므로 UnityEngine 참조 불필요.

- [ ] **Step 2: Features asmdef 생성**

```json
// Assets/Scripts/Features/FoldingFate.Features.asmdef
{
    "name": "FoldingFate.Features",
    "rootNamespace": "FoldingFate.Features",
    "references": [
        "FoldingFate.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Tests.EditMode asmdef 생성**

```json
// Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef
{
    "name": "FoldingFate.Tests.EditMode",
    "rootNamespace": "FoldingFate.Tests.EditMode",
    "references": [
        "FoldingFate.Core",
        "FoldingFate.Features"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Unity에서 컴파일 확인**

Unity Editor에서 콘솔에 에러가 없는지 확인한다.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/FoldingFate.Core.asmdef Assets/Scripts/Core/FoldingFate.Core.asmdef.meta Assets/Scripts/Features/FoldingFate.Features.asmdef Assets/Scripts/Features/FoldingFate.Features.asmdef.meta Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef.meta
git commit -m "chore: add Core, Features, Tests.EditMode assembly definitions"
```

---

## Task 2: Core Enums & Structs

카드 시스템의 기본 타입들을 정의한다. 순수 C#, Unity 의존성 없음 (StatModifier만 `System.Serializable` 사용).

**Files:**
- Create: `Assets/Scripts/Core/Enums/Suit.cs`
- Create: `Assets/Scripts/Core/Enums/Rank.cs`
- Create: `Assets/Scripts/Core/Enums/Element.cs`
- Create: `Assets/Scripts/Core/Enums/StatType.cs`
- Create: `Assets/Scripts/Core/Enums/CardCategory.cs`
- Create: `Assets/Scripts/Core/Structs/StatModifier.cs`

- [ ] **Step 1: Suit.cs 생성**

```csharp
// Assets/Scripts/Core/Enums/Suit.cs
namespace FoldingFate.Core
{
    public enum Suit
    {
        Spade,
        Heart,
        Diamond,
        Club
    }
}
```

- [ ] **Step 2: Rank.cs 생성**

```csharp
// Assets/Scripts/Core/Enums/Rank.cs
namespace FoldingFate.Core
{
    public enum Rank
    {
        Ace = 1,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }
}
```

- [ ] **Step 3: Element.cs 생성**

```csharp
// Assets/Scripts/Core/Enums/Element.cs
namespace FoldingFate.Core
{
    public enum Element
    {
        None,
        Fire,
        Water,
        Earth
    }
}
```

- [ ] **Step 4: StatType.cs 생성**

```csharp
// Assets/Scripts/Core/Enums/StatType.cs
namespace FoldingFate.Core
{
    public enum StatType
    {
        Attack,
        Point
    }
}
```

- [ ] **Step 5: CardCategory.cs 생성**

```csharp
// Assets/Scripts/Core/Enums/CardCategory.cs
namespace FoldingFate.Core
{
    public enum CardCategory
    {
        Standard,
        Joker,
        Custom
    }
}
```

- [ ] **Step 6: StatModifier.cs 생성**

```csharp
// Assets/Scripts/Core/Structs/StatModifier.cs
using System;

namespace FoldingFate.Core
{
    [Serializable]
    public struct StatModifier
    {
        public StatType Type;
        public float Value;

        public StatModifier(StatType type, float value)
        {
            Type = type;
            Value = value;
        }
    }
}
```

- [ ] **Step 7: Unity에서 컴파일 확인**

Unity Editor 콘솔에 에러가 없는지 확인한다.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Core/Enums/ Assets/Scripts/Core/Structs/
git commit -m "feat(card): add core enums and StatModifier struct"
```

---

## Task 3: BaseCard 도메인 모델 (TDD)

순수 C# BaseCard 모델을 TDD로 구현한다.

**Files:**
- Test: `Assets/Tests/EditMode/Card/BaseCardTests.cs`
- Create: `Assets/Scripts/Features/Card/Models/BaseCard.cs`

- [ ] **Step 1: BaseCardTests.cs — 실패하는 테스트 작성**

```csharp
// Assets/Tests/EditMode/Card/BaseCardTests.cs
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class BaseCardTests
    {
        [Test]
        public void Constructor_StandardCard_SetsProperties()
        {
            var card = new BaseCard(
                id: "standard_spade_ace",
                category: CardCategory.Standard,
                suit: Suit.Spade,
                rank: Rank.Ace,
                displayName: "Ace of Spades",
                description: "A standard ace of spades");

            Assert.AreEqual("standard_spade_ace", card.Id);
            Assert.AreEqual(CardCategory.Standard, card.Category);
            Assert.AreEqual(Suit.Spade, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
            Assert.AreEqual("Ace of Spades", card.DisplayName);
            Assert.AreEqual("A standard ace of spades", card.Description);
        }

        [Test]
        public void IsStandard_StandardCard_ReturnsTrue()
        {
            var card = new BaseCard(
                id: "standard_heart_king",
                category: CardCategory.Standard,
                suit: Suit.Heart,
                rank: Rank.King,
                displayName: "King of Hearts",
                description: "");

            Assert.IsTrue(card.IsStandard);
            Assert.IsFalse(card.IsJoker);
            Assert.IsFalse(card.IsCustom);
        }

        [Test]
        public void IsJoker_JokerCard_ReturnsTrue()
        {
            var card = new BaseCard(
                id: "joker_red",
                category: CardCategory.Joker,
                suit: null,
                rank: null,
                displayName: "Red Joker",
                description: "");

            Assert.IsTrue(card.IsJoker);
            Assert.IsFalse(card.IsStandard);
            Assert.IsNull(card.Suit);
            Assert.IsNull(card.Rank);
        }

        [Test]
        public void IsCustom_CustomCard_ReturnsTrue()
        {
            var card = new BaseCard(
                id: "custom_wild",
                category: CardCategory.Custom,
                suit: null,
                rank: null,
                displayName: "Wild Card",
                description: "A special custom card");

            Assert.IsTrue(card.IsCustom);
            Assert.IsFalse(card.IsStandard);
            Assert.IsFalse(card.IsJoker);
        }
    }
}
```

- [ ] **Step 2: Unity Test Runner에서 테스트 실행 — 컴파일 에러 확인**

테스트가 `BaseCard` 클래스를 찾지 못해 컴파일 에러가 발생해야 한다.

- [ ] **Step 3: BaseCard.cs 구현**

```csharp
// Assets/Scripts/Features/Card/Models/BaseCard.cs
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class BaseCard
    {
        public string Id { get; }
        public CardCategory Category { get; }
        public Suit? Suit { get; }
        public Rank? Rank { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public bool IsStandard => Category == CardCategory.Standard;
        public bool IsJoker => Category == CardCategory.Joker;
        public bool IsCustom => Category == CardCategory.Custom;

        public BaseCard(
            string id,
            CardCategory category,
            Suit? suit,
            Rank? rank,
            string displayName,
            string description)
        {
            Id = id;
            Category = category;
            Suit = suit;
            Rank = rank;
            DisplayName = displayName;
            Description = description;
        }
    }
}
```

- [ ] **Step 4: Unity Test Runner에서 테스트 실행 — 전부 PASS 확인**

EditMode 테스트 4개 모두 통과해야 한다.

- [ ] **Step 5: Commit**

```bash
git add Assets/Tests/EditMode/Card/ Assets/Scripts/Features/Card/Models/BaseCard.cs
git commit -m "feat(card): add BaseCard domain model with tests"
```

---

## Task 4: CardVariant 도메인 모델 (TDD)

CardVariant 모델을 TDD로 구현한다. BaseCard를 참조하며 외형/속성/스탯을 독립적으로 조합.

**Files:**
- Test: `Assets/Tests/EditMode/Card/CardVariantTests.cs`
- Create: `Assets/Scripts/Features/Card/Models/CardVariant.cs`

- [ ] **Step 1: CardVariantTests.cs — 실패하는 테스트 작성**

```csharp
// Assets/Tests/EditMode/Card/CardVariantTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class CardVariantTests
    {
        private BaseCard _baseSpadeAce;

        [SetUp]
        public void SetUp()
        {
            _baseSpadeAce = new BaseCard(
                id: "standard_spade_ace",
                category: CardCategory.Standard,
                suit: Suit.Spade,
                rank: Rank.Ace,
                displayName: "Ace of Spades",
                description: "");
        }

        [Test]
        public void Constructor_FullVariant_SetsAllProperties()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 3f)
            };

            var variant = new CardVariant(
                id: "fire_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Fire Ace of Spades",
                skinId: "skin_fire",
                element: Element.Fire,
                statModifiers: modifiers);

            Assert.AreEqual("fire_spade_ace", variant.Id);
            Assert.AreSame(_baseSpadeAce, variant.BaseCard);
            Assert.AreEqual("Fire Ace of Spades", variant.DisplayName);
            Assert.AreEqual("skin_fire", variant.SkinId);
            Assert.AreEqual(Element.Fire, variant.Element);
            Assert.AreEqual(1, variant.StatModifiers.Count);
        }

        [Test]
        public void HasElement_NoElement_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasElement);
        }

        [Test]
        public void HasElement_WithElement_ReturnsTrue()
        {
            var variant = new CardVariant(
                id: "water_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Water Ace of Spades",
                skinId: "",
                element: Element.Water,
                statModifiers: new List<StatModifier>());

            Assert.IsTrue(variant.HasElement);
        }

        [Test]
        public void HasSkin_EmptySkinId_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasSkin);
        }

        [Test]
        public void HasSkin_NullSkinId_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: null,
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasSkin);
        }

        [Test]
        public void HasSkin_WithSkinId_ReturnsTrue()
        {
            var variant = new CardVariant(
                id: "golden_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Golden Ace of Spades",
                skinId: "skin_golden",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsTrue(variant.HasSkin);
        }

        [Test]
        public void HasStatModifiers_Empty_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasStatModifiers);
        }

        [Test]
        public void HasStatModifiers_WithModifiers_ReturnsTrue()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 5f)
            };

            var variant = new CardVariant(
                id: "strong_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Strong Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: modifiers);

            Assert.IsTrue(variant.HasStatModifiers);
        }

        [Test]
        public void GetStatValue_SingleModifier_ReturnsValue()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 3f)
            };

            var variant = new CardVariant(
                id: "attack_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Attack Ace",
                skinId: "",
                element: Element.None,
                statModifiers: modifiers);

            Assert.AreEqual(3f, variant.GetStatValue(StatType.Attack));
        }

        [Test]
        public void GetStatValue_DuplicateStatType_ReturnsSummed()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 3f),
                new StatModifier(StatType.Attack, 5f)
            };

            var variant = new CardVariant(
                id: "double_attack_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Double Attack Ace",
                skinId: "",
                element: Element.None,
                statModifiers: modifiers);

            Assert.AreEqual(8f, variant.GetStatValue(StatType.Attack));
        }

        [Test]
        public void GetStatValue_MissingStatType_ReturnsZero()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.AreEqual(0f, variant.GetStatValue(StatType.Attack));
        }

        [Test]
        public void StatModifiers_IsReadOnly_CannotModifyExternally()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 3f)
            };

            var variant = new CardVariant(
                id: "attack_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Attack Ace",
                skinId: "",
                element: Element.None,
                statModifiers: modifiers);

            // 원본 리스트를 변경해도 CardVariant 내부에 영향 없음
            modifiers.Add(new StatModifier(StatType.Point, 10f));
            Assert.AreEqual(1, variant.StatModifiers.Count);
        }
    }
}
```

- [ ] **Step 2: Unity Test Runner에서 테스트 실행 — 컴파일 에러 확인**

`CardVariant` 클래스를 찾지 못해 컴파일 에러가 발생해야 한다.

- [ ] **Step 3: CardVariant.cs 구현**

```csharp
// Assets/Scripts/Features/Card/Models/CardVariant.cs
using System.Collections.Generic;
using System.Linq;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class CardVariant
    {
        public string Id { get; }
        public BaseCard BaseCard { get; }
        public string DisplayName { get; }
        public string SkinId { get; }
        public Element Element { get; }
        public IReadOnlyList<StatModifier> StatModifiers { get; }

        public bool HasElement => Element != Element.None;
        public bool HasSkin => !string.IsNullOrEmpty(SkinId);
        public bool HasStatModifiers => StatModifiers.Count > 0;

        public CardVariant(
            string id,
            BaseCard baseCard,
            string displayName,
            string skinId,
            Element element,
            List<StatModifier> statModifiers)
        {
            Id = id;
            BaseCard = baseCard;
            DisplayName = displayName;
            SkinId = skinId;
            Element = element;
            StatModifiers = new List<StatModifier>(statModifiers).AsReadOnly();
        }

        public float GetStatValue(StatType type)
        {
            float sum = 0f;
            for (int i = 0; i < StatModifiers.Count; i++)
            {
                if (StatModifiers[i].Type == type)
                {
                    sum += StatModifiers[i].Value;
                }
            }
            return sum;
        }
    }
}
```

Note: `GetStatValue`는 당분간 합산으로 구현. 처리 방식 변경이 필요하면 이 메서드만 수정하면 된다.

Note: LINQ 대신 for 루프 사용 — 게임 코드에서 GC 할당 최소화.

- [ ] **Step 4: Unity Test Runner에서 테스트 실행 — 전부 PASS 확인**

EditMode 테스트 12개 모두 통과해야 한다.

- [ ] **Step 5: Commit**

```bash
git add Assets/Tests/EditMode/Card/CardVariantTests.cs Assets/Scripts/Features/Card/Models/CardVariant.cs
git commit -m "feat(card): add CardVariant domain model with tests"
```

---

## Task 5: ScriptableObject 데이터 클래스

Unity Editor에서 카드 에셋을 생성할 수 있는 SO 클래스를 만든다.

**Files:**
- Create: `Assets/Scripts/Features/Card/Data/BaseCardData.cs`
- Create: `Assets/Scripts/Features/Card/Data/CardVariantData.cs`

- [ ] **Step 1: BaseCardData.cs 생성**

```csharp
// Assets/Scripts/Features/Card/Data/BaseCardData.cs
using UnityEngine;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Data
{
    [CreateAssetMenu(fileName = "NewBaseCard", menuName = "FoldingFate/Card/BaseCardData")]
    public class BaseCardData : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public CardCategory Category { get; private set; }
        [field: SerializeField] public Suit Suit { get; private set; }
        [field: SerializeField] public Rank Rank { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
    }
}
```

Note: Unity SerializeField는 nullable value type(`Suit?`)을 직렬화하지 못한다. SO에서는 non-nullable로 정의하고, Joker/Custom의 경우 Suit/Rank 값은 무시한다. 도메인 모델 변환 시 CardFactory가 Category를 보고 null로 변환한다.

- [ ] **Step 2: CardVariantData.cs 생성**

```csharp
// Assets/Scripts/Features/Card/Data/CardVariantData.cs
using System.Collections.Generic;
using UnityEngine;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Data
{
    [CreateAssetMenu(fileName = "NewCardVariant", menuName = "FoldingFate/Card/CardVariantData")]
    public class CardVariantData : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public BaseCardData BaseCard { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string SkinId { get; private set; }
        [field: SerializeField] public Element Element { get; private set; }
        [field: SerializeField] public List<StatModifier> StatModifiers { get; private set; }
    }
}
```

- [ ] **Step 3: Unity에서 컴파일 확인**

Unity Editor 콘솔에 에러가 없는지 확인. Assets > Create > FoldingFate > Card 메뉴가 나타나는지 확인.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Features/Card/Data/
git commit -m "feat(card): add BaseCardData and CardVariantData ScriptableObjects"
```

---

## Task 6: CardFactory System (TDD)

SO → 도메인 모델 변환을 담당하는 CardFactory를 TDD로 구현한다.

**Files:**
- Test: `Assets/Tests/EditMode/Card/CardFactoryTests.cs`
- Create: `Assets/Scripts/Features/Card/Systems/CardFactory.cs`

- [ ] **Step 1: CardFactoryTests.cs — 실패하는 테스트 작성**

```csharp
// Assets/Tests/EditMode/Card/CardFactoryTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FoldingFate.Core;
using FoldingFate.Features.Card.Data;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class CardFactoryTests
    {
        private CardFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CardFactory();
        }

        [Test]
        public void CreateBaseCard_StandardCard_MapsAllFields()
        {
            var data = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(data, "standard_spade_ace", CardCategory.Standard,
                Suit.Spade, Rank.Ace, "Ace of Spades", "A standard card");

            var card = _factory.CreateBaseCard(data);

            Assert.AreEqual("standard_spade_ace", card.Id);
            Assert.AreEqual(CardCategory.Standard, card.Category);
            Assert.AreEqual(Suit.Spade, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
            Assert.AreEqual("Ace of Spades", card.DisplayName);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateBaseCard_JokerCard_SuitAndRankAreNull()
        {
            var data = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(data, "joker_red", CardCategory.Joker,
                Suit.Spade, Rank.Ace, "Red Joker", "");

            var card = _factory.CreateBaseCard(data);

            Assert.AreEqual(CardCategory.Joker, card.Category);
            Assert.IsNull(card.Suit);
            Assert.IsNull(card.Rank);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateBaseCard_CustomCard_SuitAndRankAreNull()
        {
            var data = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(data, "custom_wild", CardCategory.Custom,
                Suit.Spade, Rank.Ace, "Wild Card", "");

            var card = _factory.CreateBaseCard(data);

            Assert.AreEqual(CardCategory.Custom, card.Category);
            Assert.IsNull(card.Suit);
            Assert.IsNull(card.Rank);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateCardVariant_MapsAllFields()
        {
            var baseData = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(baseData, "standard_spade_ace", CardCategory.Standard,
                Suit.Spade, Rank.Ace, "Ace of Spades", "");

            var variantData = ScriptableObject.CreateInstance<CardVariantData>();
            SetCardVariantData(variantData, "fire_spade_ace", baseData,
                "Fire Ace", "skin_fire", Element.Fire,
                new List<StatModifier> { new StatModifier(StatType.Attack, 3f) });

            var baseCard = _factory.CreateBaseCard(baseData);
            var variant = _factory.CreateCardVariant(variantData, baseCard);

            Assert.AreEqual("fire_spade_ace", variant.Id);
            Assert.AreSame(baseCard, variant.BaseCard);
            Assert.AreEqual("Fire Ace", variant.DisplayName);
            Assert.AreEqual("skin_fire", variant.SkinId);
            Assert.AreEqual(Element.Fire, variant.Element);
            Assert.AreEqual(1, variant.StatModifiers.Count);
            Assert.AreEqual(3f, variant.GetStatValue(StatType.Attack));

            Object.DestroyImmediate(baseData);
            Object.DestroyImmediate(variantData);
        }

        private static void SetBaseCardData(
            BaseCardData data, string id, CardCategory category,
            Suit suit, Rank rank, string displayName, string description)
        {
            var so = new SerializedObject(data);
            so.FindProperty("<Id>k__BackingField").stringValue = id;
            so.FindProperty("<Category>k__BackingField").enumValueIndex = (int)category;
            so.FindProperty("<Suit>k__BackingField").enumValueIndex = (int)suit;
            so.FindProperty("<Rank>k__BackingField").enumValueIndex = (int)rank;
            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<Description>k__BackingField").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCardVariantData(
            CardVariantData data, string id, BaseCardData baseCard,
            string displayName, string skinId, Element element,
            List<StatModifier> statModifiers)
        {
            var so = new SerializedObject(data);
            so.FindProperty("<Id>k__BackingField").stringValue = id;
            so.FindProperty("<BaseCard>k__BackingField").objectReferenceValue = baseCard;
            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<SkinId>k__BackingField").stringValue = skinId;
            so.FindProperty("<Element>k__BackingField").enumValueIndex = (int)element;

            var modsProp = so.FindProperty("<StatModifiers>k__BackingField");
            modsProp.ClearArray();
            for (int i = 0; i < statModifiers.Count; i++)
            {
                modsProp.InsertArrayElementAtIndex(i);
                var elem = modsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Type").enumValueIndex = (int)statModifiers[i].Type;
                elem.FindPropertyRelative("Value").floatValue = statModifiers[i].Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
```

Note: `[field: SerializeField]` 프로퍼티는 backing field 이름이 `<PropertyName>k__BackingField` 형태이므로, `SerializedObject`를 통해 테스트용 값을 설정한다. `UnityEditor` 네임스페이스를 사용하므로 EditMode 테스트에서만 가능.

- [ ] **Step 2: Unity Test Runner에서 테스트 실행 — 컴파일 에러 확인**

`CardFactory` 클래스를 찾지 못해 컴파일 에러가 발생해야 한다.

- [ ] **Step 3: CardFactory.cs 구현**

```csharp
// Assets/Scripts/Features/Card/Systems/CardFactory.cs
using System.Collections.Generic;
using FoldingFate.Core;
using FoldingFate.Features.Card.Data;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Card.Systems
{
    public class CardFactory
    {
        public BaseCard CreateBaseCard(BaseCardData data)
        {
            Suit? suit = data.Category == CardCategory.Standard ? data.Suit : null;
            Rank? rank = data.Category == CardCategory.Standard ? data.Rank : null;

            return new BaseCard(
                id: data.Id,
                category: data.Category,
                suit: suit,
                rank: rank,
                displayName: data.DisplayName,
                description: data.Description);
        }

        public CardVariant CreateCardVariant(CardVariantData data, BaseCard baseCard)
        {
            var modifiers = new List<StatModifier>();
            if (data.StatModifiers != null)
            {
                modifiers.AddRange(data.StatModifiers);
            }

            return new CardVariant(
                id: data.Id,
                baseCard: baseCard,
                displayName: data.DisplayName,
                skinId: data.SkinId,
                element: data.Element,
                statModifiers: modifiers);
        }
    }
}
```

- [ ] **Step 4: CardFactoryTests.cs에 UnityEditor using 추가**

테스트 파일 상단에 `using UnityEditor;`를 추가한다 (`SerializedObject` 사용을 위해).

```csharp
using UnityEditor;
```

- [ ] **Step 5: Unity Test Runner에서 테스트 실행 — 전부 PASS 확인**

CardFactory 테스트 4개 모두 통과해야 한다.

- [ ] **Step 6: Commit**

```bash
git add Assets/Tests/EditMode/Card/CardFactoryTests.cs Assets/Scripts/Features/Card/Systems/
git commit -m "feat(card): add CardFactory system with tests"
```

---

## Task 7: 전체 테스트 실행 및 최종 확인

모든 테스트를 한번에 실행하고, 프로젝트 구조를 최종 확인한다.

- [ ] **Step 1: Unity Test Runner에서 EditMode 전체 테스트 실행**

총 테스트 수: BaseCardTests(4) + CardVariantTests(12) + CardFactoryTests(4) = 20개. 전부 PASS 확인.

- [ ] **Step 2: Unity 콘솔에 에러/워닝 없는지 확인**

- [ ] **Step 3: 최종 폴더 구조 확인**

```
Assets/Scripts/
  Core/
    FoldingFate.Core.asmdef
    Enums/
      Suit.cs
      Rank.cs
      Element.cs
      StatType.cs
      CardCategory.cs
    Structs/
      StatModifier.cs
  Features/
    FoldingFate.Features.asmdef
    Card/
      Models/
        BaseCard.cs
        CardVariant.cs
      Data/
        BaseCardData.cs
        CardVariantData.cs
      Systems/
        CardFactory.cs
Assets/Tests/
  EditMode/
    FoldingFate.Tests.EditMode.asmdef
    Card/
      BaseCardTests.cs
      CardVariantTests.cs
      CardFactoryTests.cs
```

- [ ] **Step 4: Commit (구조 확인 후 필요 시)**

변경 사항이 있으면 커밋한다.
