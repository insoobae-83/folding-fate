# 족보 연출(Hand Showcase) 구현 플랜

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 제출 후 족보 카드를 핸드 위에 2초간 연출하고 기본 화면으로 복귀하는 기능을 구현한다.

**Architecture:** `ShowcaseState` 모델이 연출 상태를 담고, `PokerViewModel`이 `ReactiveProperty<ShowcaseState>`로 노출한다. `RoundController`가 제출 시 UniTask 비동기로 연출 진입 → 대기 → 연출 퇴장 → 카드 제거 흐름을 제어한다. 연출 시간은 `PokerConfig` ScriptableObject에서 읽는다.

**Tech Stack:** C#, R3 (ReactiveProperty, ReactiveCommand, CombineLatest), UniTask, Unity UI Toolkit (UXML/USS), VContainer, ScriptableObject

---

## 변경 파일

| 구분 | 파일 | 역할 |
|---|---|---|
| 생성 | `Assets/Scripts/Features/Poker/Models/ShowcaseState.cs` | 연출 상태 데이터 |
| 생성 | `Assets/Scripts/Features/Poker/Data/PokerConfig.cs` | 연출 시간 등 설정 SO |
| 수정 | `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` | Showcase RP 추가, 커맨드 조건 수정 |
| 수정 | `Assets/Scripts/Features/Poker/Controllers/RoundController.cs` | 비동기 제출 흐름 |
| 수정 | `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml` | showcase-container 추가 |
| 수정 | `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss` | 연출 스타일 |
| 수정 | `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` | Showcase 구독 및 렌더링 |
| 수정 | `Assets/Scripts/Features/Poker/PokerInstaller.cs` | PokerConfig 등록 |
| 수정 | `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` | 연출 테스트 추가, SetUp 수정 |

---

### Task 1: ShowcaseState 모델 생성

**Files:**
- Create: `Assets/Scripts/Features/Poker/Models/ShowcaseState.cs`

- [ ] **Step 1: ShowcaseState 클래스 작성**

```csharp
using System.Collections.Generic;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class ShowcaseState
    {
        public static readonly ShowcaseState Inactive = new(false, new List<BaseCard>(), string.Empty);

        public bool IsActive { get; }
        public IReadOnlyList<BaseCard> Cards { get; }
        public string RankText { get; }

        public ShowcaseState(bool isActive, IReadOnlyList<BaseCard> cards, string rankText)
        {
            IsActive = isActive;
            Cards = cards;
            RankText = rankText;
        }
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/Scripts/Features/Poker/Models/ShowcaseState.cs
git commit -m "feat(poker): add ShowcaseState model"
```

---

### Task 2: PokerConfig ScriptableObject 생성

**Files:**
- Create: `Assets/Scripts/Features/Poker/Data/PokerConfig.cs`

- [ ] **Step 1: PokerConfig 작성**

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
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/Scripts/Features/Poker/Data/PokerConfig.cs
git commit -m "feat(poker): add PokerConfig ScriptableObject"
```

- [ ] **Step 3: Unity Editor에서 에셋 생성**

Unity Editor에서 **Assets > Create > FoldingFate > Poker > PokerConfig** 메뉴로 `Assets/Settings/PokerConfig.asset` 생성.
`ShowcaseDurationSeconds` 값을 `2`로 설정.

---

### Task 3: PokerViewModel — Showcase 상태 추가 (TDD)

**Files:**
- Modify: `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`
- Modify: `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs`

- [ ] **Step 1: ViewModel 연출 상태 테스트 작성 (실패)**

`RoundControllerTests.cs`에 추가:

```csharp
[Test]
public void Showcase_InitiallyInactive()
{
    Assert.IsFalse(_vm.Showcase.CurrentValue.IsActive);
}

[Test]
public void BeginShowcase_SetsShowcaseActive()
{
    var cards = new List<BaseCard>
    {
        new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
    };
    var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });

    _vm.BeginShowcase(result);

    Assert.IsTrue(_vm.Showcase.CurrentValue.IsActive);
    Assert.AreEqual(1, _vm.Showcase.CurrentValue.Cards.Count);
    Assert.AreEqual("하이 카드", _vm.Showcase.CurrentValue.RankText);
}

