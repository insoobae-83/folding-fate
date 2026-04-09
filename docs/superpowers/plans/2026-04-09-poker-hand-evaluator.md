# Poker Hand Evaluator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** n장(1~52)의 BaseCard를 입력받아 최선의 5장 포커 족보를 판정하고 강약 비교를 지원하는 HandEvaluator를 TDD로 구현한다.

**Architecture:** 단일 `HandEvaluator` 클래스가 Custom 카드 필터링 → Joker 분리 → nC5 조합 생성 → 족보 판정(HighCard~RoyalFlush) → 최선 반환을 담당한다. `HandResult : IComparable<HandResult>`가 비교 로직을 캡슐화하여 외부 API를 단순하게 유지한다.

**Tech Stack:** C# (.NET Standard 2.1), NUnit (Unity Test Framework 1.6.0), Unity 6000.3.12f1

---

## File Map

| 파일 | 변경 | 역할 |
|---|---|---|
| `Assets/Scripts/Core/Enums/HandRank.cs` | 신규 | 족보 종류 enum (값 순서 = 강도) |
| `Assets/Scripts/Features/Card/Models/HandResult.cs` | 신규 | 평가 결과 모델, IComparable |
| `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs` | 신규 | 족보 평가 로직 전체 |
| `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs` | 신규 | EditMode 단위 테스트 |

> **asmdef 참고**: `HandRank`는 `FoldingFate.Core` 어셈블리, 나머지 세 파일은 `FoldingFate.Features` / `FoldingFate.Tests.EditMode` 어셈블리에 위치한다. 기존 asmdef가 이미 이 경로들을 커버하므로 별도 수정 불필요.

---

## Task 1: HandRank enum + HandResult 모델

**Files:**
- Create: `Assets/Scripts/Core/Enums/HandRank.cs`
- Create: `Assets/Scripts/Features/Card/Models/HandResult.cs`

- [ ] **Step 1: HandRank enum 생성**

```csharp
// Assets/Scripts/Core/Enums/HandRank.cs
namespace FoldingFate.Core
{
    public enum HandRank
    {
        HighCard = 0,
        OnePair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        RoyalFlush
    }
}
```

- [ ] **Step 2: HandResult 모델 생성**

타이브레이크 값은 `private` — 외부에는 `Rank`, `BestHand`, `CompareTo()`만 노출한다.

```csharp
// Assets/Scripts/Features/Card/Models/HandResult.cs
using System;
using System.Collections.Generic;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class HandResult : IComparable<HandResult>
    {
        public HandRank Rank { get; }
        public IReadOnlyList<BaseCard> BestHand { get; }
        private readonly IReadOnlyList<int> _tiebreakValues;

        public HandResult(HandRank rank, List<BaseCard> bestHand, List<int> tiebreakValues)
        {
            Rank = rank;
            BestHand = bestHand.AsReadOnly();
            _tiebreakValues = tiebreakValues.AsReadOnly();
        }

        public int CompareTo(HandResult other)
        {
            if (other == null) return 1;
            int rankCmp = Rank.CompareTo(other.Rank);
            if (rankCmp != 0) return rankCmp;
            for (int i = 0; i < Math.Min(_tiebreakValues.Count, other._tiebreakValues.Count); i++)
            {
                int cmp = _tiebreakValues[i].CompareTo(other._tiebreakValues[i]);
                if (cmp != 0) return cmp;
            }
            return 0;
        }
    }
}
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Core/Enums/HandRank.cs \
        Assets/Scripts/Core/Enums/HandRank.cs.meta \
        Assets/Scripts/Features/Card/Models/HandResult.cs \
        Assets/Scripts/Features/Card/Models/HandResult.cs.meta
git commit -m "feat(card): add HandRank enum and HandResult model"
```

---

## Task 2: HandEvaluator scaffold + HighCard

**Files:**
- Create: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Create: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 작성**

