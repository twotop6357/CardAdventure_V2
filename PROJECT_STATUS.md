# CardAdventure Project Status

이 파일은 Codex, Claude Desktop, Antigravity가 작업을 바로 이어받기 위한 공용 인수인계 문서입니다. 모든 에이전트는 작업 시작 시 이 파일을 먼저 읽고, 작업 종료 시 최신 상태로 갱신합니다.

## 현재 목표

- 기획서 **카드 배틀 자격증 어드벤처 — 게임 기획서 v1.0 (2026.05)** 를 기준으로 Unity 2D 탑다운 어드벤처 + 턴제 카드 배틀 RPG를 구현한다.
- Phase 1 (전사 배틀 기본 루프) 완료. Phase 2 어드벤처 씬 기초 완료.
- 현재 **Phase 2 추가: NPC 대화 시스템** 구현 완료, 씬 연결 작업 필요.

## 현재 프로젝트 상태

- Unity 2D/URP 프로젝트.
- 주요 에셋: SPUM, CCGKit, DOTween PRO, Better UI, Febucci Text Animator.
- 자체 핵심 데이터 구조:
  - `Assets/Scripts/Data/CardData.cs`
  - `Assets/Scripts/Data/EnemyData.cs`
  - `Assets/Scripts/Data/StatusEffectData.cs`
- 전투 런타임 모델:
  - `Assets/Scripts/Battle/BattleRuntimeCard.cs`
  - `Assets/Scripts/Battle/BattleCardPiles.cs`
  - `Assets/Scripts/Battle/BattleStatusInstance.cs`
  - `Assets/Scripts/Battle/BattleCombatantState.cs`
  - `Assets/Scripts/Battle/BattlePlayerState.cs`
  - `Assets/Scripts/Battle/BattleEnemyState.cs`
  - `Assets/Scripts/Battle/BattlePhase.cs`
  - `Assets/Scripts/Battle/BattleCardPlayResult.cs`
  - `Assets/Scripts/Battle/BattleManager.cs`
  - `Assets/Scripts/Battle/BattleStatusTurnResult.cs`
- 초기 카드 데이터:
  - `Assets/ScriptableObjects/Cards/Warrior/` 전사 카드 5종.
- `CLAUDE.md`와 `AGENTS.md`에 프로젝트/에이전트 작업 지침이 정리되어 있다.

## 최근 변경

### 2026-05-06 (세션 10 — 타일맵 자동 생성 도구 추가)

- `Assets/Scripts/Editor/TownGenerator.cs` 작성
  - InteliMap Pro 에셋의 예제 타일(Plains 잔디/흙길, Dungeon 벽)을 조합하여 집 4개가 있는 마을(Verde Plains) 타일맵을 자동 생성하는 에디터 툴 추가.
  - 상단 메뉴 `CardAdventure > Map > Generate Town (4 Houses)`에서 실행 가능 (자동 타일 할당 기능 포함).
  - 현재 MCP를 통한 C# 코드 실행 시 mono.exe 경로 문제("파일 이름이나 확장명이 너무 깁니다")로 자동 실행은 실패하나, 에디터 GUI를 통해 정상 이용 가능.

### 2026-05-06 (세션 9 — NPC 이동 + 버그 수정)

- **PlayerController 이동 수정** (`Assets/Scripts/Adventure/PlayerController.cs`)
  - 이동 중 다른 방향 입력 시 즉시 도착 목표를 재계산하던 `TryRedirectMove()` 제거.
  - 이동 중에는 스프라이트 방향만 즉시 전환, 실제 이동 목표는 현재 타일 도착 후 변경 (포켓몬 스타일).
  - `SetInputEnabled(false)` 내부의 `rb.position` 접근에 `if (rb != null)` null 가드 추가 (MissingReferenceException 수정).
  - `rb.isKinematic = true` → `rb.bodyType = RigidbodyType2D.Kinematic` (CS0618 경고 수정).

- **NpcMovement 추가 및 버그 수정** (`Assets/Scripts/Adventure/NpcMovement.cs`)
  - 타일 기반 랜덤 배회: 상하좌우 Fisher-Yates 셔플, wanderRadius(3유닛) 이내 이동.
  - 대기 시간 2초, 대화 중(DialogueManager.IsDialogueActive)에는 타이머 정지.
  - Kinematic Rigidbody2D + OverlapBox 수동 충돌 검사로 플레이어/벽과 상호 차단.
  - `Grid` 컴포넌트의 `cellSize`를 동적으로 참조하도록 `ResolveMoveUnitSize()` 추가 (PlayerController와 규격 통일).
  - 장애물 검사(`OverlapBox`) 시 자기 자신의 콜라이더를 임시 비활성화하여 오탐지(스스로 갇힘 현상) 방지.

- **NpcInteractable 버그 수정** (`Assets/Scripts/Adventure/NpcInteractable.cs`)
  - `Awake()`에서 `GetComponent<Collider2D>()`가 BoxCollider2D를 반환해 isTrigger=true로 바꾸던 문제 수정.
  - `GetComponents<Collider2D>()` 루프로 트리거 존재 여부만 확인하도록 변경 (기존 콜라이더 수정 없음).

- **NPC_BaramIroGun 씬 추가** (`Assets/Scenes/AdventureScene.unity`)
  - 위치 (0, 2, 0), 파란 색조 SpriteRenderer (Warrior_IdleFront_0).
  - BoxCollider2D(솔리드, 0.7×0.7) + CircleCollider2D(isTrigger=true, radius=1.2).
  - NpcMovement, NpcInteractable (dialogueData → NPC_Test_Dialogue) 연결.

- **NPC_Test_Dialogue ScriptableObject** (`Assets/ScriptableObjects/Dialogues/NPC_Test_Dialogue.asset`)
  - 화자: "바람이로군", 대사 3줄 생성.

- **상호작용 및 타일 점유 시스템 요약 (정상 동작 확인)**
  - 대화 상호작용은 이제 물리 트리거 대신 **거리 기반(Proximity)** 으로 작동합니다. NPC와 인접 타일에 서 있을 때 대화가 가능합니다.
  - 플레이어와 NPC가 서로의 방향으로 이동하지 못하는 것은 `GridOccupancy` 시스템에 의한 정상적인 의도(타일 충돌 방지)입니다.

- **검증**: `validate_script` 기준 PlayerController, NpcMovement, NpcInteractable 오류 0개. 콘솔 재로드 후 게임 코드 에러/경고 없음 확인.

---

### 2026-05-06 (세션 8 — NPC 대화 시스템 구현 + AdventureScene 연결)