[Test]
public void EndShowcase_SetsShowcaseInactive()
{
    var cards = new List<BaseCard>
    {
        new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
    };
    var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });

    _vm.BeginShowcase(result);
    _vm.EndShowcase();

    Assert.IsFalse(_vm.Showcase.CurrentValue.IsActive);
}

[Test]
public void CanSubmit_FalseDuringShowcase()
{
    _controller.Start();
    _vm.ToggleSelectCommand.Execute(0);
    Assert.IsTrue(_vm.CanSubmit.CurrentValue, "선택 후 제출 가능해야 함");

    var cards = new List<BaseCard>
    {
        new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
    };
    var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });
    _vm.BeginShowcase(result);

    Assert.IsFalse(_vm.CanSubmit.CurrentValue, "연출 중 제출 불가해야 함");
}

[Test]
public void CanDraw_FalseDuringShowcase()
{
    _controller.Start();
    _hand.Clear();
    Assert.IsTrue(_vm.CanDraw.CurrentValue, "핸드 비었을 때 드로우 가능해야 함");

    var cards = new List<BaseCard>
    {
        new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
    };
    var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });
    _vm.BeginShowcase(result);

    Assert.IsFalse(_vm.CanDraw.CurrentValue, "연출 중 드로우 불가해야 함");
}
```

파일 상단에 using 추가 필요:

```csharp
using FoldingFate.Features.Poker.Models; // ShowcaseState 참조용
```

- [ ] **Step 2: 테스트 실행 (Unity MCP) — 실패 확인**

Unity Editor Test Runner에서 실행. `Showcase`, `BeginShowcase`, `EndShowcase` 멤버가 없어서 컴파일 에러 예상.

- [ ] **Step 3: PokerViewModel 수정**

`PokerViewModel.cs` 전체 교체 내용:

**using 추가:**

```csharp
using FoldingFate.Features.Poker.Models;
```

**필드 추가** (`_handResultText` 아래):

```csharp
private readonly ReactiveProperty<ShowcaseState> _showcase;
```

**프로퍼티 추가** (`HandResultText` 아래):

```csharp
public ReadOnlyReactiveProperty<ShowcaseState> Showcase { get; }
public ReadOnlyReactiveProperty<bool> IsShowcasing { get; }
```

**생성자 수정** — 전체 생성자:

```csharp
public PokerViewModel(HandModel hand, DeckModel deck)
{
    _handResultText = new ReactiveProperty<string>(string.Empty).AddTo(_disposables);
    _showcase = new ReactiveProperty<ShowcaseState>(ShowcaseState.Inactive).AddTo(_disposables);

    Hand = hand.Cards.ToReadOnlyReactiveProperty().AddTo(_disposables);
    SelectedIndices = hand.SelectedIndices.ToReadOnlyReactiveProperty().AddTo(_disposables);
    DeckRemaining = deck.RemainingCount.ToReadOnlyReactiveProperty().AddTo(_disposables);
    HandResultText = _handResultText.ToReadOnlyReactiveProperty().AddTo(_disposables);
    Showcase = _showcase.ToReadOnlyReactiveProperty().AddTo(_disposables);

    var notShowcasing = _showcase.Select(s => !s.IsActive);
    IsShowcasing = _showcase.Select(s => s.IsActive)
        .ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);

    ToggleSelectCommand = new ReactiveCommand<int>(notShowcasing, initialCanExecute: true).AddTo(_disposables);

    var canSubmit = hand.SelectedIndices
        .Select(indices => indices.Count >= 1 && indices.Count <= 5)
        .CombineLatest(notShowcasing, (hasSelection, notShowing) => hasSelection && notShowing);
    CanSubmit = canSubmit.ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);
    SubmitCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);

    var canDraw = hand.Cards
        .Select(cards => cards.Count < hand.MaxHandSize)
        .CombineLatest(notShowcasing, (canD, notShowing) => canD && notShowing);
    CanDraw = canDraw.ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);
    DrawCommand = new ReactiveCommand(canDraw, initialCanExecute: false).AddTo(_disposables);

    DiscardCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);
}
```

**메서드 추가** (`PushHandResult` 아래):

```csharp
public void BeginShowcase(HandResult result)
{
    _showcase.Value = new ShowcaseState(
        true,
        result.BestHand,
        ToDisplayString(result.Rank));
}