```csharp
// Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class HandEvaluatorTests
    {
        private HandEvaluator _evaluator;

        [SetUp]
        public void SetUp() => _evaluator = new HandEvaluator();

        // 헬퍼: Standard 카드 생성
        private static BaseCard S(Suit suit, Rank rank) =>
            new BaseCard($"{suit}_{rank}", CardCategory.Standard, suit, rank, "", "");

        // 헬퍼: Joker 카드 생성
        private static BaseCard J() =>
            new BaseCard("joker", CardCategory.Joker, null, null, "", "");

        // 헬퍼: Custom 카드 생성
        private static BaseCard Custom() =>
            new BaseCard("custom", CardCategory.Custom, null, null, "", "");

        [Test]
        public void Evaluate_FiveUnrelatedCards_ReturnsHighCard()
        {
            var cards = new List<BaseCard>
            {
                S(Suit.Spade, Rank.Two),
                S(Suit.Heart, Rank.Four),
                S(Suit.Diamond, Rank.Seven),
                S(Suit.Club, Rank.Nine),
                S(Suit.Spade, Rank.King)
            };
            var result = _evaluator.Evaluate(cards);
            Assert.AreEqual(HandRank.HighCard, result.Rank);
            Assert.AreEqual(5, result.BestHand.Count);
        }

        [Test]
        public void Evaluate_EmptyInput_ReturnsHighCard()
        {
            var result = _evaluator.Evaluate(new List<BaseCard>());
            Assert.AreEqual(HandRank.HighCard, result.Rank);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

Unity Editor → Window > General > Test Runner → EditMode 탭 → 두 테스트 실행.
Expected: FAIL (HandEvaluator not defined)

- [ ] **Step 3: HandEvaluator scaffold 구현**

```csharp
// Assets/Scripts/Features/Card/Systems/HandEvaluator.cs
using System.Collections.Generic;
using System.Linq;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Card.Systems
{
    public class HandEvaluator
    {
        public HandResult Evaluate(IReadOnlyList<BaseCard> cards)
        {
            var standard = new List<BaseCard>();
            int jokerCount = 0;
            foreach (var card in cards)
            {
                if (card.Category == CardCategory.Standard)
                    standard.Add(card);
                else if (card.Category == CardCategory.Joker)
                    jokerCount++;
                // Custom: 무시
            }

            if (standard.Count == 0 && jokerCount == 0)
                return new HandResult(HandRank.HighCard, new List<BaseCard>(), new List<int> { 0 });

            return EvaluateBest(standard, jokerCount);
        }

        private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
        {
            return MakeHighCard(cards);
        }

        private static HandResult MakeHighCard(List<BaseCard> cards)
        {
            var tiebreak = cards
                .Where(c => c.Rank.HasValue)
                .Select(c => AceHighValue(c.Rank.Value))
                .OrderByDescending(v => v)
                .ToList();
            return new HandResult(HandRank.HighCard, cards.ToList(), tiebreak);
        }

        private static int AceHighValue(Rank rank) =>
            rank == Rank.Ace ? 14 : (int)rank;

        private static Dictionary<int, int> GetRankCounts(List<BaseCard> cards)
        {
            var counts = new Dictionary<int, int>();
            foreach (var c in cards)
            {
                if (!c.Rank.HasValue) continue;
                int v = AceHighValue(c.Rank.Value);
                counts[v] = counts.GetValueOrDefault(v, 0) + 1;
            }
            return counts;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → 두 테스트 실행. Expected: 2 Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Scripts/Features/Card/Systems/HandEvaluator.cs.meta \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs.meta
git commit -m "feat(card): add HandEvaluator scaffold with HighCard detection"
```

---

## Task 3: OnePair + TwoPair

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 추가**

`HandEvaluatorTests.cs`에 다음 두 테스트 추가:

```csharp
[Test]
public void Evaluate_OnePair_ReturnsOnePair()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Three),
        S(Suit.Club, Rank.Seven),
        S(Suit.Spade, Rank.Ace)
    };
    Assert.AreEqual(HandRank.OnePair, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_TwoPair_ReturnsTwoPair()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Three),
        S(Suit.Club, Rank.Three),
        S(Suit.Spade, Rank.Ace)
    };
    Assert.AreEqual(HandRank.TwoPair, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 테스트 실패 확인**

Test Runner → 두 신규 테스트 실행. Expected: FAIL (returns HighCard)

- [ ] **Step 3: OnePair + TwoPair 구현**

`HandEvaluator.cs`에서 `EvaluateBest`를 다음으로 교체하고, `TryOnePair`, `TryTwoPair` 메서드 추가:

```csharp
private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
{
    if (TryTwoPair(cards, out var tp)) return tp;
    if (TryOnePair(cards, out var op)) return op;
    return MakeHighCard(cards);
}

private static bool TryOnePair(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();
    if (pairs.Count == 0) return false;

    int pairRank = pairs[0].Key;
    var kickers = counts
        .Where(kv => kv.Key != pairRank)
        .OrderByDescending(kv => kv.Key)
        .Take(3)
        .Select(kv => kv.Key)
        .ToList();
    var tiebreak = new List<int> { pairRank };
    tiebreak.AddRange(kickers);
    result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak);
    return true;
}

private static bool TryTwoPair(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();
    if (pairs.Count < 2) return false;

    int highPair = pairs[0].Key;
    int lowPair = pairs[1].Key;
    int kicker = counts
        .Where(kv => kv.Key != highPair && kv.Key != lowPair)
        .OrderByDescending(kv => kv.Key)
        .Select(kv => kv.Key)
        .FirstOrDefault();
    result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { highPair, lowPair, kicker });
    return true;
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → 전체 테스트 실행. Expected: All Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): add OnePair and TwoPair detection"
```

---

## Task 4: ThreeOfAKind + FullHouse + FourOfAKind

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 추가**

```csharp
[Test]
public void Evaluate_ThreeOfAKind_ReturnsThreeOfAKind()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Queen),
        S(Suit.Heart, Rank.Queen),
        S(Suit.Diamond, Rank.Queen),
        S(Suit.Club, Rank.Two),
        S(Suit.Spade, Rank.Five)
    };
    Assert.AreEqual(HandRank.ThreeOfAKind, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_FullHouse_ReturnsFullHouse()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.King),
        S(Suit.Club, Rank.Ace),
        S(Suit.Spade, Rank.Ace)
    };
    Assert.AreEqual(HandRank.FullHouse, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_FourOfAKind_ReturnsFourOfAKind()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Jack),
        S(Suit.Heart, Rank.Jack),
        S(Suit.Diamond, Rank.Jack),
        S(Suit.Club, Rank.Jack),
        S(Suit.Spade, Rank.Three)
    };
    Assert.AreEqual(HandRank.FourOfAKind, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 테스트 실패 확인**

Expected: FAIL (returns TwoPair 또는 OnePair)

- [ ] **Step 3: ThreeOfAKind + FullHouse + FourOfAKind 구현**

`EvaluateBest`를 다음으로 교체하고, 세 개의 private 메서드 추가:

```csharp
private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
{
    if (TryFourOfAKind(cards, out var foak)) return foak;
    if (TryFullHouse(cards, out var fh)) return fh;
    if (TryThreeOfAKind(cards, out var toak)) return toak;
    if (TryTwoPair(cards, out var tp)) return tp;
    if (TryOnePair(cards, out var op)) return op;
    return MakeHighCard(cards);
}

private static bool TryFourOfAKind(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var quads = counts.Where(kv => kv.Value >= 4).OrderByDescending(kv => kv.Key).ToList();
    if (quads.Count == 0) return false;

    int quadRank = quads[0].Key;
    int kicker = counts
        .Where(kv => kv.Key != quadRank)
        .OrderByDescending(kv => kv.Key)
        .Select(kv => kv.Key)
        .FirstOrDefault();
    result = new HandResult(HandRank.FourOfAKind, cards.ToList(), new List<int> { quadRank, kicker });
    return true;
}

private static bool TryFullHouse(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var triples = counts.Where(kv => kv.Value >= 3).OrderByDescending(kv => kv.Key).ToList();
    if (triples.Count == 0) return false;

    int tripleRank = triples[0].Key;
    var pairs = counts
        .Where(kv => kv.Key != tripleRank && kv.Value >= 2)
        .OrderByDescending(kv => kv.Key)
        .ToList();
    if (pairs.Count == 0) return false;

    result = new HandResult(HandRank.FullHouse, cards.ToList(),
        new List<int> { tripleRank, pairs[0].Key });
    return true;
}

private static bool TryThreeOfAKind(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var triples = counts.Where(kv => kv.Value >= 3).OrderByDescending(kv => kv.Key).ToList();
    if (triples.Count == 0) return false;

    int tripleRank = triples[0].Key;
    var kickers = counts
        .Where(kv => kv.Key != tripleRank)
        .OrderByDescending(kv => kv.Key)
        .Take(2)
        .Select(kv => kv.Key)
        .ToList();
    var tiebreak = new List<int> { tripleRank };
    tiebreak.AddRange(kickers);
    result = new HandResult(HandRank.ThreeOfAKind, cards.ToList(), tiebreak);
    return true;
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → 전체 테스트. Expected: All Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): add ThreeOfAKind, FullHouse, FourOfAKind detection"
```

---

## Task 5: Straight (Ace High + Ace Low)

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 추가**

```csharp
[Test]
public void Evaluate_Straight_ReturnsStraight()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Five),
        S(Suit.Heart, Rank.Six),
        S(Suit.Diamond, Rank.Seven),
        S(Suit.Club, Rank.Eight),
        S(Suit.Spade, Rank.Nine)
    };
    Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_AceHighStraight_ReturnsStraight()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Ten),
        S(Suit.Heart, Rank.Jack),
        S(Suit.Diamond, Rank.Queen),
        S(Suit.Club, Rank.King),
        S(Suit.Spade, Rank.Ace)
    };
    Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_AceLowStraight_ReturnsStraight()
{
    // A-2-3-4-5 (wheel)
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Ace),
        S(Suit.Heart, Rank.Two),
        S(Suit.Diamond, Rank.Three),
        S(Suit.Club, Rank.Four),
        S(Suit.Spade, Rank.Five)
    };
    Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 테스트 실패 확인**