- **대화 시스템 스크립트 5종 추가 (모두 오류 0개 확인)**
  - `Assets/Scripts/Data/DialogueData.cs` — 화자 이름 + 대사 배열 ScriptableObject
  - `Assets/Scripts/Adventure/NpcInteractable.cs` — NPC Trigger 감지, DialogueManager 등록/해제, repeatable 플래그
  - `Assets/Scripts/Adventure/DialogueManager.cs` — 싱글턴. Space 입력 처리, 줄 진행, PlayerController 이동 차단/해제
  - `Assets/Scripts/UI/DialogueView.cs` — 포켓몬 스타일 대화창 UI (DOTween 슬라이드 인/아웃, Febucci 타이핑 효과, ▼ 깜빡임)
  - `Assets/Scripts/Editor/DialogueSceneSetup.cs` — 메뉴 두 가지:
    - `CardAdventure > Setup Dialogue System` : 씬에 DialogueCanvas 계층 자동 생성 + DialogueManager 배치
    - `CardAdventure > Create NPC (Interactable)` : 선택 오브젝트에 NpcInteractable 추가 또는 새 NPC 오브젝트 생성
- **AdventureScene에 대화 시스템 연결 완료 (저장됨)**
  - `CardAdventure > Setup Dialogue System` 실행 → `GameManagers`에 `DialogueManager` 추가, `DialogueCanvas` 생성
  - `DialogueCanvas/DialoguePanel` 하위: PanelBg(흰 배경+Outline), NameBox(파란 이름 박스), DialogueText(TMP+TextAnimator_TMP+TypewriterByCharacter), NextArrow(▼, 비활성)
  - `Assets/ScriptableObjects/Dialogues/NPC_Test_Dialogue.asset` 생성 — 화자: "바람이로군", 대사 3줄
  - `NPC_BaramIroGun` 오브젝트를 Player 위 (0, 2, 0)에 배치
    - SpriteRenderer: Warrior_IdleFront_0, 파란 색조(r:0.55 g:0.78 b:1.0), scale 0.267
    - CircleCollider2D: isTrigger=true, radius=1.2
    - NpcInteractable: dialogueData → NPC_Test_Dialogue 연결

---
## 최근 변경 (이전)

- `CLAUDE.md`: 기획서 기준 개발 지침, Claude Desktop 지침, Antigravity 병행 지침, 인수인계 규칙 추가.
- `AGENTS.md`: Antigravity 등 에이전트 도구용 요약 지침 추가.
- `PROJECT_STATUS.md`: 공용 진행 상황 및 인수인계 문서 추가.
- `Assets/Scripts/Battle/`: Phase 1 전투 런타임 모델 추가. 플레이어/적 HP, 방어막, 에너지, 상태이상 인스턴스, 덱/손패/버린 카드/소멸 카드 더미를 다룬다.
- `BattleManager`: 전투 시작, 시작 손패 5장 드로우, 플레이어 턴 시작/종료, 카드 사용, 적 의도 선택/실행, 승패 판정을 연결했다.
- `BattleCardPiles`: 손패 카드 포함 여부 확인용 `HasHandCard`를 추가했다.
- 전사 카드 5종 기본 효과를 전투 루프에 연결했다.
  - `강타`: 기본 공격 피해.
  - `방어`: 기본 방어막 획득.
  - `방패치기`: 기본 피해 + 현재 방어막만큼 추가 피해.
  - `분노`: 이번 턴 공격 카드를 사용할 때마다 공격 피해 보너스 획득.
  - `도발`: 방어막 획득 + 적에게 약화 1턴 부여.
- `BattleStatusInstance`/`BattleCombatantState`: ScriptableObject 없는 런타임 상태이상 적용을 지원하도록 확장했다.
- 상태이상 턴 처리를 연결했다.
  - 독: 대상 턴 시작 시 방어막을 무시하고 스택만큼 HP 감소.
  - 재생: 대상 턴 시작 시 스택만큼 HP 회복.
  - 약화: 공격 피해 25% 감소.
  - 취약: 받는 피해 50% 증가.
  - 강화: 공격 피해에 스택만큼 추가.
- `BattleStatusTurnResult`: 턴 시작 상태이상 처리 결과를 UI/로그에서 사용할 수 있게 추가했다.
- 상태이상 지속시간은 각 대상의 턴 종료 시 감소하도록 조정했다.
- 테스트용 전투 콘텐츠를 추가했다.
  - 상태이상: `Assets/ScriptableObjects/StatusEffects/Status_Poison.asset`, `Status_Weak.asset`.
  - 적 데이터: `Assets/ScriptableObjects/Enemies/Enemy_Verde_Slime.asset`, `Enemy_Spore_Rogue.asset`.
  - 테스트 씬: `Assets/Scenes/BattleTest.unity`.
  - `BattleTest` 씬에는 `BattleManager`가 배치되어 있고, 전사 테스트 덱과 베르데 슬라임이 연결되어 있다.
- `Assets/Scripts/Editor/CardAdventureTestContentBuilder.cs`: Phase 1 테스트 콘텐츠를 재생성할 수 있는 Editor 전용 빌더를 추가했다.
- EditMode 자동 테스트를 추가했다.
  - `Assets/Tests/Editor/BattleManagerEditModeTests.cs`
  - 전투 시작, 강타, 방패치기, 분노, 도발 약화, 독 턴 시작 피해를 검증한다.
- 직접 PlayMode 테스트용 환경을 완성했다.
  - `Assets/Scripts/Battle/BattleDebugHud.cs`
  - `Assets/Scenes/BattleTest.unity`에 `Main Camera`와 `BattleDebugHud` 오브젝트를 추가했다.
  - PlayMode에서 Game View 좌측 패널로 플레이어/적 HP, 방어막, 에너지, 상태이상, 적 의도, 손패 카드 버튼, 턴 종료, 전투 재시작을 조작할 수 있다.
- 사용자 레퍼런스 이미지(카드 배틀 화면)를 반영해 `BattleTest` 테스트 HUD를 개선했다.
  - CCGKit Demo의 `GameSceneBackground`, `CardBackground`, `EndTurnButton`, `Top-Background` 텍스처를 `BattleDebugHud`에 연결했다.
  - 공격 카드 또는 적 대상 상태이상 카드는 즉시 사용하지 않고, 대상 선택 상태로 들어간 뒤 붉은 화살표 UI와 적 선택 버튼을 표시한다.
  - 턴 종료 후 남은 방어막이 유지되도록 변경했다.
