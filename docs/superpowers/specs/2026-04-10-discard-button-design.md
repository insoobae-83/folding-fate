# 버리기 버튼 설계

**날짜:** 2026-04-10
**기능:** 버리기 — 선택한 카드를 족보 평가 없이 제거

## 요약

선택한 카드를 족보 평가 없이 핸드에서 제거하는 "버리기" 버튼을 추가한다. 제거 후 플레이어가 기존 "다시 받기" 버튼으로 수동으로 채운다.

## 범위

최소 변경: 기존 `DealSystem.DiscardSelected()`를 새 커맨드 + 버튼으로 노출. 새 로직 없음.

## 변경 내용

### 1. `PokerViewModel` — `DiscardCommand` 추가

`SubmitCommand`와 동일한 canExecute 조건(1~5장 선택 시)으로 `ReactiveCommand DiscardCommand` 추가. 별도 `CanDiscard` 프로퍼티 불필요 — `canSubmit` 옵저버블 재사용.

```csharp
DiscardCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);
```

### 2. `RoundController` — `DiscardCommand` 구독 추가

`Start()`에 구독 추가:

```csharp
_vm.DiscardCommand
    .Subscribe(_ => _dealSystem.DiscardSelected())
    .AddTo(_disposables);
```

핸드 결과 푸시 없음. 평가 없음.

### 3. `PokerHUD.uxml` — `discard-button` 추가

`button-row`에 버튼 추가:

```xml
<ui:Button name="discard-button" class="action-button" text="버리기" />
```

### 4. `PokerView` — 버튼 바인딩

```csharp
_discardButton = root.Q<Button>("discard-button");
_discardButton.clicked += () => _vm.DiscardCommand.Execute(Unit.Default);
_vm.CanSubmit.Subscribe(v => _discardButton.SetEnabled(v)).AddTo(this);
```

## 변경 후 게임 흐름

1. 핸드 딜 (8장)
2. 플레이어가 1~5장 선택
3. **제출** → 족보 평가 + 선택 카드 제거 → 결과 표시 → 수동으로 다시 받기
4. **버리기** → 선택 카드 제거 (점수 없음) → 수동으로 다시 받기
5. **다시 받기** → 핸드를 최대 장수로 채움

## 범위 외

- 버리기 횟수 제한이나 비용 — 이번 이터레이션 제외
- 버리기 후 자동 드로우 — 제출 흐름과 일관성 유지를 위해 제외
- 버리기 횟수 추적/표시