Expected: FAIL (returns HighCard)

- [ ] **Step 3: Straight 구현**

`EvaluateBest`를 다음으로 교체하고, `TryStraight` 메서드 추가:

```csharp
private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
{
    if (TryFourOfAKind(cards, out var foak)) return foak;
    if (TryFullHouse(cards, out var fh)) return fh;
    if (TryStraight(cards, out var st)) return st;
    if (TryThreeOfAKind(cards, out var toak)) return toak;
    if (TryTwoPair(cards, out var tp)) return tp;
    if (TryOnePair(cards, out var op)) return op;
    return MakeHighCard(cards);
}

private static bool TryStraight(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var values = new HashSet<int>(cards
        .Where(c => c.Rank.HasValue)
        .Select(c => AceHighValue(c.Rank.Value)));
    if (values.Contains(14)) values.Add(1); // Ace Low 지원

    for (int top = 14; top >= 5; top--)
    {
        if (values.Contains(top) && values.Contains(top - 1) && values.Contains(top - 2)
            && values.Contains(top - 3) && values.Contains(top - 4))
        {
            // top=14: A-K-Q-J-10, top=5: A(1)-2-3-4-5 (wheel)
            result = new HandResult(HandRank.Straight, cards.ToList(), new List<int> { top });
            return true;
        }
    }
    return false;
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → 전체 테스트. Expected: All Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): add Straight detection with Ace high/low (Duplicate Ace)"
```

