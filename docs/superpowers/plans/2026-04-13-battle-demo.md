# Battle Demo (4v1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 포커 핸드 제출 → 캐릭터 4명이 몬스터 1마리를 공격하는 턴제 전투 데모 (Mixamo 애니메이션 + IdaFaber 3D 캐릭터 + 월드 HP바 + 전투 연출).

**Architecture:** EventBus로 Poker→Battle 연결. BattleController가 턴 오케스트레이션, BattleEffectController가 UniTask 기반 순차 연출(트윈+애니메이션). EntityView가 Entity와 3D 모델을 바인딩. 기존 3-Phase TurnSystem(Resolve→Apply)으로 데미지 처리.

**Tech Stack:** Unity 6 (C#), R3, VContainer, UniTask, Mixamo Humanoid 애니메이션, IdaFaber 3D 캐릭터

**Spec:** `docs/superpowers/specs/2026-04-13-battle-demo-design.md`

---

## File Map

### Infrastructure 레이어

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Assets/Scripts/Infrastructure/EventBus/EventBus.cs` | R3 Subject 기반 글로벌 이벤트 버스 |
| Create | `Assets/Scripts/Infrastructure/FoldingFate.Infrastructure.asmdef` | Infrastructure 어셈블리 정의 |

### Poker 피처 (수정)

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Assets/Scripts/Features/Poker/Events/HandSubmittedEvent.cs` | 핸드 제출 이벤트 |
| Modify | `Assets/Scripts/Features/Poker/Controllers/RoundController.cs` | Submit 시 이벤트 발행 추가 |

### Battle 피처 (추가)

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Assets/Scripts/Features/Battle/Data/BattleCharacterData.cs` | SO — 캐릭터 더미 스탯 + 프리팹 |
| Create | `Assets/Scripts/Features/Battle/Data/BattleMonsterData.cs` | SO — 몬스터 더미 스탯 + 프리팹 |
| Create | `Assets/Scripts/Features/Battle/Components/WorldHpBar.cs` | 머리 위 HP바 |
| Create | `Assets/Scripts/Features/Battle/Components/EntityView.cs` | Entity ↔ 3D 모델 바인딩 |
| Create | `Assets/Scripts/Features/Battle/Controllers/BattleEffectController.cs` | ActionResult 순차 연출 |
| Create | `Assets/Scripts/Features/Battle/Controllers/BattleController.cs` | 전투 생명주기 + 턴 오케스트레이션 |
| Create | `Assets/Scripts/Features/Battle/BattleSceneInstaller.cs` | 전투 씬 LifetimeScope |

### Tests

| Action | Path |
|--------|------|
| Create | `Assets/Tests/EditMode/Infrastructure/EventBusTests.cs` |

### Unity 에디터 작업 (코드 아님)

| Action | Path | Responsibility |
|--------|------|----------------|
| Configure | `Assets/ThirdParty/Mixamo/FBXs/*.fbx` | Humanoid Rig + Animation Clip 추출 |
| Create | `Assets/ThirdParty/Mixamo/BattleAnimator.controller` | 공유 AnimatorController |
| Create | `Assets/Scenes/BattleScene.unity` | 전투 데모 씬 |
| Create | SO 에셋 4개 (CharacterData) + 1개 (MonsterData) | 에디터에서 수동 생성 |

---

## Task 1: Infrastructure — EventBus

피처 간 통신을 위한 R3 Subject 기반 이벤트 버스. Infrastructure asmdef 포함.

**Files:**
- Create: `Assets/Scripts/Infrastructure/FoldingFate.Infrastructure.asmdef`
- Create: `Assets/Scripts/Infrastructure/EventBus/EventBus.cs`
- Test: `Assets/Tests/EditMode/Infrastructure/EventBusTests.cs`

- [ ] **Step 1: Infrastructure asmdef 생성**

```json
// Assets/Scripts/Infrastructure/FoldingFate.Infrastructure.asmdef
{
    "name": "FoldingFate.Infrastructure",
    "rootNamespace": "FoldingFate.Infrastructure",
    "references": [
        "FoldingFate.Core",
        "R3.Unity"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: FoldingFate.Features.asmdef에 Infrastructure 참조 추가**

`Assets/Scripts/Features/FoldingFate.Features.asmdef`의 `references` 배열에 `"FoldingFate.Infrastructure"` 추가:

```json
{
    "name": "FoldingFate.Features",
    "rootNamespace": "FoldingFate.Features",
    "references": [
        "FoldingFate.Core",
        "FoldingFate.Infrastructure",
        "R3.Unity",
        "VContainer",
        "UniTask"
    ],
    ...
}
```

- [ ] **Step 3: FoldingFate.Tests.EditMode.asmdef에 Infrastructure 참조 추가**

`Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef`의 `references` 배열에 `"FoldingFate.Infrastructure"` 추가:

```json
{
    "name": "FoldingFate.Tests.EditMode",
    "references": [
        "FoldingFate.Core",
        "FoldingFate.Features",
        "FoldingFate.Infrastructure",
        "R3.Unity",
        "VContainer"
    ],
    ...
}
```

- [ ] **Step 4: EventBus 테스트 작성**

```csharp
// Assets/Tests/EditMode/Infrastructure/EventBusTests.cs
using NUnit.Framework;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Tests.EditMode.Infrastructure
{
    public struct TestEvent
    {
        public int Value;
        public TestEvent(int value) { Value = value; }
    }

    public struct AnotherEvent
    {
        public string Message;
        public AnotherEvent(string message) { Message = message; }
    }

    [TestFixture]
    public class EventBusTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _bus.Dispose();
        }

        [Test]
        public void Publish_SubscriberReceivesEvent()
        {
            int received = -1;
            _bus.Receive<TestEvent>().Subscribe(e => received = e.Value);
            _bus.Publish(new TestEvent(42));
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_MultipleSubscribers_AllReceive()
        {
            int count = 0;
            _bus.Receive<TestEvent>().Subscribe(_ => count++);
            _bus.Receive<TestEvent>().Subscribe(_ => count++);
            _bus.Publish(new TestEvent(1));
            Assert.AreEqual(2, count);
        }

        [Test]
        public void Publish_DifferentTypes_Independent()
        {
            int testReceived = 0;
            string anotherReceived = null;
            _bus.Receive<TestEvent>().Subscribe(e => testReceived = e.Value);
            _bus.Receive<AnotherEvent>().Subscribe(e => anotherReceived = e.Message);
            _bus.Publish(new TestEvent(10));
            Assert.AreEqual(10, testReceived);
            Assert.IsNull(anotherReceived);
        }

        [Test]
        public void Receive_NoPublish_NoCallback()
        {
            int received = -1;
            _bus.Receive<TestEvent>().Subscribe(e => received = e.Value);
            Assert.AreEqual(-1, received);
        }
    }
}
```

- [ ] **Step 5: EventBus 구현**

```csharp
// Assets/Scripts/Infrastructure/EventBus/EventBus.cs
using System;
using System.Collections.Generic;
using R3;

namespace FoldingFate.Infrastructure.EventBus
{
    public class EventBus : IDisposable
    {
        private readonly Dictionary<Type, object> _subjects = new();

        public void Publish<T>(T message)
        {
            if (_subjects.TryGetValue(typeof(T), out var subject))
            {
                ((Subject<T>)subject).OnNext(message);
            }
        }

        public Observable<T> Receive<T>()
        {
            if (!_subjects.TryGetValue(typeof(T), out var subject))
            {
                subject = new Subject<T>();
                _subjects[typeof(T)] = subject;
            }
            return ((Subject<T>)subject).AsObservable();
        }

        public void Dispose()
        {
            foreach (var subject in _subjects.Values)
            {
                if (subject is IDisposable disposable)
                    disposable.Dispose();
            }
            _subjects.Clear();
        }
    }
}
```

- [ ] **Step 6: Unity MCP로 테스트 실행**

Run: `run_tests` (EditMode, filter: `FoldingFate.Tests.EditMode.Infrastructure.EventBusTests`)
Expected: 4 tests PASS

- [ ] **Step 7: 커밋**

```bash
git add Assets/Scripts/Infrastructure/ Assets/Tests/EditMode/Infrastructure/ \
  Assets/Scripts/Features/FoldingFate.Features.asmdef \
  Assets/Tests/EditMode/FoldingFate.Tests.EditMode.asmdef
git commit -m "feat(infra): add R3-based EventBus for cross-feature communication"
```

---

## Task 2: HandSubmittedEvent + RoundController 수정

포커 핸드 제출 시 EventBus로 이벤트를 발행하여 Battle 피처와 연결.

**Files:**
- Create: `Assets/Scripts/Features/Poker/Events/HandSubmittedEvent.cs`
- Modify: `Assets/Scripts/Features/Poker/Controllers/RoundController.cs`
- Modify: `Assets/Scripts/Features/Poker/PokerInstaller.cs`

- [ ] **Step 1: HandSubmittedEvent 생성**

```csharp
// Assets/Scripts/Features/Poker/Events/HandSubmittedEvent.cs
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Events
{
    public readonly struct HandSubmittedEvent
    {
        public HandResult Result { get; }

        public HandSubmittedEvent(HandResult result)
        {
            Result = result;
        }
    }
}
```

- [ ] **Step 2: RoundController에 EventBus 주입 및 이벤트 발행 추가**

`Assets/Scripts/Features/Poker/Controllers/RoundController.cs` 수정:

```csharp
using System;
using Cysharp.Threading.Tasks;
using R3;
using VContainer.Unity;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Events;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Poker.Controllers
{
    public class RoundController : IStartable, IDisposable
    {
        private readonly DealSystem _dealSystem;
        private readonly PokerViewModel _vm;
        private readonly PokerConfig _config;
        private readonly EventBus _eventBus;
        private readonly CompositeDisposable _disposables = new();

        public RoundController(DealSystem dealSystem, PokerViewModel vm, PokerConfig config, EventBus eventBus)
        {
            _dealSystem = dealSystem;
            _vm = vm;
            _config = config;
            _eventBus = eventBus;
        }

        public void Start()
        {
            _dealSystem.InitializeDeck();
            DealToFullAsync().Forget();

            _vm.ToggleSelectCommand
                .Subscribe(index => _dealSystem.ToggleSelect(index))
                .AddTo(_disposables);

            _vm.SubmitCommand
                .SubscribeAwait(async (_, ct) =>
                {
                    var result = _dealSystem.EvaluateSelected();

                    // 전투 시스템에 핸드 결과 발행
                    _eventBus.Publish(new HandSubmittedEvent(result));

                    _vm.BeginShowcase(result);

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.ShowcaseDurationSeconds),
                        cancellationToken: ct);

                    _vm.EndShowcase();
                    _dealSystem.DiscardSelected();
                    _vm.PushHandResult(result);

                    await DealToFullAsync(ct);
                }, AwaitOperation.Drop)
                .AddTo(_disposables);

            _vm.DiscardCommand
                .SubscribeAwait(async (_, ct) =>
                {
                    _dealSystem.DiscardSelected();
                    await DealToFullAsync(ct);
                }, AwaitOperation.Drop)
                .AddTo(_disposables);
        }

        private async UniTask DealToFullAsync(System.Threading.CancellationToken ct = default)
        {
            int needed = _dealSystem.CardsNeeded();
            if (needed <= 0) return;

            _vm.BeginDealing();
            for (int i = 0; i < needed; i++)
            {
                _dealSystem.DrawOne();
                if (i < needed - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.DealIntervalSeconds),
                        cancellationToken: ct);
                }
            }
            _vm.EndDealing();
        }

        public void Dispose() => _disposables.Dispose();
    }
}
```

- [ ] **Step 3: PokerInstaller에 EventBus 등록**

`Assets/Scripts/Features/Poker/PokerInstaller.cs` 수정 — EventBus를 등록 추가:

```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;
using FoldingFate.Features.Poker.UI.Views;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Poker
{
    public class PokerInstaller : LifetimeScope
    {
        [SerializeField] private PokerConfig _pokerConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.RegisterInstance(_pokerConfig);
            builder.Register<DeckModel>(Lifetime.Singleton);
            builder.Register<HandModel>(_ => new HandModel(8), Lifetime.Singleton);
            builder.Register<HandEvaluator>(Lifetime.Singleton);
            builder.Register<DealSystem>(Lifetime.Singleton);
            builder.Register<PokerViewModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RoundController>();
            builder.RegisterComponentInHierarchy<PokerView>();
        }
    }
}
```

- [ ] **Step 4: 컴파일 확인**

Unity MCP로 `read_console`을 호출하여 컴파일 에러 없는지 확인.

- [ ] **Step 5: 기존 Poker 테스트 회귀 확인**

Run: `run_tests` (EditMode)
Expected: 기존 테스트에 영향 없음 (RoundController 테스트가 있다면 EventBus mock 필요할 수 있음)

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Features/Poker/
git commit -m "feat(poker): publish HandSubmittedEvent on hand submission via EventBus"
```

---

## Task 3: Battle Data — BattleCharacterData, BattleMonsterData SO

전투 더미 데이터용 ScriptableObject 정의.

**Files:**
- Create: `Assets/Scripts/Features/Battle/Data/BattleCharacterData.cs`
- Create: `Assets/Scripts/Features/Battle/Data/BattleMonsterData.cs`

- [ ] **Step 1: BattleCharacterData SO 생성**

```csharp
// Assets/Scripts/Features/Battle/Data/BattleCharacterData.cs
using UnityEngine;

namespace FoldingFate.Features.Battle.Data
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "FoldingFate/Battle/CharacterData")]
    public class BattleCharacterData : ScriptableObject
    {
        public string DisplayName;
        public float MaxHp = 100f;
        public float Attack = 15f;
        public float Defense = 5f;
        public GameObject Prefab;
    }
}
```

- [ ] **Step 2: BattleMonsterData SO 생성**

```csharp
// Assets/Scripts/Features/Battle/Data/BattleMonsterData.cs
using UnityEngine;

namespace FoldingFate.Features.Battle.Data
{
    [CreateAssetMenu(fileName = "NewMonster", menuName = "FoldingFate/Battle/MonsterData")]
    public class BattleMonsterData : ScriptableObject
    {
        public string DisplayName;
        public float MaxHp = 200f;
        public float Attack = 12f;
        public float Defense = 3f;
        public GameObject Prefab;
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Unity MCP `read_console` — 에러 없는지 확인.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Features/Battle/Data/
git commit -m "feat(battle): add BattleCharacterData and BattleMonsterData ScriptableObjects"
```

---

## Task 4: WorldHpBar + EntityView Components

전투 시각화의 기반 MonoBehaviour 컴포넌트.

**Files:**
- Create: `Assets/Scripts/Features/Battle/Components/WorldHpBar.cs`
- Create: `Assets/Scripts/Features/Battle/Components/EntityView.cs`

- [ ] **Step 1: WorldHpBar 생성**

```csharp
// Assets/Scripts/Features/Battle/Components/WorldHpBar.cs
using UnityEngine;
using UnityEngine.UI;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Components
{
    public class WorldHpBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        public void SetValue(Health health)
        {
            if (health == null) return;
            _fillImage.fillAmount = health.CurrentHp / health.MaxHp;
        }

        public void SetFill(float ratio)
        {
            _fillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
```

- [ ] **Step 2: EntityView 생성**

```csharp
// Assets/Scripts/Features/Battle/Components/EntityView.cs
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Components
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private WorldHpBar _hpBar;

        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");
        private static readonly int VictoryTrigger = Animator.StringToHash("Victory");

        public FoldingFate.Core.Entity Entity { get; private set; }
        private Vector3 _originalPosition;

        public void Bind(FoldingFate.Core.Entity entity)
        {
            Entity = entity;
            _originalPosition = transform.position;
            UpdateHpBar();
        }

        public void PlayAttack() => _animator.SetTrigger(AttackTrigger);
        public void PlayHit() => _animator.SetTrigger(HitTrigger);
        public void PlayDeath() => _animator.SetTrigger(DeathTrigger);
        public void PlayVictory() => _animator.SetTrigger(VictoryTrigger);

        public void UpdateHpBar()
        {
            var health = Entity?.Get<Health>();
            if (health != null && _hpBar != null)
            {
                _hpBar.SetValue(health);
            }
        }

        public async UniTask MoveToward(Vector3 targetPosition, float distance, float duration)
        {
            var direction = (targetPosition - _originalPosition).normalized;
            var destination = _originalPosition + direction * distance;
            await LerpPosition(destination, duration);
        }

        public async UniTask MoveBack(float duration)
        {
            await LerpPosition(_originalPosition, duration);
        }

        private async UniTask LerpPosition(Vector3 target, float duration)
        {
            var start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                await UniTask.Yield();
            }
            transform.position = target;
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Unity MCP `read_console` — 에러 없는지 확인.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Features/Battle/Components/
git commit -m "feat(battle): add EntityView and WorldHpBar components"
```

---

## Task 5: BattleEffectController — 전투 연출

ActionResult 목록을 순차 연출하는 Controller.

**Files:**
- Create: `Assets/Scripts/Features/Battle/Controllers/BattleEffectController.cs`

- [ ] **Step 1: BattleEffectController 생성**

```csharp
// Assets/Scripts/Features/Battle/Controllers/BattleEffectController.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Components;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Entity.Models;

namespace FoldingFate.Features.Battle.Controllers
{
    public class BattleEffectController : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _moveDistance = 1.5f;
        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private float _attackAnimDelay = 0.4f;
        [SerializeField] private float _delayBetweenActions = 0.3f;

        private readonly Dictionary<string, EntityView> _entityViews = new();

        public void RegisterEntityView(FoldingFate.Core.Entity entity, EntityView view)
        {
            _entityViews[entity.Id] = view;
        }

        public EntityView GetEntityView(FoldingFate.Core.Entity entity)
        {
            _entityViews.TryGetValue(entity.Id, out var view);
            return view;
        }

        public async UniTask PlayTurnEffects(IReadOnlyList<ActionResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                await PlayActionEffect(results[i]);

                if (i < results.Count - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_delayBetweenActions),
                        cancellationToken: destroyCancellationToken);
                }
            }
        }

        public async UniTask PlayVictory(IReadOnlyList<FoldingFate.Core.Entity> winners)
        {
            for (int i = 0; i < winners.Count; i++)
            {
                var view = GetEntityView(winners[i]);
                if (view != null)
                {
                    var health = winners[i].Get<Health>();
                    if (health != null && health.IsAlive)
                        view.PlayVictory();
                }
            }
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: destroyCancellationToken);
        }

        private async UniTask PlayActionEffect(ActionResult result)
        {
            var actorView = GetEntityView(result.Source.Actor);
            var targetView = GetEntityView(result.Target);
            if (actorView == null || targetView == null) return;

            // 1. 액터 전진
            await actorView.MoveToward(
                targetView.transform.position, _moveDistance, _moveDuration);

            // 2. Attack 애니메이션
            actorView.PlayAttack();
            await UniTask.Delay(
                TimeSpan.FromSeconds(_attackAnimDelay),
                cancellationToken: destroyCancellationToken);

            // 3. 타겟 피격
            if (result.ResultType == ActionResultType.Damage && result.Value > 0)
            {
                targetView.PlayHit();
                targetView.UpdateHpBar();
            }

            // 4. 액터 복귀
            await actorView.MoveBack(_moveDuration);

            // 5. 사망 체크
            var targetHealth = result.Target.Get<Health>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                targetView.PlayDeath();
                await UniTask.Delay(
                    TimeSpan.FromSeconds(0.5f),
                    cancellationToken: destroyCancellationToken);
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity MCP `read_console` — 에러 없는지 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Features/Battle/Controllers/BattleEffectController.cs
git commit -m "feat(battle): add BattleEffectController for sequential combat visuals"
```

---

## Task 6: BattleController — 전투 오케스트레이션

포커 이벤트 수신 → Entity 생성 → 턴 진행 → 연출 트리거 → 승패 판정.

**Files:**
- Create: `Assets/Scripts/Features/Battle/Controllers/BattleController.cs`

- [ ] **Step 1: BattleController 생성**

```csharp
// Assets/Scripts/Features/Battle/Controllers/BattleController.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FoldingFate.Core;
using FoldingFate.Features.Battle.Components;
using FoldingFate.Features.Battle.Data;
using FoldingFate.Features.Battle.Models;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Models;
using FoldingFate.Features.Entity.Systems;
using FoldingFate.Features.Poker.Events;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Battle.Controllers
{
    public class BattleController : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly EntityFactory _entityFactory;
        private readonly BattleSystem _battleSystem;
        private readonly TurnSystem _turnSystem;
        private readonly BattleEffectController _effectController;
        private readonly BattleCharacterData[] _characterDataList;
        private readonly BattleMonsterData _monsterData;
        private readonly CompositeDisposable _disposables = new();

        private BattleModel _battle;
        private bool _isBattleActive;
        private bool _isProcessingTurn;

        [Inject]
        public BattleController(
            EventBus eventBus,
            EntityFactory entityFactory,
            BattleSystem battleSystem,
            TurnSystem turnSystem,
            BattleEffectController effectController,
            BattleCharacterData[] characterDataList,
            BattleMonsterData monsterData)
        {
            _eventBus = eventBus;
            _entityFactory = entityFactory;
            _battleSystem = battleSystem;
            _turnSystem = turnSystem;
            _effectController = effectController;
            _characterDataList = characterDataList;
            _monsterData = monsterData;
        }

        public void Start()
        {
            InitializeBattle();

            _eventBus.Receive<HandSubmittedEvent>()
                .Subscribe(e => OnHandSubmitted(e).Forget())
                .AddTo(_disposables);
        }

        private void InitializeBattle()
        {
            // 캐릭터 Entity 생성
            var allies = new List<FoldingFate.Core.Entity>();
            for (int i = 0; i < _characterDataList.Length; i++)
            {
                var data = _characterDataList[i];
                var entity = _entityFactory.CreateCombatEntity(
                    $"ally_{i}", EntityType.Character, data.DisplayName,
                    data.MaxHp, data.Attack, data.Defense);
                allies.Add(entity);
            }

            // 몬스터 Entity 생성
            var enemies = new List<FoldingFate.Core.Entity>();
            var monsterEntity = _entityFactory.CreateCombatEntity(
                "enemy_0", EntityType.Monster, _monsterData.DisplayName,
                _monsterData.MaxHp, _monsterData.Attack, _monsterData.Defense);
            enemies.Add(monsterEntity);

            // 전투 시작
            _battle = _battleSystem.StartBattle(allies.AsReadOnly(), enemies.AsReadOnly());
            _isBattleActive = true;
        }

        private async UniTask OnHandSubmitted(HandSubmittedEvent e)
        {
            if (!_isBattleActive || _isProcessingTurn) return;
            _isProcessingTurn = true;

            try
            {
                // === 플레이어 턴 ===
                _turnSystem.StartTurn(_battle);

                // 캐릭터 4명 모두 몬스터 공격
                var playerActions = new List<BattleAction>();
                var targetEnemy = GetFirstAliveEnemy();
                if (targetEnemy == null) return;

                for (int i = 0; i < _battle.Allies.Count; i++)
                {
                    var ally = _battle.Allies[i];
                    if (ally.Get<Health>().IsAlive)
                    {
                        playerActions.Add(new BattleAction(ally, BattleActionType.Attack, targetEnemy));
                    }
                }

                // 3-Phase: Resolve → Apply
                _turnSystem.ExecuteTurn(_battle, playerActions.AsReadOnly());

                // 연출 재생
                var lastRecord = _battle.TurnHistory[_battle.TurnHistory.Count - 1];
                await _effectController.PlayTurnEffects(lastRecord.Results);

                // 승패 판정
                _turnSystem.EndTurn(_battle);

                if (_battle.Phase.CurrentValue == BattlePhase.Victory)
                {
                    await _effectController.PlayVictory(_battle.Allies);
                    EndBattle();
                    return;
                }

                // === 적 턴 ===
                _battle.Phase.Value = BattlePhase.EnemyTurn;

                var enemyActions = new List<BattleAction>();
                for (int i = 0; i < _battle.Enemies.Count; i++)
                {
                    var enemy = _battle.Enemies[i];
                    if (enemy.Get<Health>().IsAlive)
                    {
                        var targetAlly = GetRandomAliveAlly();
                        if (targetAlly != null)
                        {
                            enemyActions.Add(new BattleAction(enemy, BattleActionType.Attack, targetAlly));
                        }
                    }
                }

                if (enemyActions.Count > 0)
                {
                    _turnSystem.ExecuteTurn(_battle, enemyActions.AsReadOnly());
                    var enemyRecord = _battle.TurnHistory[_battle.TurnHistory.Count - 1];
                    await _effectController.PlayTurnEffects(enemyRecord.Results);
                }

                // 다시 승패 판정
                _turnSystem.EndTurn(_battle);

                if (_battle.Phase.CurrentValue == BattlePhase.Defeat)
                {
                    EndBattle();
                }
            }
            finally
            {
                _isProcessingTurn = false;
            }
        }

        private FoldingFate.Core.Entity GetFirstAliveEnemy()
        {
            for (int i = 0; i < _battle.Enemies.Count; i++)
            {
                if (_battle.Enemies[i].Get<Health>().IsAlive)
                    return _battle.Enemies[i];
            }
            return null;
        }

        private FoldingFate.Core.Entity GetRandomAliveAlly()
        {
            var alive = new List<FoldingFate.Core.Entity>();
            for (int i = 0; i < _battle.Allies.Count; i++)
            {
                if (_battle.Allies[i].Get<Health>().IsAlive)
                    alive.Add(_battle.Allies[i]);
            }
            if (alive.Count == 0) return null;
            return alive[UnityEngine.Random.Range(0, alive.Count)];
        }

        private void EndBattle()
        {
            _isBattleActive = false;
            _battleSystem.EndBattle(_battle);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity MCP `read_console` — 에러 없는지 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Features/Battle/Controllers/BattleController.cs
git commit -m "feat(battle): add BattleController for turn orchestration via EventBus"
```

---

## Task 7: BattleSceneInstaller — VContainer DI 구성

전투 씬의 LifetimeScope.

**Files:**
- Create: `Assets/Scripts/Features/Battle/BattleSceneInstaller.cs`

- [ ] **Step 1: BattleSceneInstaller 생성**

```csharp
// Assets/Scripts/Features/Battle/BattleSceneInstaller.cs
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Battle.Controllers;
using FoldingFate.Features.Battle.Data;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Systems;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Battle
{
    public class BattleSceneInstaller : LifetimeScope
    {
        [SerializeField] private BattleCharacterData[] _characterDataList;
        [SerializeField] private BattleMonsterData _monsterData;

        protected override void Configure(IContainerBuilder builder)
        {
            // Infrastructure
            builder.Register<EventBus>(Lifetime.Singleton);

            // Entity Systems
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
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity MCP `read_console` — 에러 없는지 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Features/Battle/BattleSceneInstaller.cs
git commit -m "feat(battle): add BattleSceneInstaller for VContainer DI"
```

---

## Task 8: Mixamo 애니메이션 Import + AnimatorController (Unity 에디터 작업)

Unity 에디터에서 수동으로 수행해야 하는 작업. Unity MCP를 활용하여 가능한 부분은 자동화.

- [ ] **Step 1: Mixamo FBX Import 설정 — Humanoid Rig**

Unity MCP `manage_asset`으로 각 FBX의 Rig 설정을 Humanoid으로 변경:

대상 파일:
- `Assets/ThirdParty/Mixamo/FBXs/Idle.fbx`
- `Assets/ThirdParty/Mixamo/FBXs/Attack.fbx`
- `Assets/ThirdParty/Mixamo/FBXs/Hit.fbx`
- `Assets/ThirdParty/Mixamo/FBXs/Death.fbx`
- `Assets/ThirdParty/Mixamo/FBXs/Victory.fbx`

각 FBX에 대해:
1. Import Settings → Rig → Animation Type: **Humanoid**
2. Import Settings → Animation → 클립 이름 정리
3. Loop Time: Idle ✓, Victory ✓, 나머지 ✗

- [ ] **Step 2: BattleAnimator Controller 생성**

Unity MCP `manage_animation`으로 AnimatorController 생성:
- Path: `Assets/ThirdParty/Mixamo/BattleAnimator.controller`
- Default State: Idle (Loop)
- Parameters: `Attack` (Trigger), `Hit` (Trigger), `Death` (Trigger), `Victory` (Trigger)
- Transitions:
  - Idle → Attack (Trigger: Attack) → Idle
  - Any State → Hit (Trigger: Hit) → Idle
  - Any State → Death (Trigger: Death) → Death (stay, no exit)
  - Any State → Victory (Trigger: Victory) → Victory (Loop)

- [ ] **Step 3: 커밋**

```bash
git add Assets/ThirdParty/Mixamo/
git commit -m "feat(animation): configure Mixamo Humanoid animations and BattleAnimator controller"
```

---

## Task 9: 전투 씬 구성 (Unity 에디터 작업)

Unity 에디터에서 BattleScene을 구성하고, 프리팹 배치, 컴포넌트 할당.

- [ ] **Step 1: BattleScene 생성**

Unity MCP `manage_scene`으로 새 씬 생성: `Assets/Scenes/BattleScene.unity`

기본 요소:
- Main Camera (위치: 약간 위에서 내려다보는 사이드뷰)
- Directional Light

- [ ] **Step 2: 캐릭터 프리팹 4개 배치 (좌측)**

IdaFaber 프리팹을 씬에 배치:
- Ally_0: `SK_CHENGYI_01 Variant.prefab` — Position(-3, 0, 2)
- Ally_1: `SK_JUA_LEE Variant.prefab` — Position(-3, 0, 0.7)
- Ally_2: `SK_KitsuneYuna Variant.prefab` — Position(-3, 0, -0.7)
- Ally_3: `SK_SAMURAIGIRL_01 Variant.prefab` — Position(-3, 0, -2)

각 캐릭터에:
- `EntityView` 컴포넌트 추가
- `BattleAnimator` Controller를 Animator에 할당
- 자식으로 World Space Canvas + HP바 Image 추가 → `WorldHpBar` 컴포넌트

- [ ] **Step 3: 몬스터 프리팹 1개 배치 (우측)**

- Enemy_0: `SK_ShinobiGirl_inMask.prefab` — Position(3, 0, 0)

동일하게 EntityView, Animator, WorldHpBar 설정.

- [ ] **Step 4: BattleSceneInstaller 배치**

빈 GameObject에 `BattleSceneInstaller` 컴포넌트 추가.
Inspector에서:
- `_characterDataList`: 4개 CharacterData SO 할당
- `_monsterData`: MonsterData SO 할당

- [ ] **Step 5: BattleEffectController 배치**

빈 GameObject에 `BattleEffectController` 컴포넌트 추가.

- [ ] **Step 6: ScriptableObject 에셋 생성**

Unity MCP 또는 에디터에서 SO 에셋 생성:
- `Assets/Data/Battle/Character_ChengYi.asset` — DisplayName: "ChengYi", MaxHp: 100, Attack: 15, Defense: 5, Prefab: ChengYi 프리팹
- `Assets/Data/Battle/Character_JuaLee.asset` — DisplayName: "Jua Lee", MaxHp: 90, Attack: 18, Defense: 4, Prefab: JuaLee 프리팹
- `Assets/Data/Battle/Character_KitsuneYuna.asset` — DisplayName: "Kitsune Yuna", MaxHp: 110, Attack: 12, Defense: 7, Prefab: KitsuneYuna 프리팹
- `Assets/Data/Battle/Character_SamuraiGirl.asset` — DisplayName: "Samurai Girl", MaxHp: 95, Attack: 16, Defense: 6, Prefab: SamuraiGirl 프리팹
- `Assets/Data/Battle/Monster_ShinobiGirl.asset` — DisplayName: "Shinobi Girl", MaxHp: 200, Attack: 12, Defense: 3, Prefab: ShinobiGirl 프리팹

- [ ] **Step 7: 포커 UI 배치**

기존 PokerInstaller + PokerView를 BattleScene에 추가하여 화면 하단에 포커 UI 표시.
BattleSceneInstaller를 PokerInstaller의 parent로 설정하거나, 같은 씬에 두 Installer를 배치.

- [ ] **Step 8: BattleController에서 EntityView 바인딩 연결**

BattleController.InitializeBattle()에서 Entity 생성 후 씬의 EntityView들을 찾아서 바인딩하는 로직 추가. `BattleEffectController.RegisterEntityView()`를 호출.

구체적으로: BattleController에 EntityView[] 참조를 추가하거나, BattleSceneInstaller에서 EntityView들을 등록.

- [ ] **Step 9: Play 테스트**

Unity Editor에서 BattleScene을 열고 Play:
1. 포커 카드가 화면 하단에 표시되는지 확인
2. 카드 선택 → Submit
3. 캐릭터 4명이 순차적으로 몬스터를 공격하는 연출 확인
4. 몬스터가 캐릭터 중 하나를 반격하는 연출 확인
5. HP바가 감소하는지 확인
6. 몬스터 사망 시 Death 애니메이션 + 캐릭터 Victory 연출 확인

- [ ] **Step 10: 커밋**

```bash
git add Assets/Scenes/BattleScene.unity Assets/Data/
git commit -m "feat(battle): set up BattleScene with characters, monster, and poker UI"
```
