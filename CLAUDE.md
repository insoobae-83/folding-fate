# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**folding-fate** is a Unity 6 game project using the Universal Render Pipeline (URP). It is currently in early development — the project is a blank URP canvas with infrastructure in place but no game logic implemented yet.

- **Unity Version**: 6000.3.12f1 (Unity 6 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP) 17.3.0
- **License**: MIT

## Opening and Running

This project must be opened through the **Unity Editor** (version 6000.3.12f1):

1. Use Unity Hub to open the project folder
2. Open `Assets/Scenes/SampleScene.unity`
3. Press **Play** in the editor to run

There is no CLI build command — building is done via Unity's **File > Build Settings** menu. Target platforms configured: Standalone (PC/Mac/Linux), Android, iOS, WebGL, Dedicated Server.

## Key Packages

Defined in `Packages/manifest.json`:

| Package | Version | Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering |
| `com.unity.inputsystem` | 1.19.0 | New Input System |
| `com.unity.ai.navigation` | 2.0.11 | NavMesh AI navigation |
| `com.unity.timeline` | 1.8.11 | Cinematic sequencing |
| `com.unity.visualscripting` | 1.9.11 | Node-based scripting |
| `com.unity.test-framework` | 1.6.0 | Unit/integration tests |
| `jp.hadashikick.vcontainer` | 1.17.0 | Dependency Injection (VContainer) |
| `com.cysharp.unitask` | 2.5.10 | async/await for Unity |
| `com.cysharp.r3` | 1.3.0 | Reactive Extensions (ViewModel용) — NuGet R3 별도 설치 필요 |
| `com.github-glitchenzo.nugetforunity` | latest | NuGet 패키지 관리 (R3 의존성) |
| `com.coplaydev.unity-mcp` | latest | Claude Code ↔ Unity Editor MCP 연동 |

## Architecture

### Design Patterns & Principles