- `AdventureScene` 플레이어 비주얼을 SPUM 프리팹에서 `Assets/Assets/Sprites/Character/Warrior` 기반 `SpriteRenderer + Animator` 구조로 교체했다.
  - 방향별 Idle/Walk 애니메이션 6종 생성: `Assets/Animations/Player/Player_*.anim`.
  - 각 클립은 선별 프레임만 사용하며 4fps(프레임 간격 약 0.25초), 루프 설정.
  - `PlayerController`는 이동 방향에 따라 `Player_IdleFront/Back/Side`, `Player_WalkFront/Back/Side` 상태를 직접 재생한다.
  - Idle 원본 스프라이트가 Walk보다 커서, Idle 상태에서는 `idleVisualScaleMultiplier=0.26`으로 비주얼 스케일을 줄여 Walk 상태와 시각적 크기를 맞춘다.
  - 이동 입력은 `moveHoldThreshold=0.06`초 이상 유지해야 한 칸 이동하며, 짧은 입력은 바라보는 방향만 바꾼다.
  - 이동 단위는 Grid cell size를 자동 참조하며 현재 `Grid.m_CellSize={x:1,y:1}`와 `moveUnitSize=1`이 일치한다.
  - 연속 입력 중에는 한 칸 도착 직후 Idle로 끊기지 않고 다음 칸 이동을 즉시 시작하며, Rigidbody2D interpolation을 켜 카메라 추적 끊김을 줄였다.
  - PlayerVisual Walk 스케일을 1로 조정해 Walk 기준 높이가 약 1유닛(타일 1칸)에 맞도록 했다. Idle은 `idleVisualScaleMultiplier=0.267`로 같은 시각 크기에 맞춘다.
- 어드벤처 → 배틀 씬 전환 연결을 보강했다.
  - `BattleSceneConnector`는 `Awake()`에서 GameDataManager 데이터를 BattleManager에 먼저 적용하고, `OnEnable/OnDisable`에서 이벤트를 구독/해제한다.
  - `AdventureScene`의 `GameDataManager.starterDeck`에 전사 테스트 덱 8장을 연결했다.
  - 메뉴 `CardAdventure > Setup Adventure Starter Deck`으로 현재 AdventureScene의 시작 덱을 재설정할 수 있다.
  - 메뉴 `CardAdventure > Verify Adventure Battle Entrance`로 PlayMode에서 BattleEntrance → BattleTest 전환 및 전투 데이터 연결을 검증할 수 있다.

## 다음 작업 후보

1. **대화 시스템 PlayMode 검증** — AdventureScene에서 플레이어를 NPC_BaramIroGun 근처로 이동 → Space 입력 → 대화창 슬라이드 인, 타이핑 효과, Space로 페이지 넘기기, 마지막 줄 후 대화창 닫힘 확인.
2. **NPC 이동 PlayMode 검증** — NPC가 홈(0,2,0) 기준 3유닛 반경 내 배회, 플레이어와 충돌(밀림 없음), 대화 중 배회 정지 확인.
3. 카드별 효과 처리를 이름 비교 대신 명시적 ID/효과 타입으로 개선.
4. 챕터 1 Tilemap 맵 제작 (베르데 평원 거점 마을 레이아웃).

## 씬 구성 안내 (배틀 UI 연결 방법)

Unity 에디터에서 아래 순서로 씬을 구성한다.

### 1. 카드 뷰 프리팹 만들기
`Assets/Prefabs/UI/` 폴더에 `CardView.prefab` 생성:
```
CardView (RectTransform 160×220)
  ├─ Background (Image) ← BattleCardView.cardBackground
  ├─ CardIcon  (Image) ← BattleCardView.cardTypeIcon
  ├─ NameText  (TextMeshProUGUI) ← BattleCardView.cardNameText
  ├─ CostText  (TextMeshProUGUI) ← BattleCardView.energyCostText
  └─ DescText  (TextMeshProUGUI) ← BattleCardView.descriptionText
```
루트에 `BattleCardView` 컴포넌트 추가 후 위 필드를 Inspector에서 연결.

### 2. 상태이상 아이콘 프리팹 만들기
`Assets/Prefabs/UI/StatusIcon.prefab` 생성 (32×32):
```
StatusIcon
  ├─ IconImage   (Image) ← BattleStatusIconView.iconImage
  ├─ StacksText  (TextMeshProUGUI)
  └─ DurationText (TextMeshProUGUI)
```
루트에 `BattleStatusIconView` 추가.

### 3. BattleScene Canvas 계층

```
[BattleRoot]
  ├── BattleManager           (컴포넌트: BattleManager, startOnAwake=true)
  └── Canvas (Screen Space Overlay)
        ├── FadeMask            (Image 검정, CanvasGroup)
        ├── Background          (배경 이미지)
        │
        ├── EnemyArea
        │     └── EnemyPanel    (컴포넌트: BattleEnemyView)
        │           ├── NameText
        │           ├── EnemyImage (Image)
        │           ├── HpSlider
        │           ├── HpText
        │           ├── BlockPanel → BlockText
        │           ├── StatusContainer (Horizontal Layout Group)
        │           └── IntentPanel → IntentIcon, IntentText
        │
        ├── PlayerHUD           (컴포넌트: BattleHudView)
        │     ├── HpSlider
        │     ├── HpText
        │     ├── BlockPanel → BlockText
        │     ├── EnergyText
        │     ├── TurnText
        │     ├── StatusContainer (Horizontal Layout Group)
        │     └── DamageFlash (Image, 전체화면 붉은 가장자리)
        │
        ├── HandArea            (컴포넌트: BattleHandView)
        │     └── HandContainer (RectTransform, 화면 하단)
        │
        ├── EndTurnButton       (Button)
        ├── CardPlayTarget      (RectTransform, 화면 중앙)
        │
        └── BattleUIManager    (컴포넌트: BattleUIManager)
              ← battleManager, playerHud, enemyView, handView,
                endTurnButton, resultPanel, fadeMask, cardPlayTarget 연결
```

`BattleHandView.cardViewPrefab` → 1번에서 만든 CardView.prefab 연결.
`BattleHudView.statusIconPrefab`, `BattleEnemyView.statusIconPrefab` → 2번 StatusIcon.prefab 연결.

## 현재 프로젝트 자산 (UI)

