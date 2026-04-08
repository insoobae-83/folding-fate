# Card System Design

## Overview

카드 데이터 모델 시스템. 트럼프 카드 기반의 카드 정의와, 외형/속성/스탯이 독립적으로 조합된 카드 변형을 지원한다.

## Requirements

- 표준 트럼프 52장 (4문양 x 13랭크) + 조커 + 커스텀 카드 확장 가능
- 기본 카드(BaseCard)를 베이스로, 외형 변형 / 속성 / 추가 스탯이 각각 독립적으로 조합되어 고정된 카드 변형(CardVariant)을 구성
- 속성은 카드당 최대 1개 (없거나 1개)
- 추가 스탯은 종류/개수 자유 확장 가능
- 같은 StatType 중복 허용 (합산 등 처리 방식은 추후 확정)
- 카드 소유/수집 시스템은 이 설계 범위 밖

## Architecture: B안 (베이스 + 변형 분리)

기본 카드 정의와 변형을 2단 구조로 분리한다.

### Core Enums & Structs

`Assets/Scripts/Core/`에 배치. 카드 외 다른 피처에서도 사용 가능.

```csharp
public enum Suit { Spade, Heart, Diamond, Club }

public enum Rank
{
    Ace = 1, Two, Three, Four, Five, Six, Seven,
    Eight, Nine, Ten, Jack, Queen, King
}

public enum Element { None, Fire, Water, Earth }

public enum StatType { Attack, Point }

public enum CardCategory { Standard, Joker, Custom }

[Serializable]
public struct StatModifier
{
    public StatType Type;
    public float Value;
}
```

- `Element`, `StatType`은 나중에 값 추가로 확장
- `StatModifier`는 종류 + 값 쌍

### ScriptableObject (불변 설정 데이터)

`Assets/Scripts/Features/Card/Data/`에 배치.

#### BaseCardData

```csharp
[CreateAssetMenu(fileName = "NewBaseCard", menuName = "FoldingFate/Card/BaseCardData")]
public class BaseCardData : ScriptableObject
{
    [field: SerializeField] public string Id { get; private set; }
    [field: SerializeField] public CardCategory Category { get; private set; }
    [field: SerializeField] public Suit? Suit { get; private set; }
    [field: SerializeField] public Rank? Rank { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
}
```

- 표준 52장: Category=Standard, Suit+Rank 조합
- 조커: Category=Joker, Suit/Rank는 null
- 커스텀: Category=Custom, 자유 정의

#### CardVariantData

```csharp
[CreateAssetMenu(fileName = "NewCardVariant", menuName = "FoldingFate/Card/CardVariantData")]
public class CardVariantData : ScriptableObject
{
    [field: SerializeField] public string Id { get; private set; }
    [field: SerializeField] public BaseCardData BaseCard { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public string SkinId { get; private set; }
    [field: SerializeField] public Element Element { get; private set; }
    [field: SerializeField] public List<StatModifier> StatModifiers { get; private set; }
}
```

- 외형(SkinId), 속성(Element), 스탯(StatModifiers)은 각각 독립적으로 있거나 없을 수 있음
- 하나의 BaseCard에 대해 여러 CardVariantData 에셋 가능

### Domain Models (순수 C#, Anemic Domain Model)

`Assets/Scripts/Features/Card/Models/`에 배치. Unity 의존성 없음.

#### BaseCard

```csharp
public class BaseCard
{
    public string Id { get; }
    public CardCategory Category { get; }
    public Suit? Suit { get; }
    public Rank? Rank { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public bool IsStandard => Category == CardCategory.Standard;
    public bool IsJoker => Category == CardCategory.Joker;
    public bool IsCustom => Category == CardCategory.Custom;
}
```

#### CardVariant

```csharp
public class CardVariant
{
    public string Id { get; }
    public BaseCard BaseCard { get; }
    public string DisplayName { get; }
    public string SkinId { get; }
    public Element Element { get; }
    public IReadOnlyList<StatModifier> StatModifiers { get; }

    public bool HasElement => Element != Element.None;
    public bool HasSkin => !string.IsNullOrEmpty(SkinId);
    public bool HasStatModifiers => StatModifiers.Count > 0;
}
```

### System

`Assets/Scripts/Features/Card/Systems/`에 배치.

#### CardFactory

```csharp
public class CardFactory
{
    public BaseCard CreateBaseCard(BaseCardData data) => ...;
    public CardVariant CreateCardVariant(CardVariantData data, BaseCard baseCard) => ...;
}
```

- ScriptableObject → 순수 C# 도메인 모델 변환 담당
- VContainer로 주입

#### CardStatSystem

```csharp
public class CardStatSystem
{
    public float GetStatValue(CardVariant variant, StatType type) => ...;
}
```

- 동일 StatType 중복 시 합산 (변경 가능)

## Folder Structure

```
Assets/Scripts/
  Core/
    Enums/
      Suit.cs
      Rank.cs
      Element.cs
      StatType.cs
      CardCategory.cs
    Structs/
      StatModifier.cs

  Features/
    Card/
      Models/
        BaseCard.cs
        CardVariant.cs
      Data/
        BaseCardData.cs
        CardVariantData.cs
      Systems/
        CardFactory.cs

Assets/Tests/
  EditMode/
    Card/
      BaseCardTests.cs
      CardVariantTests.cs
```

## Assembly Definition References

- `FoldingFate.Core`: 없음
- `FoldingFate.Features`: Core 참조
- `FoldingFate.Tests.EditMode`: Features, Core 참조

## Test Strategy

EditMode 단위 테스트 (순수 C# 모델이므로 Unity 런타임 불필요).

### BaseCard Tests
- Standard 카드 생성 시 Suit/Rank 정상 설정
- Joker 카드 생성 시 IsJoker == true, Suit/Rank는 null
- Custom 카드 생성 확인

### CardVariant Tests
- 속성 없는 변형: HasElement == false
- 스킨 없는 변형: HasSkin == false
- 스탯 없는 변형: HasStatModifiers == false
- StatModifier 여러 개 추가 후 GetStatValue 조회
- 동일 StatType 중복 시 동작 확인

### CardFactory Tests
- BaseCardData → BaseCard 변환 정상 동작
- CardVariantData → CardVariant 변환 정상 동작

## Open Decisions

- [ ] StatModifier 동일 StatType 중복 시 처리 방식 (합산 / 최대값 / 커스텀 로직)
- [ ] SkinId가 참조할 외형 리소스 체계 (Sprite, Prefab, Addressable 등)
- [ ] 카드 소유/수집 시스템 설계