---

## Task 6: Flush + StraightFlush + RoyalFlush

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 추가**

```csharp
[Test]
public void Evaluate_Flush_ReturnsFlush()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Heart, Rank.Two),
        S(Suit.Heart, Rank.Five),
        S(Suit.Heart, Rank.Seven),
        S(Suit.Heart, Rank.Nine),
        S(Suit.Heart, Rank.King)
    };
    Assert.AreEqual(HandRank.Flush, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_StraightFlush_ReturnsStraightFlush()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Diamond, Rank.Five),
        S(Suit.Diamond, Rank.Six),
        S(Suit.Diamond, Rank.Seven),
        S(Suit.Diamond, Rank.Eight),
        S(Suit.Diamond, Rank.Nine)
    };
    Assert.AreEqual(HandRank.StraightFlush, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_RoyalFlush_ReturnsRoyalFlush()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Club, Rank.Ten),
        S(Suit.Club, Rank.Jack),
        S(Suit.Club, Rank.Queen),
        S(Suit.Club, Rank.King),
        S(Suit.Club, Rank.Ace)
    };
    Assert.AreEqual(HandRank.RoyalFlush, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 테스트 실패 확인**

Expected: FAIL

- [ ] **Step 3: Flush + StraightFlush + RoyalFlush 구현**

`EvaluateBest`를 다음으로 교체하고, 세 개의 private 메서드 추가:

```csharp
private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
{
    if (TryRoyalFlush(cards, out var rf)) return rf;
    if (TryStraightFlush(cards, out var sf)) return sf;
    if (TryFourOfAKind(cards, out var foak)) return foak;
    if (TryFullHouse(cards, out var fh)) return fh;
    if (TryFlush(cards, out var fl)) return fl;
    if (TryStraight(cards, out var st)) return st;
    if (TryThreeOfAKind(cards, out var toak)) return toak;
    if (TryTwoPair(cards, out var tp)) return tp;
    if (TryOnePair(cards, out var op)) return op;
    return MakeHighCard(cards);
}

private static bool TryRoyalFlush(List<BaseCard> cards, out HandResult result)
{
    result = null;
    var royalValues = new HashSet<int> { 10, 11, 12, 13, 14 };
    foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
    {
        var matching = cards
            .Where(c => c.Suit == suit && c.Rank.HasValue
                        && royalValues.Contains(AceHighValue(c.Rank.Value)))
            .ToList();
        if (matching.Count >= 5)
        {
            result = new HandResult(HandRank.RoyalFlush, matching, new List<int> { 0 });
            return true;
        }
    }
    return false;
}