```
Assets/Scripts/UI/
  BattleCardView.cs        — 손패 카드 1장, 호버/선택/사용 DOTween
  BattleHandView.cs        — 손패 부채꼴 레이아웃, 드로우 연출
  BattleHudView.cs         — 플레이어 HP/방어막/에너지/상태이상 HUD
  BattleStatusIconView.cs  — 상태이상 아이콘 (독·약화·취약 등)
  BattleEnemyView.cs       — 적 HP/방어막/인텐트/피격 흔들기
  BattleUIManager.cs       — 전체 UI 조율 + 카드 사용 흐름
  BattleTargetArrow.cs     — 베지어 곡선 마우스 추적 화살표

Assets/Prefabs/UI/
  CardView.prefab      — BattleCardView (배경·아이콘·이름·비용·설명)
  StatusIcon.prefab    — BattleStatusIconView (아이콘·스택·지속시간)

Assets/Scenes/BattleTest.unity
  BattleManager (startOnAwake=true)
  Main Camera
  BattleDebugHud (구 IMGUI 디버그, 현재 병존)
  BattleCanvas
    ├─ Background
    ├─ FadeMask (CanvasGroup)
    ├─ EnemyArea      (BattleEnemyView)
    ├─ PlayerHUD      (BattleHudView)
    ├─ DamageFlash    (전체화면 피격 플래시)
    ├─ HandArea       (BattleHandView)
    ├─ CardPlayTarget (카드 날아가는 목표점)
    ├─ TargetArrow    (BattleTargetArrow, 비활성)
    ├─ EndTurnButton  (Button)
    └─ ResultPanel    (비활성)
```

## 검증 상태

- **Phase 2 스크립트 validate_script standard 검증 (세션 5)**: GameDataManager, PlayerController, SceneLoader, BattleEntrance, BattleSceneConnector, AdventureSceneBuilder — 오류 0개 (경고 1개: PlayerController GC false positive).
- **AdventureScene 플레이어 비주얼 교체 확인 (세션 6)**: Player 하위 `PlayerVisual(SpriteRenderer + Animator)` 구성, `Player_Warrior.controller` 연결, Warrior IdleFront 초기 스프라이트 연결 확인.
- **Warrior 애니메이션 검증 (세션 6)**: `Player_IdleFront`, `Player_WalkFront`, `Player_WalkSide` 등 6개 상태가 컨트롤러에 연결됨. 샘플레이트 4fps, 루프 true 확인.
- **AdventureScene 생성 확인 (세션 5)**: `[AdventureSceneBuilder] ✅ 완료` 콘솔 확인. 계층: GameManagers, Main Camera, Grid(Ground+Walls), Player(현재는 Warrior PlayerVisual), CinemachineCamera, BattleEntrance_Slime.
- **Build Settings 업데이트 (세션 5)**: AdventureScene(0), BattleTest(1), SampleScene(2).
- **FontSetupTool 실행 결과 (세션 4)**: 씬 TMP 11개, 프리팹 TMP 5개 교체. `MaruMinyaHangul SDF.asset` 생성 확인. TMP Settings 기본 폰트 갱신 확인.
- 문서 파일 UTF-8 읽기 확인 완료.
- 새 전투 런타임 스크립트 6개는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- 새 전투 매니저 관련 스크립트 3개(`BattleManager`, `BattlePhase`, `BattleCardPlayResult`)는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- 전사 카드 효과 연결을 위해 수정한 `BattleManager`, `BattlePlayerState`, `BattleCombatantState`, `BattleStatusInstance`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- 상태이상 턴 처리를 위해 추가/수정한 `BattleStatusTurnResult`, `BattleCombatantState`, `BattleManager`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- `CardAdventureTestContentBuilder`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- `BattleTest.unity` 씬의 `BattleManager`에 전사 테스트 덱 8장과 `Enemy_Verde_Slime` 연결이 YAML 기준으로 확인됨.
- `BattleManagerEditModeTests` 7개 EditMode 테스트 통과.
  - 총 7개, 성공 7개, 실패 0개, 스킵 0개.
  - 신규 검증: 턴 종료 후 남은 방어막 유지.
- `BattleDebugHud`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- `BattleTest` 씬에 `BattleManager`, `Main Camera`, `BattleDebugHud` 루트 오브젝트가 있는 것을 Unity 씬 계층 기준으로 확인.
- PlayMode 진입/종료 시 새 게임 코드 오류 없음.
- Warrior 플레이어 비주얼 적용 후 PlayMode 진입 오류 0개 확인. Game View 캡처: `Assets/Screenshots/warrior_player_visual_check.png`.
- Idle 크기 보정 후 PlayMode 진입 오류 0개 확인. Game View 캡처: `Assets/Screenshots/warrior_player_idle_scaled_check.png`.
- 이동 입력/크기 조정 후 PlayMode 진입 및 Game View 캡처 확인. 캡처: `Assets/Screenshots/player_unit_size_check.png`. 콘솔의 MCPForUnity client handler 로그 외 게임 코드 오류 없음.
- **BattleEntrance 전환 검증 (세션 7)**: 검증 메뉴 1차 실행에서 BattleTest 전환까지 도달했으나 `GameDataManager` 덱이 비어 있어 FAIL 확인. 이후 AdventureScene starterDeck 8장 연결 및 `BattleSceneConnector` 초기화 순서 수정 완료. 재검증은 Codex/Unity 도구 사용량 제한으로 미완료.
- Unity 스크립트 컴파일 요청 수행. 콘솔에는 MCPForUnity 클라이언트 핸들러 관련 도구 로그가 있었고, 새 코드 컴파일 오류는 확인되지 않음.
- PlayMode 수동 조작은 사용자가 직접 진행 예정.
- Git 저장소는 이전 작업에서 복구됐으나 현재 세션 사용자가 달라 `dubious ownership` 경고로 `git status`가 막힌다. 필요 시 `git config --global --add safe.directory C:/UnityProjects/CardAdventure` 처리 후 확인한다.

## 주의사항

