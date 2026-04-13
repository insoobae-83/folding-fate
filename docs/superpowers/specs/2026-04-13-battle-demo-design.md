# Battle Demo Design — 4v1 턴제 전투 연출

## Overview

포커 핸드 제출 → 캐릭터 4명이 몬스터 1마리를 공격하는 턴제 전투 데모. Mixamo 애니메이션과 IdaFaber 3D 캐릭터를 사용하여, 공격/피격/사망/승리 연출이 포함된 시각적 전투를 구현한다.

## Requirements

- 캐릭터 4명 vs 몬스터 1마리 전투
- 캐릭터: IdaFaber 프리팹 4종 (ChengYi, JuaLee, KitsuneYuna, SamuraiGirl)
- 몬스터: IdaFaber ShinobiGirl 프리팹
- 더미 스탯 데이터: CharacterData/MonsterData SO
- Mixamo 애니메이션 5종 (Idle, Attack, Hit, Death, Victory) → Humanoid 리타겟팅
- 월드 스페이스 HP바 (캐릭터/몬스터 머리 위)
- 포커 핸드 제출이 전투 행동을 트리거

## Prerequisites

이미 구현된 시스템:
- Entity CBD (Entity, Stats, Health, Combat, StatsSystem, HealthSystem, EntityFactory)
- Battle 3-Phase (BattleAction, ActionResult, TurnRecord, BattleModel, ResolveSystem, ApplySystem, TurnSystem, BattleSystem)
- Poker (RoundController, DealSystem, HandEvaluator, PokerViewModel, PokerView)

## Architecture

### 전투 흐름

```
1. 씬 시작
   BattleController가 EntityFactory로 캐릭터 4명 + 몬스터 1마리 Entity 생성
   BattleSystem.StartBattle() → BattleModel 생성
   EntityView들이 Entity와 3D 모델을 바인딩

2. 포커 핸드 제출
   RoundController → HandSubmittedEvent(HandResult) 발행 (R3 Subject)

3. 플레이어 턴
   BattleController가 HandSubmittedEvent 수신
   → 캐릭터 4명의 BattleAction(Attack, 몬스터) 생성
   → TurnSystem.ExecuteTurn() → Resolve → Apply
   → BattleEffectController에 ActionResult 목록 전달

4. 전투 연출 (순차, UniTask async)
   캐릭터1: 전진 트윈 → Attack 애니메이션 → 몬스터 Hit 애니메이션 + HP바 감소 → 복귀 트윈
   캐릭터2: 같은 연출 반복
   캐릭터3, 4 반복

5. 승패 판정
   TurnSystem.EndTurn()
   몬스터 사망 → Death 애니메이션 → 캐릭터들 Victory 애니메이션 → 전투 종료
   몬스터 생존 → 적 턴으로

6. 적 턴
   BattleController가 몬스터의 BattleAction(Attack, 랜덤 캐릭터) 생성
   → TurnSystem.ExecuteTurn() → Resolve → Apply
   → 몬스터 공격 연출 → 캐릭터 Hit 연출
   → 캐릭터 사망 시 Death 애니메이션
   → EndTurn → 전멸 시 패배, 아니면 다음 포커 턴 대기
```

### 이벤트 연결 (Poker → Battle)

RoundController에서 핸드 제출 시 R3 Subject로 이벤트 발행:

```csharp
// Infrastructure/EventBus/ 또는 Poker/Events/
public record HandSubmittedEvent(HandResult Result);
```

RoundController의 Submit 흐름에 이벤트 발행을 추가:
```csharp
// 기존 코드 (line 37)
var result = _dealSystem.EvaluateSelected();
// 추가: 이벤트 발행
_handSubmittedSubject.OnNext(new HandSubmittedEvent(result));
// 기존 코드 계속...
```

BattleController가 이 이벤트를 구독하여 전투 턴 시작.

### 이벤트 버스

피처 간 통신을 위한 최소 EventBus. R3 Subject 기반:

```csharp
// Assets/Scripts/Infrastructure/EventBus/EventBus.cs
public class EventBus
{
    private readonly Dictionary<Type, object> _subjects = new();

    public void Publish<T>(T message) { ... }
    public Observable<T> Receive<T>() { ... }
}
```

VContainer에 Singleton으로 등록. Poker와 Battle이 EventBus를 통해서만 통신.

### 애니메이션 구성

Mixamo FBX 5종을 Humanoid으로 Import:

**AnimatorController: `BattleAnimator`**
- Default State: Idle (Loop)
- Trigger Parameters: `Attack`, `Hit`, `Death`, `Victory`
- Transitions:
  - Any State → Attack (Trigger) → Idle
  - Any State → Hit (Trigger) → Idle
  - Any State → Death (Trigger) → Death (stay)
  - Any State → Victory (Trigger) → Victory (Loop)

모든 캐릭터/몬스터가 동일한 AnimatorController 사용 (Humanoid 리타겟팅).

### 씬 배치

```
BattleScene (새 씬)
├── Main Camera (전투 필드를 바라보는 고정 카메라)
├── Directional Light
├── AllyGroup (좌측)
│   ├── Ally_0 (ChengYi 프리팹 + EntityView + WorldHpBar)
│   ├── Ally_1 (JuaLee 프리팹 + EntityView + WorldHpBar)
│   ├── Ally_2 (KitsuneYuna 프리팹 + EntityView + WorldHpBar)
│   └── Ally_3 (SamuraiGirl 프리팹 + EntityView + WorldHpBar)
├── EnemyGroup (우측)
│   └── Enemy_0 (ShinobiGirl 프리팹 + EntityView + WorldHpBar)
├── BattleController (MonoBehaviour)
├── BattleEffectController (MonoBehaviour)
├── BattleInstaller (LifetimeScope)
└── PokerUI (기존 PokerView + PokerInstaller — 화면 하단에 배치)
```

캐릭터 좌측 일렬, 몬스터 우측 배치. 카메라는 사이드뷰 또는 약간 위에서 내려다보는 각도.

### 전투 연출 상세

**BattleEffectController** (MonoBehaviour, UniTask):

```csharp
public class BattleEffectController : MonoBehaviour
{
    // EntityView 참조 (씬에서 할당 또는 VContainer 주입)

    public async UniTask PlayTurnEffects(IReadOnlyList<ActionResult> results)
    {
        for (int i = 0; i < results.Count; i++)
        {
            await PlayActionEffect(results[i]);
        }
    }

    private async UniTask PlayActionEffect(ActionResult result)
    {
        var actorView = GetEntityView(result.Source.Actor);
        var targetView = GetEntityView(result.Target);

        // 1. 액터 전진 (타겟 방향으로 트윈)
        await actorView.MoveToward(targetView.transform.position, attackMoveDistance, moveDuration);

        // 2. Attack 애니메이션
        actorView.PlayAttack();
        await UniTask.Delay(TimeSpan.FromSeconds(attackAnimDelay));

        // 3. 타겟 피격
        if (result.ResultType == ActionResultType.Damage && result.Value > 0)
        {
            targetView.PlayHit();
            targetView.UpdateHpBar();
        }

        // 4. 액터 복귀
        await actorView.MoveBack(moveDuration);

        // 5. 사망 체크
        if (targetView.Entity.Get<Health>().IsDead)
        {
            targetView.PlayDeath();
        }
    }
}
```

**EntityView** (MonoBehaviour):

```csharp
public class EntityView : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private WorldHpBar _hpBar;

    public Entity Entity { get; private set; }
    private Vector3 _originalPosition;

    public void Bind(Entity entity) { ... }
    public void PlayAttack() => _animator.SetTrigger("Attack");
    public void PlayHit() => _animator.SetTrigger("Hit");
    public void PlayDeath() => _animator.SetTrigger("Death");
    public void PlayVictory() => _animator.SetTrigger("Victory");
    public void UpdateHpBar() => _hpBar.SetValue(entity.Get<Health>());

    public async UniTask MoveToward(Vector3 target, float distance, float duration) { ... }
    public async UniTask MoveBack(float duration) { ... }
}
```

**WorldHpBar** (MonoBehaviour, World Space Canvas):

```csharp
public class WorldHpBar : MonoBehaviour
{
    [SerializeField] private Image _fillImage;

    public void SetValue(Health health)
    {
        _fillImage.fillAmount = health.CurrentHp / health.MaxHp;
    }
}
```

### 더미 데이터

전투 데모용 더미 스탯. 나중에 카드 효과/장비 시스템이 붙으면 교체.