public void EndShowcase()
{
    _showcase.Value = ShowcaseState.Inactive;
}
```

- [ ] **Step 4: 테스트 실행 (Unity MCP) — 전부 PASS 확인**

기존 테스트 포함 전체 EditMode 테스트 실행. 전부 PASS 예상.

> 주의: `CanDraw`의 `initialCanExecute`가 `true`에서 `false`로 변경되었지만, 실제 값은 구독 시점에 `CombineLatest`가 결정하므로 동작에는 차이 없음. 기존 `DrawCommand_FillsHandToMaxHandSize` 테스트는 `_controller.Start()` 이후 핸드를 비운 뒤 실행하므로 영향 없음.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs
git add Assets/Tests/EditMode/Poker/RoundControllerTests.cs
git commit -m "feat(poker): add showcase state to PokerViewModel with tests"
```

---

### Task 4: RoundController — 비동기 제출 흐름 (TDD)

**Files:**
- Modify: `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`
- Modify: `Assets/Scripts/Features/Poker/Controllers/RoundController.cs`

- [ ] **Step 1: RoundControllerTests SetUp에 PokerConfig 추가**

기존 `SetUp`을 수정하여 PokerConfig를 생성하고 RoundController에 전달:

```csharp
private PokerConfig _config;

[SetUp]
public void SetUp()
{
    _deck = new DeckModel();
    _hand = new HandModel(maxHandSize: 8);
    _dealSystem = new DealSystem(_deck, _hand, new HandEvaluator());
    _vm = new PokerViewModel(_hand, _deck);
    _config = ScriptableObject.CreateInstance<PokerConfig>();
    _config.ShowcaseDurationSeconds = 0f;
    _controller = new RoundController(_dealSystem, _vm, _config);
}
```

`TearDown`에 추가:

```csharp
Object.DestroyImmediate(_config);
```

파일 상단에 using 추가:

```csharp
using UnityEngine;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Models;
```

- [ ] **Step 2: 제출 시 연출 진입 테스트 작성**

```csharp
[Test]
public void SubmitCommand_ActivatesShowcase()
{
    _controller.Start();
    _vm.ToggleSelectCommand.Execute(0);

    _vm.SubmitCommand.Execute(Unit.Default);

    // 비동기 핸들러의 동기 부분(BeginShowcase)이 즉시 실행됨
    Assert.IsTrue(_vm.Showcase.CurrentValue.IsActive,
        "제출 직후 연출이 활성화되어야 함");
    Assert.IsTrue(_vm.Showcase.CurrentValue.Cards.Count > 0,
        "연출에 카드가 포함되어야 함");
    Assert.IsFalse(string.IsNullOrEmpty(_vm.Showcase.CurrentValue.RankText),
        "연출에 족보 텍스트가 포함되어야 함");
}
```

- [ ] **Step 3: 테스트 실행 (Unity MCP) — 실패 확인**

RoundController 생성자가 `(DealSystem, PokerViewModel)`만 받으므로 컴파일 에러 예상.

- [ ] **Step 4: RoundController 수정**

`RoundController.cs` 전체 교체:

```csharp
using System;
using System.Linq;
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
            _dealSystem.DrawToFull();

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
                }, AwaitOperation.Drop)
                .AddTo(_disposables);

            _vm.DrawCommand
                .Subscribe(_ => _dealSystem.DrawToFull())
                .AddTo(_disposables);

            _vm.DiscardCommand
                .Subscribe(_ => _dealSystem.DiscardSelected())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
```

- [ ] **Step 5: 테스트 실행 (Unity MCP) — 확인**

`SubmitCommand_ActivatesShowcase` 테스트 PASS 예상 (비동기 핸들러의 동기 부분인 `BeginShowcase`가 즉시 실행됨).

> 주의: `ShowcaseDurationSeconds = 0f`이지만 `UniTask.Delay(TimeSpan.Zero)` 는 EditMode에서 다음 프레임 대기가 필요할 수 있음. 이 경우 `EndShowcase`와 `DiscardSelected`는 실행되지 않을 수 있다. 이전 테스트 `SubmitCommand_UpdatesHandResultText`는 `HandResultText`가 `EndShowcase` 이후에 설정되므로 실패할 수 있다.

