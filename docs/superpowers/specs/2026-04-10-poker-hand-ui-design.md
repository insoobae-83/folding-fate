# Poker Hand UI — Design Spec

**Date:** 2026-04-10  
**Status:** Approved  

---

## Overview

1인 플레이어에게 표준 52장 덱에서 카드 8장을 지급하고, 플레이어가 1~5장을 선택해 제출하면 족보를 계산하여 표시한다. 이후 "다시 받기" 버튼으로 핸드를 최대 수량까지 보충한다. 덱은 라운드 간 유지되며 소진 시 자동 리셋·재셔플된다.

---

## Scope

- **포함**: 딜링, 카드 선택(1~5장), 족보 계산 표시, 핸드 보충, 덱 소진 리셋
- **제외**: 멀티플레이어, 베팅/칩, 실제 카드 아트 리소스 (placeholder로 대체)

---

## Feature Structure

```
Assets/Scripts/Features/Poker/
  Models/
    DeckModel.cs
    HandModel.cs
  Systems/
    DealSystem.cs          // 순수 비즈니스 로직
  Controllers/
    RoundController.cs     // 흐름 제어 (IStartable)
  UI/
    ViewModels/
      PokerViewModel.cs
    Views/
      PokerView.cs
    Uxml/
      PokerHUD.uxml
      CardElement.uxml
    Uss/
      PokerHUD.uss
  PokerInstaller.cs

Assets/Tests/EditMode/Poker/
  DeckModelTests.cs
  HandModelTests.cs
  DealSystemTests.cs
  RoundControllerTests.cs
```

---

## Architecture

### 데이터 흐름

```
[입력]
PokerView ──Execute──→ PokerViewModel(Command)
                              │ (Command는 IObservable — RC가 구독)
                       RoundController ──call──→ DealSystem (순수 메서드)
                              │                      │
                              │               DeckModel / HandModel
                              │               (ReactiveProperty 갱신)
                              │ push result           │
[출력]                 PokerViewModel ←─Subscribe────┘
PokerView ←──Subscribe── PokerViewModel
```

- `DealSystem`은 순수 메서드만 제공. Observable 없음, 흐름 제어 없음.
- `RoundController`가 ViewModel 커맨드를 구독하고 DealSystem을 호출하는 유일한 조율자.
- `DealSystem`은 ViewModel과 RoundController를 모른다.

### 레이어 관계

| 레이어 | 알고 있는 것 | 모르는 것 |
|---|---|---|
| PokerView | PokerViewModel | 나머지 전부 |
| PokerViewModel | DeckModel/HandModel (RP 구독) | RoundController, DealSystem |
| RoundController | DealSystem, PokerViewModel (커맨드 구독 + 결과 push) | View |
| DealSystem | DeckModel, HandModel, HandEvaluator | ViewModel, Controller, View |
| DeckModel / HandModel | 자기 자신 | 모든 외부 레이어 |

---

## Models

### DeckModel (순수 C#)

표준 52장 덱의 상태와 순수 조작을 소유한다. Shuffle/Draw는 외부 의존성 없이 자신의 데이터만 다루므로 모델 내부에 위치한다.

```
List<BaseCard> _cards                      // 남은 카드 (private)
ReactiveProperty<int> RemainingCount       // ViewModel이 구독
void Initialize(CardFactory)              // 52장 생성 및 셔플
void Shuffle()                            // Fisher-Yates
IReadOnlyList<BaseCard> Draw(int count)   // 뽑기. 부족 시 자동 리셋 후 재셔플
```

**자동 리셋 정책**: `Draw()` 호출 시 남은 카드가 요청 수량보다 적으면 덱을 즉시 리셋·재셔플 후 드로우한다.

**Joker 확장**: 나중에 Joker를 추가할 경우 `Initialize()` 내부에서 `CardCategory.Joker` 카드를 덱에 포함하기만 하면 된다. `HandEvaluator`는 이미 Joker를 지원하므로 족보 계산 로직 수정 불필요.

### HandModel (순수 C#, Anemic)

현재 핸드 상태 데이터만 소유한다. 선택 제한(1~5) 강제는 DealSystem이 담당한다.