```csharp
// ScriptableObject — 에디터에서 설정
[CreateAssetMenu(menuName = "FoldingFate/Battle/CharacterData")]
public class BattleCharacterData : ScriptableObject
{
    public string DisplayName;
    public float MaxHp = 100f;
    public float Attack = 15f;
    public float Defense = 5f;
    public GameObject Prefab;  // IdaFaber 프리팹 참조
}

[CreateAssetMenu(menuName = "FoldingFate/Battle/MonsterData")]
public class BattleMonsterData : ScriptableObject
{
    public string DisplayName;
    public float MaxHp = 200f;
    public float Attack = 12f;
    public float Defense = 3f;
    public GameObject Prefab;  // ShinobiGirl 프리팹 참조
}
```

### 피처 구조

```
Assets/Scripts/
  Infrastructure/
    EventBus/
      EventBus.cs                — R3 Subject 기반 글로벌 이벤트 버스

  Features/
    Poker/
      Events/
        HandSubmittedEvent.cs    — record(HandResult)
      Controllers/
        RoundController.cs       — (수정) Submit 시 HandSubmittedEvent 발행

    Battle/
      Controllers/
        BattleController.cs      — 전투 생명주기 + 턴 오케스트레이션
        BattleEffectController.cs — ActionResult 순차 연출
      Components/
        EntityView.cs            — Entity ↔ 3D 모델 바인딩
        WorldHpBar.cs            — 머리 위 HP바
      Data/
        BattleCharacterData.cs   — SO (캐릭터 더미 스탯 + 프리팹)
        BattleMonsterData.cs     — SO (몬스터 더미 스탯 + 프리팹)
      BattleSceneInstaller.cs    — 전투 씬 LifetimeScope

Assets/ThirdParty/
  Mixamo/
    FBXs/                        — 5종 FBX (이미 존재)
    Animations/                  — Import 후 추출된 AnimationClip
    BattleAnimator.controller    — 공유 Animator Controller
```

### VContainer 구성

```csharp
public class BattleSceneInstaller : LifetimeScope
{
    [SerializeField] private BattleCharacterData[] _characterDataList;
    [SerializeField] private BattleMonsterData _monsterData;

    protected override void Configure(IContainerBuilder builder)
    {
        // Infrastructure
        builder.Register<EventBus>(Lifetime.Singleton);

        // Entity
        builder.Register<EntityFactory>(Lifetime.Singleton);
        builder.Register<StatsSystem>(Lifetime.Singleton);
        builder.Register<HealthSystem>(Lifetime.Singleton);

        // Battle Systems
        builder.Register<BattleSystem>(Lifetime.Singleton);
        builder.Register<ResolveSystem>(Lifetime.Singleton);
        builder.Register<ApplySystem>(Lifetime.Singleton);
        builder.Register<TurnSystem>(Lifetime.Singleton);

        // Data
        builder.RegisterInstance(_characterDataList);
        builder.RegisterInstance(_monsterData);

        // Controllers
        builder.RegisterEntryPoint<BattleController>();
        builder.RegisterComponentInHierarchy<BattleEffectController>();

        // Views
        // EntityView, WorldHpBar는 프리팹에 붙어 있으므로 씬에서 직접 참조
    }
}
```

### Mixamo 애니메이션 Import 설정

Unity에서 FBX Import Settings:
1. Rig 탭: Animation Type → **Humanoid**
2. Animation 탭: 각 클립 이름 정리
   - Idle: Loop Time ✓
   - Attack: Loop Time ✗
   - Hit: Loop Time ✗
   - Death: Loop Time ✗
   - Victory: Loop Time ✓

AnimatorController는 Unity Editor에서 수동 생성 (코드로 생성하지 않음).

## Scope

### 이번 구현 범위
- EventBus (Infrastructure)
- HandSubmittedEvent + RoundController 수정
- BattleController, BattleEffectController
- EntityView, WorldHpBar
- BattleCharacterData, BattleMonsterData SO
- Mixamo 애니메이션 Humanoid Import + AnimatorController
- BattleScene 씬 구성
- 전투 연출 (트윈 + 애니메이션)

### 이번 범위 밖
- 포커 결과에 따른 데미지 차등 (고정 데미지 사용)
- 적 AI 고도화 (랜덤 타겟 공격만)
- Dungeon 시스템 연결
- 전투 UI (파티 정보, 턴 표시 등 — 월드 HP바만)
- 사운드/이펙트

## Open Decisions

- [ ] 카메라 각도/위치 구체적 수치
- [ ] 트윈 라이브러리 선택 (UniTask 수동 Lerp vs DOTween)
- [ ] 전투 종료 후 다음 흐름 (씬 리로드? 대기?)
