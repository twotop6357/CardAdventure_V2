# CardAdventure 발표자료 작성 브리프 (Claude Desktop 참고용)

> 이 문서는 Claude Desktop이 CardAdventure 프로젝트의 **포트폴리오용 발표자료(10~15슬라이드)** 를 만들 때 그대로 참고할 수 있도록 정리한 자료다.  
> 작성일: 2026-05-19 / 기준 커밋: PROJECT_STATUS.md 최신 헤드(2026-05-19 Antigravity — 초급 배틀러 자격증 엔딩 UI 완료)

---

## 0. 발표 한 줄 요약

> **카르타니아 왕국의 초급 배틀러 자격증을 따기 위한 1챕터 완성형 싱글플레이 어드벤처.**  
> 탑다운 픽셀 어드벤처와 턴제 카드 배틀을 결합한 Unity 2D 프로젝트이며, 4종의 AI 코딩 에이전트(Antigravity, Claude Desktop, Codex, Claude Code)가 단일 인수인계 문서(`PROJECT_STATUS.md`)를 공유하며 협업한 멀티 에이전트 개발 워크플로 사례이기도 하다.

---

## 1. 발표 가이드라인

- **대상**: 포트폴리오/일반 소개 (게임 비전공자가 봐도 흐름이 잡힐 정도)
- **분량**: 10~15슬라이드
- **톤**: 밝고 차분한 픽셀 RPG 톤. 과한 게임 용어는 풀어쓰고, 카드/직업/상태이상 같은 핵심 용어는 캡션으로 보조 설명
- **언어**: 한국어 발표 기준
- **권장 형식**: `.pptx` (시각 위주). 슬라이드당 텍스트 5줄 이내, 좌측 텍스트/우측 스크린샷 또는 다이어그램 구성 권장
- **금지/주의**: 3챕터 장기 구상, 멀티플레이, 노트 필기/스케치 풍 UI 등 **기획서 v1.1에서 빠진 요소는 슬라이드에 넣지 않는다**.

---

## 2. 슬라이드 구성안 (12슬라이드 기본 / 부록 3슬라이드)

### Slide 1 — 표지
- 제목: **CardAdventure — 초급 배틀러가 되는 길**
- 부제: 탑다운 카드 배틀 어드벤처 RPG / Unity 2D 싱글플레이어
- 배경 후보: 루미나 마을 또는 시험장 스크린샷
- 우하단 캡션: "1챕터 베르데 평원 완성형 · 2026.05 빌드"

### Slide 2 — 한 장 요약
- **장르**: 턴제 카드 배틀 + 탑다운 픽셀 어드벤처 RPG
- **엔진**: Unity 6 / URP 17.3 (2D Renderer, Pixel Perfect, Cinemachine, Input System 1.19)
- **범위**: 챕터 1 베르데 평원 완성형 (싱글플레이어 전용)
- **세계관**: 마법과 모험가 자격증 제도가 있는 밝은 판타지 왕국 **카르타니아**
- **목표 경험 우선순위**: 카드 전략 → 수집/덱빌딩 → 탑다운 탐험 → NPC 대화 → 자격증 서사 → 픽셀 감성
- **개발 특이점**: 4종 AI 에이전트 분담 + 단일 `PROJECT_STATUS.md` 인수인계 워크플로

### Slide 3 — 게임 플레이 흐름 (다이어그램)
다음 흐름을 박스/화살표로 시각화:

```
LobbyScene (새 게임 인트로 대화)
   ↓ 최초 직업 선택 (전사 / 마법사 / 도적)
AdventureScene
   ├─ 루미나 마을 (전직관, 상점, NPC 대화)
   ├─ 베르데 평원 (이벤트 전투 NPC · 추격자 NPC)
   └─ 길드 시험장 (시험관 릴라)
         ↓ 시험관 보스 배틀
BattleScene (인트로 연출 → 턴 진행 → 결과)
   ├─ 승리 → 3장 중 1장 카드 보상 + 골드
   └─ 패배 → 패배 결과 UI → 슬롯 불러오기
         ↓ 시험관 처치
초급 배틀러 자격증 (LicenseCanvas) → 로비 복귀
```

