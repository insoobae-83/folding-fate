# 버리기 버튼 구현 플랜

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 선택한 카드를 족보 평가 없이 핸드에서 제거하는 "버리기" 버튼을 추가한다.

**Architecture:** `PokerViewModel`에 `DiscardCommand`를 추가하고, `RoundController`에서 구독하여 `DealSystem.DiscardSelected()`를 호출한다. `DealSystem.DiscardSelected()`는 이미 구현되어 있으므로 새 로직 없음.

**Tech Stack:** C#, R3 (ReactiveCommand), Unity UI Toolkit (UXML), VContainer

---

## 변경 파일

| 구분 | 파일 |
|---|---|
| 수정 | `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` |
| 수정 | `Assets/Scripts/Features/Poker/Controllers/RoundController.cs` |
| 수정 | `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml` |
| 수정 | `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` |
| 수정 | `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` |

---

### Task 1: RoundController — DiscardCommand 테스트 추가

**Files:**
- Modify: `Assets/Tests/EditMode/Poker/RoundControllerTests.cs`

- [ ] **Step 1: 실패하는 테스트 작성**

`RoundControllerTests.cs`의 기존 테스트 아래에 추가:

```csharp
[Test]
public void DiscardCommand_RemovesSelectedCardsWithoutSettingResult()
{
    _controller.Start();
    // 카드 2장 선택
    _vm.ToggleSelectCommand.Execute(0);
    _vm.ToggleSelectCommand.Execute(1);
    Assert.AreEqual(2, _hand.SelectedCount);

    _vm.DiscardCommand.Execute(Unit.Default);

    // 선택한 2장이 제거됨
    Assert.AreEqual(6, _hand.Cards.Value.Count);
    // 선택 상태 해제
    Assert.AreEqual(0, _hand.SelectedCount);
    // 족보 결과 텍스트는 변경되지 않음
    Assert.IsTrue(string.IsNullOrEmpty(_vm.HandResultText.CurrentValue),
        "DiscardCommand는 족보 결과를 설정하면 안 됨");
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

Unity Editor에서 **Window > General > Test Runner** 열고 `DiscardCommand_RemovesSelectedCardsWithoutSettingResult` 실행.
예상: FAIL (`DiscardCommand` 멤버가 없어서 컴파일 에러)

---

### Task 2: PokerViewModel — DiscardCommand 추가

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs`

- [ ] **Step 1: DiscardCommand 프로퍼티 선언 추가**

기존 `DrawCommand` 선언 아래에 추가:

```csharp
public ReactiveCommand DiscardCommand { get; }
```

- [ ] **Step 2: 생성자에서 DiscardCommand 초기화**

생성자 내 `DrawCommand = ...` 줄 아래에 추가:

```csharp
DiscardCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);
```

`canSubmit`은 이미 위에서 정의된 로컬 변수(`var canSubmit = hand.SelectedIndices.Select(...)`)이므로 재사용.

완성된 생성자 관련 부분:

```csharp
var canSubmit = hand.SelectedIndices.Select(indices => indices.Count >= 1 && indices.Count <= 5);
CanSubmit = canSubmit.ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);
SubmitCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);

var canDraw = hand.Cards.Select(cards => cards.Count < hand.MaxHandSize);
CanDraw = canDraw.ToReadOnlyReactiveProperty(initialValue: true).AddTo(_disposables);
DrawCommand = new ReactiveCommand(canDraw, initialCanExecute: true).AddTo(_disposables);

DiscardCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);
```

- [ ] **Step 3: 테스트 재실행**

Test Runner에서 `DiscardCommand_RemovesSelectedCardsWithoutSettingResult` 실행.
예상: FAIL (커맨드는 존재하지만 구독이 없으므로 핸드 카드 수가 바뀌지 않음)

---

### Task 3: RoundController — DiscardCommand 구독 추가

**Files:**
- Modify: `Assets/Scripts/Features/Poker/Controllers/RoundController.cs`

