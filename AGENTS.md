# CardAdventure Agent Instructions

이 프로젝트는 Unity 2D 기반 **카드 배틀 자격증 어드벤처**입니다. Antigravity 등 에이전트형 도구는 이 파일과 루트 `CLAUDE.md`를 함께 읽고 작업합니다.

## 기본 원칙

- 답변과 작업 기록은 한국어로 간결하고 정확하게 작성합니다.
- 작업 시작 시 루트 `PROJECT_STATUS.md`를 먼저 읽고, 작업 종료 시 진행 상황과 다음 작업을 갱신합니다.
- 현재 기준 기획서는 **카드 배틀 자격증 어드벤처 — 게임 기획서 v1.0 (2026.05)** 입니다.
- 장르는 탑다운 2D 어드벤처 + 턴제 카드 배틀 RPG이며, 싱글플레이어 전용입니다.
- 자체 코드는 `CardAdventure` 네임스페이스를 사용합니다.
- 코드나 Unity 설정을 바꿀 때는 기존 코드, ScriptableObject 직렬화, URP/Input System/TextMeshPro/DOTween/BetterUI/Febucci/SPUM 호환성을 확인합니다.

## 현재 에셋 활용

- `Assets/Assets/SPUM/`: 플레이어, NPC, 적, 시험관 픽셀 캐릭터와 애니메이션 제작에 우선 사용합니다.
- `Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/`: 2D 픽셀 스타일의 UI 요소(버튼, 아이콘, 대화창 패널, 컨테이너 등) 제작에 사용합니다. (최근 추가됨)
- `Assets/Assets/CCGKit/`: 카드 게임 구조와 데모 UI 참고용입니다. 네트워킹/Mirror 기반 구조는 사용하지 않습니다.
- `Assets/Plugins/Demigiant/`: DOTween PRO로 카드/전투/UI 연출을 구현합니다.
- `Assets/Assets/TheraBytes/BetterUI/`, `Assets/TheraBytes/`: 반응형 UI 레이아웃에 사용합니다.
- `Assets/Plugins/Febucci/Text Animator/`: NPC 대화와 카드 텍스트 연출에 사용합니다.
- `Assets/Scripts/Data/`: 현재 프로젝트의 핵심 데이터 구조(`CardData`, `EnemyData`, `StatusEffectData`)입니다.
- `Assets/ScriptableObjects/Cards/Warrior/`: Phase 1 전사 시작 카드 데이터입니다.

## 구현 우선순위

1. Phase 1: 전사 덱 기반 카드 배틀 기본 루프.
2. Phase 2: 탑다운 이동, 챕터 1 베르데 평원, NPC 대화, 상점.
3. Phase 3: 마법사/도적, 챕터 2~3, 카드 드롭.
4. Phase 4: 밸런싱, 사운드, UI 폴리싱, 저장/로드.

## 주의사항

- `Assets/Assets/`와 `Assets/Plugins/`의 서드파티 코드는 직접 수정하지 않습니다.
- CCGKit의 `CardType`과 `CardAdventure.CardType`처럼 이름이 겹치는 타입은 네임스페이스를 명시합니다.
- 신규 ScriptableObject 필드는 기본값을 제공하고, 기존 필드 삭제/이름 변경은 피합니다.
- 씬 전환 전 DOTween 트윈 정리를 고려합니다.
- Claude Desktop과 병행 작업 시 같은 파일을 동시에 수정하지 않도록 작업 범위를 분리합니다.
- 플레이어와 모든 NPC의 화면상 키는 테스트 NPC `NPC_BaramIroGun`의 SpriteRenderer 월드 높이 `1.3304521`을 기준으로 통일합니다. 새 NPC/플레이어 비주얼을 추가하거나 플레이어 직업을 변경할 때는 `AdventureGridUtility.ReferenceCharacterVisualHeight`와 `GetVisualScaleForReferenceHeight(Sprite)`를 사용해 **스프라이트 높이만** 기준값에 맞추고, 폭은 원본 비율에 맡깁니다. 런타임에서 플레이어 비주얼을 교체하면 `PlayerController.SetVisual(...)` 또는 `RefreshVisualAlignment()`를 호출해 직업 변경 후에도 키 기준을 다시 적용합니다.
- 플레이어/NPC의 이동 충돌 콜라이더는 전신이 아니라 발밑 기준으로 둡니다. 현재 기준: Player 실제 충돌 `BoxCollider2D size=(1,1), offset=(0,-0.5)`로 발 위치가 bounds 중심이 되게 유지하며, Player의 기존 `CircleCollider2D`는 비활성입니다. NPC 실제 충돌은 월드 bounds `1x1` 기준의 `BoxCollider2D`를 사용합니다. 루트 스케일이 1이 아니면 `AdventureGridUtility.ConfigureFootCollider(...)`로 BoxCollider2D 로컬 size를 역보정해 월드 기준 목표 크기를 유지합니다.
- NPC의 BoxCollider2D 중심은 반드시 해당 NPC `SpriteRenderer.bounds.min.y`(스프라이트 하단/발 위치)에 맞춥니다. 루트에 SpriteRenderer가 직접 붙은 NPC도 `offset=0`으로 두지 말고 `AdventureGridUtility.ConfigureFootCollider(collider, transform, spriteRenderer, cellSize)`를 사용해 각 스프라이트의 발 위치로 offset을 계산합니다.
- 모든 NPC는 시작 시 발 콜라이더 중심을 가장 가까운 Grid 셀 중심으로 스냅해야 합니다. 움직이는 NPC는 `NpcMovement`, 움직이지 않는 대화/상점/전직 NPC는 `NpcTileAlignment` 또는 동일한 `AdventureGridUtility.SnapOwnerFootToNearestCell(...)` 흐름을 사용합니다. 새 NPC를 추가할 때는 `Rigidbody2D(Kinematic, gravityScale=0, freezeRotation=true)` + 솔리드 `BoxCollider2D` + `NpcInteractable`을 기본으로 붙이고, 충돌용 `CircleCollider2D`는 추가하지 않습니다.

## 인수인계

- 모든 에이전트는 `PROJECT_STATUS.md`를 공용 진행 상황 문서로 사용합니다.
- 작업을 마치기 전에는 변경 파일, 검증 결과, 다음 작업, 주의사항을 갱신합니다.
- 검증하지 못한 항목은 “미검증”으로 명확히 남깁니다.
