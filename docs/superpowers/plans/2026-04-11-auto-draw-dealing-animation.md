# Auto Draw + Dealing Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Auto-draw after submit/discard with dealing animation from deck visual to hand.

**Architecture:** `DealSystem.DrawOne()` draws one card at a time. `RoundController` loops with delay for dealing rhythm. `PokerViewModel.IsDealing` gates interaction. `PokerView` animates each new card from deck position to hand slot. Draw button removed entirely.

**Tech Stack:** C#, Unity UI Toolkit (USS transitions), UniTask, VContainer, R3, NUnit

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Features/Poker/Data/PokerConfig.cs` | Modify | Add deal timing config fields |
| `Assets/Scripts/Features/Poker/Systems/DealSystem.cs` | Modify | Add `DrawOne()` method |
| `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` | Modify | Remove DrawCommand/CanDraw, add IsDealing |
| `Assets/Scripts/Features/Poker/Controllers/RoundController.cs` | Modify | Auto-draw loop with dealing, remove Draw subscription |
| `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml` | Modify | Remove draw-button, add deck-area |
| `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss` | Modify | Deck visual styles, dealing animation |
| `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` | Modify | Remove draw button, add deck visual, dealing animation |
| `Assets/Tests/EditMode/Poker/DealSystemTests.cs` | Modify | Add DrawOne test |
| `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` | Modify | Update/remove draw tests, add dealing state tests |

---

### Task 1: PokerConfig — add deal timing fields

**Files:**
- Modify: `Assets/Scripts/Features/Poker/Data/PokerConfig.cs`

- [ ] **Step 1: Add deal timing fields to PokerConfig**

```csharp
using UnityEngine;

namespace FoldingFate.Features.Poker.Data
{
    [CreateAssetMenu(fileName = "PokerConfig", menuName = "FoldingFate/Poker/PokerConfig")]
    public class PokerConfig : ScriptableObject
    {
        [Tooltip("족보 연출 표시 시간 (초)")]
        [Min(0f)]
        public float ShowcaseDurationSeconds = 2f;

        [Tooltip("딜링 시 카드 간 간격 (초)")]
        [Min(0f)]
        public float DealIntervalSeconds = 0.1f;