private static bool TryStraightFlush(List<BaseCard> cards, out HandResult result)
{
    result = null;
    foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
    {
        var suitCards = cards.Where(c => c.Suit == suit && c.Rank.HasValue).ToList();
        if (suitCards.Count < 5) continue;

        var values = new HashSet<int>(suitCards.Select(c => AceHighValue(c.Rank.Value)));
        if (values.Contains(14)) values.Add(1); // Ace Low 지원

        // top=14는 RoyalFlush가 처리하므로 13부터 시작
        for (int top = 13; top >= 5; top--)
        {
            if (values.Contains(top) && values.Contains(top - 1) && values.Contains(top - 2)
                && values.Contains(top - 3) && values.Contains(top - 4))
            {
                result = new HandResult(HandRank.StraightFlush, suitCards, new List<int> { top });
                return true;
            }
        }
    }
    return false;
}

private static bool TryFlush(List<BaseCard> cards, out HandResult result)
{
    result = null;
    foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
    {
        var suitCards = cards
            .Where(c => c.Suit == suit && c.Rank.HasValue)
            .OrderByDescending(c => AceHighValue(c.Rank.Value))
            .ToList();
        if (suitCards.Count < 5) continue;

        var top5 = suitCards.Take(5).ToList();
        var tiebreak = top5.Select(c => AceHighValue(c.Rank.Value)).ToList();
        result = new HandResult(HandRank.Flush, top5, tiebreak);
        return true;
    }
    return false;
}
```

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → 전체 테스트. Expected: All Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): add Flush, StraightFlush, RoyalFlush detection"
```

---

## Task 7: Best-of-n 선택 (nC5 조합)

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 추가**

```csharp
[Test]
public void Evaluate_SevenCards_ReturnsBestHand()
{
    // Heart 5장(Flush) + Spade Ace + Club Ace → 최선은 Flush
    var cards = new List<BaseCard>
    {
        S(Suit.Heart, Rank.Two),
        S(Suit.Heart, Rank.Five),
        S(Suit.Heart, Rank.Seven),
        S(Suit.Heart, Rank.Nine),
        S(Suit.Heart, Rank.King),
        S(Suit.Spade, Rank.Ace),
        S(Suit.Club, Rank.Ace)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(HandRank.Flush, result.Rank);
    Assert.AreEqual(5, result.BestHand.Count);
}

[Test]
public void Evaluate_SixCards_PicksBestFive()
{
    // FourOfAKind(J) + OnePair(K) → FourOfAKind 선택
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Jack),
        S(Suit.Heart, Rank.Jack),
        S(Suit.Diamond, Rank.Jack),
        S(Suit.Club, Rank.Jack),
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King)
    };
    Assert.AreEqual(HandRank.FourOfAKind, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 테스트 실패 확인**

Test Runner → 두 신규 테스트 실행.
Expected: FAIL (7장 모두 EvaluateBest로 넘어가므로 잘못된 결과)

- [ ] **Step 3: nC5 조합 로직 추가**

`Evaluate` 메서드를 다음으로 교체하고, `GetCombinations` 메서드 추가:

```csharp
public HandResult Evaluate(IReadOnlyList<BaseCard> cards)
{
    var standard = new List<BaseCard>();
    int jokerCount = 0;
    foreach (var card in cards)
    {
        if (card.Category == CardCategory.Standard)
            standard.Add(card);
        else if (card.Category == CardCategory.Joker)
            jokerCount++;
        // Custom: 무시
    }

    if (standard.Count == 0 && jokerCount == 0)
        return new HandResult(HandRank.HighCard, new List<BaseCard>(), new List<int> { 0 });

    if (standard.Count <= 5)
        return EvaluateBest(standard, jokerCount);

    // n > 5: nC5 조합 중 최선 선택
    HandResult best = null;
    foreach (var combo in GetCombinations(standard, 5))
    {
        var r = EvaluateBest(combo, jokerCount);
        if (best == null || r.CompareTo(best) > 0) best = r;
    }
    return best;
}

private static IEnumerable<List<BaseCard>> GetCombinations(List<BaseCard> list, int k)
{
    if (k == 0) { yield return new List<BaseCard>(); yield break; }
    for (int i = 0; i <= list.Count - k; i++)
    {
        var rest = list.GetRange(i + 1, list.Count - i - 1);
        foreach (var combo in GetCombinations(rest, k - 1))
        {
            combo.Insert(0, list[i]);
            yield return combo;
        }
    }
}
```

`GetCombinations`의 `using System.Collections.Generic;`은 이미 파일 상단에 있으므로 별도 추가 불필요.

- [ ] **Step 4: 테스트 통과 확인**

Test Runner → 전체 테스트. Expected: All Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): add best-of-n selection with nC5 combinations"
```

---

## Task 8: Joker 와일드 카드 지원

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: 테스트 추가**

