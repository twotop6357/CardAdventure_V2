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

## 인수인계

- 모든 에이전트는 `PROJECT_STATUS.md`를 공용 진행 상황 문서로 사용합니다.
- 작업을 마치기 전에는 변경 파일, 검증 결과, 다음 작업, 주의사항을 갱신합니다.
- 검증하지 못한 항목은 “미검증”으로 명확히 남깁니다.
