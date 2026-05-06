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

## 다음 작업 후보

1. PlayMode 또는 EditMode 테스트로 전투 시작/카드 사용/적 턴 전환/전사 카드 효과/상태이상 처리를 검증한다.
2. 기본 배틀 UI: HP, 방어막, 에너지, 손패, 적 의도 표시.
3. 카드별 효과 처리를 이름 비교 대신 명시적 ID/효과 타입으로 옮길지 결정한다.
4. 상태이상 UI 표시를 위해 `BattleStatusTurnResult`와 `BattleCombatantState.Statuses`를 연결한다.
5. `BattleTest` 씬에 간단한 디버그 조작 UI 또는 로그 표시기를 붙인다.

## 검증 상태

- 문서 파일 UTF-8 읽기 확인 완료.
- 새 전투 런타임 스크립트 6개는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- 새 전투 매니저 관련 스크립트 3개(`BattleManager`, `BattlePhase`, `BattleCardPlayResult`)는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- 전사 카드 효과 연결을 위해 수정한 `BattleManager`, `BattlePlayerState`, `BattleCombatantState`, `BattleStatusInstance`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- 상태이상 턴 처리를 위해 추가/수정한 `BattleStatusTurnResult`, `BattleCombatantState`, `BattleManager`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- `CardAdventureTestContentBuilder`는 Unity MCP `validate_script` 표준 검증에서 오류/경고 0개 확인.
- `BattleTest.unity` 씬의 `BattleManager`에 전사 테스트 덱 8장과 `Enemy_Verde_Slime` 연결이 YAML 기준으로 확인됨.
- Unity 스크립트 컴파일 요청 수행. 콘솔에는 MCPForUnity 클라이언트 핸들러 관련 도구 로그가 있었고, 새 코드 컴파일 오류는 확인되지 않음.
- PlayMode 검증은 아직 수행하지 않음.
- Git 저장소는 이전 작업에서 복구되어 사용 가능하다. 이번 변경은 아직 커밋하지 않음.

## 주의사항

- `Assets/Assets/`와 `Assets/Plugins/`의 서드파티 코드는 직접 수정하지 않는다.
- `CardAdventure.CardType`과 CCGKit `CardType` 이름 충돌에 주의한다.
- 기존 ScriptableObject 필드 삭제/이름 변경은 피한다.
- CCGKit은 단기 구현에서 자체 `CardData` 구조를 대체하지 않고 참조/어댑터 방식으로 활용한다.
- `BattleManager`의 전사 특수 효과는 현재 `CardData.name` 기준으로 분기한다. 장기적으로는 `CardData`에 안정적인 effect id/타입을 추가하는 편이 좋다.
- 상태이상 지속시간은 각 대상의 턴 종료 시 감소한다. `도발`의 약화 1턴은 적 행동에 적용된 뒤 적 턴 종료 시 제거된다.
- `execute_code`와 `execute_menu_item`은 현재 Unity 경로/세션 문제로 테스트 콘텐츠 생성 실행에 실패했다. 실제 자산은 MCP ScriptableObject/Scene 도구로 생성했고, Editor 빌더 스크립트는 향후 Unity 메뉴에서 재사용 가능하도록 남겨두었다.
- 작업 종료 시 이 파일의 “최근 변경”, “다음 작업 후보”, “검증 상태”, “주의사항”을 갱신한다.

## 작업 로그

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
