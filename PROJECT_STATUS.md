# CardAdventure Project Status

이 파일은 Codex, Claude Desktop, Antigravity가 작업을 바로 이어받기 위한 공용 인수인계 문서입니다. 모든 에이전트는 작업 시작 시 이 파일을 먼저 읽고, 작업 종료 시 최신 상태로 갱신합니다.

## 현재 목표

- 기획서 **카드 배틀 자격증 어드벤처 — 게임 기획서 v1.0 (2026.05)** 를 기준으로 Unity 2D 탑다운 어드벤처 + 턴제 카드 배틀 RPG를 구현한다.
- 우선순위는 Phase 1: **전사 덱 기반 카드 배틀 기본 루프** 완성이다.

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

## 다음 작업 후보

1. **PlayMode 수동 검증** — BattleTest 씬 PlayMode 진입 → 카드 클릭 → 공격 화살표 → 턴 종료 → 승패 패널 흐름 확인.
2. 플레이어·적 스프라이트 연결 — SPUM 파츠(Human_1.png 등)를 BattleManager EnemyData.enemySprite에 연결.
3. 카드별 효과 처리를 이름 비교 대신 명시적 ID/효과 타입으로 개선.
4. 손패 카드 드로우 애니메이션 diff-patch 최적화(StateChanged마다 전체 재생성 → 기존 뷰 재사용).
5. 챕터 1 탑다운 어드벤처 씬 기초 작업 시작(Phase 2).

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
- Unity 스크립트 컴파일 요청 수행. 콘솔에는 MCPForUnity 클라이언트 핸들러 관련 도구 로그가 있었고, 새 코드 컴파일 오류는 확인되지 않음.
- PlayMode 수동 조작은 사용자가 직접 진행 예정.
- Git 저장소는 이전 작업에서 복구됐으나 현재 세션 사용자가 달라 `dubious ownership` 경고로 `git status`가 막힌다. 필요 시 `git config --global --add safe.directory C:/UnityProjects/CardAdventure` 처리 후 확인한다.

## 주의사항

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

## 주의사항 (UI 관련 추가)

- `BattleUIManager.RefreshHand`는 StateChanged마다 손패를 전체 재생성한다. 카드 수가 많을 때 드로우 애니메이션이 중복 재생될 수 있으므로, 추후 diff-patch 방식으로 개선을 고려한다.
- `BattleHandView.ArrangeCards(animate: true)`는 매 RefreshHand마다 드로우 연출을 실행한다. 이미 손패에 있는 카드를 다시 연출하지 않으려면 카드 인스턴스 ID로 기존 뷰를 재사용하는 방식으로 확장해야 한다.
- 단일 적 전투 전용이므로 카드 사용 시 대상 선택 없이 바로 PlayCard를 호출한다. 다수 적 지원 시에는 대상 선택 UI를 BattleUIManager에 별도로 추가해야 한다.
- BetterUI 반응형 컴포넌트(BetterContentSizeFitter 등)는 씬 구성 시 Unity Layout Group 대신 직접 Inspector에서 교체한다.

## 작업 로그

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