```csharp
[Test]
public void Evaluate_OneJokerWithPair_ReturnsThreeOfAKind()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Three),
        S(Suit.Club, Rank.Seven),
        J()
    };
    Assert.AreEqual(HandRank.ThreeOfAKind, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_TwoJokersWithPair_ReturnsFourOfAKind()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Three),
        J(),
        J()
    };
    Assert.AreEqual(HandRank.FourOfAKind, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_OneJokerWithFourFlushCards_ReturnsFlush()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Heart, Rank.Two),
        S(Suit.Heart, Rank.Five),
        S(Suit.Heart, Rank.Seven),
        S(Suit.Heart, Rank.Nine),
        J()
    };
    Assert.AreEqual(HandRank.Flush, _evaluator.Evaluate(cards).Rank);
}

[Test]
public void Evaluate_OneJokerWithFourStraightCards_ReturnsStraight()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Five),
        S(Suit.Heart, Rank.Six),
        S(Suit.Diamond, Rank.Seven),
        S(Suit.Club, Rank.Eight),
        J()
    };
    Assert.AreEqual(HandRank.Straight, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 테스트 실패 확인**

Expected: FAIL (jokerCount가 아직 각 체커에 전달되지 않음)

- [ ] **Step 3: jokerCount를 모든 체커에 전달하도록 EvaluateBest 수정**

`EvaluateBest`를 다음으로 교체:

```csharp
private HandResult EvaluateBest(List<BaseCard> cards, int jokerCount)
{
    if (TryRoyalFlush(cards, jokerCount, out var rf)) return rf;
    if (TryStraightFlush(cards, jokerCount, out var sf)) return sf;
    if (TryFourOfAKind(cards, jokerCount, out var foak)) return foak;
    if (TryFullHouse(cards, jokerCount, out var fh)) return fh;
    if (TryFlush(cards, jokerCount, out var fl)) return fl;
    if (TryStraight(cards, jokerCount, out var st)) return st;
    if (TryThreeOfAKind(cards, jokerCount, out var toak)) return toak;
    if (TryTwoPair(cards, jokerCount, out var tp)) return tp;
    if (TryOnePair(cards, jokerCount, out var op)) return op;
    return MakeHighCard(cards);
}
```

- [ ] **Step 4: 각 체커에 jokerCount 매개변수 추가 및 와일드 로직 구현**

기존 `TryXxx(List<BaseCard> cards, out HandResult result)` 시그니처를 모두 `TryXxx(List<BaseCard> cards, int jokerCount, out HandResult result)`로 변경하고, 각 메서드에 joker 처리 추가:

**TryRoyalFlush:**
```csharp
private static bool TryRoyalFlush(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var royalValues = new HashSet<int> { 10, 11, 12, 13, 14 };
    foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
    {
        var matching = cards
            .Where(c => c.Suit == suit && c.Rank.HasValue
                        && royalValues.Contains(AceHighValue(c.Rank.Value)))
            .ToList();
        if (matching.Count + jokerCount >= 5)
        {
            result = new HandResult(HandRank.RoyalFlush, matching, new List<int> { 0 });
            return true;
        }
    }
    // joker만 5장 이상이면 RoyalFlush
    if (jokerCount >= 5)
    {
        result = new HandResult(HandRank.RoyalFlush, new List<BaseCard>(), new List<int> { 0 });
        return true;
    }
    return false;
}
```

**TryStraightFlush:**
```csharp
private static bool TryStraightFlush(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
    {
        var suitCards = cards.Where(c => c.Suit == suit && c.Rank.HasValue).ToList();
        var values = new HashSet<int>(suitCards.Select(c => AceHighValue(c.Rank.Value)));
        if (values.Contains(14)) values.Add(1);

        for (int top = 13; top >= 5; top--)
        {
            int needed = 0;
            for (int i = 0; i < 5; i++)
                if (!values.Contains(top - i)) needed++;
            if (needed <= jokerCount)
            {
                result = new HandResult(HandRank.StraightFlush, suitCards, new List<int> { top });
                return true;
            }
        }
    }
    return false;
}
```

**TryFourOfAKind:**
```csharp
private static bool TryFourOfAKind(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    if (counts.Count == 0) return false;

    // 가장 높은 rank부터 joker로 채울 수 있는지 확인
    var sorted = counts.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).ToList();
    foreach (var kv in sorted)
    {
        if (kv.Value + jokerCount >= 4)
        {
            int quadRank = kv.Key;
            int kicker = counts
                .Where(k => k.Key != quadRank)
                .OrderByDescending(k => k.Key)
                .Select(k => k.Key)
                .FirstOrDefault();
            result = new HandResult(HandRank.FourOfAKind, cards.ToList(), new List<int> { quadRank, kicker });
            return true;
        }
    }
    return false;
}
```

**TryFullHouse:**
```csharp
private static bool TryFullHouse(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var sorted = counts.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).ToList();
    if (sorted.Count == 0) return false;

    // 가장 높은 그룹에 joker를 써서 triple 만들기
    int jokersLeft = jokerCount;
    int tripleRank = -1;
    foreach (var kv in sorted)
    {
        int needed = System.Math.Max(0, 3 - kv.Value);
        if (needed <= jokersLeft)
        {
            tripleRank = kv.Key;
            jokersLeft -= needed;
            break;
        }
    }
    if (tripleRank == -1) return false;

    // 남은 카드 + 남은 joker로 pair 만들기
    foreach (var kv in sorted)
    {
        if (kv.Key == tripleRank) continue;
        int needed = System.Math.Max(0, 2 - kv.Value);
        if (needed <= jokersLeft)
        {
            result = new HandResult(HandRank.FullHouse, cards.ToList(),
                new List<int> { tripleRank, kv.Key });
            return true;
        }
    }
    // 남은 joker가 2장 이상이면 joker로 pair 구성
    if (jokersLeft >= 2)
    {
        // pair rank = 두 번째로 높은 rank (또는 Ace=14)
        int pairRank = sorted.Where(kv => kv.Key != tripleRank)
                             .Select(kv => kv.Key)
                             .FirstOrDefault();
        if (pairRank == 0) pairRank = 14; // 카드가 없으면 Ace로 간주
        result = new HandResult(HandRank.FullHouse, cards.ToList(),
            new List<int> { tripleRank, pairRank });
        return true;
    }
    return false;
}
```

**TryFlush:**
```csharp
private static bool TryFlush(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
    {
        var suitCards = cards
            .Where(c => c.Suit == suit && c.Rank.HasValue)
            .OrderByDescending(c => AceHighValue(c.Rank.Value))
            .ToList();
        if (suitCards.Count + jokerCount >= 5)
        {
            var top5 = suitCards.Take(5).ToList();
            var tiebreak = top5.Select(c => AceHighValue(c.Rank.Value)).ToList();
            result = new HandResult(HandRank.Flush, top5, tiebreak);
            return true;
        }
    }
    return false;
}
```

**TryStraight:**
```csharp
private static bool TryStraight(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var values = new HashSet<int>(cards
        .Where(c => c.Rank.HasValue)
        .Select(c => AceHighValue(c.Rank.Value)));
    if (values.Contains(14)) values.Add(1);

    for (int top = 14; top >= 5; top--)
    {
        int needed = 0;
        for (int i = 0; i < 5; i++)
            if (!values.Contains(top - i)) needed++;
        if (needed <= jokerCount)
        {
            result = new HandResult(HandRank.Straight, cards.ToList(), new List<int> { top });
            return true;
        }
    }
    return false;
}
```

**TryThreeOfAKind:**
```csharp
private static bool TryThreeOfAKind(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var sorted = counts.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).ToList();
    foreach (var kv in sorted)
    {
        if (kv.Value + jokerCount >= 3)
        {
            int tripleRank = kv.Key;
            var kickers = counts
                .Where(k => k.Key != tripleRank)
                .OrderByDescending(k => k.Key)
                .Take(2)
                .Select(k => k.Key)
                .ToList();
            var tiebreak = new List<int> { tripleRank };
            tiebreak.AddRange(kickers);
            result = new HandResult(HandRank.ThreeOfAKind, cards.ToList(), tiebreak);
            return true;
        }
    }
    if (jokerCount >= 3)
    {
        result = new HandResult(HandRank.ThreeOfAKind, new List<BaseCard>(), new List<int> { 14, 0, 0 });
        return true;
    }
    return false;
}
```

**TryTwoPair:**
```csharp
private static bool TryTwoPair(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();

    // joker 없이 TwoPair
    if (pairs.Count >= 2)
    {
        int highPair = pairs[0].Key;
        int lowPair = pairs[1].Key;
        int kicker = counts.Where(kv => kv.Key != highPair && kv.Key != lowPair)
                           .OrderByDescending(kv => kv.Key).Select(kv => kv.Key).FirstOrDefault();
        result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { highPair, lowPair, kicker });
        return true;
    }
    // joker 1장으로 두 번째 페어 구성
    if (jokerCount >= 1 && pairs.Count >= 1)
    {
        int existingPair = pairs[0].Key;
        int secondPairRank = counts.Where(kv => kv.Key != existingPair)
                                   .OrderByDescending(kv => kv.Key).Select(kv => kv.Key).FirstOrDefault();
        if (secondPairRank > 0)
        {
            result = new HandResult(HandRank.TwoPair, cards.ToList(),
                new List<int> { System.Math.Max(existingPair, secondPairRank),
                                System.Math.Min(existingPair, secondPairRank), 0 });
            return true;
        }
    }
    return false;
}
```

**TryOnePair:**
```csharp
private static bool TryOnePair(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();

    if (pairs.Count >= 1)
    {
        int pairRank = pairs[0].Key;
        var kickers = counts.Where(kv => kv.Key != pairRank)
                            .OrderByDescending(kv => kv.Key).Take(3).Select(kv => kv.Key).ToList();
        var tiebreak = new List<int> { pairRank };
        tiebreak.AddRange(kickers);
        result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak);
        return true;
    }
    if (jokerCount >= 1 && counts.Count >= 1)
    {
        int bestRank = counts.Keys.Max();
        var kickers = counts.Keys.Where(k => k != bestRank).OrderByDescending(k => k).Take(3).ToList();
        var tiebreak = new List<int> { bestRank };
        tiebreak.AddRange(kickers);
        result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak);
        return true;
    }
    return false;
}
```

- [ ] **Step 5: 테스트 통과 확인**

Test Runner → 전체 테스트. Expected: All Passed

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs \
        Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): add Joker wild card support to all hand checkers"
```

