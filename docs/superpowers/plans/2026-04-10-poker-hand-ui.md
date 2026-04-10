# Poker Hand UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 1인 플레이어가 52장 덱에서 8장을 받아 1~5장 선택 후 족보를 확인하고 핸드를 보충하는 포커 프로토타입 UI를 구현한다.

**Architecture:** DeckModel/HandModel이 ReactiveProperty로 상태를 노출하고 PokerViewModel이 구독해 View에 바인딩한다. RoundController(IStartable)가 흐름을 조율하며 DealSystem(순수 비즈니스 로직)을 호출한다. View는 ViewModel의 ReactiveCommand만 Execute한다.

**Tech Stack:** C# 9, Unity 6 UI Toolkit (UXML/USS), R3 (ReactiveProperty/ReactiveCommand), VContainer (IStartable/IDisposable), NUnit

---

## File Map

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Features/Poker/Models/DeckModel.cs` | 52장 덱 상태 + shuffle/draw |
| `Assets/Scripts/Features/Poker/Models/HandModel.cs` | 핸드 상태 + 선택 토글 |
| `Assets/Scripts/Features/Poker/Systems/DealSystem.cs` | 순수 비즈니스 로직 (5장 제한, 평가, 보충) |
| `Assets/Scripts/Features/Poker/Controllers/RoundController.cs` | 흐름 제어 (IStartable), ViewModel 커맨드 구독 |
| `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` | R3 상태 노출 + ReactiveCommand |
| `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` | UIDocument 바인딩 컨트롤러 |
| `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml` | 전체 HUD 레이아웃 |
| `Assets/Scripts/Features/Poker/UI/Uxml/CardElement.uxml` | 카드 한 장 템플릿 |
| `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss` | Placeholder 스타일 |
| `Assets/Scripts/Features/Poker/PokerInstaller.cs` | VContainer LifetimeScope |
| `Assets/Scripts/Features/FoldingFate.Features.asmdef` | R3/VContainer 참조 추가 (수정) |
| `Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef` | R3 참조 추가 (수정) |
| `Assets/Tests/EditMode/Poker/DeckModelTests.cs` | DeckModel 단위 테스트 |
| `Assets/Tests/EditMode/Poker/HandModelTests.cs` | HandModel 단위 테스트 |
| `Assets/Tests/EditMode/Poker/DealSystemTests.cs` | DealSystem 단위 테스트 |
| `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` | RoundController 통합 테스트 |

---

## Task 1: Assembly References 추가

**Files:**
- Modify: `Assets/Scripts/Features/FoldingFate.Features.asmdef`
- Modify: `Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef`

- [ ] **Step 1: VContainer/R3 assembly 이름 확인**

Unity Editor에서 `Window > Package Manager`를 열고 VContainer, R3 패키지를 선택한 뒤 하단에 표시되는 assembly 이름을 확인한다.
일반적으로: VContainer → `"VContainer"`, R3 → `"R3"` and `"R3.Unity"`.

또는 프로젝트 내 패키지 캐시에서 asmdef 파일을 확인한다:
```
Library/PackageCache/jp.hadashikick.vcontainer@*/VContainer.asmdef
Library/PackageCache/com.cysharp.r3@*/R3.asmdef
```

- [ ] **Step 2: FoldingFate.Features.asmdef 업데이트**

`Assets/Scripts/Features/FoldingFate.Features.asmdef`를 아래와 같이 수정한다:

```json
{
    "name": "FoldingFate.Features",
    "rootNamespace": "FoldingFate.Features",
    "references": [
        "FoldingFate.Core",
        "VContainer",
        "R3",
        "R3.Unity"
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

> assembly 이름이 Step 1에서 확인한 것과 다르면 해당 이름으로 교체한다.

- [ ] **Step 3: FoldingFate.Tests.EditMode.asmdef 업데이트**

```json
{
    "name": "FoldingFate.Tests.EditMode",
    "rootNamespace": "FoldingFate.Tests.EditMode",
    "references": [
        "FoldingFate.Core",
        "FoldingFate.Features",
        "R3",
        "R3.Unity"
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

- [ ] **Step 4: Unity Editor에서 컴파일 오류 없음 확인**

Unity Editor 하단 Console에 오류 없이 컴파일 완료되는지 확인한다.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/FoldingFate.Features.asmdef
git add Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef
git commit -m "chore: add R3 and VContainer references to feature assemblies"
```

---

## Task 2: DeckModel

**Files:**
- Create: `Assets/Scripts/Features/Poker/Models/DeckModel.cs`
- Create: `Assets/Tests/EditMode/Poker/DeckModelTests.cs`

- [ ] **Step 1: 테스트 파일 작성**

`Assets/Tests/EditMode/Poker/DeckModelTests.cs`:

```csharp
using NUnit.Framework;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class DeckModelTests
    {
        private DeckModel _deck;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _deck.Initialize();
        }

        [Test]
        public void Initialize_Creates52Cards()
        {
            Assert.AreEqual(52, _deck.RemainingCount.Value);
        }

        [Test]
        public void Draw_ReturnsRequestedCount_AndReducesRemaining()
        {
            var drawn = _deck.Draw(5);
            Assert.AreEqual(5, drawn.Count);
            Assert.AreEqual(47, _deck.RemainingCount.Value);
        }

        [Test]
        public void Draw_WhenNotEnoughCards_AutoResetsAndRedraws()
        {
            // 50장 드로우해서 2장만 남김
            _deck.Draw(50);
            Assert.AreEqual(2, _deck.RemainingCount.Value);

            // 5장 요청 → 자동 리셋 후 재드로우
            var drawn = _deck.Draw(5);
            Assert.AreEqual(5, drawn.Count);
            // 리셋 후 52장에서 5장 드로우 → 47장 남음
            Assert.AreEqual(47, _deck.RemainingCount.Value);
        }

        [Test]
        public void Shuffle_ChangesCardOrder()
        {
            var deck2 = new DeckModel();
            deck2.Initialize();

            // 두 덱의 첫 10장을 비교 (셔플 후 동일 순서일 확률은 극히 낮음)
            var drawn1 = _deck.Draw(10);
            var drawn2 = deck2.Draw(10);

            bool allSame = true;
            for (int i = 0; i < 10; i++)
                if (drawn1[i].Id != drawn2[i].Id) { allSame = false; break; }

            Assert.IsFalse(allSame, "두 독립 셔플의 결과가 동일해서는 안 됩니다");
        }

        [Test]
        public void Draw_ContainsAllFourSuitsAndThirteenRanks()
        {
            var all = _deck.Draw(52);
            var suits = new System.Collections.Generic.HashSet<string>();
            var ranks = new System.Collections.Generic.HashSet<string>();
            foreach (var c in all)
            {
                suits.Add(c.Suit.ToString());
                ranks.Add(c.Rank.ToString());
            }
            Assert.AreEqual(4, suits.Count);
            Assert.AreEqual(13, ranks.Count);
        }
    }
}
```

- [ ] **Step 2: Test Runner에서 실패 확인**

Unity Editor → Window > General > Test Runner → EditMode 탭 → `DeckModelTests` 실행.
예상: `DeckModel` 클래스 없음으로 컴파일 오류 또는 테스트 실패.

- [ ] **Step 3: DeckModel 구현**

`Assets/Scripts/Features/Poker/Models/DeckModel.cs`:

```csharp
using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class DeckModel
    {
        private readonly List<BaseCard> _cards = new();
        private readonly Random _random = new();

        public ReactiveProperty<int> RemainingCount { get; } = new(0);

        public void Initialize()
        {
            _cards.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new BaseCard(
                        id: $"{suit}_{rank}",
                        category: CardCategory.Standard,
                        suit: suit,
                        rank: rank,
                        displayName: $"{rank} of {suit}s",
                        description: string.Empty));
                }
            }
            Shuffle();
            RemainingCount.Value = _cards.Count;
        }

        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public IReadOnlyList<BaseCard> Draw(int count)
        {
            if (_cards.Count < count)
            {
                Initialize();
            }

            var drawn = _cards.GetRange(0, count);
            _cards.RemoveRange(0, count);
            RemainingCount.Value = _cards.Count;
            return drawn;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → `DeckModelTests` 실행. 예상: 4개 테스트 모두 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/Models/DeckModel.cs
git add Assets/Tests/EditMode/Poker/DeckModelTests.cs
git commit -m "feat(poker): add DeckModel with shuffle and draw"
```

---

## Task 3: HandModel

**Files:**
- Create: `Assets/Scripts/Features/Poker/Models/HandModel.cs`
- Create: `Assets/Tests/EditMode/Poker/HandModelTests.cs`

- [ ] **Step 1: 테스트 파일 작성**

`Assets/Tests/EditMode/Poker/HandModelTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class HandModelTests
    {
        private HandModel _hand;

        private static BaseCard MakeCard(Suit suit, Rank rank) =>
            new BaseCard($"{suit}_{rank}", CardCategory.Standard, suit, rank, "", "");

        [SetUp]
        public void SetUp() => _hand = new HandModel(maxHandSize: 8);

        [Test]
        public void InitialState_IsEmpty()
        {
            Assert.AreEqual(0, _hand.Cards.Value.Count);
            Assert.AreEqual(0, _hand.SelectedIndices.Value.Count);
            Assert.IsFalse(_hand.IsFull);
        }

        [Test]
        public void AddCards_UpdatesCardsAndCount()
        {
            var cards = new List<BaseCard>
            {
                MakeCard(Suit.Spade, Rank.Ace),
                MakeCard(Suit.Heart, Rank.King)
            };
            _hand.AddCards(cards);
            Assert.AreEqual(2, _hand.Cards.Value.Count);
        }

        [Test]
        public void IsFull_TrueWhenAtMaxHandSize()
        {
            var cards = new List<BaseCard>();
            for (int i = 0; i < 8; i++)
                cards.Add(MakeCard(Suit.Spade, Rank.Ace));
            _hand.AddCards(cards);
            Assert.IsTrue(_hand.IsFull);
        }

        [Test]
        public void ToggleSelect_SelectsCard()
        {
            _hand.AddCards(new List<BaseCard> { MakeCard(Suit.Spade, Rank.Ace) });
            _hand.ToggleSelect(0);
            Assert.AreEqual(1, _hand.SelectedIndices.Value.Count);
            Assert.AreEqual(1, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_DeselectsAlreadySelectedCard()
        {
            _hand.AddCards(new List<BaseCard> { MakeCard(Suit.Spade, Rank.Ace) });
            _hand.ToggleSelect(0);
            _hand.ToggleSelect(0);
            Assert.AreEqual(0, _hand.SelectedIndices.Value.Count);
        }

        [Test]
        public void Clear_RemovesAllCardsAndSelections()
        {
            _hand.AddCards(new List<BaseCard> { MakeCard(Suit.Spade, Rank.Ace) });
            _hand.ToggleSelect(0);
            _hand.Clear();
            Assert.AreEqual(0, _hand.Cards.Value.Count);
            Assert.AreEqual(0, _hand.SelectedIndices.Value.Count);
        }
    }
}
```

- [ ] **Step 2: Test Runner에서 실패 확인**

Test Runner → `HandModelTests` 실행. 예상: 컴파일 오류 또는 실패.

- [ ] **Step 3: HandModel 구현**

`Assets/Scripts/Features/Poker/Models/HandModel.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using R3;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class HandModel
    {
        private readonly List<BaseCard> _cards = new();
        private readonly HashSet<int> _selectedIndices = new();

        public int MaxHandSize { get; }
        public ReactiveProperty<IReadOnlyList<BaseCard>> Cards { get; } = new(new List<BaseCard>());
        public ReactiveProperty<IReadOnlyList<int>> SelectedIndices { get; } = new(new List<int>());

        public int SelectedCount => _selectedIndices.Count;
        public bool IsFull => _cards.Count >= MaxHandSize;

        public HandModel(int maxHandSize = 8)
        {
            MaxHandSize = maxHandSize;
        }

        public void AddCards(IReadOnlyList<BaseCard> cards)
        {
            _cards.AddRange(cards);
            Cards.Value = _cards.ToList();
        }

        public void ToggleSelect(int index)
        {
            if (_selectedIndices.Contains(index))
                _selectedIndices.Remove(index);
            else
                _selectedIndices.Add(index);

            SelectedIndices.Value = _selectedIndices.OrderBy(i => i).ToList();
        }

        public void Clear()
        {
            _cards.Clear();
            _selectedIndices.Clear();
            Cards.Value = new List<BaseCard>();
            SelectedIndices.Value = new List<int>();
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → `HandModelTests` 실행. 예상: 6개 테스트 모두 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/Models/HandModel.cs
git add Assets/Tests/EditMode/Poker/HandModelTests.cs
git commit -m "feat(poker): add HandModel with reactive state and selection"
```

---

## Task 4: DealSystem

**Files:**
- Create: `Assets/Scripts/Features/Poker/Systems/DealSystem.cs`
- Create: `Assets/Tests/EditMode/Poker/DealSystemTests.cs`

- [ ] **Step 1: 테스트 파일 작성**

`Assets/Tests/EditMode/Poker/DealSystemTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class DealSystemTests
    {
        private DeckModel _deck;
        private HandModel _hand;
        private DealSystem _system;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _hand = new HandModel(maxHandSize: 8);
            _system = new DealSystem(_deck, _hand, new HandEvaluator());
            _system.InitializeDeck();
        }

        [Test]
        public void InitializeDeck_DeckHas52Cards()
        {
            Assert.AreEqual(52, _deck.RemainingCount.Value);
        }

        [Test]
        public void Deal_AddsCardsToHand()
        {
            _system.Deal(5);
            Assert.AreEqual(5, _hand.Cards.Value.Count);
        }

        [Test]
        public void DrawToFull_FillsHandToMaxHandSize()
        {
            _system.Deal(3);
            _system.DrawToFull();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }

        [Test]
        public void ToggleSelect_SelectsCard()
        {
            _system.Deal(5);
            _system.ToggleSelect(0);
            Assert.AreEqual(1, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_IgnoresWhenAt5CardsAndAddingNew()
        {
            _system.Deal(8);
            // 5장 선택
            for (int i = 0; i < 5; i++) _system.ToggleSelect(i);
            Assert.AreEqual(5, _hand.SelectedCount);

            // 6번째 선택 시도 → 무시
            _system.ToggleSelect(5);
            Assert.AreEqual(5, _hand.SelectedCount);
        }

        [Test]
        public void ToggleSelect_AllowsDeselectWhenAt5Cards()
        {
            _system.Deal(8);
            for (int i = 0; i < 5; i++) _system.ToggleSelect(i);

            // 이미 선택된 카드 해제는 허용
            _system.ToggleSelect(0);
            Assert.AreEqual(4, _hand.SelectedCount);
        }

        [Test]
        public void EvaluateSelected_ReturnsPairForTwoSameRankCards()
        {
            // 같은 랭크 카드 2장이 포함된 핸드를 직접 구성
            var cards = new List<BaseCard>
            {
                new BaseCard("s_k", CardCategory.Standard, Suit.Spade, Rank.King, "", ""),
                new BaseCard("h_k", CardCategory.Standard, Suit.Heart, Rank.King, "", ""),
                new BaseCard("d_2", CardCategory.Standard, Suit.Diamond, Rank.Two, "", ""),
            };
            _hand.AddCards(cards);
            _hand.ToggleSelect(0);
            _hand.ToggleSelect(1);
            _hand.ToggleSelect(2);

            var result = _system.EvaluateSelected();
            Assert.AreEqual(HandRank.OnePair, result.Rank);
        }
    }
}
```

- [ ] **Step 2: Test Runner에서 실패 확인**

Test Runner → `DealSystemTests` 실행. 예상: 컴파일 오류 또는 실패.

- [ ] **Step 3: DealSystem 구현**

`Assets/Scripts/Features/Poker/Systems/DealSystem.cs`:

```csharp
using System.Linq;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Features.Poker.Systems
{
    public class DealSystem
    {
        private readonly DeckModel _deck;
        private readonly HandModel _hand;
        private readonly HandEvaluator _evaluator;

        public DealSystem(DeckModel deck, HandModel hand, HandEvaluator evaluator)
        {
            _deck = deck;
            _hand = hand;
            _evaluator = evaluator;
        }

        public void InitializeDeck() => _deck.Initialize();

        public void Deal(int count)
        {
            var drawn = _deck.Draw(count);
            _hand.AddCards(drawn);
        }

        public void ToggleSelect(int index)
        {
            bool isSelected = _hand.SelectedIndices.Value.Contains(index);
            if (!isSelected && _hand.SelectedCount >= 5) return;
            _hand.ToggleSelect(index);
        }

        public HandResult EvaluateSelected()
        {
            var cards = _hand.Cards.Value;
            var selectedCards = _hand.SelectedIndices.Value
                .Select(i => cards[i])
                .ToList();
            return _evaluator.Evaluate(selectedCards);
        }

        public void DrawToFull()
        {
            int needed = _hand.MaxHandSize - _hand.Cards.Value.Count;
            if (needed <= 0) return;
            var drawn = _deck.Draw(needed);
            _hand.AddCards(drawn);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → `DealSystemTests` 실행. 예상: 7개 테스트 모두 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/Systems/DealSystem.cs
git add Assets/Tests/EditMode/Poker/DealSystemTests.cs
git commit -m "feat(poker): add DealSystem with pure business logic"
```

---

## Task 5: PokerViewModel

**Files:**
- Create: `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs`

> PokerViewModel은 R3 ReactiveCommand의 CanExecute 조건을 위해 모델의 ReactiveProperty를 구독한다. 순수 C# 이므로 EditMode 테스트 가능하나, ReactiveCommand의 구독 기반 CanExecute는 Unity Test Runner에서 동기 검증이 어려울 수 있다. 핵심 로직(PushHandResult, 텍스트 변환)만 테스트하고 나머지는 수동 검증한다.

- [ ] **Step 1: PokerViewModel 구현**

`Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs`:

```csharp
using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Features.Poker.UI.ViewModels
{
    public class PokerViewModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveProperty<string> _handResultText = new(string.Empty);

        public ReadOnlyReactiveProperty<IReadOnlyList<BaseCard>> Hand { get; }
        public ReadOnlyReactiveProperty<IReadOnlyList<int>> SelectedIndices { get; }
        public ReadOnlyReactiveProperty<int> DeckRemaining { get; }
        public ReadOnlyReactiveProperty<string> HandResultText { get; }

        public ReactiveCommand<int> ToggleSelectCommand { get; }
        public ReactiveCommand SubmitCommand { get; }
        public ReactiveCommand DrawCommand { get; }

        public PokerViewModel(HandModel hand, DeckModel deck)
        {
            Hand = hand.Cards.ToReadOnlyReactiveProperty().AddTo(_disposables);
            SelectedIndices = hand.SelectedIndices.ToReadOnlyReactiveProperty().AddTo(_disposables);
            DeckRemaining = deck.RemainingCount.ToReadOnlyReactiveProperty().AddTo(_disposables);
            HandResultText = _handResultText.ToReadOnlyReactiveProperty().AddTo(_disposables);

            ToggleSelectCommand = new ReactiveCommand<int>().AddTo(_disposables);

            var canSubmit = hand.SelectedIndices.Select(indices => indices.Count >= 1 && indices.Count <= 5);
            SubmitCommand = new ReactiveCommand(canSubmit).AddTo(_disposables);

            var canDraw = hand.Cards.Select(cards => cards.Count < hand.MaxHandSize);
            DrawCommand = new ReactiveCommand(canDraw).AddTo(_disposables);
        }

        public void PushHandResult(HandResult result)
        {
            _handResultText.Value = ToDisplayString(result.Rank);
        }

        private static string ToDisplayString(HandRank rank) => rank switch
        {
            HandRank.RoyalFlush    => "로열 플러시",
            HandRank.StraightFlush => "스트레이트 플러시",
            HandRank.FourOfAKind   => "포 오브 어 카인드",
            HandRank.FullHouse     => "풀 하우스",
            HandRank.Flush         => "플러시",
            HandRank.Straight      => "스트레이트",
            HandRank.ThreeOfAKind  => "쓰리 오브 어 카인드",
            HandRank.TwoPair       => "투 페어",
            HandRank.OnePair       => "원 페어",
            HandRank.HighCard      => "하이 카드",
            _                      => rank.ToString()
        };

        public void Dispose() => _disposables.Dispose();
    }
}
```

- [ ] **Step 2: Unity Editor 컴파일 확인**

Console에 오류 없음을 확인한다.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs
git commit -m "feat(poker): add PokerViewModel with R3 reactive state and commands"
```

---

## Task 6: RoundController

**Files:**
- Create: `Assets/Scripts/Features/Poker/Controllers/RoundController.cs`
- Create: `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`

- [ ] **Step 1: 테스트 파일 작성**

`Assets/Tests/EditMode/Poker/RoundControllerTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class RoundControllerTests
    {
        private DeckModel _deck;
        private HandModel _hand;
        private DealSystem _dealSystem;
        private PokerViewModel _vm;
        private RoundController _controller;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _hand = new HandModel(maxHandSize: 8);
            _dealSystem = new DealSystem(_deck, _hand, new HandEvaluator());
            _vm = new PokerViewModel(_hand, _deck);
            _controller = new RoundController(_dealSystem, _vm);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            _vm.Dispose();
        }

        [Test]
        public void Start_DealsMaxHandSizeCards()
        {
            _controller.Start();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }

        [Test]
        public void Start_DeckHas44CardsRemaining()
        {
            _controller.Start();
            Assert.AreEqual(44, _deck.RemainingCount.Value);
        }

        [Test]
        public void SubmitCommand_UpdatesHandResultText()
        {
            _controller.Start();

            // 원 페어가 되는 카드 2장 선택
            var cards = _hand.Cards.Value;
            // 같은 랭크 카드 인덱스 찾기
            int firstIdx = -1, secondIdx = -1;
            for (int i = 0; i < cards.Count && secondIdx == -1; i++)
            {
                for (int j = i + 1; j < cards.Count && secondIdx == -1; j++)
                {
                    if (cards[i].Rank == cards[j].Rank)
                    {
                        firstIdx = i;
                        secondIdx = j;
                    }
                }
            }

            if (firstIdx == -1)
            {
                // 페어가 없으면 아무 카드나 1장 선택하여 HighCard 확인
                _vm.ToggleSelectCommand.Execute(0);
                _vm.SubmitCommand.Execute();
                Assert.AreEqual("하이 카드", _vm.HandResultText.Value);
            }
            else
            {
                _vm.ToggleSelectCommand.Execute(firstIdx);
                _vm.ToggleSelectCommand.Execute(secondIdx);
                _vm.SubmitCommand.Execute();
                Assert.AreEqual("원 페어", _vm.HandResultText.Value);
            }
        }

        [Test]
        public void DrawCommand_FillsHandToMaxHandSize()
        {
            _controller.Start();
            // 3장 선택 후 제출 (핸드는 그대로 8장)
            _vm.ToggleSelectCommand.Execute(0);
            _vm.SubmitCommand.Execute();

            // 핸드에서 3장을 수동으로 제거한 상태를 시뮬레이션하려면
            // 실제로 핸드를 비우고 5장만 추가
            _hand.Clear();
            var partial = new List<BaseCard>
            {
                new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
                new("s2", CardCategory.Standard, Suit.Spade, Rank.Two, "", ""),
                new("s3", CardCategory.Standard, Suit.Spade, Rank.Three, "", ""),
            };
            _hand.AddCards(partial);
            Assert.AreEqual(3, _hand.Cards.Value.Count);

            _vm.DrawCommand.Execute();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }
    }
}
```

- [ ] **Step 2: Test Runner에서 실패 확인**

Test Runner → `RoundControllerTests` 실행. 예상: 컴파일 오류 또는 실패.

- [ ] **Step 3: RoundController 구현**

`Assets/Scripts/Features/Poker/Controllers/RoundController.cs`:

```csharp
using System;
using R3;
using VContainer.Unity;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.Controllers
{
    public class RoundController : IStartable, IDisposable
    {
        private readonly DealSystem _dealSystem;
        private readonly PokerViewModel _vm;
        private readonly CompositeDisposable _disposables = new();

        public RoundController(DealSystem dealSystem, PokerViewModel vm)
        {
            _dealSystem = dealSystem;
            _vm = vm;
        }

        public void Start()
        {
            _dealSystem.InitializeDeck();
            _dealSystem.DrawToFull();

            _vm.ToggleSelectCommand
                .Subscribe(index => _dealSystem.ToggleSelect(index))
                .AddTo(_disposables);

            _vm.SubmitCommand
                .Subscribe(_ =>
                {
                    var result = _dealSystem.EvaluateSelected();
                    _vm.PushHandResult(result);
                })
                .AddTo(_disposables);

            _vm.DrawCommand
                .Subscribe(_ => _dealSystem.DrawToFull())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → `RoundControllerTests` 실행. 예상: 3개 테스트 모두 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/Controllers/RoundController.cs
git add Assets/Tests/EditMode/Poker/RoundControllerTests.cs
git commit -m "feat(poker): add RoundController for game flow orchestration"
```

---

## Task 7: PokerInstaller (VContainer DI)

**Files:**
- Create: `Assets/Scripts/Features/Poker/PokerInstaller.cs`

- [ ] **Step 1: PokerInstaller 구현**

`Assets/Scripts/Features/Poker/PokerInstaller.cs`:

```csharp
using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;
using FoldingFate.Features.Poker.UI.Views;

namespace FoldingFate.Features.Poker
{
    public class PokerInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<DeckModel>(Lifetime.Singleton);
            builder.Register<HandModel>(Lifetime.Singleton);
            builder.Register<HandEvaluator>(Lifetime.Singleton);
            builder.Register<DealSystem>(Lifetime.Singleton);
            builder.Register<PokerViewModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RoundController>();
            builder.RegisterComponentInHierarchy<PokerView>();
        }
    }
}
```

- [ ] **Step 2: Unity Editor 컴파일 확인**

Console에 오류 없음을 확인한다.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Features/Poker/PokerInstaller.cs
git commit -m "feat(poker): add PokerInstaller VContainer LifetimeScope"
```

---

## Task 8: UXML / USS

**Files:**
- Create: `Assets/Scripts/Features/Poker/UI/Uxml/CardElement.uxml`
- Create: `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml`
- Create: `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss`

- [ ] **Step 1: CardElement.uxml 작성**

`Assets/Scripts/Features/Poker/UI/Uxml/CardElement.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="card-root" class="card">
        <ui:Label name="card-top-rank" class="card-rank card-rank--top" />
        <ui:Label name="card-suit" class="card-suit card-suit--top" />
        <ui:Label name="card-center-suit" class="card-center-suit" />
        <ui:VisualElement name="selected-overlay" class="card-selected-overlay" />
    </ui:VisualElement>
</ui:UXML>
```

- [ ] **Step 2: PokerHUD.uxml 작성**

`Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:uie="UnityEditor.UIElements">
    <ui:VisualElement name="poker-root" class="poker-root">
        <ui:Label name="deck-count-label" class="deck-count-label" text="남은 카드: 52" />
        <ui:VisualElement name="hand-container" class="hand-container" />
        <ui:Label name="result-label" class="result-label" text="" />
        <ui:VisualElement class="button-row">
            <ui:Button name="submit-button" class="action-button" text="제출" />
            <ui:Button name="draw-button" class="action-button" text="다시 받기" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

- [ ] **Step 3: PokerHUD.uss 작성**

`Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss`:

```css
.poker-root {
    flex-direction: column;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    background-color: rgb(30, 100, 50);
    padding: 20px;
}

.deck-count-label {
    color: white;
    font-size: 16px;
    margin-bottom: 10px;
}

.hand-container {
    flex-direction: row;
    justify-content: center;
    flex-wrap: nowrap;
    margin-bottom: 20px;
}

.card {
    width: 80px;
    height: 120px;
    background-color: white;
    border-radius: 8px;
    margin: 0 4px;
    padding: 6px;
    align-items: center;
    position: relative;
    transition-duration: 0.15s;
    transition-property: translate;
}

.card--selected {
    translate: 0 -12px;
    border-width: 3px;
    border-color: rgb(60, 130, 255);
}

.card-rank {
    font-size: 18px;
    -unity-font-style: bold;
    position: absolute;
    top: 4px;
    left: 8px;
}

.card-suit--top {
    font-size: 14px;
    position: absolute;
    top: 24px;
    left: 8px;
}

.card-center-suit {
    font-size: 36px;
    position: absolute;
    top: 50%;
    left: 50%;
    translate: -50% -50%;
}

.card--red .card-rank,
.card--red .card-suit--top,
.card--red .card-center-suit {
    color: rgb(200, 30, 30);
}

.card--black .card-rank,
.card--black .card-suit--top,
.card--black .card-center-suit {
    color: rgb(20, 20, 20);
}

.card-selected-overlay {
    display: none;
}

.result-label {
    color: white;
    font-size: 28px;
    -unity-font-style: bold;
    margin-bottom: 20px;
    min-height: 40px;
}

.button-row {
    flex-direction: row;
    justify-content: center;
}

.action-button {
    font-size: 18px;
    padding: 10px 30px;
    margin: 0 10px;
    border-radius: 6px;
}

.action-button:disabled {
    opacity: 0.4;
}
```

- [ ] **Step 4: Unity Editor에서 UXML 확인**

Unity Editor에서 `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml` 파일을 더블클릭해 UI Builder가 열리고 레이아웃이 표시되는지 확인한다.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/Uxml/
git add Assets/Scripts/Features/Poker/UI/Uss/
git commit -m "feat(poker): add placeholder UXML and USS for poker HUD"
```

---

## Task 9: PokerView

**Files:**
- Create: `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs`

- [ ] **Step 1: PokerView 구현**

`Assets/Scripts/Features/Poker/UI/Views/PokerView.cs`:

```csharp
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.UI.Views
{
    [RequireComponent(typeof(UIDocument))]
    public class PokerView : MonoBehaviour
    {
        [Inject] private PokerViewModel _vm;

        private UIDocument _doc;
        private VisualElement _handContainer;
        private Label _resultLabel;
        private Label _deckCountLabel;
        private Button _submitButton;
        private Button _drawButton;

        private readonly List<VisualElement> _cardElements = new();

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = _doc.rootVisualElement;
            _handContainer = root.Q("hand-container");
            _resultLabel = root.Q<Label>("result-label");
            _deckCountLabel = root.Q<Label>("deck-count-label");
            _submitButton = root.Q<Button>("submit-button");
            _drawButton = root.Q<Button>("draw-button");

            _submitButton.clicked += () => _vm.SubmitCommand.Execute();
            _drawButton.clicked += () => _vm.DrawCommand.Execute();

            var token = destroyCancellationToken;

            _vm.Hand.Subscribe(RenderHand).AddTo(token);
            _vm.HandResultText.Subscribe(text => _resultLabel.text = text).AddTo(token);
            _vm.DeckRemaining.Subscribe(count => _deckCountLabel.text = $"남은 카드: {count}").AddTo(token);

            _vm.SubmitCommand.CanExecute
                .Subscribe(v => _submitButton.SetEnabled(v)).AddTo(token);
            _vm.DrawCommand.CanExecute
                .Subscribe(v => _drawButton.SetEnabled(v)).AddTo(token);
        }

        private void RenderHand(IReadOnlyList<BaseCard> cards)
        {
            _handContainer.Clear();
            _cardElements.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                int capturedIndex = i;
                var card = cards[i];
                var cardEl = CreateCardElement(card, capturedIndex);
                _handContainer.Add(cardEl);
                _cardElements.Add(cardEl);
            }

            // 선택 상태 구독 갱신
            _vm.SelectedIndices.Subscribe(UpdateSelectionVisuals).AddTo(destroyCancellationToken);
        }

        private VisualElement CreateCardElement(BaseCard card, int index)
        {
            var el = new VisualElement();
            el.AddToClassList("card");

            bool isRed = card.Suit == Suit.Heart || card.Suit == Suit.Diamond;
            el.AddToClassList(isRed ? "card--red" : "card--black");

            var rankLabel = new Label { name = "card-top-rank" };
            rankLabel.AddToClassList("card-rank");
            rankLabel.text = RankToDisplay(card.Rank);

            var suitTopLabel = new Label { name = "card-suit" };
            suitTopLabel.AddToClassList("card-suit--top");
            suitTopLabel.text = SuitToSymbol(card.Suit);

            var centerSuitLabel = new Label { name = "card-center-suit" };
            centerSuitLabel.AddToClassList("card-center-suit");
            centerSuitLabel.text = SuitToSymbol(card.Suit);

            el.Add(rankLabel);
            el.Add(suitTopLabel);
            el.Add(centerSuitLabel);

            el.RegisterCallback<ClickEvent>(_ => _vm.ToggleSelectCommand.Execute(index));

            return el;
        }

        private void UpdateSelectionVisuals(IReadOnlyList<int> selectedIndices)
        {
            var selectedSet = new HashSet<int>(selectedIndices);
            for (int i = 0; i < _cardElements.Count; i++)
            {
                if (selectedSet.Contains(i))
                    _cardElements[i].AddToClassList("card--selected");
                else
                    _cardElements[i].RemoveFromClassList("card--selected");
            }
        }

        private static string SuitToSymbol(Suit? suit) => suit switch
        {
            Suit.Spade   => "♠",
            Suit.Heart   => "♥",
            Suit.Diamond => "♦",
            Suit.Club    => "♣",
            _            => "?"
        };

        private static string RankToDisplay(Rank? rank) => rank switch
        {
            Rank.Ace   => "A",
            Rank.Jack  => "J",
            Rank.Queen => "Q",
            Rank.King  => "K",
            null       => "?",
            _          => ((int)rank.Value + 1).ToString()
        };
    }
}
```

> **Note:** `Rank` enum에서 `Two = 2`, `Three = 3` ... `Ten = 10` 이므로 숫자 표시는 `(int)rank` 직접 사용 또는 `rank.ToString()`을 확인한다. 기존 `Rank.cs`에서 `Ace = 1, Two, Three...King` 이므로 Two = 2, Ten = 10. RankToDisplay에서 `(int)rank.Value` 는 Ace=1, Two=2... 이므로 Ace는 별도 처리, 나머지는 `rank.Value.ToString()` 또는 `((int)rank.Value).ToString()`을 사용한다.

- [ ] **Step 2: RankToDisplay 수정**

기존 `Rank` enum: `Ace=1, Two=2, Three=3 ... Ten=10, Jack=11, Queen=12, King=13`

`PokerView.cs`의 `RankToDisplay`를 아래로 교체한다:

```csharp
private static string RankToDisplay(Rank? rank) => rank switch
{
    Rank.Ace   => "A",
    Rank.Jack  => "J",
    Rank.Queen => "Q",
    Rank.King  => "K",
    null       => "?",
    _          => ((int)rank.Value).ToString()
};
```

- [ ] **Step 3: Unity Editor 컴파일 확인**

Console에 오류 없음을 확인한다.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/Views/PokerView.cs
git commit -m "feat(poker): add PokerView UIDocument controller with card rendering"
```

---

## Task 10: Scene Setup

**Files:** Unity 씬 오브젝트 배치 (에디터 작업)

- [ ] **Step 1: 씬 열기**

Unity Editor → `Assets/Scenes/SampleScene.unity` 열기.

- [ ] **Step 2: PokerInstaller 오브젝트 생성**

Hierarchy에서 빈 GameObject 생성 → 이름: `PokerInstaller`.
Inspector에서 `Add Component` → `PokerInstaller` 스크립트 추가.

- [ ] **Step 3: UIDocument 오브젝트 생성**

Hierarchy에서 빈 GameObject 생성 → 이름: `PokerHUD`.
Inspector에서 `Add Component` → `UIDocument` 추가.
`UIDocument`의 `Source Asset` 필드에 `PokerHUD.uxml` 드래그 앤 드롭.
`Add Component` → `PokerView` 추가.

- [ ] **Step 4: StyleSheet 연결**

`UIDocument` 컴포넌트의 `Panel Settings`를 생성하거나 기존 것을 사용한다.
`PokerHUD.uxml`을 UI Builder에서 열고 StyleSheet 패널에서 `PokerHUD.uss`를 추가한다.

- [ ] **Step 5: 씬 저장 및 Play 테스트**

`Ctrl+S`로 씬 저장 → Play 버튼 클릭.
예상 동작:
1. 화면에 8장 카드가 가로 일렬로 표시됨
2. 카드 클릭 시 위로 올라가며 선택 표시 (파란 테두리)
3. 1~5장 선택 시 "제출" 버튼 활성화
4. "제출" 클릭 시 족보 텍스트 표시 (예: "원 페어")
5. "다시 받기" 버튼으로 핸드 보충
6. 남은 카드 수가 화면 상단에 갱신됨

- [ ] **Step 6: 씬 파일 및 meta 파일 Commit**

```bash
git add Assets/Scenes/SampleScene.unity
git add Assets/Scenes/SampleScene.unity.meta
git commit -m "feat(poker): wire poker hand UI prototype in SampleScene"
```

---

## Self-Review Checklist

| 스펙 요구사항 | 구현 태스크 |
|---|---|
| 52장 표준 덱 | Task 2 — DeckModel.Initialize() |
| 셔플 및 덱 유지 | Task 2 — DeckModel.Shuffle(), Draw() 자동 리셋 |
| 8장 딜링 | Task 6 — RoundController.Start() → DrawToFull() |
| 카드 선택 1~5장 | Task 4 — DealSystem.ToggleSelect() 5장 제한 |
| 족보 계산 및 표시 | Task 6 — EvaluateSelected() → PushHandResult() |
| 한글 족보 텍스트 | Task 5 — PokerViewModel.ToDisplayString() |
| 다시 받기 (보충) | Task 4 — DealSystem.DrawToFull() |
| ReactiveCommand CanExecute | Task 5 — canSubmit, canDraw Observable |
| View → ViewModel만 알기 | Task 9 — PokerView는 PokerViewModel만 참조 |
| System은 ViewModel 모름 | Task 4/6 — DealSystem은 VM 참조 없음 |
| RoundController 흐름 제어 | Task 6 — IStartable.Start(), 커맨드 구독 |
| VContainer DI | Task 7 — PokerInstaller |
| 덱 소진 자동 리셋 | Task 2 — DeckModel.Draw() 자동 리셋 |
| Joker 나중 추가 가능 | DeckModel.Initialize()만 수정 (구조적 지원) |