발표 캡션: 어드벤처 ↔ 배틀 씬을 분리하고 `SceneLoader`로 전환, 진입 연출은 원형 아이리스 트랜지션으로 일원화.

### Slide 4 — 카드 배틀 규칙 (핵심 한 장)
- **시작 손패**: 5장 (`BattlePlayerState.DefaultStartingHandSize = 5`)
- **기본 에너지**: 매 턴 3 (`BattlePlayerState.DefaultMaxEnergy = 3`, 카드/효과로 확장 가능)
- **드로우**: 매 턴 시작 시 1장 추가, 손패 유지(턴 종료 시 손패가 버려지지 않음)
- **덱 처리**: 덱 소진 시 버린 카드 더미 재셔플 (`BattleCardPiles`)
- **방어막(Block)**: 피해를 먼저 흡수, 카드/직업 효과에 따라 유지 가능
- **상태이상 7종** (스택+턴 동시 관리):
  - 공격적: **독(Poison)**, **화상(Burn)**
  - 약화형: **약화(Weak)**, **취약(Vulnerable)**
  - 강화형: **힘(Strength)**, **재생(Regeneration)**, **회피(Dodge)**
- **적 의도(Intent)**: 공격/방어/버프/디버프/회복 아이콘 + 수치 미리보기 (`BattleEnemyState`)

### Slide 5 — 직업과 직업별 덱 (63장)
- **3 직업**: 전사 / 마법사 / 도적 (공용 카드 없음, 모두 직업 전용)
- 직업별 카드 풀: **각 21장 × 3 = 총 63장** (`Assets/ScriptableObjects/Cards/{Warrior|Mage|Rogue}/`)
- 대표 카드 예시 (발표용으로 한 줄씩만 발췌):
  - **전사**: Strike, Defend, Rage, ShieldBash, BerserkerBlow, IronWill, BloodArmor, CounterStance 등 — 방어/타격 콤보
  - **마법사**: Fireball, IceShard, Lightning, ChainLightning, MeteorShower, ManaBarrier, PowerSurge 등 — 원소·상태이상 중심
  - **도적**: DaggerThrow, PoisonStab, ShadowStep, BackStab, DualBlade, Afterimage, DodgeAmplify 등 — 회피·독 중심
- **전직**: 루미나 마을 `NPC_JobChanger`(전직관)에서 자유 전직 가능, 직업별 덱은 별도로 저장
- **카드 효과 종류**: `CardEffectType` 50종 이상 (단일 타격/연타/연쇄/상태이상 부여/방어막/에너지·드로우 보조 등)

### Slide 6 — 어드벤처(탑다운) 시스템
- **이동**: WASD + Shift 달리기, **1×1 셀 기반 그리드 스냅** (초기 포켓몬스터식 한 칸 감각)
- **충돌 규칙**: 캐릭터의 발 위치를 기준으로 `BoxCollider2D(size=1×1, offset=(0,-0.5))`를 사용 → 시각적 키와 충돌 박스 분리
- **시각 통일**: 모든 NPC/플레이어의 화면상 키는 기준 NPC `NPC_BaramIroGun`의 SpriteRenderer 월드 높이 `1.3304521` 에 맞춤 (`AdventureGridUtility.ReferenceCharacterVisualHeight`)
- **씬**: `LobbyScene`, `AdventureScene`, `BattleScene`, `BattleTest`
- **카메라**: Cinemachine 2D + Pixel Perfect, 16:9 고정 + 레터박스