- [ ] **Step 1: Start()에 DiscardCommand 구독 추가**

기존 `_vm.DrawCommand.Subscribe(...)` 블록 아래에 추가:

```csharp
_vm.DiscardCommand
    .Subscribe(_ => _dealSystem.DiscardSelected())
    .AddTo(_disposables);
```

완성된 `Start()`:

```csharp
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
            _dealSystem.DiscardSelected();
            _vm.PushHandResult(result);
        })
        .AddTo(_disposables);

    _vm.DrawCommand
        .Subscribe(_ => _dealSystem.DrawToFull())
        .AddTo(_disposables);

    _vm.DiscardCommand
        .Subscribe(_ => _dealSystem.DiscardSelected())
        .AddTo(_disposables);
}
```

- [ ] **Step 2: 테스트 재실행**

Test Runner에서 `DiscardCommand_RemovesSelectedCardsWithoutSettingResult` 실행.
예상: PASS

- [ ] **Step 3: 전체 Poker 테스트 실행**

Test Runner에서 `EditMode > Poker` 폴더 전체 실행.
예상: 전부 PASS (기존 테스트 깨지지 않음)

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs
git add Assets/Scripts/Features/Poker/Controllers/RoundController.cs
git add Assets/Tests/EditMode/Poker/RoundControllerTests.cs
git commit -m "feat(poker): add DiscardCommand to PokerViewModel and RoundController"
```

---

### Task 4: UI — 버리기 버튼 추가

**Files:**
- Modify: `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml`
- Modify: `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs`

- [ ] **Step 1: UXML에 버튼 추가**

`PokerHUD.uxml`의 `button-row` 안, `submit-button`과 `draw-button` 사이에 추가:

```xml
<ui:VisualElement class="button-row">
    <ui:Button name="submit-button" class="action-button" text="제출" />
    <ui:Button name="discard-button" class="action-button" text="버리기" />
    <ui:Button name="draw-button" class="action-button" text="다시 받기" />
</ui:VisualElement>
```

- [ ] **Step 2: PokerView에 필드 추가**

`PokerView.cs`의 기존 `private Button _drawButton;` 아래에 추가:

```csharp
private Button _discardButton;
```

- [ ] **Step 3: Start()에서 버튼 조회 및 바인딩**

`_drawButton = root.Q<Button>("draw-button");` 줄 아래에 추가:

```csharp
_discardButton = root.Q<Button>("discard-button");
```

`_drawButton.clicked += ...` 줄 아래에 추가:

```csharp
_discardButton.clicked += () => _vm.DiscardCommand.Execute(Unit.Default);
```

`_vm.CanDraw.Subscribe(v => _drawButton.SetEnabled(v)).AddTo(this);` 줄 아래에 추가:

```csharp
_vm.CanSubmit.Subscribe(v => _discardButton.SetEnabled(v)).AddTo(this);
```

완성된 `Start()` 전체:

```csharp
private void Start()
{
    var root = _doc.rootVisualElement;
    _handContainer = root.Q("hand-container");
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

    _vm.CanSubmit.Subscribe(v => _submitButton.SetEnabled(v)).AddTo(this);
    _vm.CanDraw.Subscribe(v => _drawButton.SetEnabled(v)).AddTo(this);
    _vm.CanSubmit.Subscribe(v => _discardButton.SetEnabled(v)).AddTo(this);
}
```

- [ ] **Step 4: Unity Editor에서 플레이 모드로 동작 확인**

1. Unity Editor에서 Play 모드 진입
2. 카드 1~5장 선택 → "버리기" 버튼 활성화 확인
3. "버리기" 클릭 → 선택한 카드 제거, 족보 결과 텍스트 변화 없음 확인
4. 0장 또는 6장 이상 선택 시 "버리기" 버튼 비활성화 확인

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml
git add Assets/Scripts/Features/Poker/UI/Views/PokerView.cs
git commit -m "feat(poker): add discard button to PokerHUD UI"
```