- **기본 패턴**: CBD (Component-Based Design)
- **폴더 구조**: Feature-based — 기능 단위로 관련 코드(Model, System, Component, UI)를 한 곳에 모음
- **데이터/로직 분리**: 불변 설정 데이터는 ScriptableObject, 런타임 게임 상태와 비즈니스 로직은 순수 C#
- **이벤트/반응형**: R3 (Reactive Extensions) 통일 — SO 이벤트 채널(Ryan Hipple) 사용하지 않음
- **게임플레이 월드**: CBD 기반 MonoBehaviour — MonoBehaviour는 View 또는 Controller 역할을 할 수 있으며, 역할이 명확하면 분리한다. Unity 엔진 기능(Collider 반응, Animator 구동 등)과 강결합되어 분리가 오히려 복잡성을 높이는 경우에만 하나의 MonoBehaviour가 두 역할을 겸한다. 비즈니스 로직과 게임 상태는 순수 C# 레이어에 위임
- **UI**: MVVM 패턴 철저히 적용 + Unity UI Toolkit (UXML/USS). ViewModel(순수 C#)과 View(UIDocument)를 명확히 분리
- **의존성 주입**: VContainer (`jp.hadashikick.vcontainer`)

### Folder Structure

```
Assets/Scripts/
  Core/                     # [asmdef: FoldingFate.Core]
    Interfaces/             # 공통 인터페이스 (IDisposable 래퍼, IService 등)
    Extensions/             # 확장 메서드
    Constants/              # 전역 상수
  Infrastructure/           # [asmdef: FoldingFate.Infrastructure]
    Audio/                  # AudioService
    SceneManagement/        # SceneLoader, SceneTransition
    Pooling/                # ObjectPool
    Save/                   # 저장/로드 시스템
    Input/                  # InputAction 래핑, 입력 추상화
    EventBus/               # R3 기반 글로벌 이벤트 버스
  Features/                 # [asmdef: FoldingFate.Features]
    Combat/                 # 전투 기능 예시
      Models/               # 게임 상태 모델 (순수 C#)
      Systems/              # 게임 로직 (순수 C#, VContainer ITickable/IStartable)
      Components/           # MonoBehaviour (View+Controller 통합)
      UI/
        ViewModels/         # ViewModel (순수 C#)
        Views/              # UIDocument 바인딩 컨트롤러
        Uxml/               # UXML 레이아웃
        Uss/                # USS 스타일시트
      Data/                 # ScriptableObject (불변 설정 데이터)
      CombatInstaller.cs    # VContainer LifetimeScope (피처별)
    Inventory/              # 같은 구조 반복
      ...
  Shared/                   # [asmdef: FoldingFate.Shared]
    Data/                   # 여러 피처가 공유하는 ScriptableObject
    UI/                     # 공통 UI 컴포넌트 (HealthBar, Tooltip 등)
      Uxml/
      Uss/
  Installers/               # [asmdef: FoldingFate.Installers]
    RootInstaller.cs        # 프로젝트 루트 LifetimeScope
    SceneInstaller.cs       # 씬 단위 LifetimeScope
Assets/Tests/
  EditMode/                 # [asmdef: FoldingFate.Tests.EditMode]
  PlayMode/                 # [asmdef: FoldingFate.Tests.PlayMode]
```

> **피처 폴더 규칙**: 모든 기능은 `Features/{FeatureName}/` 아래에 관련 코드를 모은다. 하위 폴더(Models, Systems, Components, UI, Data)는 해당 피처에 필요한 것만 생성한다 — 빈 폴더를 미리 만들지 않는다.

### Layer Responsibilities

| 레이어 | 타입 | 역할 |
|---|---|---|
| Core | 순수 C# | 인터페이스, 확장 메서드, 상수 — 다른 레이어에 의존하지 않음 |
| Infrastructure | 순수 C# + MonoBehaviour | 크로스커팅 서비스 (Audio, Scene, Pool, Save, EventBus) |
| Features/*/Models | 순수 C# | 피처별 게임 상태, 도메인 모델 |
| Features/*/Systems | 순수 C# | 피처별 게임 로직 (VContainer `ITickable`/`IStartable`로 생명주기 구동) |
| Features/*/Data | ScriptableObject | 피처별 디자이너 설정값, 불변 데이터 |
| Features/*/UI | ViewModel + View | 피처별 MVVM UI (ViewModel은 순수 C#, View는 UIDocument 컨트롤러) |
| Features/*/Components | MonoBehaviour | Unity 엔진 연동 — View 또는 Controller 역할 (역할이 명확하면 분리, 강결합 시 통합) |
| Shared | 혼합 | 여러 피처가 공유하는 데이터, UI 컴포넌트 |
| Installers | VContainer LifetimeScope | 루트/씬 단위 DI 바인딩 등록 |

> **게임플레이 월드**: MonoBehaviour는 View 또는 Controller 역할을 할 수 있으며, 역할이 명확하면 분리한다. Unity 엔진 기능(Collider 반응, Animator 구동 등)과 강결합되어 분리가 오히려 복잡성을 높이는 경우에만 하나의 MonoBehaviour가 두 역할을 겸한다. 비즈니스 로직과 게임 상태는 반드시 순수 C# 레이어(Models/Systems)에 둘 것.
>
> **UI**: ViewModel(순수 C#)과 View(UIDocument 컨트롤러)를 철저히 분리하는 MVVM을 적용한다.
>
> **Systems 생명주기**: `Gameplay/Systems`의 순수 C# 클래스는 VContainer의 `ITickable`(매 프레임), `IStartable`(초기화), `IAsyncStartable`(비동기 초기화)을 구현하여 MonoBehaviour 없이 생명주기를 갖는다.

### Assembly Definition 전략

의존 방향을 컴파일 타임에 강제하고, 증분 컴파일을 최적화하기 위해 asmdef를 레이어별로 분리한다.

```
의존 방향 (→ = 참조 가능):

Installers → Features, Infrastructure, Shared, Core
Features → Infrastructure, Shared, Core
Infrastructure → Core
Shared → Core
Tests.EditMode → Features, Infrastructure, Shared, Core
Tests.PlayMode → 전체

※ Features 간 직접 참조 금지 — 피처 간 통신은 Infrastructure/EventBus(R3)를 통해서만
※ Core는 어떤 레이어도 참조하지 않음 (최하위 의존성)
```

| Assembly Definition | 경로 | 참조 대상 |
|---|---|---|
| `FoldingFate.Core` | `Assets/Scripts/Core/` | (없음) |
| `FoldingFate.Infrastructure` | `Assets/Scripts/Infrastructure/` | Core |
| `FoldingFate.Shared` | `Assets/Scripts/Shared/` | Core |
| `FoldingFate.Features` | `Assets/Scripts/Features/` | Infrastructure, Shared, Core |
| `FoldingFate.Installers` | `Assets/Scripts/Installers/` | Features, Infrastructure, Shared, Core |
| `FoldingFate.Tests.EditMode` | `Assets/Tests/EditMode/` | Features, Infrastructure, Shared, Core |
| `FoldingFate.Tests.PlayMode` | `Assets/Tests/PlayMode/` | 전체 |

> 모든 asmdef에 `VContainer`, `UniTask`, `R3` 패키지 참조를 필요에 따라 추가한다. `Auto Referenced`는 false로 설정하여 불필요한 컴파일 의존을 방지한다.

### R3 (Reactive Extensions) 사용 기준

이벤트와 반응형 데이터 바인딩은 **R3로 통일**한다. ScriptableObject 이벤트 채널(Ryan Hipple 패턴)은 사용하지 않는다.

**사용 영역:**

| 용도 | R3 타입 | 예시 |
|---|---|---|
| 상태 변경 알림 | `ReactiveProperty<T>` | `ReactiveProperty<int> Hp` — Model이 소유, ViewModel/Component가 구독 |
| 일회성 이벤트 | `Observable` (Subject) | `Subject<DamageEvent>` — System이 발행, Component가 구독 |
| 글로벌 이벤트 버스 | `MessageBroker` (R3 기반) | 피처 간 통신 — `Infrastructure/EventBus/` |
| UI 바인딩 | `ReadOnlyReactiveProperty<T>` | ViewModel이 Model의 RP를 변환하여 View에 노출 |
| 컬렉션 변경 | `ObservableList<T>` | 인벤토리 아이템 목록 등 |

**구독 해제 규칙:**
- MonoBehaviour: `this.destroyCancellationToken` 또는 `AddTo(this)` 사용
- 순수 C# (VContainer 관리): `IDisposable`로 `CompositeDisposable`에 모아서 `Dispose()`
- ViewModel: `CompositeDisposable` 패턴, View가 해제 시 함께 Dispose

**피처 간 통신:**
- 피처 간 직접 참조 금지 — `Infrastructure/EventBus/`의 R3 기반 메시지 브로커를 통해서만 통신
- 이벤트 정의는 발행하는 피처의 `Models/` 또는 `Shared/`에 위치

### ScriptableObject 사용 기준

ScriptableObject는 **디자이너가 에디터에서 편집하는 불변 설정 데이터 전용**으로 사용한다.

**사용하는 경우:**
- 디자이너가 조정하는 설정값 (ItemData, EnemyData 등 Config/Tuning)
- enum 대신 타입 안전 상수 (ItemType.asset 등 에셋으로 참조)

**사용하지 않는 경우:**
- 이벤트 채널 — R3 Observable로 대체
- 공유 변수/런타임 상태 — R3 ReactiveProperty로 대체
- 플레이어 런타임 상태 (HP, 위치 등) — SO는 에디터에서 값이 유지되어 테스트 오염 위험
- 씬별로 다른 인스턴스가 필요한 데이터 — 모든 씬이 같은 에셋을 공유하므로 격리 불가
- 복잡한 비즈니스 로직 — 순수 C# 클래스가 테스트 가능성 높음
- 자주 생성/소멸되는 객체 — SO는 에셋이므로 런타임 동적 생성에 부적합

**이 프로젝트 기준:** SO는 `Features/*/Data/` 또는 `Shared/Data/`에 위치하며, 기획에서 정의하고 게임 중 변하지 않는 불변 데이터만 담는다. 플레이 중 변하는 상태는 순수 C# 모델 + R3 ReactiveProperty에 둘 것.

### Renderer Configuration

Two renderer assets exist for dual-platform targeting:
- `Assets/Settings/PC_Renderer.asset` — high-fidelity PC rendering
- `Assets/Settings/Mobile_Renderer.asset` — optimized mobile rendering

Post-processing is controlled via `Assets/Settings/DefaultVolumeProfile.asset`.

### Input System

Input is configured via `Assets/InputSystem_Actions.inputactions` using the new Unity Input System (not the legacy `Input` class). New input bindings should be added to this asset.

### Testing

테스트 코드 작성 및 유닛 테스트 수행은 이 프로젝트의 기본 개발 방식입니다.

- 새 기능 구현 시 테스트 코드를 함께 작성할 것
- 순수 C# 클래스 (Gameplay/Models, Systems 등)는 EditMode 테스트로 Unity 없이 단위 테스트 가능
- MonoBehaviour 관련 통합 테스트는 PlayMode 테스트 사용
- 테스트는 `Assets/Tests/EditMode/` 또는 `Assets/Tests/PlayMode/` 에 위치
- 실행: **Window > General > Test Runner** in the Unity Editor

## Future Considerations

게임 설계가 구체화되면 논의할 항목들:

- [ ] **피처별 asmdef 분리** — 현재 `FoldingFate.Features` 단일 어셈블리로는 피처 간 직접 참조를 컴파일 타임에 차단할 수 없음. 피처가 5개 이상이고 **팀 규모가 커져서 피처 오너십 분리가 필요할 때** `FoldingFate.Features.Combat`, `FoldingFate.Features.Inventory` 등으로 분리 검토. 소규모 팀에서는 `FoldingFate.Features` 단일 asmdef 유지가 실용적
- [ ] **게임 상태(State) 관리 패턴** — Menu → Loading → Gameplay → Pause → GameOver 등 앱 레벨 상태 전환 전략. VContainer `LifetimeScope` 계층과 연동하는 State Machine 또는 씬 기반 상태 관리
- [ ] **씬 전략** — 단일 씬 vs 멀티 씬(Additive Loading) 결정, Addressables 도입 여부
- [ ] **로깅/디버그 전략** — `Debug.Log` 래핑, 조건부 로깅, 릴리즈 빌드 시 로그 스트리핑

## Code Conventions

- **C# 표준 코딩 컨벤션**을 따른다 ([Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions))
- All game scripts are C# placed under `Assets/`
- Scripts are organized in subdirectories by feature/system
- `Assets/TutorialInfo/` contains editor-only utility scripts (not game logic)
- The project uses **Linear color space** (set in Player Settings)