```
List<BaseCard> _cards                          // 현재 핸드 카드 (private)
HashSet<int> _selectedIndices                  // 선택된 인덱스 (private)
int MaxHandSize                                // 기본값 8
ReactiveProperty<IReadOnlyList<BaseCard>> Cards
ReactiveProperty<IReadOnlyList<int>> SelectedIndices
int SelectedCount
bool IsFull                                    // Cards.Count >= MaxHandSize
void AddCards(IReadOnlyList<BaseCard>)
void ToggleSelect(int index)
void Clear()
```

---

## Systems

### DealSystem (순수 C#)

순수 도메인 연산만 담당한다. 흐름 제어 없음. Observable 없음. 호출되면 실행할 뿐.

**의존성 주입**: `DeckModel`, `HandModel`, `HandEvaluator`, `CardFactory`

**책임 (순수 메서드)**:
```csharp
void InitializeDeck()                  // 52장 생성 및 셔플
void Deal(int count)                   // DeckModel에서 count장 드로우 → HandModel에 추가
void ToggleSelect(int index)           // 선택 1~5장 제한 강제 후 HandModel에 위임
HandResult EvaluateSelected()          // 선택 카드로 HandEvaluator.Evaluate() → 결과 반환
void DrawToFull()                      // HandModel.MaxHandSize까지 보충
```

`RoundController`가 언제 이 메서드들을 호출할지 결정한다. DealSystem은 순서를 모른다.

---

## Controllers

### RoundController (순수 C#, VContainer IStartable)

라운드 흐름을 조율하는 유일한 지휘자. ViewModel 커맨드를 구독하고 DealSystem을 호출하며, 처리 결과를 ViewModel에 전달한다.

**의존성 주입**: `DealSystem`, `PokerViewModel`

**책임**:
- `IStartable.Start()`: `DealSystem.InitializeDeck()` → `DealSystem.Deal(MaxHandSize)` (초기 딜)
- `vm.ToggleSelectCommand` 구독 → `DealSystem.ToggleSelect(index)`
- `vm.SubmitCommand` 구독 → `DealSystem.EvaluateSelected()` → 결과를 `vm.PushHandResult(result)`로 전달
- `vm.DrawCommand` 구독 → `DealSystem.DrawToFull()`

**족보 텍스트 매핑**: `HandRank` → 표시 문자열 변환은 PokerViewModel의 `PushHandResult()` 내부에서 담당.

**구독 해제**: `CompositeDisposable`, VContainer `IDisposable` 관리.

---

## ViewModel

### PokerViewModel (순수 C#, IDisposable)

View에 노출하는 상태와 커맨드를 소유한다.

Model의 ReactiveProperty를 구독하여 상태를 조립한다. RoundController와 DealSystem을 직접 알지 못한다.

**구독 대상 (생성자에서 바인딩)**:
- `HandModel.Cards` → `Hand` 파생
- `HandModel.SelectedIndices` → `SelectedIndices` 파생
- `DeckModel.RemainingCount` → `DeckRemaining` 파생

**노출 상태 (ReadOnlyReactiveProperty)**:
```csharp
ReadOnlyReactiveProperty<IReadOnlyList<BaseCard>> Hand
ReadOnlyReactiveProperty<IReadOnlyList<int>> SelectedIndices
ReadOnlyReactiveProperty<string> HandResultText   // 초기값 ""
ReadOnlyReactiveProperty<int> DeckRemaining
```

**커맨드 (ReactiveCommand)** — View가 Execute, RoundController가 구독:
```csharp
ReactiveCommand<int> ToggleSelectCommand  // CanExecute: 항상 true
ReactiveCommand SubmitCommand             // CanExecute: 선택 1~5장 (HandModel.SelectedIndices 파생)
ReactiveCommand DrawCommand               // CanExecute: !HandModel.IsFull (HandModel.Cards 파생)
```

**RoundController 결과 수신**:
```csharp
void PushHandResult(HandResult result)    // RC가 호출 → HandResultText 갱신 (HandRank → 표시 문자열 변환 내부 처리)
```