---

## Task 9: 엣지케이스 + 타이브레이크 비교 테스트

**Files:**
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

이 Task는 구현 없이 테스트만 추가한다 — 모두 이미 동작해야 한다.

- [ ] **Step 1: Custom 카드 무시 테스트 추가**

```csharp
[Test]
public void Evaluate_CustomCardsIgnored_EvaluatesRemainingCards()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Three),
        S(Suit.Club, Rank.Seven),
        S(Suit.Spade, Rank.Ace),
        Custom(),
        Custom()
    };
    Assert.AreEqual(HandRank.OnePair, _evaluator.Evaluate(cards).Rank);
}
```

- [ ] **Step 2: 5장 미만 엣지케이스 테스트 추가**

```csharp
[Test]
public void Evaluate_ThreeCards_ReturnsValidResult()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Ace),
        S(Suit.Heart, Rank.Ace),
        S(Suit.Diamond, Rank.King)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(HandRank.OnePair, result.Rank);
    Assert.AreEqual(3, result.BestHand.Count);
}
```

- [ ] **Step 3: 타이브레이크 비교 테스트 추가**

```csharp
[Test]
public void Compare_HigherHandRankWins()
{
    var flush = _evaluator.Evaluate(new List<BaseCard>
    {
        S(Suit.Heart, Rank.Two), S(Suit.Heart, Rank.Five),
        S(Suit.Heart, Rank.Seven), S(Suit.Heart, Rank.Nine), S(Suit.Heart, Rank.King)
    });
    var straight = _evaluator.Evaluate(new List<BaseCard>
    {
        S(Suit.Spade, Rank.Five), S(Suit.Heart, Rank.Six),
        S(Suit.Diamond, Rank.Seven), S(Suit.Club, Rank.Eight), S(Suit.Spade, Rank.Nine)
    });
    Assert.Greater(flush.CompareTo(straight), 0);
}

[Test]
public void Compare_SameRankHigherKickerWins()
{
    // OnePair K with Ace kicker vs OnePair K with Queen kicker
    var pairKingAceKicker = _evaluator.Evaluate(new List<BaseCard>
    {
        S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Ace), S(Suit.Club, Rank.Three), S(Suit.Spade, Rank.Two)
    });
    var pairKingQueenKicker = _evaluator.Evaluate(new List<BaseCard>
    {
        S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Queen), S(Suit.Club, Rank.Three), S(Suit.Spade, Rank.Two)
    });
    Assert.Greater(pairKingAceKicker.CompareTo(pairKingQueenKicker), 0);
}

[Test]
public void Compare_IdenticalHands_ReturnsZero()
{
    var hand1 = _evaluator.Evaluate(new List<BaseCard>
    {
        S(Suit.Spade, Rank.King), S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.Ace), S(Suit.Club, Rank.Three), S(Suit.Spade, Rank.Two)
    });
    var hand2 = _evaluator.Evaluate(new List<BaseCard>
    {
        S(Suit.Club, Rank.King), S(Suit.Diamond, Rank.King),
        S(Suit.Spade, Rank.Ace), S(Suit.Heart, Rank.Three), S(Suit.Club, Rank.Two)
    });
    Assert.AreEqual(0, hand1.CompareTo(hand2));
}
```

- [ ] **Step 4: 전체 테스트 통과 확인**

Test Runner → 전체 테스트 실행. Expected: All Passed

- [ ] **Step 5: 커밋**

```bash
git add Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "test(card): add edge case and tiebreak comparison tests"
```
