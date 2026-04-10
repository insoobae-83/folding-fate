# Card UI Selection Fix + Contributing Cards Highlight Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix card selection visual shrink and highlight only hand-contributing cards in showcase.

**Architecture:** Add `ContributingCards` to `HandResult` so `HandEvaluator` reports which cards form the hand rank. Propagate through `ShowcaseState` → `PokerViewModel` → `PokerView` for conditional styling. Fix CSS border-box issue with transparent base border.

**Tech Stack:** C#, Unity UI Toolkit (USS/UXML), NUnit, VContainer, R3

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss` | Modify | CSS fix: transparent base border, dimmed showcase style |
| `Assets/Scripts/Features/Card/Models/HandResult.cs` | Modify | Add `ContributingCards` property |
| `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs` | Modify | Pass contributing cards in each TryXxx |
| `Assets/Scripts/Features/Poker/Models/ShowcaseState.cs` | Modify | Add `HighlightedCards` set |
| `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` | Modify | Pass ContributingCards to ShowcaseState |
| `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` | Modify | Conditional card styling in showcase |
| `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs` | Modify | Add ContributingCards assertions |
| `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` | Modify | Update HandResult constructor calls |

---

### Task 1: Fix card selection CSS — transparent base border

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss:24-41`

- [ ] **Step 1: Add transparent border to base `.card` style**

In `PokerHUD.uss`, add `border-width` and `border-color` to `.card` (line 24-35):

```css
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
    border-width: 3px;
    border-color: transparent;
}
```

- [ ] **Step 2: Remove `border-width` from `.card--selected`, keep only `border-color`**

Change `.card--selected` (line 37-41) to:

```css
.card--selected {
    translate: 0 -12px;
    border-color: rgb(60, 130, 255);
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss
git commit -m "fix(poker): prevent card size shrink on selection by using transparent base border"
```

---

### Task 2: Add `ContributingCards` to `HandResult`

**Files:**
- Modify: `Assets/Scripts/Features/Card/Models/HandResult.cs`
- Modify: `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` (fix compilation)

- [ ] **Step 1: Write failing test — HandResult stores ContributingCards**

Add to `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`:

```csharp
[Test]
public void HandResult_ContributingCards_StoresProvidedCards()
{
    var card1 = S(Suit.Spade, Rank.Ace);
    var card2 = S(Suit.Heart, Rank.Ace);
    var allCards = new List<BaseCard> { card1, card2, S(Suit.Diamond, Rank.Three) };
    var contributing = new List<BaseCard> { card1, card2 };

    var result = new HandResult(HandRank.OnePair, allCards, new List<int> { 14 }, contributing);

    Assert.AreEqual(2, result.ContributingCards.Count);
    Assert.Contains(card1, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(card2, (System.Collections.ICollection)result.ContributingCards);
}
```

- [ ] **Step 2: Run test via Unity MCP `run_tests` — verify FAIL (constructor mismatch)**

Expected: compilation error — `HandResult` constructor doesn't accept 4 parameters yet.

- [ ] **Step 3: Implement `ContributingCards` in HandResult**

Replace `Assets/Scripts/Features/Card/Models/HandResult.cs` with:

```csharp
using System;
using System.Collections.Generic;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class HandResult : IComparable<HandResult>
    {
        public HandRank Rank { get; }
        public IReadOnlyList<BaseCard> BestHand { get; }
        public IReadOnlyList<BaseCard> ContributingCards { get; }
        private readonly IReadOnlyList<int> _tiebreakValues;

        public HandResult(HandRank rank, List<BaseCard> bestHand,
                          List<int> tiebreakValues, List<BaseCard> contributingCards)
        {
            if (bestHand == null) throw new ArgumentNullException(nameof(bestHand));
            if (tiebreakValues == null) throw new ArgumentNullException(nameof(tiebreakValues));
            if (contributingCards == null) throw new ArgumentNullException(nameof(contributingCards));
            Rank = rank;
            BestHand = bestHand.AsReadOnly();
            ContributingCards = contributingCards.AsReadOnly();
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

- [ ] **Step 4: Fix all existing `HandResult` constructor calls**

Every existing `new HandResult(rank, bestHand, tiebreak)` call must add a 4th argument. For now, pass `bestHand` as the contributing cards (will be refined in Task 3).

In `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`, update every `new HandResult(...)` call to add `cards.ToList()` (or the appropriate list) as the 4th parameter. Specifically:

- `MakeHighCard` (line 62): change to `new HandResult(HandRank.HighCard, cards.ToList(), tiebreak, new List<BaseCard> { cards.OrderByDescending(c => AceHighValue(c.Rank.Value)).First() })`
- `TryOnePair` (lines 93, 103): add `cards.ToList()` as 4th arg (placeholder)
- `TryTwoPair` (lines 121, 134): add `cards.ToList()` as 4th arg (placeholder)
- `TryThreeOfAKind` (line 258): add `cards.ToList()` as 4th arg (placeholder)
- `TryFourOfAKind` (lines 149, 166): add `cards.ToList()` or `new List<BaseCard>()` as 4th arg (placeholder)
- `TryFullHouse` (lines 202, 212): add `cards.ToList()` as 4th arg (placeholder)
- `TryStraight` (line 233): add `cards.ToList()` as 4th arg
- `TryFlush` (line 354): add `top5` as 4th arg
- `TryStraightFlush` (line 335): add `bestSuitCards` as 4th arg
- `TryRoyalFlush` (lines 291, 297): add `matching` or `new List<BaseCard>()` as 4th arg
- Empty input case (line 25): add `new List<BaseCard>()` as 4th arg

In `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`, update 3 `new HandResult(...)` calls (lines 153, 169, 188) to add `cards` as the 4th argument:

```csharp
// Line 153 (BeginShowcase_SetsShowcaseActive):
var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);