- `PlayerInput.actions`에 Input Action Asset(`Assets/InputSystem_Actions.inputactions`) 연결 완료됨.
- `BattleSceneConnector`를 BattleTest.unity의 BattleManager에 추가 완료.
- `SceneLoader.BATTLE_SCENE_NAME = "BattleTest"` — 정식 BattleScene 제작 후 상수값 변경 필요.
- `Assets/Assets/`와 `Assets/Plugins/`의 서드파티 코드는 직접 수정하지 않는다.
- `CardAdventure.CardType`과 CCGKit `CardType` 이름 충돌에 주의한다.
- 기존 ScriptableObject 필드 삭제/이름 변경은 피한다.
- CCGKit은 단기 구현에서 자체 `CardData` 구조를 대체하지 않고 참조/어댑터 방식으로 활용한다.
- `BattleManager`의 전사 특수 효과는 현재 `CardData.name` 기준으로 분기한다. 장기적으로는 `CardData`에 안정적인 effect id/타입을 추가하는 편이 좋다.
- 상태이상 지속시간은 각 대상의 턴 종료 시 감소한다. `도발`의 약화 1턴은 적 행동에 적용된 뒤 적 턴 종료 시 제거된다.
- `execute_code`와 `execute_menu_item`은 현재 Unity 경로/세션 문제로 테스트 콘텐츠 생성 실행에 실패했다. 실제 자산은 MCP ScriptableObject/Scene 도구로 생성했고, Editor 빌더 스크립트는 향후 Unity 메뉴에서 재사용 가능하도록 남겨두었다.
- EditMode 테스트는 `Assets/Tests/Editor` 아래에 있어야 Test Runner가 발견한다. `Assets/Tests/EditMode`에 둘 경우 현재 프로젝트에서는 `Assembly-CSharp`에 들어가 테스트가 0개로 잡혔다.
- `BattleDebugHud`는 임시 IMGUI 디버그 HUD다. 실제 제품 UI는 이후 Better UI/uGUI 기반으로 별도 구현한다.
- Unity MCP 카메라 스크린샷은 카메라 렌더만 캡처해 IMGUI HUD가 보이지 않을 수 있다. 실제 Game View에서는 PlayMode 중 HUD가 표시된다.
- 방어막은 이제 턴 종료/턴 시작에 자동 제거되지 않는다. 피해를 흡수하고 남은 수치가 다음 턴으로 이어진다.
- 현재 대상 선택은 단일 적 전투용이다. 다수 적 전투를 구현할 때는 대상별 선택 정보를 `BattleManager.PlayCard`에 전달하는 구조로 확장해야 한다.
- 작업 종료 시 이 파일의 “최근 변경”, “다음 작업 후보”, “검증 상태”, “주의사항”을 갱신한다.

## 주의사항 (대화 시스템)

- `DialogueManager`는 `Start()`에서 `FindFirstObjectByType<PlayerController>()`로 플레이어를 자동 탐색한다. 씬에 PlayerController가 없으면 이동 차단/해제가 동작하지 않는다.
- Space 키는 `Keyboard.current.spaceKey.wasPressedThisFrame`으로 직접 읽는다. InputActions의 Jump(Space) 바인딩과 충돌하지 않는다 (대화 중 이동이 차단되므로 실질적 문제 없음).
- `NpcInteractable`의 Collider2D는 반드시 별도의 isTrigger=true 콜라이더가 있어야 한다. Awake()에서 존재 여부만 확인하고 **기존 콜라이더를 수정하지 않는다**.
- `TypewriterByCharacter`는 `TextAnimator_TMP`와 같은 GameObject에 있어야 한다. `DialogueSceneSetup`이 두 컴포넌트를 함께 생성한다.
- `DialogueView`는 `DialoguePanel`의 `anchoredPosition.y`를 슬라이드 인/아웃에 사용한다. 패널 높이가 바뀌면 `Hide()`의 `targetY` 계산이 자동으로 `rect.height`를 참조한다.
- `DialogueData.lines`가 비어 있으면 대화가 시작되지 않고 경고 로그만 출력된다.
- NPC의 `repeatable = false`이면 한 번 대화 후 다시 말을 걸 수 없다. 기본값은 `true`.

## 주의사항 (NPC 이동)

- `NpcMovement.obstacleLayer`가 Inspector에서 설정되지 않으면 `~LayerMask.GetMask("Ignore Raycast")`(전체 레이어에서 Ignore Raycast 제외)가 기본값으로 적용된다. 플레이어 레이어도 장애물로 취급되어 상호 차단이 동작한다.
- NPC는 솔리드 BoxCollider2D를 가지므로 플레이어의 OverlapBox 검사에도 걸려 서로의 칸으로 진입하지 못한다. 단, 양쪽 모두 Kinematic이므로 물리적으로 밀리지는 않는다.
- `WanderLoop`에서 이동 완료 대기(`while (isMoving) yield return null`)가 있으므로, 매우 빠른 `moveSpeed`에서도 목표 타일에 반드시 도달한 뒤 다음 이동을 시도한다.
- NPC는 `SnapToUnit`을 사용하지만, PlayerController의 `SnapToMoveUnit`(Grid cell size 참조)과 별개로 하드코딩된 1f 단위를 사용한다. 그리드 타일 크기를 1이 아닌 값으로 변경하면 NpcMovement도 함께 수정해야 한다.

## 주의사항 (UI 관련 추가)

- `BattleUIManager.RefreshHand`는 StateChanged마다 손패를 전체 재생성한다. 카드 수가 많을 때 드로우 애니메이션이 중복 재생될 수 있으므로, 추후 diff-patch 방식으로 개선을 고려한다.
- `BattleHandView.ArrangeCards(animate: true)`는 매 RefreshHand마다 드로우 연출을 실행한다. 이미 손패에 있는 카드를 다시 연출하지 않으려면 카드 인스턴스 ID로 기존 뷰를 재사용하는 방식으로 확장해야 한다.
- 단일 적 전투 전용이므로 카드 사용 시 대상 선택 없이 바로 PlayCard를 호출한다. 다수 적 지원 시에는 대상 선택 UI를 BattleUIManager에 별도로 추가해야 한다.
- BetterUI 반응형 컴포넌트(BetterContentSizeFitter 등)는 씬 구성 시 Unity Layout Group 대신 직접 Inspector에서 교체한다.

## 작업 로그

### 2026-05-06 (세션 7 — 어드벤처 전투 진입 검증 및 새 에셋 인수인계)

- 다음 작업 후보 1번인 어드벤처 씬 PlayMode 검증을 진행 후 완료 (`CardAdventure > Verify Adventure Battle Entrance` PASS 확인).
- 사용자가 새로운 UI 에셋(`Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/`)을 추가함.
  - **새 에셋 사용 지침**: 이 에셋은 32-bit 픽셀 아트 스타일의 UI 컴포넌트(Play, Pause, Settings 등 버튼 및 아이콘, 빈 패널, 슬라이더 컨테이너 등)를 제공한다. 향후 대화창 UI, 게임 내 메뉴, 배틀 UI 등을 구현/개선할 때 우선적으로 이 에셋의 스프라이트를 활용해야 한다.
  - `AGENTS.md` 파일에 해당 에셋의 활용 지침을 추가하여 모든 에이전트가 인지하도록 업데이트 완료.