**구독 해제**: `CompositeDisposable` 패턴, VContainer가 `IDisposable`로 Dispose 관리.

---

## View

### PokerView (MonoBehaviour, UIDocument 컨트롤러)

ViewModel만 알고 있다. DealSystem, Models를 직접 참조하지 않는다.

**바인딩**:
```csharp
void Bind(PokerViewModel vm) {
    // 커맨드 연결
    cardElement.clicked += () => vm.ToggleSelectCommand.Execute(index);
    submitButton.clicked += () => vm.SubmitCommand.Execute();
    drawButton.clicked += () => vm.DrawCommand.Execute();

    // 상태 구독
    vm.Hand.Subscribe(RenderHand).AddTo(destroyCancellationToken);
    vm.SelectedIndices.Subscribe(UpdateSelection).AddTo(destroyCancellationToken);
    vm.HandResultText.Subscribe(UpdateResult).AddTo(destroyCancellationToken);
    vm.DeckRemaining.Subscribe(UpdateDeckCount).AddTo(destroyCancellationToken);

    // 버튼 활성화는 커맨드 CanExecute가 자동 처리
    vm.SubmitCommand.CanExecute.Subscribe(v => submitButton.SetEnabled(v)).AddTo(destroyCancellationToken);
    vm.DrawCommand.CanExecute.Subscribe(v => drawButton.SetEnabled(v)).AddTo(destroyCancellationToken);
}
```

### UXML 구조

```
PokerHUD.uxml
├── #deck-count-label          // "남은 카드: 44"
├── #hand-container            // 가로 Flex, 카드 8장
│   └── CardElement.uxml ×N
│       ├── #card-top-rank     // "A", "K", "10" 등
│       ├── #card-suit         // ♠ ♥ ♦ ♣
│       ├── #card-center-suit  // 중앙 큰 수트 기호
│       └── .selected-overlay  // 선택 시 강조 (USS 토글)
├── #result-label              // "로열 플러시" 등
├── #submit-button             // "제출"
└── #draw-button               // "다시 받기"
```

### USS Placeholder 스타일

- 카드 배경: 흰색 사각형, 모서리 라운드
- 하트/다이아: 빨강, 스페이드/클럽: 검정
- 선택 시: 파란 테두리 + `translate-y: -8px` (위로 8px)
- 버튼 비활성화: opacity 0.4

---

## DI — PokerInstaller (VContainer LifetimeScope)

```csharp
container.Register<DeckModel>(Lifetime.Singleton);
container.Register<HandModel>(Lifetime.Singleton);
container.Register<HandEvaluator>(Lifetime.Singleton);
container.Register<CardFactory>(Lifetime.Singleton);
container.Register<DealSystem>(Lifetime.Singleton);         // 순수 비즈니스 로직, IStartable 없음
container.Register<PokerViewModel>(Lifetime.Singleton);     // DeckModel, HandModel 주입
container.RegisterEntryPoint<RoundController>();            // IStartable, DealSystem + PokerViewModel 주입
container.RegisterComponentInHierarchy<PokerView>();        // PokerViewModel 주입
```

---

## Testing

| 테스트 파일 | 주요 케이스 |
|---|---|
| `DeckModelTests` | 초기화 52장, 셔플 후 순서 변경, 드로우 감소, 소진 시 자동 리셋 |
| `HandModelTests` | 카드 추가, 선택/해제 토글, IsFull 조건 |
| `DealSystemTests` | 선택 5장 초과 거부, EvaluateSelected 족보 반환, DrawToFull 보충량 |
| `RoundControllerTests` | Start 후 핸드 8장 확인, Submit → HandResult 반환 및 VM 갱신, Draw → 핸드 보충 |

---

## Assumptions & Constraints

- Joker 카드는 덱에 포함하지 않음 (표준 52장)
- `HandEvaluator`는 기존 구현 그대로 사용, 수정 없음
- 카드 아트 리소스 교체 시 `CardElement.uxml`/`PokerHUD.uss`만 수정하면 됨
- `MaxHandSize`는 `HandModel` 생성 시 주입 가능하도록 설계 (기본값 8)