// Line 169 (EndShowcase_SetsShowcaseInactive):
var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);

// Line 188 (CanSubmit_FalseDuringShowcase):
var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);
```

And in `CanDraw_FalseDuringShowcase` (line 205):
```csharp
var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);
```

- [ ] **Step 5: Run tests via Unity MCP `run_tests` — verify ALL PASS**

Expected: all existing tests pass + new `HandResult_ContributingCards_StoresProvidedCards` passes.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Features/Card/Models/HandResult.cs Assets/Tests/EditMode/Card/HandEvaluatorTests.cs Assets/Tests/EditMode/Poker/RoundControllerTests.cs Assets/Scripts/Features/Card/Systems/HandEvaluator.cs
git commit -m "feat(card): add ContributingCards property to HandResult"
```

---

### Task 3: HandEvaluator — pass correct contributing cards per hand rank

**Files:**
- Modify: `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`
- Modify: `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs`

- [ ] **Step 1: Write failing tests for ContributingCards per hand rank**

Add these tests to `HandEvaluatorTests.cs`:

```csharp
[Test]
public void Evaluate_HighCard_ContributingCardsIsHighestCard()
{
    var king = S(Suit.Spade, Rank.King);
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Two),
        S(Suit.Heart, Rank.Four),
        S(Suit.Diamond, Rank.Seven),
        S(Suit.Club, Rank.Nine),
        king
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(1, result.ContributingCards.Count);
    Assert.AreEqual(king, result.ContributingCards[0]);
}

[Test]
public void Evaluate_OnePair_ContributingCardsIsPairOnly()
{
    var kingS = S(Suit.Spade, Rank.King);
    var kingH = S(Suit.Heart, Rank.King);
    var cards = new List<BaseCard>
    {
        kingS, kingH,
        S(Suit.Diamond, Rank.Three),
        S(Suit.Club, Rank.Seven),
        S(Suit.Spade, Rank.Ace)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(2, result.ContributingCards.Count);
    Assert.Contains(kingS, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(kingH, (System.Collections.ICollection)result.ContributingCards);
}

[Test]
public void Evaluate_TwoPair_ContributingCardsIsBothPairs()
{
    var kingS = S(Suit.Spade, Rank.King);
    var kingH = S(Suit.Heart, Rank.King);
    var threeD = S(Suit.Diamond, Rank.Three);
    var threeC = S(Suit.Club, Rank.Three);
    var cards = new List<BaseCard>
    {
        kingS, kingH, threeD, threeC,
        S(Suit.Spade, Rank.Ace)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(4, result.ContributingCards.Count);
    Assert.Contains(kingS, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(kingH, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(threeD, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(threeC, (System.Collections.ICollection)result.ContributingCards);
}

[Test]
public void Evaluate_ThreeOfAKind_ContributingCardsIsTripleOnly()
{
    var qS = S(Suit.Spade, Rank.Queen);
    var qH = S(Suit.Heart, Rank.Queen);
    var qD = S(Suit.Diamond, Rank.Queen);
    var cards = new List<BaseCard>
    {
        qS, qH, qD,
        S(Suit.Club, Rank.Two),
        S(Suit.Spade, Rank.Five)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(3, result.ContributingCards.Count);
    Assert.Contains(qS, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(qH, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(qD, (System.Collections.ICollection)result.ContributingCards);
}

[Test]
public void Evaluate_FourOfAKind_ContributingCardsIsQuadOnly()
{
    var jS = S(Suit.Spade, Rank.Jack);
    var jH = S(Suit.Heart, Rank.Jack);
    var jD = S(Suit.Diamond, Rank.Jack);
    var jC = S(Suit.Club, Rank.Jack);
    var cards = new List<BaseCard>
    {
        jS, jH, jD, jC,
        S(Suit.Spade, Rank.Three)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(4, result.ContributingCards.Count);
    Assert.Contains(jS, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(jH, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(jD, (System.Collections.ICollection)result.ContributingCards);
    Assert.Contains(jC, (System.Collections.ICollection)result.ContributingCards);
}

[Test]
public void Evaluate_Straight_ContributingCardsIsAll()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.Five),
        S(Suit.Heart, Rank.Six),
        S(Suit.Diamond, Rank.Seven),
        S(Suit.Club, Rank.Eight),
        S(Suit.Spade, Rank.Nine)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(5, result.ContributingCards.Count);
}

[Test]
public void Evaluate_Flush_ContributingCardsIsAll()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Heart, Rank.Two),
        S(Suit.Heart, Rank.Five),
        S(Suit.Heart, Rank.Seven),
        S(Suit.Heart, Rank.Nine),
        S(Suit.Heart, Rank.King)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(5, result.ContributingCards.Count);
}

[Test]
public void Evaluate_FullHouse_ContributingCardsIsAll()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Spade, Rank.King),
        S(Suit.Heart, Rank.King),
        S(Suit.Diamond, Rank.King),
        S(Suit.Club, Rank.Ace),
        S(Suit.Spade, Rank.Ace)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(5, result.ContributingCards.Count);
}

[Test]
public void Evaluate_StraightFlush_ContributingCardsIsAll()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Diamond, Rank.Five),
        S(Suit.Diamond, Rank.Six),
        S(Suit.Diamond, Rank.Seven),
        S(Suit.Diamond, Rank.Eight),
        S(Suit.Diamond, Rank.Nine)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(5, result.ContributingCards.Count);
}

[Test]
public void Evaluate_RoyalFlush_ContributingCardsIsAll()
{
    var cards = new List<BaseCard>
    {
        S(Suit.Club, Rank.Ten),
        S(Suit.Club, Rank.Jack),
        S(Suit.Club, Rank.Queen),
        S(Suit.Club, Rank.King),
        S(Suit.Club, Rank.Ace)
    };
    var result = _evaluator.Evaluate(cards);
    Assert.AreEqual(5, result.ContributingCards.Count);
}
```

