# 족보 연출(Hand Showcase) 설계

**날짜:** 2026-04-10
**기능:** 제출 후 족보 카드를 핸드 위에 연출하고, 설정 시간 후 기본 화면으로 복귀

## 요약

제출 버튼 클릭 → 족보 평가 완료 후, 제출한 카드들과 족보 텍스트를 핸드 위에 인라인으로 표시한다. 설정된 시간(기본 2초)이 지나면 연출이 사라지고 제출한 카드가 빠진 핸드 상태로 복귀한다.

## 연출 흐름

1. 플레이어가 카드 선택 후 **제출** 클릭
2. 족보 평가 실행
3. **연출 진입**:
   - 핸드 컨테이너 위에 `showcase-container` 영역이 페이드인 (opacity 0→1, 0.3초)
   - `showcase-container` 안에 제출한 카드들 + 족보 텍스트 표시
   - 핸드, 버튼 영역은 opacity를 낮춰 비활성화 상태로 전환
   - 모든 버튼(제출, 버리기, 다시 받기) 비활성화
4. **설정 시간 유지** (기본 2초, `PokerConfig.ShowcaseDurationSeconds`에서 읽음)
5. **연출 퇴장**:
   - `showcase-container`가 페이드아웃 (opacity 1→0, 0.3초)
   - 핸드, 버튼 영역 opacity 복원
6. **기본 화면 복귀**:
   - 제출한 카드가 빠진 핸드 상태 (자동 드로우 없음)
   - 버튼 활성화 복원

## 연출 중 제약

- 모든 버튼 비활성화 (CanSubmit, CanDraw 모두 false)
- 카드 선택/해제 불가
- 플레이어 조작 완전 차단 (스킵 불가)

## 변경 대상

### 새 파일

| 파일 | 용도 |
|---|---|
| `Assets/Scripts/Features/Poker/Data/PokerConfig.cs` | ScriptableObject — 연출 시간 등 설정값 |
| `Assets/Settings/PokerConfig.asset` | PokerConfig 인스턴스 (에디터에서 생성) |

### 수정 파일

| 파일 | 변경 |
|---|---|
| `PokerViewModel.cs` | `ShowcaseState` (ReactiveProperty) 추가 — 연출 중 카드 목록, 족보 텍스트, 연출 활성 여부 |
| `RoundController.cs` | 제출 흐름을 비동기로 변경 — 평가 → 연출 진입 → UniTask.Delay → 연출 퇴장 → 카드 제거 |
| `PokerHUD.uxml` | `hand-container` 위에 `showcase-container` 영역 추가 |
| `PokerHUD.uss` | `showcase-container` 및 하위 요소 스타일, 페이드 트랜지션, 비활성화 opacity |
| `PokerView.cs` | `ShowcaseState` 구독 → showcase-container 렌더링 및 페이드 클래스 토글 |
| `PokerInstaller.cs` | PokerConfig를 VContainer에 등록 |

## 데이터 모델

### PokerConfig (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "PokerConfig", menuName = "FoldingFate/Poker/PokerConfig")]
public class PokerConfig : ScriptableObject
{
    [Tooltip("족보 연출 표시 시간 (초)")]
    public float ShowcaseDurationSeconds = 2f;
}
```

### ShowcaseState (순수 C# 레코드 또는 클래스)

```csharp
public record ShowcaseState(
    bool IsActive,
    IReadOnlyList<BaseCard> Cards,
    string RankText
);
```

- `IsActive = true`: 연출 중 (showcase-container 표시, 핸드/버튼 비활성화)
- `IsActive = false`: 기본 상태 (showcase-container 숨김)

## ViewModel 변경

```csharp
// 새로 추가
public ReactiveProperty<ShowcaseState> Showcase { get; }
public ReadOnlyReactiveProperty<bool> IsShowcasing { get; }  // Showcase.IsActive 바인딩용
```

- `CanSubmit`, `CanDraw`의 조건에 `IsShowcasing == false`를 AND로 추가
- 카드 선택(`ToggleSelectCommand`)도 `IsShowcasing == false`일 때만 실행

## RoundController 제출 흐름 변경

기존 (동기):
```
평가 → 카드 제거 → 결과 텍스트 표시
```

변경 (비동기):
```
평가 → 연출 진입(ShowcaseState active) → Delay(설정 시간) → 연출 퇴장(ShowcaseState inactive) → 카드 제거
```

- `IStartable` → `IAsyncStartable` 변경 불필요 — SubmitCommand 구독 내에서 UniTask 사용
- `PokerConfig`를 생성자로 주입받음

## USS 스타일

```css
.showcase-container {
    align-items: center;
    opacity: 0;
    transition-property: opacity;
    transition-duration: 0.3s;
}

.showcase-container--active {
    opacity: 1;
}

.showcase-card {
    /* 기존 .card와 유사하되, 금색 테두리 강조 */
    border-color: rgb(218, 165, 32);
    border-width: 2px;
}

.showcase-rank-text {
    color: rgb(218, 165, 32);
    font-size: 24px;
    -unity-font-style: bold;
}

/* 연출 중 핸드/버튼 비활성화 */
.poker-root--showcasing .hand-container {
    opacity: 0.3;
}

.poker-root--showcasing .button-row {
    opacity: 0.3;
}
```

## UXML 구조 변경

```xml
<ui:VisualElement name="poker-root" class="poker-root">
    <ui:Label name="deck-count-label" ... />
    <!-- 새로 추가: 연출 영역 -->
    <ui:VisualElement name="showcase-container" class="showcase-container" />
    <!-- 기존 핸드 -->
    <ui:VisualElement name="hand-container" class="hand-container" />
    <ui:Label name="result-label" ... />
    <ui:VisualElement class="button-row">...</ui:VisualElement>
</ui:VisualElement>
```

## 범위 외

- 연출 스킵 기능
- 사운드/파티클 효과
- 연출 후 자동 드로우
- 점수/콤보 시스템