### Slide 7 — NPC와 대화 시스템
- **NPC 4종**:
  - **JobChangerNpc** — 전직 (직업별 덱 분리 저장)
  - **ShopKeeperNpc** — 카드 상점 + 포션 상점 (탭 분리, 갱신/SOLD OUT)
  - **ExaminerNpc** — 시험관 릴라 (질문 → 보스전 → 종료 대화 자동 재생 → 자격증 UI)
  - **NpcChaser** — 추격자(매직 디어 / 매직 래빗 중 **50% 무작위** 조우, 성별별 전용 대화 에셋)
- **대화 시스템**:
  - `DialogueData` ScriptableObject 기반 분기 구조 + 초상화
  - Febucci Text Animator로 글자별 타이핑 효과(절제된 톤)
  - 시험관 보스전 후 어드벤처 복귀 시 `NPC_ExaminerBattleEnd_Dialogue` 자동 트리거
  - 대화 종료 시점에 플레이어 위치/FacingDirection을 캡처해 SaveData에 즉시 저장

### Slide 8 — 보상 / 상점 / 회복 / 저장
- **전투 보상**: 일반 전투 = 골드 + **카드 3장 중 1장 선택** (`BattleRewardUIController`, 현재 직업 카드 풀에서 중복 없이 무작위 3장)
- **상점**:
  - 첫 전투 이후 개방
  - **회복 물약 고정 판매 + 카드는 전투 1회마다 갱신**, 매진 시 SOLD OUT
  - 카드/포션 화면 완전 분리 + 뒤로가기 내비게이션
- **회복 아이템**: 필드에서 사용하는 작은/큰 회복약 (`FieldPotionItem`, 전투 중 사용 불가)
- **저장**: **JSON 기반 3슬롯** (`SaveManager.MAX_SAVE_SLOTS = 3`, `Application.persistentDataPath/save_slot_{0..2}.json`)
- **인게임 메뉴**: ESC → 계속/저장/내 카드/설정/아이템 (Setup 툴 자동 재생성, ESC 이중 트리거 등 다수의 안정화 이슈 해결됨)

### Slide 9 — 엔딩: 초급 배틀러 자격증 (LicenseCanvas)
- 시험관 처치 → 보상 선택 → 검은 페이드 → **최상단 독립 Canvas 오버레이** 로 `LicenseCanvas` 팝업
- 표시 항목:
  - 직업 초상화
  - 플레이 시간 / 최종 직업 / 승리 횟수 / 수집 카드 수
  - 사용한 포션 수, 카드 구매에 쓴 골드, 최강 카드 피해
  - 직업별 칭호
- 클릭 시 부드러운 페이드아웃으로 로비 복귀
- **자격증은 성장 시스템이 아닌 1챕터 클리어 기념 UI**. 추후 챕터 확장의 출발점이 되도록 디자인.

### Slide 10 — 기술 스택 / 에셋
좌측 텍스트, 우측 아이콘 격자 권장.
- **엔진/패키지**: Unity 6, URP 17.3 (2D Renderer), Pixel Perfect, Cinemachine, Input System 1.19, Tilemap, Aseprite Importer, PSD Importer, SpriteShape, TextMeshPro
- **연출**: **DOTween PRO** (카드 이동, 데미지 피드백, 씬 페이드, UI 트랜지션 — 씬 전환 전 `DOTween.KillAll()` 필수)
- **UI**: **TheraBytes BetterUI** (해상도 독립 반응형 레이아웃)
- **텍스트 효과**: **Febucci Text Animator** (대화 타이핑, 카드 텍스트 강조)
- **카드 게임 참고**: **CCGKit** (구조/UI 패턴 참고용, 네트워킹 코드는 미사용 — Mirror는 `MirrorStub`만 존재)
- **캐릭터**: **SPUM v1.8.8** (픽셀 파츠 후보), 기준 캐릭터 비주얼은 자체 프리팹 우선
- **UI 픽셀 키트**: DEVNIK 2D / 자체 `ClassicPixelUiTheme`

### Slide 11 — 데이터 아키텍처 (ScriptableObject 중심)
하나의 다이어그램으로 정리 권장. 핵심 데이터 자산:

| 카테고리 | 위치 | 수량/요약 |
| --- | --- | --- |
| 카드 데이터 | `Assets/ScriptableObjects/Cards/{Warrior,Mage,Rogue}/` | 21 × 3 = **63** |
| 적 데이터 | `Assets/ScriptableObjects/Enemies/` | **6** (MagicCrow, MagicDeer, MagicDeer_Female, MagicRabbit, Spore_Rogue, Verde_Slime) |
| 상태이상 | `Assets/ScriptableObjects/StatusEffects/` | **7** (Poison, Burn, Weak, Vulnerable, Strength, Regeneration, Dodge) |
| 직업 정의 | `Assets/ScriptableObjects/Jobs/` | **3** (전사/마법사/도적) |
| 대화 | `Assets/ScriptableObjects/Dialogues/` | **20+** (직업, 이벤트 전투, 시험관, 상점, 인트로 등) |
| 배틀 인트로 | `Assets/ScriptableObjects/BattleIntros/` | **3** (Examiner, MaleChaser, FemaleChaser) |
| 카드 아트 매핑 | `Assets/ScriptableObjects/CardSpriteLibrary.asset` | 자동 로드 |

자체 코드 네임스페이스 = `CardAdventure`. CCGKit 내부 클래스는 직접 수정하지 않고 어댑터/상속으로 확장.

스크립트 폴더 구조 (`Assets/Scripts/`):
- `Core/` — GameDatabase, SaveManager, GameDataManager, SceneLoader 등
- `Data/` — CardData, EnemyData, StatusEffectData, JobClassInfo, DialogueData, BattleIntroData, SaveData
- `Battle/` — BattleManager(턴 상태머신/이벤트 허브), BattlePlayerState, BattleEnemyState, BattleCardPiles, BattleIntroDirector, BattleVfxController 등 18개
- `Adventure/` — PlayerController, AdventureGridUtility, NpcInteractable, JobChangerNpc, ShopKeeperNpc, ExaminerNpc, NpcChaser, DialogueManager 등 16개
- `UI/` — BattleUIManager, BattleHandView/CardView, BattleRewardUIController, ShopUIController, EndingLicenseUI, InGameMenuController 등 27개
- `Editor/` — 씬·콘텐츠 자동 빌더 37종 (BattleSceneBuilder, AdventureSceneBuilder, SetupLobbyScene 등)

### Slide 11.5 — (선택) 직업 변경 흐름 UI
- 어드벤처 씬 전직관 NPC와 인게임 메뉴 양쪽에서 호출 가능
- 직업별 덱이 분리 저장되어 전직 후 손해 없음
- 직업 설명은 상점 스타일과 통일된 UI 톤

### Slide 12 — 멀티 AI 에이전트 협업 워크플로 (포트폴리오 차별점)

발표에서 가장 강조할 만한 차별점.

- **참여 에이전트**: Antigravity / Claude Desktop / Codex / Claude Code (한 명의 개발자가 4종을 분담)
- **단일 인수인계 문서**: 루트 **`PROJECT_STATUS.md`** (현재 약 **5,100행 / 380KB**, 2026-05-12 ~ 2026-05-19 약 일주일간 누적)
- **공통 규칙** (`AGENTS.md` + `CLAUDE.md`):
  - 작업 **시작 전**: 최신 진행 상황, 다음 작업, 주의사항 확인
  - 작업 **종료 전**: 이번 작업 요약 / 변경 파일 / 검증 결과(또는 "미검증" 표시) / 다음 에이전트 할 일 / 충돌 주의사항 갱신
  - 한국어로 간결하게, 같은 파일을 동시에 수정하지 않도록 작업 범위 분리