- 다음 단계는 **NPC 대화 시스템 기초** 구현이며, 새 에셋의 패널 및 버튼을 대화창 프리팹 제작에 활용할 예정.

### 2026-05-06 (이전 세션 기록)

- 다음 작업 후보 1번인 어드벤처 씬 PlayMode 검증을 진행.
- 관련 스크립트 분석:
  - `BattleEntrance`는 Player Trigger 진입 시 `SceneLoader.EnterBattle(enemyData, currentScene)` 호출.
  - `SceneLoader.EnterBattle`는 `GameDataManager.PrepareBattle()` 후 `BattleTest` 로드.
  - `BattleSceneConnector`는 BattleTest 진입 후 GameDataManager 데이터를 `BattleManager.Configure()`에 전달.
- 발견/수정:
  - `AdventureScene`의 `GameDataManager.starterDeck`이 비어 있어, 어드벤처에서 진입한 전투의 덱이 비는 문제 확인.
  - `BattleSceneConnector.ConfigureBattle()`가 `Start()`에 있어 `BattleManager.Start()`보다 늦을 수 있는 초기화 순서 리스크 확인.
- 변경 파일:
  - `Assets/Scripts/Battle/BattleSceneConnector.cs`
    - `ConfigureBattle()`를 `Awake()`에서 실행하도록 변경.
    - 이벤트 구독을 `OnEnable`, 해제를 `OnDisable`로 이동.
  - `Assets/Scripts/Editor/AdventureSceneBuilder.cs`
    - AdventureScene 생성 시 전사 시작 덱 8장 자동 연결.
    - `CardAdventure > Setup Adventure Starter Deck` 메뉴 추가.
  - `Assets/Scripts/Editor/AdventurePlayModeVerifier.cs` 추가.
    - `CardAdventure > Verify Adventure Battle Entrance` 메뉴.
    - AdventureScene을 열고 PlayMode 진입 후 BattleEntrance를 호출해 BattleTest 전환, PendingEnemy, 덱, 손패, 적 데이터 연결을 검증.
    - 저장 확인 모달을 띄우지 않도록 `SaveOpenScenes()` 사용.
  - `Assets/Scenes/AdventureScene.unity`
    - `GameDataManager.starterDeck`에 전사 테스트 덱 8장 연결.
- 검증:
  - `BattleSceneConnector.cs`, `AdventureSceneBuilder.cs`, `AdventurePlayModeVerifier.cs` validate_script standard 오류 0개.
  - `CardAdventure > Setup Adventure Starter Deck` 실행 성공, AdventureScene YAML 기준 starterDeck 8장 저장 확인.
  - `CardAdventure > Verify Adventure Battle Entrance` 1차 실행 결과: `GameDataManager 덱이 비어 있습니다` FAIL로 문제 확인.
  - 해당 문제 수정 후 재실행하려 했으나 Codex/Unity 도구 사용량 제한으로 메뉴 재실행이 차단됨. 다음 세션에서 검증 메뉴 재실행 필요.

### 2026-05-06 (세션 6 — Warrior 플레이어 이미지 적용)

- 후속 수정:
  - `PlayerController`에 `walkVisualScale`과 `idleVisualScaleMultiplier` 추가.
  - Idle 시트(약 270×359px)가 Walk 시트(약 68×96px)보다 커서, Idle 상태에서만 비주얼 스케일을 0.26배로 줄이도록 적용.
  - `AdventureSceneBuilder`, `WarriorPlayerVisualSetup`도 같은 기본값을 직렬화하도록 수정.
  - 현재 `AdventureScene` PlayerController 필드 갱신 및 저장.
  - PlayMode 진입 오류 0개, 캡처 `Assets/Screenshots/warrior_player_idle_scaled_check.png` 확인.
- 이동/크기 후속 수정:
  - `PlayerController`에 `moveUnitSize`, `useGridCellSize`, `moveHoldThreshold` 추가.
  - 현재 Grid cell size 1과 이동 단위 1이 일치함을 확인하고, Grid cell size 자동 참조 로직 추가.
  - 짧은 방향키 입력은 이동하지 않고 바라보는 방향만 바꾸도록 변경. 임계값은 `0.12`초에서 `0.06`초로 단축.
  - 방향키를 계속 누르는 동안에는 칸 경계에서 Idle 상태로 돌아가지 않고 다음 이동을 즉시 시작하도록 변경.
  - 이동 중 방향을 바꾸면 임계값 없이 즉시 새 방향으로 도착점을 재계산한다. 도착점은 현재 위치를 가장 가까운 타일 좌표로 스냅한 뒤 새 입력 방향으로 1유닛 떨어진 칸으로 잡는다.
  - Rigidbody2D interpolation을 Interpolate로 설정해 카메라 추적 시 물리 프레임 기반 끊김을 완화.
  - PlayerVisual Walk 스케일을 1로 변경해 타일 1칸 높이에 가깝게 맞추고, Idle 보정값을 0.267로 조정.
  - Player CircleCollider2D radius를 0.45로 조정.
  - PlayMode 진입 및 캡처 `Assets/Screenshots/player_unit_size_check.png` 확인. 실제 키보드 연속 입력 체감은 추가 수동 검증 권장.
- 이전 에이전트 작업 분석:
  - Phase 2 어드벤처 기초가 구현되어 있었고, `AdventureScene`에는 PlayerController/Input/Rigidbody2D/Collider2D와 SPUM 기반 Warrior 자식 오브젝트가 연결되어 있었다.
  - 다음 단계는 어드벤처 PlayMode 수동 검증이었으나, 사용자 요청에 따라 먼저 플레이어 이미지를 프로젝트 내부 Warrior 스프라이트로 교체했다.
- `Assets/Scripts/Adventure/PlayerController.cs` 수정:
  - SPUM 전용 `SPUM_Prefabs._anim.Play("IDLE"/"MOVE")` 호출 제거.
  - `SpriteRenderer`/`Animator` 직렬화 필드 추가.
  - 4방향 타일 이동은 유지하면서 이동 방향에 따라 Front/Back/Side Idle/Walk 상태를 직접 재생.
  - 좌우 이동은 Side 애니메이션을 공유하고 `SpriteRenderer.flipX`로 좌우 반전.
- `Assets/Scripts/Editor/AdventureSceneBuilder.cs` 수정:
  - 새로 씬을 빌드할 때 SPUM 프리팹 대신 `PlayerVisual` 자식 오브젝트를 만들고 `Player_Warrior.controller`를 연결하도록 변경.
