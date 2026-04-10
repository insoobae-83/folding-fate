# Auto Draw + Dealing Animation Design

**Date:** 2026-04-11

## Problem

현재 제출/버리기 후 수동으로 "다시 받기" 버튼을 눌러야 핸드를 채울 수 있다. 자동으로 채워지도록 하고, 덱에서 핸드로 카드가 날아오는 딜링 연출을 추가한다.

## Requirements

1. 제출/버리기 후 자동으로 `DrawToFull` 호출
2. "다시 받기" 버튼 및 관련 코드(`DrawCommand`, `CanDraw`) 제거
3. 덱을 카드 뒷면 스택으로 화면 **우측 하단**에 시각적으로 표시
4. 딜링 연출: 덱 위치에서 카드가 한 장씩 핸드 목표 위치로 이동
5. 딜링 중 상호작용(선택/제출/버리기) 비활성화
6. 딜링 속도는 `PokerConfig`에서 설정 가능

## Design

### 1. RoundController 흐름 변경

**제출 흐름:**
```
Submit → EvaluateSelected → BeginShowcase → (wait ShowcaseDuration) → EndShowcase → DiscardSelected → PushHandResult → DrawToFull (with animation)
```

**버리기 흐름:**
```
Discard → DiscardSelected → DrawToFull (with animation)
```

**게임 시작:**
```
Start → InitializeDeck → DrawToFull (with animation)
```

`DrawToFull`은 이제 딜링 연출을 포함하는 비동기 메서드. RoundController에서 연출을 조율한다.

### 2. "다시 받기" 버튼 제거

**제거 대상:**
- UXML: `draw-button` 요소
- PokerView: `_drawButton` 필드, 클릭 핸들러, `CanDraw` 구독
- PokerViewModel: `DrawCommand`, `CanDraw` 프로퍼티
- RoundController: `DrawCommand` 구독

### 3. 덱 비주얼 — 우측 하단 카드 뒷면 스택

**UXML 구조:**
```xml
<ui:VisualElement name="deck-area" class="deck-area">
    <ui:VisualElement name="deck-stack" class="deck-stack" />
    <ui:Label name="deck-count-label" class="deck-count-label" />
</ui:VisualElement>
```

**USS:**
```css
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
```

기존 `deck-count-label`은 `deck-area` 안으로 이동하여 덱 스택 아래에 배치.

### 4. 딜링 연출

**메커니즘:** PokerView에서 딜링 연출을 처리한다.

1. ViewModel이 `DealingState`를 발행 (딜링 시작/종료)
2. View가 `DealingState` 구독
3. 딜링 시작 시: 필요한 카드 수만큼 순차적으로 연출
   - 덱 스택 위치에 카드 생성 (절대 위치)
   - 핸드 컨테이너의 목표 슬롯 위치로 이동 애니메이션
   - 카드 간 `DealIntervalSeconds` 딜레이
   - 한 장의 이동: `DealAnimationDurationSeconds` 동안 translate 변환
4. 모든 카드 딜링 완료 후 딜링 종료

**애니메이션 방식:** UI Toolkit의 USS transition을 활용. 카드를 덱 위치에 absolute로 배치한 뒤, 목표 위치의 translate 값으로 전환. 전환 완료 후 카드를 핸드 컨테이너의 정상적인 flow에 삽입.

### 5. PokerConfig 추가 필드

```csharp
[field: SerializeField] public float DealIntervalSeconds { get; set; } = 0.1f;
[field: SerializeField] public float DealAnimationDurationSeconds { get; set; } = 0.15f;
```

### 6. 딜링 중 상호작용 제어

PokerViewModel에 `IsDealing` 상태 추가:

```csharp
private readonly ReactiveProperty<bool> _isDealing;
public ReadOnlyReactiveProperty<bool> IsDealing { get; }
```

기존 커맨드의 `canExecute` 조건에 `notDealing`을 추가:
- `ToggleSelectCommand`: `notShowcasing && notDealing`
- `SubmitCommand`: `hasSelection && notShowcasing && notDealing`
- `DiscardCommand`: 동일

### 7. DealSystem 변경

`DrawToFull`은 그대로 모델 레이어에서 카드를 즉시 추가하는 동기 메서드로 유지. 딜링 연출은 RoundController + View 레이어에서 처리. 모델은 즉시 상태 반영, View가 연출을 담당.

**대안 고려:** DealSystem에 한 장씩 드로우하는 `DrawOne()` 메서드 추가. RoundController가 반복 호출하면서 카드 간 딜레이를 조율. 이렇게 하면 View는 Hand 구독으로 자연스럽게 카드 추가를 감지하여 딜링 연출 가능.

**채택 방식:** `DrawOne()` 메서드 추가 방식. 한 장씩 모델에 추가 → View가 Hand 변경 감지 → 새 카드에 딜링 애니메이션 적용.

RoundController 딜링 루프:
```
BeginDealing()
for (needed count):
    DrawOne()
    await UniTask.Delay(DealIntervalSeconds)
EndDealing()
```

View에서 Hand 구독 시, 딜링 중이면 새로 추가된 카드에 애니메이션 적용.

## Files to Modify

| File | Change |
|---|---|
| `Assets/Scripts/Features/Poker/Data/PokerConfig.cs` | `DealIntervalSeconds`, `DealAnimationDurationSeconds` 추가 |
| `Assets/Scripts/Features/Poker/Systems/DealSystem.cs` | `DrawOne()` 메서드 추가 |
| `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` | `DrawCommand`/`CanDraw` 제거, `IsDealing` 추가, `BeginDealing`/`EndDealing` 추가 |
| `Assets/Scripts/Features/Poker/Controllers/RoundController.cs` | 자동 드로우 + 딜링 루프, `DrawCommand` 구독 제거 |
| `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` | 덱 비주얼, 딜링 애니메이션, draw 버튼 제거 |
| `Assets/Scripts/Features/Poker/UI/Uxml/PokerHUD.uxml` | `draw-button` 제거, `deck-area` 추가 |
| `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss` | `.deck-area`, `.deck-stack`, 딜링 애니메이션 스타일 추가 |
| `Assets/Tests/EditMode/Poker/RoundControllerTests.cs` | Draw 관련 테스트 업데이트, 딜링 상태 테스트 추가 |
