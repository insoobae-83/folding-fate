# Poker Hand Evaluator — Design Spec

**Date**: 2026-04-09  
**Feature**: `Features/Card` — Hand Evaluation

---

## Overview

n장의 카드(1~52장)를 입력받아 그 중 최선의 5장 조합으로 포커 족보를 판정하고, 족보 간 강약 비교를 지원하는 모듈.

---

## Requirements

- n장(1 ≤ n ≤ 52)을 입력받아 best 5장 조합의 족보를 반환한다
- Joker(`CardCategory.Joker`)는 와일드 카드로 처리한다 (입력에 없으면 자동 제외)
- Custom 카드(`CardCategory.Custom`)는 족보 판정에서 무시한다
- Ace는 High(14) / Low(1) 둘 다 유효하다 (A-2-3-4-5 wheel, 10-J-Q-K-A 모두 허용)
- 족보 간 강약 비교 및 같은 족보끼리 세밀한 타이브레이크 비교를 지원한다
- 유효한 Standard 카드가 5장 미만이면 있는 카드로 최선 판정한다

---

## Architecture

### 새로 추가되는 파일

| 파일 | 위치 | 역할 |
|---|---|---|
| `HandRank.cs` | `Assets/Scripts/Core/Enums/` | 족보 종류 enum |
| `HandResult.cs` | `Assets/Scripts/Features/Card/Models/` | 평가 결과 모델 |
| `HandEvaluator.cs` | `Assets/Scripts/Features/Card/Systems/` | 족보 평가 로직 |
| `HandEvaluatorTests.cs` | `Assets/Tests/EditMode/Card/` | EditMode 단위 테스트 |

### 의존성

`HandEvaluator`는 기존 레이어 규칙을 따른다.

```
HandEvaluator (Features/Card/Systems)
  └─ HandResult (Features/Card/Models)
       └─ HandRank (Core/Enums)
       └─ BaseCard (Features/Card/Models)  ← 기존
```

---

## Data Structures

### `HandRank` enum

족보 종류를 나타내며, **값의 순서 = 강도 순서**로 정의한다.

```csharp
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

### `HandResult`

평가 결과 모델. 외부에는 `Rank`와 `BestHand`만 노출하고, 타이브레이크 로직은 `IComparable<HandResult>` 내부에 캡슐화한다.

```csharp
public class HandResult : IComparable<HandResult>
{
    public HandRank Rank { get; }
    public IReadOnlyList<BaseCard> BestHand { get; }  // 최선의 5장 (5장 미만 가능)

    private readonly IReadOnlyList<int> _tiebreakValues;

    public int CompareTo(HandResult other)
    {
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
```

#### `_tiebreakValues` 구성 규칙

| 족보 | 값 순서 |
|---|---|
| HighCard | [카드5, 카드4, 카드3, 카드2, 카드1] (Rank 내림차순) |
| OnePair | [페어Rank, 키커3, 키커2, 키커1] |
| TwoPair | [높은페어, 낮은페어, 키커] |
| ThreeOfAKind | [트리플Rank, 키커2, 키커1] |
| Straight | [TopCard Rank (Ace=14, Wheel=5)] |
| Flush | [카드5, 카드4, 카드3, 카드2, 카드1] (Rank 내림차순) |
| FullHouse | [트리플Rank, 페어Rank] |
| FourOfAKind | [쿼드Rank, 키커] |
| StraightFlush | [TopCard Rank] |
| RoyalFlush | [0] (항상 동점) |

Ace는 int 변환 시 High=14, Low=1로 처리한다.

---

## Evaluation Logic

### `HandEvaluator`

```csharp
public class HandEvaluator
{
    // n장(1~52)을 받아 최선의 족보 반환
    public HandResult Evaluate(IReadOnlyList<BaseCard> cards);
}
```

### 평가 흐름

1. `CardCategory.Custom` 카드 제거
2. Joker 분리 및 카운트
3. Standard 카드에서 5장 조합(nC5) 생성
   - 5장 미만이면 있는 카드 전체를 단일 조합으로 처리
4. 각 조합 + Joker 수로 족보 판정
5. 모든 조합 중 `HandResult.CompareTo()` 최댓값 반환

### Joker 와일드 처리

Joker 수에 상한 없음 — 입력에 포함된 Joker 전부를 와일드로 처리한다. 족보 체커 내부에서 `jokerCount` 매개변수로 전달한다.

예시:
- Joker 1장 + 페어 → ThreeOfAKind
- Joker 2장 + 페어 → FourOfAKind
- Joker 1장 + 4장 동일 문양 → Flush

### Ace High/Low 처리 (Duplicate Ace)

스트레이트 판정 시 Ace가 있으면 값 집합에 1과 14를 모두 추가하여 단일 패스로 처리한다.

```csharp
// Ace가 있으면 14도 추가
if (values.Contains(1)) values.Add(14);

// top=14: A-K-Q-J-10, top=5: A-2-3-4-5
for (int top = 14; top >= 5; top--)
{
    if (values.Contains(top) && values.Contains(top-1) && ...
        values.Contains(top-4))
    {
        topCard = (top == 14) ? Rank.Ace : (Rank)top; // Wheel이면 TopCard=Five
        return true;
    }
}
```

---

## Testing

`HandEvaluatorTests.cs` — EditMode 단위 테스트 (순수 C# 이므로 Unity 불필요)

테스트 커버리지:
- 10가지 족보 각각 정상 판정
- Joker 와일드 조합 (0장, 1장, 2장, 다수 Joker)
- Ace High / Ace Low 스트레이트
- 같은 족보끼리 타이브레이크 비교
- n장 중 best 5 선택 (7장 입력 등)
- Custom 카드 무시
- 유효 카드 5장 미만 엣지케이스