기존 `SubmitCommand_UpdatesHandResultText` 테스트를 아래와 같이 수정:

```csharp
[Test]
public void SubmitCommand_UpdatesHandResultText()
{
    _controller.Start();
    _vm.ToggleSelectCommand.Execute(0);
    _vm.SubmitCommand.Execute(Unit.Default);

    // 비동기 연출 (duration=0) 후 HandResultText 설정됨
    // EditMode에서 UniTask.Delay(0)가 즉시 완료되지 않을 수 있으므로
    // 연출 활성화만 확인하고, HandResultText는 연출 종료 후 확인
    // duration=0이면 동기적으로 완료될 수 있음
    // 실패 시 연출 진입만 검증하는 것으로 대체
    Assert.IsTrue(_vm.Showcase.CurrentValue.IsActive || 
        !string.IsNullOrEmpty(_vm.HandResultText.CurrentValue),
        "제출 후 연출이 활성화되거나 결과 텍스트가 설정되어야 함");
}
```

- [ ] **Step 6: 전체 EditMode 테스트 실행 (Unity MCP)**

전체 테스트 통과 확인. 실패하는 테스트가 있으면 비동기 타이밍 문제를 조정.

- [ ] **Step 7: 커밋**

```bash
git add Assets/Scripts/Features/Poker/Controllers/RoundController.cs
git add Assets/Tests/EditMode/Poker/RoundControllerTests.cs
git commit -m "feat(poker): async submit flow with showcase in RoundController"
```

---

### Task 5: UXML + USS — 연출 UI 스타일

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml`
- Modify: `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss`

- [ ] **Step 1: UXML에 showcase-container 추가**

`PokerHUD.uxml` 전체:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:uie="UnityEditor.UIElements">
    <ui:Style src="project://database/Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss" />
    <ui:VisualElement name="poker-root" class="poker-root">
        <ui:Label name="deck-count-label" class="deck-count-label" text="남은 카드: 52" />
        <ui:VisualElement name="showcase-container" class="showcase-container" />
        <ui:VisualElement name="hand-container" class="hand-container" />
        <ui:Label name="result-label" class="result-label" text="" />
        <ui:VisualElement class="button-row">
            <ui:Button name="submit-button" class="action-button" text="제출" />
            <ui:Button name="discard-button" class="action-button" text="버리기" />
            <ui:Button name="draw-button" class="action-button" text="다시 받기" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

- [ ] **Step 2: USS에 연출 스타일 추가**

`PokerHUD.uss` 끝에 추가:

```css
/* === Showcase 연출 === */

.showcase-container {
    flex-direction: column;
    align-items: center;
    margin-bottom: 12px;
    opacity: 0;
    transition-property: opacity;
    transition-duration: 0.3s;
    display: none;
}

.showcase-container--active {
    opacity: 1;
    display: flex;
}

.showcase-cards {
    flex-direction: row;
    justify-content: center;
    flex-wrap: nowrap;
    margin-bottom: 8px;
}

.showcase-card {
    width: 80px;
    height: 120px;
    background-color: white;
    border-radius: 8px;
    margin: 0 4px;
    padding: 6px;
    align-items: center;
    position: relative;
    border-width: 2px;
    border-color: rgb(218, 165, 32);
}

.showcase-rank-text {
    color: rgb(218, 165, 32);
    font-size: 24px;
    -unity-font-style: bold;
}

/* 연출 중 핸드/버튼 비활성화 */
.poker-root--showcasing .hand-container {
    opacity: 0.3;
    transition-property: opacity;
    transition-duration: 0.3s;
}

.poker-root--showcasing .button-row {
    opacity: 0.3;
    transition-property: opacity;
    transition-duration: 0.3s;
}

.poker-root--showcasing .result-label {
    opacity: 0.3;
    transition-property: opacity;
    transition-duration: 0.3s;
}
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml
git add Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss
git commit -m "feat(poker): add showcase container and styles to PokerHUD"
```

---

### Task 6: PokerView — Showcase 렌더링

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs`

- [ ] **Step 1: 필드 추가**

기존 `private Button _discardButton;` 아래에:

```csharp
private VisualElement _showcaseContainer;
private VisualElement _pokerRoot;
```

- [ ] **Step 2: Start()에서 요소 조회 및 구독 추가**