- **역할 분담 예 (실제 로그 기반)**:
  - **Antigravity**: 배틀 드래그&드롭, 카드 프리뷰 버그, 시험관 자동 대화, 자격증 UI, 16:9 레터박스, 상점 SOLD OUT 시스템 등 — 런타임/연출 안정화
  - **Claude Desktop**: 인게임 ESC 메뉴, "내 카드" 패널, UI 스타일 통일, 한글 폰트 복원 등 — 메뉴/UI 폴리싱
  - **Codex**: 카드 데이터(전사/마법사/도적 각 11~21장 시리즈 추가), 대화·상태이상·연출 디테일, NPC 추가, 로비 인트로 흐름 등 — 콘텐츠/데이터 작업의 큰 축
  - **Claude Code**: 초기 전투/플레이어 애니메이션 시스템 셋업, 도적 카드/회피 시스템 등 — 핵심 시스템 구축
- **장점**: 동일 컨텍스트를 잃지 않고 24시간 비동기 협업 / 강점이 다른 도구를 병렬 활용 / 모든 작업이 텍스트 로그로 남아 회고·재현이 용이

### Slide 13 — 기획 v1.1 대비 변경/추가된 사항
> 기준: `CardAdventure 기획서 v1.1 (2026.05)` (CLAUDE.md / AGENTS.md 기재) vs 2026-05-19 실제 빌드

**기획서 그대로 구현된 항목**
- 1챕터 베르데 평원 / 카르타니아 / 초급 배틀러 자격증 컨셉
- 시작 손패 5장 · 기본 에너지 3 · 매 턴 1장 드로우 · 손패 유지 · 덱 소진 시 재셔플
- 상태이상 5종(독·약화·취약·힘·재생) — **실제 구현은 7종으로 확장**
- 전투 보상 3장 중 1장 + 골드
- 직업 3종(전사/마법사/도적) · 직업별 카드 풀 · 자유 전직
- 상점: 회복 물약 고정 + 카드는 전투 1회마다 갱신
- 회복 아이템(작은/큰 회복약) — 필드에서만 사용
- 저장 3슬롯 / 게임오버 → 슬롯 불러오기
- 초급 배틀러 자격증 엔딩 UI (플레이 시간·최종 직업·승리 수·수집 카드 수·직업별 칭호)

**기획서 대비 확장/변경된 항목 (발표에서 강조 가능)**
- **상태이상 +2종**: 기획서 5종 → 실제 **7종** (화상·회피 추가)
- **추격자 NPC 무작위 조우**: 매직 디어/매직 래빗 **50% 무작위** + 성별(남/여)별 전용 대화·인트로 에셋
- **시험관 자동 복귀 대화**: 보스전 종료 후 어드벤처 복귀 시 종료 대화 자동 재생, 위치/시선 자동 저장
- **상호작용 시선 4방향 정렬**: 대화 시 NPC가 플레이어 방향을 자동으로 바라봄
- **입장 연출 통합**: 일반/보스 전투 진입을 원형 아이리스 트랜지션 하나로 일원화
- **자격증 UI 강화**: 사용한 포션 수, 카드 구매 골드, 최강 카드 피해 등 통계 추가
- **상점 SOLD OUT + 카드/포션 화면 분리** (기획서엔 단순 갱신만 명시)
- **16:9 고정 + 레터박스**, 카드 일러스트 비율 보존
- **카드 풀 규모**: 직업당 21장 (기획서엔 수량 미명시) — Phase 1 카드 시드는 완료, Phase 4 폴리싱 단계로 진입
- **비주얼 톤 확정**: 기획 v1.0의 노트 필기/스케치 스타일 폐기 → **클래식 픽셀 UI 통일** (`ClassicPixelUiTheme`)
- **드래그&드롭 카드 사용**: 호버 프리뷰 + 드래그 → 사용 영역 드롭, 다단계 버그(프록시 잔존/마우스 추적) 완전 해결
- **카드 상점/대화/UI 톤 통일**: 상점 스타일 기준으로 직업 설명, 대화 UI 등 전체 적용

### Slide 14 — 현재 진행 상황 & 다음 단계
- **현재 위치**: Phase 3 본격 진행 단계  
  Phase 1 전투 루프 안정화 ✅ / Phase 2 베르데 평원 콘텐츠 ✅ / Phase 3 저장·상점·엔딩 (진행 중) / Phase 4 폴리싱·밸런싱 (예정)