        [Tooltip("카드 한 장의 이동 애니메이션 시간 (초)")]
        [Min(0f)]
        public float DealAnimationDurationSeconds = 0.15f;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Features/Poker/Data/PokerConfig.cs
git commit -m "feat(poker): add deal timing config fields to PokerConfig"
```

---

### Task 2: DealSystem — add DrawOne method

**Files:**
- Modify: `Assets/Scripts/Features/Poker/Systems/DealSystem.cs`
- Modify: `Assets/Tests/EditMode/Poker/DealSystemTests.cs`

- [ ] **Step 1: Write failing test for DrawOne**

Add to `Assets/Tests/EditMode/Poker/DealSystemTests.cs`:

```csharp
[Test]
public void DrawOne_AddsOneCardToHand()
{
    Assert.AreEqual(0, _hand.Cards.Value.Count);
    _system.DrawOne();
    Assert.AreEqual(1, _hand.Cards.Value.Count);
    Assert.AreEqual(51, _deck.RemainingCount.Value);
}

[Test]
public void DrawOne_DoesNothingWhenHandIsFull()
{
    _system.DrawToFull();
    Assert.AreEqual(8, _hand.Cards.Value.Count);
    int deckBefore = _deck.RemainingCount.Value;

    _system.DrawOne();

    Assert.AreEqual(8, _hand.Cards.Value.Count);
    Assert.AreEqual(deckBefore, _deck.RemainingCount.Value);
}
```

- [ ] **Step 2: Run tests via Unity MCP `run_tests` — verify FAIL**

Expected: compilation error — `DrawOne` doesn't exist yet.

- [ ] **Step 3: Implement DrawOne in DealSystem**

Add to `Assets/Scripts/Features/Poker/Systems/DealSystem.cs` after the `Deal` method:

```csharp
public void DrawOne()
{
    if (_hand.IsFull) return;
    var drawn = _deck.Draw(1);
    _hand.AddCards(drawn);
}
```

- [ ] **Step 4: Run tests via Unity MCP `run_tests` — verify ALL PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Features/Poker/Systems/DealSystem.cs Assets/Tests/EditMode/Poker/DealSystemTests.cs
git commit -m "feat(poker): add DrawOne method to DealSystem"
```

---

### Task 3: PokerViewModel — remove DrawCommand/CanDraw, add IsDealing

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs`
- Modify: `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`

- [ ] **Step 1: Write failing tests for IsDealing**

Add to `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`:

```csharp
[Test]
public void IsDealing_InitiallyFalse()
{
    Assert.IsFalse(_vm.IsDealing.CurrentValue);
}

[Test]
public void BeginDealing_SetsIsDealingTrue()
{
    _vm.BeginDealing();
    Assert.IsTrue(_vm.IsDealing.CurrentValue);
}

[Test]
public void EndDealing_SetsIsDealingFalse()
{
    _vm.BeginDealing();
    _vm.EndDealing();
    Assert.IsFalse(_vm.IsDealing.CurrentValue);
}

[Test]
public void CanSubmit_FalseDuringDealing()
{
    _controller.Start();
    _vm.ToggleSelectCommand.Execute(0);
    Assert.IsTrue(_vm.CanSubmit.CurrentValue);

    _vm.BeginDealing();

    Assert.IsFalse(_vm.CanSubmit.CurrentValue, "딜링 중 제출 불가해야 함");
}
```

- [ ] **Step 2: Remove `DrawCommand_FillsHandToMaxHandSize` and `CanDraw_FalseDuringShowcase` tests**

Delete these two tests from `RoundControllerTests.cs`:

- `DrawCommand_FillsHandToMaxHandSize` (lines 102-118)
- `CanDraw_FalseDuringShowcase` (lines 194-209)

- [ ] **Step 3: Run tests via Unity MCP `run_tests` — verify FAIL**

Expected: compilation error — `IsDealing`, `BeginDealing`, `EndDealing` don't exist yet.

- [ ] **Step 4: Implement PokerViewModel changes**

Replace `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` with:

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
        private readonly ReactiveProperty<string> _handResultText;
        private readonly ReactiveProperty<ShowcaseState> _showcase;
        private readonly ReactiveProperty<bool> _isDealing;

        public ReadOnlyReactiveProperty<IReadOnlyList<BaseCard>> Hand { get; }
        public ReadOnlyReactiveProperty<IReadOnlyList<int>> SelectedIndices { get; }
        public ReadOnlyReactiveProperty<int> DeckRemaining { get; }
        public ReadOnlyReactiveProperty<string> HandResultText { get; }
        public ReadOnlyReactiveProperty<ShowcaseState> Showcase { get; }
        public ReadOnlyReactiveProperty<bool> IsShowcasing { get; }
        public ReadOnlyReactiveProperty<bool> IsDealing { get; }

        public ReadOnlyReactiveProperty<bool> CanSubmit { get; }

        public ReactiveCommand<int> ToggleSelectCommand { get; }
        public ReactiveCommand SubmitCommand { get; }
        public ReactiveCommand DiscardCommand { get; }

        public PokerViewModel(HandModel hand, DeckModel deck)
        {
            _handResultText = new ReactiveProperty<string>(string.Empty).AddTo(_disposables);
            _showcase = new ReactiveProperty<ShowcaseState>(ShowcaseState.Inactive).AddTo(_disposables);
            _isDealing = new ReactiveProperty<bool>(false).AddTo(_disposables);

            Hand = hand.Cards.ToReadOnlyReactiveProperty().AddTo(_disposables);
            SelectedIndices = hand.SelectedIndices.ToReadOnlyReactiveProperty().AddTo(_disposables);
            DeckRemaining = deck.RemainingCount.ToReadOnlyReactiveProperty().AddTo(_disposables);
            HandResultText = _handResultText.ToReadOnlyReactiveProperty().AddTo(_disposables);
            Showcase = _showcase.ToReadOnlyReactiveProperty().AddTo(_disposables);

            var notShowcasing = _showcase.Select(s => !s.IsActive);
            IsShowcasing = _showcase.Select(s => s.IsActive)
                .ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);
            var notDealing = _isDealing.Select(d => !d);
            IsDealing = _isDealing.ToReadOnlyReactiveProperty().AddTo(_disposables);

            var notBusy = notShowcasing.CombineLatest(notDealing, (a, b) => a && b);

            ToggleSelectCommand = new ReactiveCommand<int>(notBusy, initialCanExecute: true).AddTo(_disposables);

            var canSubmit = hand.SelectedIndices
                .Select(indices => indices.Count >= 1 && indices.Count <= 5)
                .CombineLatest(notBusy, (hasSelection, free) => hasSelection && free);
            CanSubmit = canSubmit.ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);
            SubmitCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);

            DiscardCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);
        }

        public void PushHandResult(HandResult result)
        {
            _handResultText.Value = ToDisplayString(result.Rank);
        }

        public void BeginShowcase(HandResult result)
        {
            var highlighted = new HashSet<BaseCard>(result.ContributingCards);
            _showcase.Value = new ShowcaseState(
                true,
                result.BestHand,
                highlighted,
                ToDisplayString(result.Rank));
        }

        public void EndShowcase()
        {
            _showcase.Value = ShowcaseState.Inactive;
        }

        public void BeginDealing()
        {
            _isDealing.Value = true;
        }

        public void EndDealing()
        {
            _isDealing.Value = false;
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

- [ ] **Step 5: Run tests via Unity MCP `run_tests` — verify ALL PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs Assets/Tests/EditMode/Poker/RoundControllerTests.cs
git commit -m "feat(poker): remove DrawCommand/CanDraw, add IsDealing to PokerViewModel"
```

---

### Task 4: RoundController — auto-draw with dealing loop

**Files:**
- Modify: `Assets/Scripts/Features/Poker/Controllers/RoundController.cs`

- [ ] **Step 1: Replace RoundController with auto-draw dealing loop**

```csharp
using System;
using Cysharp.Threading.Tasks;
using R3;
using VContainer.Unity;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.Controllers
{
    public class RoundController : IStartable, IDisposable
    {
        private readonly DealSystem _dealSystem;
        private readonly PokerViewModel _vm;
        private readonly PokerConfig _config;
        private readonly CompositeDisposable _disposables = new();

        public RoundController(DealSystem dealSystem, PokerViewModel vm, PokerConfig config)
        {
            _dealSystem = dealSystem;
            _vm = vm;
            _config = config;
        }

        public void Start()
        {
            _dealSystem.InitializeDeck();
            DealToFullAsync().Forget();

            _vm.ToggleSelectCommand
                .Subscribe(index => _dealSystem.ToggleSelect(index))
                .AddTo(_disposables);

            _vm.SubmitCommand
                .SubscribeAwait(async (_, ct) =>
                {
                    var result = _dealSystem.EvaluateSelected();
                    _vm.BeginShowcase(result);

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.ShowcaseDurationSeconds),
                        cancellationToken: ct);

                    _vm.EndShowcase();
                    _dealSystem.DiscardSelected();
                    _vm.PushHandResult(result);

                    await DealToFullAsync(ct);
                }, AwaitOperation.Drop)
                .AddTo(_disposables);

            _vm.DiscardCommand
                .SubscribeAwait(async (_, ct) =>
                {
                    _dealSystem.DiscardSelected();
                    await DealToFullAsync(ct);
                }, AwaitOperation.Drop)
                .AddTo(_disposables);
        }

        private async UniTask DealToFullAsync(System.Threading.CancellationToken ct = default)
        {
            int needed = _dealSystem.CardsNeeded();
            if (needed <= 0) return;

            _vm.BeginDealing();
            for (int i = 0; i < needed; i++)
            {
                _dealSystem.DrawOne();
                if (i < needed - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.DealIntervalSeconds),
                        cancellationToken: ct);
                }
            }
            _vm.EndDealing();
        }

        public void Dispose() => _disposables.Dispose();
    }
}
```

- [ ] **Step 2: Add `CardsNeeded()` to DealSystem**

In `Assets/Scripts/Features/Poker/Systems/DealSystem.cs`, add:

```csharp
public int CardsNeeded()
{
    return _hand.MaxHandSize - _hand.Cards.Value.Count;
}
```

- [ ] **Step 3: Add test for CardsNeeded in DealSystemTests**

Add to `Assets/Tests/EditMode/Poker/DealSystemTests.cs`:

```csharp
[Test]
public void CardsNeeded_ReturnsCorrectCount()
{
    Assert.AreEqual(8, _system.CardsNeeded());
    _system.Deal(3);
    Assert.AreEqual(5, _system.CardsNeeded());
    _system.DrawToFull();
    Assert.AreEqual(0, _system.CardsNeeded());
}
```

- [ ] **Step 4: Update `DiscardCommand_RemovesSelectedCardsWithoutSettingResult` test**

In `RoundControllerTests.cs`, the discard test now triggers auto-draw which is async. Update it to check immediate behavior only (discard removes cards). The auto-draw is async so hand count may still be 6 synchronously:

```csharp
[Test]
public void DiscardCommand_RemovesSelectedCards()
{
    _controller.Start();
    // Start triggers async dealing — wait for hand to fill synchronously isn't reliable
    // Instead, manually set up state
    _hand.Clear();
    var cards = new List<BaseCard>();
    for (int i = 0; i < 8; i++)
        cards.Add(new BaseCard($"s{i}", CardCategory.Standard, Suit.Spade, (Rank)(i + 2), "", ""));
    _hand.AddCards(cards);

    _vm.ToggleSelectCommand.Execute(0);
    _vm.ToggleSelectCommand.Execute(1);
    Assert.AreEqual(2, _hand.SelectedCount);

    _vm.DiscardCommand.Execute(Unit.Default);

    // Discard happens synchronously — cards removed immediately
    // Auto-draw is async, so we only verify discard worked
    Assert.AreEqual(0, _hand.SelectedCount);
}
```

- [ ] **Step 5: Run tests via Unity MCP `run_tests` — verify ALL PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Features/Poker/Controllers/RoundController.cs Assets/Scripts/Features/Poker/Systems/DealSystem.cs Assets/Tests/EditMode/Poker/DealSystemTests.cs Assets/Tests/EditMode/Poker/RoundControllerTests.cs
git commit -m "feat(poker): auto-draw with dealing loop in RoundController"
```

---

### Task 5: UXML/USS — remove draw button, add deck visual

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml`
- Modify: `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss`

- [ ] **Step 1: Update PokerHUD.uxml — remove draw-button, add deck-area**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:uie="UnityEditor.UIElements">
    <ui:Style src="project://database/Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss" />
    <ui:VisualElement name="poker-root" class="poker-root">
        <ui:VisualElement name="showcase-container" class="showcase-container" />
        <ui:VisualElement name="hand-container" class="hand-container" />
        <ui:Label name="result-label" class="result-label" text="" />
        <ui:VisualElement class="button-row">
            <ui:Button name="submit-button" class="action-button" text="제출" />
            <ui:Button name="discard-button" class="action-button" text="버리기" />
        </ui:VisualElement>
        <ui:VisualElement name="deck-area" class="deck-area">
            <ui:VisualElement name="deck-stack" class="deck-stack" />
            <ui:Label name="deck-count-label" class="deck-count-label" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

Changes:
- Removed `<ui:Label name="deck-count-label">` from top
- Removed `<ui:Button name="draw-button">`
- Added `deck-area` with `deck-stack` and `deck-count-label` at bottom

- [ ] **Step 2: Add deck visual and dealing animation styles to USS**

Add to the end of `PokerHUD.uss` (before the showcasing rules), and update `.deck-count-label`:

Replace the existing `.deck-count-label` (lines 11-15) with:

```css
.deck-count-label {
    color: white;
    font-size: 14px;
    margin-top: 4px;
}
```

Add after `.action-button:disabled` block:

```css
/* === Deck Visual === */

.deck-area {
    position: absolute;
    right: 20px;
    bottom: 20px;
    align-items: center;
}

.deck-stack {
    width: 80px;
    height: 120px;
    background-color: rgb(40, 60, 120);
    border-radius: 8px;
    border-width: 2px;
    border-color: rgb(60, 80, 150);
}

/* === Dealing Animation === */

.card--dealing {
    position: absolute;
    transition-property: translate;
    transition-duration: 0.15s;
    transition-timing-function: ease-out;
}
```

- [ ] **Step 3: Add dimming for deck-area during showcasing**

Add to the showcasing section:

```css
.poker-root--showcasing .deck-area {
    opacity: 0.3;
    transition-property: opacity;
    transition-duration: 0.3s;
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss
git commit -m "feat(poker): add deck visual, remove draw button from UI"
```

---

### Task 6: PokerView — remove draw button, add deck visual, dealing animation

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs`

- [ ] **Step 1: Update PokerView — full replacement**

```csharp
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.UI.Views
{
    [RequireComponent(typeof(UIDocument))]
    public class PokerView : MonoBehaviour
    {
        [Inject] private PokerViewModel _vm;
        [Inject] private PokerConfig _config;

        private UIDocument _doc;
        private VisualElement _handContainer;
        private Label _resultLabel;
        private Label _deckCountLabel;
        private Button _submitButton;
        private Button _discardButton;
        private VisualElement _showcaseContainer;
        private VisualElement _pokerRoot;
        private VisualElement _deckStack;

        private readonly List<VisualElement> _cardElements = new();
        private readonly List<VisualElement> _cardOverlays = new();
        private int _previousHandCount;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = _doc.rootVisualElement;
            _pokerRoot = root.Q("poker-root");
            _handContainer = root.Q("hand-container");
            _showcaseContainer = root.Q("showcase-container");
            _resultLabel = root.Q<Label>("result-label");
            _deckCountLabel = root.Q<Label>("deck-count-label");
            _submitButton = root.Q<Button>("submit-button");
            _discardButton = root.Q<Button>("discard-button");
            _deckStack = root.Q("deck-stack");

            _submitButton.clicked += () => _vm.SubmitCommand.Execute(Unit.Default);
            _discardButton.clicked += () => _vm.DiscardCommand.Execute(Unit.Default);

            _vm.Hand.Subscribe(RenderHand).AddTo(this);
            _vm.SelectedIndices.Subscribe(UpdateSelectionVisuals).AddTo(this);
            _vm.HandResultText.Subscribe(text => _resultLabel.text = text).AddTo(this);
            _vm.DeckRemaining.Subscribe(count => _deckCountLabel.text = $"남은 카드: {count}").AddTo(this);
            _vm.Showcase.Subscribe(RenderShowcase).AddTo(this);

            _vm.CanSubmit.Subscribe(v => _submitButton.SetEnabled(v)).AddTo(this);
            _vm.CanSubmit.Subscribe(v => _discardButton.SetEnabled(v)).AddTo(this);
        }

        private void RenderHand(IReadOnlyList<BaseCard> cards)
        {
            int prevCount = _cardElements.Count;
            bool isDealing = _vm.IsDealing.CurrentValue;

            _handContainer.Clear();
            _cardElements.Clear();
            _cardOverlays.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                int capturedIndex = i;
                var card = cards[i];
                var (cardEl, overlay) = CreateCardElement(card, capturedIndex);

                // New card added during dealing → animate from deck
                if (isDealing && i >= prevCount)
                {
                    AnimateDealCard(cardEl).Forget();
                }

                _handContainer.Add(cardEl);
                _cardElements.Add(cardEl);
                _cardOverlays.Add(overlay);
            }
        }