- [ ] **Step 2: Run tests via Unity MCP `run_tests` — verify new tests FAIL**

Expected: tests fail because Task 2 placeholder passes `cards.ToList()` for all hand ranks instead of the correct subset.

- [ ] **Step 3: Implement correct contributing cards in HandEvaluator**

Update each method in `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs`:

**`MakeHighCard`** — highest rank card only:
```csharp
private static HandResult MakeHighCard(List<BaseCard> cards)
{
    var tiebreak = cards
        .Where(c => c.Rank.HasValue)
        .Select(c => AceHighValue(c.Rank.Value))
        .OrderByDescending(v => v)
        .ToList();
    var highest = cards
        .Where(c => c.Rank.HasValue)
        .OrderByDescending(c => AceHighValue(c.Rank.Value))
        .Take(1)
        .ToList();
    return new HandResult(HandRank.HighCard, cards.ToList(), tiebreak, highest);
}
```

**`TryOnePair`** — filter cards matching pair rank:
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
        var contributing = cards.Where(c => c.Rank.HasValue && AceHighValue(c.Rank.Value) == pairRank).ToList();
        result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak, contributing);
        return true;
    }
    if (jokerCount >= 1 && counts.Count >= 1)
    {
        int bestRank = counts.Keys.Max();
        var kickers = counts.Keys.Where(k => k != bestRank).OrderByDescending(k => k).Take(3).ToList();
        var tiebreak = new List<int> { bestRank };
        tiebreak.AddRange(kickers);
        var contributing = cards.Where(c => c.Rank.HasValue && AceHighValue(c.Rank.Value) == bestRank).ToList();
        result = new HandResult(HandRank.OnePair, cards.ToList(), tiebreak, contributing);
        return true;
    }
    return false;
}
```

**`TryTwoPair`** — filter cards matching both pair ranks:
```csharp
private static bool TryTwoPair(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    var pairs = counts.Where(kv => kv.Value >= 2).OrderByDescending(kv => kv.Key).ToList();

    if (pairs.Count >= 2)
    {
        int highPair = pairs[0].Key;
        int lowPair = pairs[1].Key;
        int kicker = counts.Where(kv => kv.Key != highPair && kv.Key != lowPair)
                           .OrderByDescending(kv => kv.Key).Select(kv => kv.Key).FirstOrDefault();
        var contributing = cards.Where(c => c.Rank.HasValue &&
            (AceHighValue(c.Rank.Value) == highPair || AceHighValue(c.Rank.Value) == lowPair)).ToList();
        result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { highPair, lowPair, kicker }, contributing);
        return true;
    }
    if (jokerCount >= 1 && pairs.Count >= 1)
    {
        int existingPair = pairs[0].Key;
        int secondPairRank = counts.Where(kv => kv.Key != existingPair)
                                   .OrderByDescending(kv => kv.Key).Select(kv => kv.Key).FirstOrDefault();
        if (secondPairRank > 0)
        {
            int high = Math.Max(existingPair, secondPairRank);
            int low = Math.Min(existingPair, secondPairRank);
            var contributing = cards.Where(c => c.Rank.HasValue &&
                (AceHighValue(c.Rank.Value) == existingPair || AceHighValue(c.Rank.Value) == secondPairRank)).ToList();
            result = new HandResult(HandRank.TwoPair, cards.ToList(), new List<int> { high, low, 0 }, contributing);
            return true;
        }
    }
    return false;
}
```

**`TryThreeOfAKind`** — filter cards matching triple rank:
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
            var contributing = cards.Where(c => c.Rank.HasValue && AceHighValue(c.Rank.Value) == tripleRank).ToList();
            result = new HandResult(HandRank.ThreeOfAKind, cards.ToList(), tiebreak, contributing);
            return true;
        }
    }
    return false;
}
```