- **최근 한 주 주요 마일스톤** (2026-05-12 ~ 05-19):
  - 5/12 NPC 배틀 전환 + MagicDeer 연동
  - 5/13~14 카드 시드(63장) 완성, 상태이상 UI/툴팁, 적 의도 한글화
  - 5/14 도적 카드 11종 + 회피 시스템
  - 5/18 상점 SOLD OUT, ESC 메뉴 "내 카드" 패널, 클래식 픽셀 UI 통일, 드래그&드롭 안정화
  - 5/19 시험관 자동 대화, 추격자 무작위 조우, **초급 배틀러 자격증 엔딩 UI 완성**
- **다음 작업**:
  1. 세이브 슬롯 UI 고도화 (3개 슬롯 선택/기록/불러오기 흐름)
  2. 상점 갱신 로직 심화 + 필드 회복 아이템 배치
  3. 1챕터 베르데 평원 완성형 빌드 폴리싱 / 밸런싱

### Slide 15 — 마무리 / 회고 포인트
발표용 클로징 메시지 후보:
- 1챕터에 집중해 게임의 "처음부터 끝까지의 한 사이클"을 완성하는 것을 우선했다.
- ScriptableObject 중심 데이터 설계 덕분에 카드/적/대화/상태이상 콘텐츠를 빠르게 확장할 수 있었다.
- 4종의 AI 코딩 에이전트를 단일 인수인계 문서로 묶는 워크플로는 1인 개발의 생산성을 크게 끌어올렸다.
- 다음 챕터를 확장한다면, 자격증 UI를 출발점으로 한 "중급 배틀러 → 상급 배틀러" 서사로 자연스럽게 이어진다.

---

## 3. 발표 슬라이드용 스크린샷 / 캡처 가이드

Claude Desktop이 슬라이드 시안을 만들 때 다음 시점/UI를 캡처해 좌·우 분할 배치를 추천한다.

| 슬라이드 | 캡처 대상 | Unity 씬/경로 |
| --- | --- | --- |
| 1, 2 | 루미나 마을 또는 시험장 풍경 | `AdventureScene` |
| 3 | 어드벤처 → 배틀 진입 아이리스 트랜지션 | `BattleScene` 입장 시 |
| 4 | 배틀 HUD (HP/에너지/턴), 적 의도 아이콘, 상태이상 아이콘 | `BattleScene` / `BattleTest` |
| 5 | 손패에 3직업 대표 카드가 보이는 컷 | `BattleTest` |
| 6 | 그리드 이동 중 플레이어 + NPC 셀 정렬 | `AdventureScene` |
| 7 | 시험관 / 상점 / 전직관 / 추격자 대화창 (4분할) | `AdventureScene` |
| 8 | 3선택1 카드 보상 UI / 상점 카드·포션 탭 / 인게임 ESC 메뉴 | `BattleScene`, `AdventureScene` |
| 9 | **`LicenseCanvas` 자격증 화면** (핵심 컷) | 시험관 처치 후 |
| 11 | 프로젝트 폴더 트리(`Assets/Scripts`, `Assets/ScriptableObjects`) 캡처 | Unity Project 창 |
| 12 | `PROJECT_STATUS.md` 헤드(최근 5개 헤더) 캡처 | 텍스트 에디터 |
| 13 | 픽셀 UI 통일 전/후 비교(있다면) | 변경 전 스샷이 없으면 생략 |

---

## 4. 발표용 핵심 숫자 모음 (그대로 사용 가능)