- `Assets/Scripts/Editor/WarriorPlayerVisualSetup.cs` 추가:
  - 메뉴 `CardAdventure > Setup Warrior Player Visual`.
  - Warrior 스프라이트 클립/컨트롤러를 생성 또는 갱신하고 현재 `AdventureScene`의 Player 비주얼을 재구성.
- 생성/갱신된 애니메이션 에셋:
  - `Assets/Animations/Player/Player_IdleFront.anim`: `Warrior_IdleFront` 프레임 0,2,4,2 사용.
  - `Assets/Animations/Player/Player_IdleBack.anim`: `Warrior_IdleBack` 프레임 0,2,4,2 사용.
  - `Assets/Animations/Player/Player_IdleSide.anim`: `Warrior_IdleBeside` 프레임 0,2,4,2 사용.
  - `Assets/Animations/Player/Player_WalkFront.anim`: `Warrior_WalkFront` 프레임 0,2,4,6 사용.
  - `Assets/Animations/Player/Player_WalkBack.anim`: `Warrior_WalkBack` 프레임 0,2,4,6 사용.
  - `Assets/Animations/Player/Player_WalkSide.anim`: `Warrior_WalkBeside` 프레임 0,2,4,6 사용.
  - 모든 클립은 4fps(약 0.25초 간격), loop true.
- `Assets/Scenes/AdventureScene.unity` 수정:
  - 기존 Player 하위 `Warrior` 프리팹 제거.
  - `PlayerVisual` 자식 추가: `SpriteRenderer`, `Animator`, `Player_Warrior.controller`, 초기 `Warrior_IdleFront_0` 스프라이트 연결.
  - `PlayerController.spriteRenderer`, `PlayerController.animator` 참조 연결.
- 검증:
  - `PlayerController.cs`, `AdventureSceneBuilder.cs`, `WarriorPlayerVisualSetup.cs` validate_script standard 수행. 오류 0개. 경고는 도구성 false positive/권장 경고만 확인.
  - `CardAdventure > Setup Warrior Player Visual` 메뉴 실행 성공 로그 확인.
  - `Player_Warrior.controller` 6개 상태와 기본 상태 `Player_IdleFront` 확인.
  - PlayMode 진입 후 콘솔 에러 0개 확인.
  - Game View 캡처 `Assets/Screenshots/warrior_player_visual_check.png`에서 플레이어 표시 확인.
- 다음 작업:
  - 실제 키보드 입력으로 방향별 Walk/Idle 전환 체감 확인.
  - BattleEntrance 트리거 후 `BattleTest` 씬 전환 및 복귀 흐름 수동 검증.

### 2026-05-06 (세션 5 — Phase 2 계속)

- `FontSetupTool.cs` 수정: Dynamic SDF 폰트 생성 시 텍스처 아틀라스가 누락되어 발생하던 에러(UnassignedReferenceException) 해결을 위해 `atlasTextures`와 `material`을 Sub-Asset으로 추가하도록 변경.
- `MaruMinyaHangul SDF.asset` 재생성 완료 (정상 용량 8MB 확인). 콘솔 에러 모두 해결.
- `AdventureScene`의 `PlayerInput`에 `InputSystem_Actions.inputactions` 할당 확인.
- `BattleTest` 씬의 `BattleManager`에 `BattleSceneConnector` 컴포넌트 추가 및 저장 완료.
- `PlayerController.cs` 수정: 포켓몬스터 4세대 스타일의 타일(1x1) 기반 4방향 연속 이동 구현 (isMoving, targetPosition 상태 기반, 대각선 무시, 장애물 OverlapBox 검사 적용).
- 이동 방향에 맞춰 플레이어 스프라이트가 좌우로 회전하도록 적용.
- `AdventureScene`의 `Player` 하위 오브젝트를 `Warrior.prefab`으로 교체.
- 다음 에이전트는 어드벤처 씬 PlayMode(타일 기반 Player 이동 정상 작동 여부, BattleEntrance 트리거 씬 전환)를 수동 검증하면 된다.
### 2026-05-06 (세션 5 — Phase 2 시작)

**Phase 2: 어드벤처 씬 기초**

- `Assets/Scripts/Core/GameDataManager.cs` 추가 — DontDestroyOnLoad 싱글턴. 플레이어 덱, HP, 골드, 챕터 진행도, 전투 복귀 씬명 관리.
- `Assets/Scripts/Core/SceneLoader.cs` 추가 — DOTween 페이드(0.4s) 씬 전환 싱글턴. `EnterBattle(enemy, returnScene)` / `ReturnFromBattle(hp, reward)` API.
- `Assets/Scripts/Adventure/PlayerController.cs` 추가 — Input System Send Messages 방식, 8방향 탑다운 이동, SPUM 애니메이션(IDLE/MOVE), 스프라이트 좌우 플립.
- `Assets/Scripts/Adventure/BattleEntrance.cs` 추가 — Trigger2D 전투 진입, EnemyData 연결, 클리어 후 비활성화.
- `Assets/Scripts/Battle/BattleSceneConnector.cs` 추가 — BattleScene 진입 시 GameDataManager → BattleManager.Configure() 연결, 전투 종료 시 어드벤처 씬 복귀.
- `Assets/Scenes/AdventureScene.unity` 생성 (메뉴: CardAdventure > Build Adventure Scene):
  - GameManagers (GameDataManager + SceneLoader)
  - Main Camera (URP + CinemachineBrain)
  - Grid → Ground Tilemap + Walls Tilemap (CompositeCollider2D)
  - Player (PlayerController + PlayerInput + CircleCollider2D + SPUM 자식)
  - CinemachineCamera (팔로우 + PositionComposer 감쇠 0.5)
  - BattleEntrance_Slime (Enemy_Verde_Slime 연결, 위치 x=3)
- Build Settings: AdventureScene(0), BattleTest(1), SampleScene(2) 등록.
- SceneLoader: 배틀 씬 이름 `"BattleTest"` 상수로 관리 (추후 `BattleScene` 정식화).
- 다음 에이전트는 Input Action Asset 연결(`PlayerInput.actions`) + BattleSceneConnector를 BattleTest 씬 BattleManager에 추가하는 작업을 진행하면 된다.

### 2026-05-06 (세션 4)

- `Assets/Fonts/MaruMinyaHangul.ttf` 추가 (사용자 제공).
- `Assets/Scripts/Editor/FontSetupTool.cs` 추가 — Dynamic SDF TMP Font Asset 생성 및 프로젝트 전체 적용 도구.
  - `CardAdventure > Setup Korean Font` 메뉴 실행으로 한 번에 완료.
