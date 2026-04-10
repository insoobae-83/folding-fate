# Card UI Selection Fix + Hand Contributing Cards Highlight

**Date:** 2026-04-10

## Problem

1. **카드 선택 시 크기 축소**: `.card` 기본 스타일에 `border-width`가 없어서(0) `.card--selected`에서 `border-width: 3px`를 추가하면 UI Toolkit border-box 모델로 인해 내부 콘텐츠 영역이 줄어듦
2. **족보 연출에서 전체 카드 하이라이트**: 쇼케이스에서 `BestHand` 5장 모두에 골든 테두리 적용. 족보에 기여하는 카드만 하이라이트해야 함

## Design

### 1. CSS Fix — 카드 선택 시 크기 유지

`PokerHUD.uss`의 `.card` 기본 스타일에 투명 border 추가:

```css
.card {
    /* 기존 스타일 유지 */
    border-width: 3px;
    border-color: transparent;
}
```

`.card--selected`에서는 `border-color`만 변경:

```css
.card--selected {
    translate: 0 -12px;
    border-color: rgb(60, 130, 255);
}
```

### 2. HandResult — ContributingCards 추가

`HandResult`에 `IReadOnlyList<BaseCard> ContributingCards` 프로퍼티 추가. 족보에 기여하는 카드만 담는다.

```csharp
public class HandResult : IComparable<HandResult>
{
    public HandRank Rank { get; }
    public IReadOnlyList<BaseCard> BestHand { get; }
    public IReadOnlyList<BaseCard> ContributingCards { get; }
    private readonly IReadOnlyList<int> _tiebreakValues;

    public HandResult(HandRank rank, List<BaseCard> bestHand,
                      List<int> tiebreakValues, List<BaseCard> contributingCards)
    { ... }
}
```

### 3. HandEvaluator — 각 TryXxx에서 기여 카드 분리

족보별 기여 카드 기준:

| 족보 | 기여 카드 | 비기여 (키커) |
|---|---|---|
| HighCard | 최고 rank 카드 1장 | 나머지 |
| OnePair | 페어 2장 | 나머지 3장 |
| TwoPair | 두 페어 4장 | 나머지 1장 |
| ThreeOfAKind | 트리플 3장 | 나머지 2장 |
| Straight | 전체 5장 | 없음 |
| Flush | 전체 5장 | 없음 |
| FullHouse | 전체 5장 | 없음 |
| FourOfAKind | 포카드 4장 | 나머지 1장 |
| StraightFlush | 전체 5장 | 없음 |
| RoyalFlush | 전체 5장 | 없음 |

각 `TryXxx` 메서드에서 rank count 기반으로 해당 rank를 가진 카드를 `cards`에서 필터링하여 `contributingCards`로 전달.

### 4. ShowcaseState — 하이라이트 정보 전달

`ShowcaseState`에 `IReadOnlyCollection<BaseCard> HighlightedCards` 추가:

```csharp
public class ShowcaseState
{
    public bool IsActive { get; }
    public IReadOnlyList<BaseCard> Cards { get; }
    public IReadOnlyCollection<BaseCard> HighlightedCards { get; }
    public string RankText { get; }
}
```

`PokerViewModel.BeginShowcase`에서 `HandResult.ContributingCards`를 `HashSet<BaseCard>`으로 변환하여 전달.

### 5. View — 조건부 스타일 적용

`PokerView.RenderShowcase`에서 각 카드가 `HighlightedCards`에 포함되는지 확인하여 스타일 분기:

- 기여 카드: `.showcase-card` (골든 테두리, 기존)
- 비기여 카드: `.showcase-card--dimmed` (테두리 없음 또는 회색)

USS 추가:

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

### 6. Testing

- `HandEvaluatorTests`: 각 족보별 `ContributingCards` 검증 케이스 추가
- 기존 테스트: `HandResult` 생성자 변경에 맞춰 업데이트

## Files to Modify

| File | Change |
|---|---|
| `Assets/Scripts/Features/Poker/UI/Uss/PokerHUD.uss` | `.card` 투명 border 추가, `.card--selected` border-width 제거, `.showcase-card--dimmed` 추가 |
| `Assets/Scripts/Features/Card/Models/HandResult.cs` | `ContributingCards` 프로퍼티 + 생성자 파라미터 추가 |
| `Assets/Scripts/Features/Card/Systems/HandEvaluator.cs` | 각 TryXxx에서 기여 카드 분리 |
| `Assets/Scripts/Features/Poker/Models/ShowcaseState.cs` | `HighlightedCards` 추가 |
| `Assets/Scripts/Features/Poker/UI/ViewModels/PokerViewModel.cs` | `BeginShowcase`에서 ContributingCards 전달 |
| `Assets/Scripts/Features/Poker/UI/Views/PokerView.cs` | 조건부 스타일 적용 |
| `Assets/Tests/EditMode/Card/HandEvaluatorTests.cs` | ContributingCards 검증 추가 |