- 시작 손패 **5장** / 기본 에너지 **3** / 매 턴 드로우 **+1장**
- 손패 유지 **O** / 덱 소진 시 버린 카드 재셔플 **O**
- 직업 **3종** / 직업당 카드 **21종** / 총 카드 **63종**
- 상태이상 **7종** (Poison · Burn · Weak · Vulnerable · Strength · Regeneration · Dodge)
- 적 데이터 **6종** (시험관 보스 + 무작위 추격 2종 + 평원 일반 3종)
- 대화 에셋 **20종 이상**
- 배틀 인트로 **3종** (시험관 / 남자 추격자 / 여자 추격자)
- 저장 슬롯 **3개** (`save_slot_0.json` ~ `save_slot_2.json`)
- 추격자 조우 확률 **매직 디어 50% / 매직 래빗 50%**
- 카드 효과 종류 **50종 이상** (`CardEffectType` enum)
- 씬 **4개** (Lobby, Adventure, Battle, BattleTest)
- 자체 스크립트 폴더 **7개 + Editor 툴 37종**
- 화면 비율 **16:9 고정 + 레터박스**
- 시각적 캐릭터 키 기준: SpriteRenderer 월드 높이 **1.3304521**
- 협업 AI 에이전트 **4종** / 인수인계 문서 **약 5,100행 / 380KB**

---

## 5. 발표 시 주의해서 다룰 표현

- **"멀티플레이"라는 단어를 절대 사용하지 않는다.** 본 프로젝트는 싱글플레이어 전용이며, CCGKit의 네트워킹/Mirror 관련 부분은 사용하지 않는다. (`MirrorStub`는 컴파일 스텁임을 강조하거나 아예 언급하지 않아도 됨)
- **"로그라이크"라는 표현도 사용하지 않는다.** 일반 RPG 진행과 3슬롯 저장 구조를 사용한다.
- **3챕터 장기 구상 / 항구 도시 / 길드 본부 / 복잡한 엔딩 분기**는 향후 확장 후보일 뿐 현재 빌드 범위가 아니므로 슬라이드에 넣지 않는다.
- **AI 에이전트 협업** 슬라이드는 "AI가 게임을 만들었다"가 아니라 **"개발자가 4종의 AI 도구를 단일 인수인계 문서로 묶어 분담했다"** 는 톤으로 표현한다.
- 캐릭터 그래픽은 SPUM이 후보 에셋이지만 실제 사용 비중은 낮으므로, "SPUM 기반"이라고 단정하지 말고 "픽셀 아트 베이스(자체 프리팹 + SPUM 후보)"로 표기한다.

---

## 6. 부록: Claude Desktop이 PPTX 생성 시 참고할 톤·레이아웃

- 폰트: 한글 본문은 굵기 차이가 있는 산세리프(예: Pretendard / 노토 산스 KR), 픽셀 폰트는 표지/포인트 텍스트에만 제한 사용
- 색상: 밝은 베이지/세이지/소프트 블루 계열. 강조색은 자격증 골드(#D4A24C 계열) 1개로 통일
- 슬라이드 비율: **16:9**
- 슬라이드당 텍스트는 5줄을 넘기지 않고, 본문 폰트 크기 20pt 이상 유지
- 핵심 슬라이드(3·9·12)는 다이어그램/큰 스크린샷으로 시각 중심 구성
- 캡션은 한 줄 이내, 카드 용어는 괄호로 풀어쓰기 (예: 취약(받는 피해 50% 증가))

---

## 7. 참고 문서 위치

- `C:\UnityProjects\CardAdventure\CLAUDE.md` — 프로젝트 지침 / 기획 v1.1 요약
- `C:\UnityProjects\CardAdventure\AGENTS.md` — 에이전트 공통 규칙
- `C:\UnityProjects\CardAdventure\PROJECT_STATUS.md` — 일자별 작업 로그 (약 5,100행)
- `C:\UnityProjects\CardAdventure\Assets\Scripts\` — 실제 구현 코드
- `C:\UnityProjects\CardAdventure\Assets\ScriptableObjects\` — 카드·적·대화·자격증·인트로 데이터

> Claude Desktop은 이 문서만으로 발표자료 초안을 만들 수 있도록 작성되었다. 더 깊은 사실 확인이 필요하면 위 4개 문서를 우선 참조한다.