- `Assets/Fonts/MaruMinyaHangul SDF.asset` 생성 완료 (Dynamic, 2048×2048, multiAtlas).
- TMP Settings 기본 폰트 → MaruMinyaHangul SDF, 한글 줄바꿈 규칙(ModernHangul) 활성화.
- 씬 TMP 11개, 프리팹 TMP 5개(CardView 3 + StatusIcon 2) 교체 완료.
- BattleUIManager 아키텍처 재설계 — StateChanged에서 손패 갱신 제거, DetachCardView/ReattachCardView 흐름으로 카드 애니메이션 충돌 방지.
- BattleManager.startOnAwake: Awake()→Start()로 이동해 이벤트 구독 타이밍 문제 해결.
- EventSystem 추가로 카드 클릭 상호작용 활성화.
- BattleDebugHud 비활성화 (구 IMGUI HUD와 새 Canvas UI 중복 제거).
- 다음 에이전트는 PlayMode 진입 후 전투 흐름(카드 클릭 → 화살표 마우스 추적 → 사용 확정 → 카드 애니메이션 → 턴 종료 → 승패 패널)을 수동 검증하면 된다.

### 2026-05-06 (세션 3)

- `BattleTargetArrow.cs` 추가 — 베지어 곡선 + 8개 세그먼트 펄스 화살표, 마우스 추적.
- `BattleUIManager` 업데이트 — 공격/상태이상 카드 선택 시 화살표 표시, 배경 클릭 사용 확정, 우클릭 취소.
- `BattleSceneBuilder.cs` (Editor) 추가 — 메뉴 CardAdventure > Build Battle UI Scene 실행으로 전체 Canvas 계층 자동 생성.
- `BattleTest.unity` 씬에 BattleCanvas 구성 완료, 씬 저장.
- `Assets/Prefabs/UI/CardView.prefab`, `StatusIcon.prefab` 자동 생성.
- `BattleManager.startOnAwake = true` 설정.
- 다음 에이전트는 PlayMode 진입 후 전투 흐름(카드 사용 → 화살표 → 턴 종료 → 승패 패널)을 수동 검증하면 된다.

### 2026-05-06 (세션 2)

- 배틀 UI 프로토타입 스크립트 6종 추가. 모두 validate_script standard 검증 오류/경고 0개.
  - `Assets/Scripts/UI/BattleCardView.cs` — 손패 카드 1장 뷰
  - `Assets/Scripts/UI/BattleHandView.cs` — 손패 레이아웃 / 부채꼴 배치
  - `Assets/Scripts/UI/BattleHudView.cs` — 플레이어 HP/방어막/에너지 HUD
  - `Assets/Scripts/UI/BattleStatusIconView.cs` — 상태이상 아이콘
  - `Assets/Scripts/UI/BattleEnemyView.cs` — 적 HP/방어막/인텐트 표시
  - `Assets/Scripts/UI/BattleUIManager.cs` — 전체 UI 조율 컨트롤러
- PROJECT_STATUS.md에 씬 구성 안내(Canvas 계층, 프리팹 구조) 추가.
- 다음 에이전트는 Unity 에디터에서 씬 Canvas 계층 구성 + 카드/상태이상 프리팹 생성 후 PlayMode 검증을 진행하면 된다.

### 2026-05-06

- 프로젝트 에셋과 기존 지침을 확인했다.
- 기획서 기준 지침을 `CLAUDE.md`에 추가했다.
- Antigravity용 `AGENTS.md`를 추가했다.
- 에이전트 인수인계를 위해 이 `PROJECT_STATUS.md`를 추가했다.
- Phase 1 첫 작업으로 전투 런타임 모델을 추가했다.
- 추가 파일: `BattleRuntimeCard`, `BattleCardPiles`, `BattleStatusInstance`, `BattleCombatantState`, `BattlePlayerState`, `BattleEnemyState`.
- 다음 에이전트는 `BattlePlayerState`, `BattleEnemyState`, `BattleCardPiles`를 사용해 전투 매니저를 구현하면 된다.
- Phase 1 두 번째 작업으로 `BattleManager`, `BattlePhase`, `BattleCardPlayResult`를 추가했다.
- `BattleManager`는 전투 시작, 플레이어 턴, 카드 사용, 손패 폐기, 적 행동, 승패 판정을 관리한다.
- 다음 에이전트는 전사 카드별 효과 처리와 상태이상 턴 처리를 구현하면 된다.
- Phase 1 세 번째 작업으로 전사 카드 5종 효과를 전투 루프에 연결했다.
- `BattlePlayerState`에 이번 턴 공격 피해 보너스와 공격 시 보너스 획득 규칙을 추가했다.
- `BattleCombatantState`에 상태 타입 기반 적용/조회 기능을 추가했다.
- 다음 에이전트는 독/재생 등 상태이상 턴 처리와 테스트 데이터/테스트 씬 구성을 진행하면 된다.
- Phase 1 네 번째 작업으로 상태이상 턴 처리를 구현했다.
- 독/재생은 대상 턴 시작에 처리하고, 지속시간은 대상 턴 종료에 감소한다.
- 다음 에이전트는 적 테스트 데이터와 전투 테스트 씬 또는 자동 테스트를 구성하면 된다.
- Phase 1 다섯 번째 작업으로 테스트용 상태이상 2종, 적 2종, `BattleTest` 씬을 구성했다.
- 다음 에이전트는 `BattleTest` 씬을 PlayMode로 열어 자동 시작 전투 상태를 확인하거나, EditMode/PlayMode 테스트를 추가하면 된다.
- Phase 1 여섯 번째 작업으로 `BattleManagerEditModeTests`를 추가하고 EditMode 테스트 6개를 통과시켰다.
- 다음 에이전트는 기본 배틀 UI 또는 PlayMode 씬 검증을 진행하면 된다.
- 수동 PlayMode 테스트를 위해 `BattleTest` 씬에 `Main Camera`와 `BattleDebugHud`를 추가했다.
- 다음 에이전트는 사용자의 수동 테스트 피드백을 반영하거나 실제 배틀 UI 구현을 시작하면 된다.
- 방어막 유지 규칙을 적용하고 `BattleManagerEditModeTests`에 `Block_RemainsAfterTurnEnds`를 추가했다.
- 레퍼런스 이미지에 맞춰 CCGKit Demo 텍스처를 `BattleDebugHud`에 연결하고, 공격 카드 대상 선택 화살표 UI를 추가했다.