**`TryFourOfAKind`** — filter cards matching quad rank:
```csharp
private static bool TryFourOfAKind(List<BaseCard> cards, int jokerCount, out HandResult result)
{
    result = null;
    var counts = GetRankCounts(cards);
    if (counts.Count == 0)
    {
        if (jokerCount >= 4)
        {
            result = new HandResult(HandRank.FourOfAKind, new List<BaseCard>(), new List<int> { 14, 0 }, new List<BaseCard>());
            return true;
        }
        return false;
    }

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
            var contributing = cards.Where(c => c.Rank.HasValue && AceHighValue(c.Rank.Value) == quadRank).ToList();
            result = new HandResult(HandRank.FourOfAKind, cards.ToList(), new List<int> { quadRank, kicker }, contributing);
            return true;
        }
    }
    return false;
}
```

**`TryFullHouse`** — all 5 cards contribute:
```csharp
// In both return paths, pass cards.ToList() as contributing:
result = new HandResult(HandRank.FullHouse, cards.ToList(),
    new List<int> { tripleRank, kv.Key }, cards.ToList());
// ...
result = new HandResult(HandRank.FullHouse, cards.ToList(),
    new List<int> { tripleRank, pairRank }, cards.ToList());
```

**`TryStraight`** — all 5 cards contribute:
```csharp
result = new HandResult(HandRank.Straight, cards.ToList(), new List<int> { top }, cards.ToList());
```

**`TryFlush`** — all 5 (top5) cards contribute:
```csharp
result = new HandResult(HandRank.Flush, top5, tiebreak, top5.ToList());
```

**`TryStraightFlush`** — all suit cards contribute:
```csharp
result = new HandResult(HandRank.StraightFlush, bestSuitCards, new List<int> { bestTop }, bestSuitCards.ToList());
```

**`TryRoyalFlush`** — all matching cards contribute:
```csharp
result = new HandResult(HandRank.RoyalFlush, matching, new List<int> { 0 }, matching.ToList());
// joker-only case:
result = new HandResult(HandRank.RoyalFlush, new List<BaseCard>(), new List<int> { 0 }, new List<BaseCard>());
```

**Empty input case (line 25):**
```csharp
return new HandResult(HandRank.HighCard, new List<BaseCard>(), new List<int> { 0 }, new List<BaseCard>());
```

- [ ] **Step 4: Run tests via Unity MCP `run_tests` — verify ALL PASS**