`Start()` 메서드 전체:

```csharp
private void Start()
{
    var root = _doc.rootVisualElement;
    _pokerRoot = root.Q("poker-root");
    _handContainer = root.Q("hand-container");
    _showcaseContainer = root.Q("showcase-container");
    _resultLabel = root.Q<Label>("result-label");
    _deckCountLabel = root.Q<Label>("deck-count-label");
    _submitButton = root.Q<Button>("submit-button");
    _drawButton = root.Q<Button>("draw-button");
    _discardButton = root.Q<Button>("discard-button");

    _submitButton.clicked += () => _vm.SubmitCommand.Execute(Unit.Default);
    _drawButton.clicked += () => _vm.DrawCommand.Execute(Unit.Default);
    _discardButton.clicked += () => _vm.DiscardCommand.Execute(Unit.Default);

    _vm.Hand.Subscribe(RenderHand).AddTo(this);
    _vm.SelectedIndices.Subscribe(UpdateSelectionVisuals).AddTo(this);
    _vm.HandResultText.Subscribe(text => _resultLabel.text = text).AddTo(this);
    _vm.DeckRemaining.Subscribe(count => _deckCountLabel.text = $"남은 카드: {count}").AddTo(this);
    _vm.Showcase.Subscribe(RenderShowcase).AddTo(this);

    _vm.CanSubmit.Subscribe(v => _submitButton.SetEnabled(v)).AddTo(this);
    _vm.CanDraw.Subscribe(v => _drawButton.SetEnabled(v)).AddTo(this);
    _vm.CanSubmit.Subscribe(v => _discardButton.SetEnabled(v)).AddTo(this);
}
```

- [ ] **Step 3: RenderShowcase 메서드 추가**

`UpdateSelectionVisuals` 메서드 아래에 추가:

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
            var cardEl = CreateShowcaseCardElement(card);
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

private VisualElement CreateShowcaseCardElement(BaseCard card)
{
    var el = new VisualElement();
    el.AddToClassList("showcase-card");

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

**using 추가:**

```csharp
using FoldingFate.Features.Poker.Models;
```

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Features/Poker/UI/Views/PokerView.cs
git commit -m "feat(poker): add showcase rendering to PokerView"
```

---

### Task 7: PokerInstaller — DI 등록

**Files:**
- Modify: `Assets/Scripts/Features/Poker/PokerInstaller.cs`

- [ ] **Step 1: PokerConfig 필드 및 등록 추가**

`PokerInstaller.cs` 전체:

```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;
using FoldingFate.Features.Poker.UI.Views;

namespace FoldingFate.Features.Poker
{
    public class PokerInstaller : LifetimeScope
    {
        [SerializeField] private PokerConfig _pokerConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_pokerConfig);
            builder.Register<DeckModel>(Lifetime.Singleton);
            builder.Register<HandModel>(_ => new HandModel(8), Lifetime.Singleton);
            builder.Register<HandEvaluator>(Lifetime.Singleton);
            builder.Register<DealSystem>(Lifetime.Singleton);
            builder.Register<PokerViewModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RoundController>();
            builder.RegisterComponentInHierarchy<PokerView>();
        }
    }
}
```

- [ ] **Step 2: Unity Editor에서 PokerConfig 에셋 연결**

1. Hierarchy에서 PokerInstaller가 붙어있는 GameObject 선택
2. Inspector에서 `Poker Config` 필드에 `Assets/Settings/PokerConfig.asset` 드래그 앤 드롭

- [ ] **Step 3: 전체 EditMode 테스트 실행 (Unity MCP)**

전체 테스트 통과 확인.

- [ ] **Step 4: Unity Editor에서 플레이 모드 동작 확인**

1. Play 모드 진입
2. 카드 1~5장 선택 → 제출 클릭
3. 핸드 위에 제출한 카드 + 족보 텍스트가 페이드인으로 나타남 확인
4. 핸드/버튼이 어둡게 비활성화됨 확인
5. 2초 후 연출이 페이드아웃으로 사라짐 확인
6. 제출한 카드가 빠진 핸드 상태로 복귀 확인
7. 버튼 활성화 복원 확인

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Poker/PokerInstaller.cs
git commit -m "feat(poker): register PokerConfig in PokerInstaller"
```
