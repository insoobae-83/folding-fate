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
    DealSystem.cs
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
```

---

## Architecture

### 데이터 흐름

```
DeckModel ──→ DealSystem ──→ HandModel
                  │
            PokerViewModel (R3 ReactiveProperty + ReactiveCommand)
                  │
             PokerView (UIDocument)
```

### 레이어 관계

| 레이어 | 알고 있는 것 | 모르는 것 |
|---|---|---|
| PokerView | PokerViewModel | DealSystem, Models |
| PokerViewModel | DealSystem (커맨드 위임) | Models 직접 접근 없음 |
| DealSystem | DeckModel, HandModel, HandEvaluator, PokerViewModel | View |
| DeckModel / HandModel | 자기 자신 | 모든 외부 레이어 |

---

## Models

### DeckModel (순수 C#)

표준 52장 덱의 상태와 순수 조작을 소유한다. Shuffle/Draw는 외부 의존성 없이 자신의 데이터만 다루므로 모델 내부에 위치한다.

```
List<BaseCard> _cards          // 남은 카드 (private)
int RemainingCount             // 읽기 전용 프로퍼티
void Initialize(CardFactory)   // 52장 생성 및 셔플
void Shuffle()                 // Fisher-Yates
IReadOnlyList<BaseCard> Draw(int count)  // 뽑기. 부족 시 자동 리셋 후 재셔플
```

**자동 리셋 정책**: `Draw()` 호출 시 남은 카드가 요청 수량보다 적으면 덱을 즉시 리셋·재셔플 후 드로우한다.

### HandModel (순수 C#, Anemic)

현재 핸드 상태 데이터만 소유한다. 선택 제한(1~5) 강제는 DealSystem이 담당한다.

```
List<BaseCard> _cards          // 현재 핸드 카드
HashSet<int> _selectedIndices  // 선택된 인덱스
int MaxHandSize                // 기본값 8
IReadOnlyList<BaseCard> Cards
IReadOnlyList<int> SelectedIndices
int SelectedCount
bool IsFull                    // Cards.Count >= MaxHandSize
void AddCards(IReadOnlyList<BaseCard>)
void ToggleSelect(int index)
void Clear()
```

---

## Systems

### DealSystem (순수 C#, VContainer IStartable)

게임 흐름 제어 — 딜·선택·제출·보충을 조율한다.

**의존성 주입**: `DeckModel`, `HandModel`, `HandEvaluator`, `PokerViewModel`, `CardFactory`

**책임**:
- `StartAsync()`: 덱 초기화 → 핸드 첫 딜 → ViewModel 상태 초기화
- `ToggleSelect(int index)`: 선택 1~5장 제한 강제 후 HandModel에 위임
- `SubmitHand()`: 선택 카드로 `HandEvaluator.Evaluate()` → 결과 텍스트를 ViewModel에 푸시
- `DrawToFull()`: `HandModel.MaxHandSize - Hand.Count` 만큼 DeckModel에서 드로우

**족보 텍스트 매핑**: `HandRank` enum → 한글/영문 표시 문자열 (예: `HandRank.RoyalFlush` → "로열 플러시")

---

## ViewModel

### PokerViewModel (순수 C#, IDisposable)

View에 노출하는 상태와 커맨드를 소유한다.

**상태 (ReactiveProperty)**:
```csharp
ReactiveProperty<IReadOnlyList<BaseCard>> Hand
ReactiveProperty<IReadOnlyList<int>> SelectedIndices
ReactiveProperty<string> HandResultText   // 제출 후 족보 텍스트, 초기값 ""
ReactiveProperty<int> DeckRemaining
```

**커맨드 (ReactiveCommand)**:
```csharp
ReactiveCommand<int> ToggleSelectCommand  // 카드 인덱스 전달
ReactiveCommand SubmitCommand             // CanExecute: SelectedCount는 1 이상 5 이하
ReactiveCommand DrawCommand              // CanExecute: !HandModel.IsFull
```

커맨드 내부에서 DealSystem 메서드를 호출한다. View는 커맨드만 Execute하고 DealSystem을 직접 참조하지 않는다.

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
container.RegisterEntryPoint<DealSystem>();   // IStartable
container.Register<DeckModel>(Lifetime.Singleton);
container.Register<HandModel>(Lifetime.Singleton);
container.Register<HandEvaluator>(Lifetime.Singleton);
container.Register<CardFactory>(Lifetime.Singleton);
container.Register<PokerViewModel>(Lifetime.Singleton);
container.RegisterComponentInHierarchy<PokerView>();
```

---

## Testing

| 테스트 파일 | 주요 케이스 |
|---|---|
| `DeckModelTests` | 초기화 52장, 셔플 후 순서 변경, 드로우 감소, 소진 시 자동 리셋 |
| `HandModelTests` | 카드 추가, 선택/해제 토글, IsFull 조건 |
| `DealSystemTests` | 선택 5장 초과 거부, SubmitHand 족보 텍스트 검증, DrawToFull 보충량 |

---

## Assumptions & Constraints

- Joker 카드는 덱에 포함하지 않음 (표준 52장)
- `HandEvaluator`는 기존 구현 그대로 사용, 수정 없음
- 카드 아트 리소스 교체 시 `CardElement.uxml`/`PokerHUD.uss`만 수정하면 됨
- `MaxHandSize`는 `HandModel` 생성 시 주입 가능하도록 설계 (기본값 8)