        private async UniTaskVoid AnimateDealCard(VisualElement cardEl)
        {
            if (_deckStack == null) return;

            // Get deck position relative to poker-root
            var deckRect = _deckStack.worldBound;
            var handRect = _handContainer.worldBound;

            // Start card at deck position (offset from its natural position)
            float offsetX = deckRect.x - handRect.x - (_cardElements.Count - 1) * 88f;
            float offsetY = deckRect.y - handRect.y;

            cardEl.style.translate = new Translate(offsetX, offsetY);
            cardEl.AddToClassList("card--dealing");

            // Wait one frame for the initial position to apply
            await UniTask.Yield(PlayerLoopTiming.Update, this.destroyCancellationToken);

            // Animate to natural position
            cardEl.style.transitionDuration = new List<TimeValue> { new(
                _config.DealAnimationDurationSeconds, TimeUnit.Second) };
            cardEl.style.translate = new Translate(0, 0);

            // Wait for animation to complete
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_config.DealAnimationDurationSeconds),
                cancellationToken: this.destroyCancellationToken);

            cardEl.RemoveFromClassList("card--dealing");
            cardEl.style.transitionDuration = StyleKeyword.Null;
        }

        private (VisualElement card, VisualElement overlay) CreateCardElement(BaseCard card, int index)
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

            var overlay = new VisualElement();
            overlay.AddToClassList("card-overlay");

            el.Add(rankLabel);
            el.Add(suitTopLabel);
            el.Add(centerSuitLabel);
            el.Add(overlay);

            el.RegisterCallback<ClickEvent>(_ => _vm.ToggleSelectCommand.Execute(index));

            return (el, overlay);
        }

        private void UpdateSelectionVisuals(IReadOnlyList<int> selectedIndices)
        {
            var selectedSet = new HashSet<int>(selectedIndices);
            for (int i = 0; i < _cardElements.Count; i++)
            {
                bool selected = selectedSet.Contains(i);
                if (selected)
                {
                    _cardElements[i].AddToClassList("card--selected");
                    _cardOverlays[i].AddToClassList("card-overlay--selected");
                }
                else
                {
                    _cardElements[i].RemoveFromClassList("card--selected");
                    _cardOverlays[i].RemoveFromClassList("card-overlay--selected");
                }
            }
        }

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

            if (isHighlighted)
            {
                var overlay = new VisualElement();
                overlay.AddToClassList("showcase-overlay");
                el.Add(overlay);
            }

            return el;
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
            _          => ((int)rank.Value).ToString()
        };
    }
}
```

Key changes:
- Removed `_drawButton`, its click handler, `CanDraw` subscription
- Added `_deckStack` field, `_config` injection
- `RenderHand` detects new cards during dealing and triggers `AnimateDealCard`
- `AnimateDealCard` calculates offset from deck position to hand, animates via translate transition

- [ ] **Step 2: Register PokerConfig in PokerInstaller for View injection**

Check `Assets/Scripts/Features/Poker/PokerInstaller.cs` — verify `PokerConfig` is already registered. If not, add it. (It should already be registered from prior work.)

- [ ] **Step 3: Run tests via Unity MCP `run_tests` — verify ALL PASS**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Features/Poker/UI/Views/PokerView.cs
git commit -m "feat(poker): remove draw button, add deck visual and dealing animation to PokerView"
```

---

### Task 7: Final verification

- [ ] **Step 1: Run full test suite via Unity MCP `run_tests`**

Expected: ALL tests pass.

- [ ] **Step 2: Visual verification in Unity Editor**

Enter Play mode and verify:
1. 게임 시작 시 덱에서 핸드로 카드가 한 장씩 날아오는 딜링 연출
2. 우측 하단에 덱 카드 뒷면 스택 표시 + 남은 카드 수
3. "다시 받기" 버튼 없음
4. 제출 후: 쇼케이스 연출 → 카드 제거 → 자동 딜링으로 핸드 채움
5. 버리기 후: 카드 제거 → 자동 딜링으로 핸드 채움
6. 딜링 중 카드 선택/제출/버리기 비활성화
7. 쇼케이스 중 덱 영역도 함께 어두워짐