Expected: all existing + new ContributingCards tests pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Card/Systems/HandEvaluator.cs Assets/Tests/EditMode/Card/HandEvaluatorTests.cs
git commit -m "feat(card): implement correct ContributingCards per hand rank in HandEvaluator"
```

---

### Task 4: ShowcaseState — add HighlightedCards

**Files:**
- Modify: `Assets/Scripts/Features/Poker/Models/ShowcaseState.cs`

- [ ] **Step 1: Add `HighlightedCards` to ShowcaseState**

Replace `Assets/Scripts/Features/Poker/Models/ShowcaseState.cs` with:

```csharp
using System.Collections.Generic;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class ShowcaseState
    {
        public static readonly ShowcaseState Inactive = new(false, new List<BaseCard>(), new HashSet<BaseCard>(), string.Empty);

        private readonly HashSet<BaseCard> _highlightedSet;

        public bool IsActive { get; }
        public IReadOnlyList<BaseCard> Cards { get; }
        public string RankText { get; }

        public ShowcaseState(bool isActive, IReadOnlyList<BaseCard> cards,
                             HashSet<BaseCard> highlightedCards, string rankText)
        {
            IsActive = isActive;
            Cards = cards;
            _highlightedSet = highlightedCards;
            RankText = rankText;
        }

        public bool IsHighlighted(BaseCard card) => _highlightedSet.Contains(card);
    }
}
```

- [ ] **Step 2: Update PokerViewModel.BeginShowcase to pass ContributingCards**

In `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs`, update `BeginShowcase` (line 68-74):

```csharp
public void BeginShowcase(HandResult result)
{
    var highlighted = new HashSet<BaseCard>(result.ContributingCards);
    _showcase.Value = new ShowcaseState(
        true,
        result.BestHand,
        highlighted,
        ToDisplayString(result.Rank));
}
```

- [ ] **Step 3: Run tests via Unity MCP `run_tests` — verify ALL PASS**

Expected: all tests pass (ShowcaseState constructor change is compatible, RoundControllerTests create HandResult via evaluator or via updated constructor).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Features/Poker/Models/ShowcaseState.cs Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs
git commit -m "feat(poker): add HighlightedCards to ShowcaseState for conditional card styling"
```

---

### Task 5: View — conditional showcase card styling + dimmed USS

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs:119-176`
- Modify: `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss:130-141`

- [ ] **Step 1: Add `.showcase-card--dimmed` style to USS**

In `PokerHUD.uss`, after the `.showcase-card` block (line 141), add:

```css
.showcase-card--dimmed {
    width: 80px;
    height: 120px;
    background-color: white;
    border-radius: 8px;
    margin: 0 4px;
    padding: 6px;
    align-items: center;
    position: relative;
    border-width: 2px;
    border-color: rgb(180, 180, 180);
    opacity: 0.6;
}
```

- [ ] **Step 2: Update `RenderShowcase` to use `IsHighlighted`**

In `PokerView.cs`, update `RenderShowcase` (line 119-149):

```csharp
private void RenderShowcase(ShowcaseState state)
{
    _showcaseContainer.Clear();

    if (state.IsActive)
    {
        _showcaseContainer.AddToClassList("showcase-container--active");
        _pokerRoot.AddToClassList("poker-root--showcasing");

        var cardsRow = new VisualElement();
        cardsRow.AddToClassList("showcase-cards");

        foreach (var card in state.Cards)
        {
            var cardEl = CreateShowcaseCardElement(card, state.IsHighlighted(card));
            cardsRow.Add(cardEl);
        }

        var rankLabel = new Label();
        rankLabel.AddToClassList("showcase-rank-text");
        rankLabel.text = state.RankText;

        _showcaseContainer.Add(cardsRow);
        _showcaseContainer.Add(rankLabel);
    }
    else
    {
        _showcaseContainer.RemoveFromClassList("showcase-container--active");
        _pokerRoot.RemoveFromClassList("poker-root--showcasing");
    }
}
```

- [ ] **Step 3: Update `CreateShowcaseCardElement` to accept `isHighlighted`**

In `PokerView.cs`, update `CreateShowcaseCardElement` (line 151-176):

```csharp
private VisualElement CreateShowcaseCardElement(BaseCard card, bool isHighlighted)
{
    var el = new VisualElement();
    el.AddToClassList(isHighlighted ? "showcase-card" : "showcase-card--dimmed");

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

    return el;
}
```

- [ ] **Step 4: Run tests via Unity MCP `run_tests` — verify ALL PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/Views/PokerView.cs Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss
git commit -m "feat(poker): highlight only contributing cards in showcase, dim kickers"
```

---

### Task 6: Final verification

- [ ] **Step 1: Run full test suite via Unity MCP `run_tests`**

Expected: ALL tests pass.

- [ ] **Step 2: Visual verification in Unity Editor**

Enter Play mode and verify:
1. 카드 선택 시 크기 유지, 파란 테두리 + 위로 이동만 적용
2. Submit 후 쇼케이스에서 족보 기여 카드만 골든 테두리, 나머지 카드는 회색 + 반투명
