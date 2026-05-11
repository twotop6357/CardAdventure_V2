# CardAdventure — 프로젝트 개발 지침

> Unity 2D · 싱글플레이어 카드 배틀 어드벤처 RPG  
> 이 문서는 Claude가 이 프로젝트에서 작업할 때 항상 준수해야 할 에셋 사용 규칙과 구조적 지침입니다.

---

## 프로젝트 개요

- **장르**: 탑다운 어드벤처 + 턴제 카드 배틀 RPG (싱글플레이어)
- **엔진**: Unity 2D (URP)
- **현재 기획 범위**: 1챕터 베르데 평원 완성형
- **주요 씬 구조**: 어드벤처 씬 ↔ 카드 배틀 씬 분리, SceneManager로 전환
- **네임스페이스**: 자체 코드는 `CardAdventure` 네임스페이스 사용

---

## 설치된 에셋 목록 및 사용 규칙

### 1. CCGKit (Core)
**위치**: `Assets/Assets/CCGKit/Core/Scripts/Foundation/`  
**역할**: 카드 게임 데이터 구조와 데모 UI 패턴 참고용

**사용 규칙**
- 현재 프로젝트의 단기 구현은 자체 `CardAdventure.CardData`, `EnemyData`, `StatusEffectData` 구조를 기준으로 한다.
- CCGKit의 `Card`, `CardType`, `CardSet`, `RuntimeCard`, `GameConfiguration`은 참고용으로 활용하되, 기존 자체 데이터 구조와 충돌하지 않게 어댑터/변환 계층을 둔다.
- 상태이상(독, 약화, 취약 등)은 현재 `StatusEffectData`와 전투 상태 시스템을 우선 사용한다. CCGKit의 `Stat`, `Effect`, `Trigger` 구조는 설계 참고용이다.
- `CCGKit` 네임스페이스의 클래스를 직접 수정하지 않는다. 필요 시 상속(Inheritance)으로 확장한다.
- **네트워킹 관련 클래스 (`Foundation/Networking/`, `Demo/Scripts/Networking/`) 는 이 프로젝트에서 절대 사용하지 않는다.** 싱글플레이어 전용 프로젝트이다.
- `MirrorStub` (`Assets/Plugins/MirrorStub/`)은 컴파일 에러 방지용 스텁이며 실제 기능이 없다. Mirror의 어떤 API도 게임 로직에서 호출하지 않는다.

**핵심 클래스 참조**
```
GameConfiguration  — 전체 게임 데이터 (카드, 타입, 키워드, 존)
Card               — 카드 정의 데이터 (ScriptableObject 방식)
RuntimeCard        — 전투 중 카드 인스턴스
CardType           — 공격 / 방어 / 스킬 / 상태이상 등 타입 정의
Deck               — 덱 컨테이너
GameState          — 현재 게임 상태 스냅샷
EffectSolver       — 카드 효과 해석기 (서버 코드이나 싱글플레이 로직 참조용)
```

---

### 2. CCGKit Demo UI 스크립트
**위치**: `Assets/Assets/CCGKit/Demo/Scripts/Game/`  
**역할**: 카드 배틀 UI의 참조 구현 (CardView, HandCard, GameUI 등)

**사용 규칙**
- Demo 스크립트는 **참조(레퍼런스) 용도**로만 활용한다. 직접 씬에 붙여 쓰지 않는다.
- `CardView`, `HandCard`, `BoardCreature` 등의 UI 패턴을 학습해 자체 UI 스크립트를 작성한다.
- `GameScreen`, `LobbyScreen` 등 멀티플레이어 전제 스크립트는 참조하지 않는다.
- 카드 드래그/드롭은 `IPointerDragHandler`, `IDropHandler` 인터페이스를 사용한다 (HandCard.cs 참조).

---

### 3. DOTween PRO
**위치**: `Assets/Plugins/Demigiant/DOTween/`, `Assets/Plugins/Demigiant/DOTweenPro/`  
**역할**: UI 및 게임오브젝트 애니메이션 (트위닝)

**사용 규칙**
- 모든 트위닝 코드 상단에 `using DG.Tweening;` 을 명시한다.
- **코드 기반 트위닝 우선**: 간단한 이동/페이드/스케일은 코드로 작성한다.
- **DOTweenAnimation 컴포넌트**: Inspector에서 시각적으로 설정하는 경우에만 사용 (복잡한 시퀀스 제외).
- 씬 전환 시 반드시 `DOTween.KillAll()` 을 호출해 진행 중인 트윈을 모두 정리한다.
- 카드 연출의 기본 패턴:
  ```csharp
  // 카드 사용 연출
  card.transform.DOMove(targetPos, 0.4f).SetEase(Ease.OutSine);
  
  // 데미지 피격 연출
  DOTween.Sequence()
      .Append(target.GetComponent<Image>().DOColor(Color.red, 0.1f))
      .Append(target.GetComponent<Image>().DOColor(Color.white, 0.1f));
  
  // 씬 전환 전 정리
  DOTween.KillAll();
  ```
- `DOTweenStub` (`Assets/Plugins/DOTweenStub/`) 파일은 비어있다. DOTween Pro가 실제 구현을 제공한다.
- TextMeshPro 대상 트위닝은 `using DG.Tweening;` 만으로 사용 가능하다 (DOTweenTextMeshPro 모듈 포함).

---

### 4. SPUM (Soonsoon Pixel Unit Maker) v1.8.8
**위치**: `Assets/Assets/SPUM/`  
**역할**: 픽셀 아트 2D 캐릭터 스프라이트 조합 및 애니메이션 후보 시스템

**사용 규칙**
- SPUM은 현재 프로젝트에서 거의 사용되지 않으므로, 새 캐릭터 제작 전에 먼저 실제 씬/프리팹과 생성된 에셋을 확인한다.
- SPUM 캐릭터를 새로 사용할 경우 프리팹은 SPUM 에디터 (`Assets/Assets/SPUM/Scene/SPUM_Scene`)에서 생성·편집한다. 코드로 직접 조합하지 않는다.
- 생성된 유닛 프리팹은 `Assets/Resources/SPUM/SPUM_Units/` 에 저장된다. 이 경로를 변경하지 않는다.
- 스프라이트 리소스는 `Assets/Resources/SPUM/SPUM_Sprites/` 에 위치한다.
- 런타임에서 캐릭터 애니메이션은 `SPUM_Prefabs` 컴포넌트를 통해 제어한다:
  ```csharp
  // 애니메이션 상태 변경
  spumPrefabs._anim.Play("IDLE");  // IDLE, MOVE, ATTACK, DAMAGED, DEBUFF, DEATH, OTHER
  ```
- `PlayerState` 열거형을 활용해 상태 전이를 명확히 관리한다:
  ```csharp
  public enum PlayerState { IDLE, MOVE, ATTACK, DAMAGED, DEBUFF, DEATH, OTHER }
  ```
- SPUM 업데이트는 `Assets/Assets/SPUM/Package/SPUM188.unitypackage`를 통해 제공된다. **직접 임포트하기 전에 기존 SPUM 폴더를 삭제**해야 한다 (README 지침).
- `SPUM_AnimationController`는 애니메이션 재생/속도/타임라인 유틸리티 클래스다. 배틀 씬에서 적 유닛 연출에 활용한다.
- 카드 배틀 씬에서 플레이어/적 캐릭터 비주얼은 현재 구성된 프리팹과 스프라이트를 우선 사용하고, SPUM 프리팹은 필요할 때만 후보로 검토한다.

---

### 5. Better UI v3.2 (TheraBytes)
**위치**: `Assets/TheraBytes/BetterUI/`, `Assets/Assets/TheraBytes/BetterUI/`  
**역할**: 해상도 독립적인 반응형 UI 레이아웃 시스템

**사용 규칙**
- **모든 UI 레이아웃**은 BetterUI의 `BetterContentSizeFitter`, `BetterAspectRatioFitter`, `BetterGridLayoutGroup` 등을 기본으로 사용한다. Unity 기본 LayoutGroup을 그대로 쓰지 않는다.
- 해상도별 UI 설정(위치, 크기, 여백)은 BetterUI의 Screen Configuration 시스템으로 관리한다.
- TextMeshPro 연동이 활성화되어 있다 (`TheraBytes.BetterUi.Runtime.asmdef` 설치 완료). TMP 기반 UI 요소에도 BetterUI 컴포넌트를 적용할 수 있다.
- 카드 배틀 UI의 손패 배치, 에너지 바, HP 바 등은 모두 BetterUI로 구현한다.
- BetterUI 관련 추가 예제 패키지 (`Packages/` 폴더 내 `.unitypackage`) 는 필요시 직접 임포트한다.

---

### 6. Febucci Text Animator
**위치**: `Assets/Plugins/Febucci/Text Animator/`  
**역할**: TextMeshPro 텍스트에 애니메이션 효과 적용 (흔들림, 페이드인, 웨이브 등)

**사용 규칙**
- 대화 시스템, NPC 말풍선, 카드 효과 설명 텍스트에 활용한다.
- `TextAnimatorPlayer` 컴포넌트를 TMP 오브젝트에 붙여 사용한다.
- 태그 기반 문법으로 인라인 애니메이션을 적용한다:
  ```
  <shake>흔들리는 텍스트</shake>
  <wave>물결치는 텍스트</wave>
  <fade>페이드인 텍스트</fade>
  ```
- 어셈블리 참조: `Febucci.TextAnimator.TMP.Runtime` (TMP 통합 버전)
- 타이핑 효과(타자기 효과)는 `TextAnimatorPlayer`의 `ShowText()` 메서드로 구현한다.
- 추가 통합 패키지 (`Extra/`, `Integrations/` 폴더의 `.unitypackage`)는 필요할 때만 임포트한다.

---

## 전체 아키텍처 규칙

### 씬 구조
```
AdventureScene   — 탑다운 이동, NPC 대화, 상점
BattleScene      — 카드 배틀 턴제 시스템
```
- 씬 간 데이터 전달은 `ScriptableObject` 기반 공유 데이터나 `DontDestroyOnLoad` 싱글턴으로 처리한다.
- 씬 전환 전 반드시 `DOTween.KillAll()` 호출.

### 스크립트 작성 규칙
- 네임스페이스: `namespace CardAdventure { ... }`
- CCGKit 클래스 확장 시 상속 사용. `CCGKit` 내부 코드는 직접 수정하지 않는다.
- SPUM, BetterUI, Febucci 라이브러리 코드도 직접 수정하지 않는다.

### UI 계층
```
Canvas (BetterUI Screen)
  ├── HUD Layer        — HP바, 에너지, 턴 표시 (BetterUI)
  ├── Card Layer       — 손패, 보드 카드 (CCGKit 참조 + 자체 구현)
  ├── Dialogue Layer   — NPC 대화창 (Febucci Text Animator)
  └── Popup Layer      — 알림, 확인창 (BetterUI + DOTween)
```

### 애니메이션 우선순위
1. **캐릭터 스프라이트 애니메이션** → 현재 씬/프리팹의 Animator 우선, SPUM 사용 시 `SPUM_Prefabs._anim`
2. **UI 전환/연출** → DOTween PRO
3. **텍스트 효과** → Febucci Text Animator
4. **카드 움직임** → DOTween PRO (`transform.DOMove`, `DOScale`)

---

## 파일 및 폴더 규칙

```
Assets/
├── Assets/          — 서드파티 에셋 (CCGKit, SPUM, TheraBytes)
├── Plugins/         — 플러그인 (DOTween, Febucci, MirrorStub)
├── Resources/
│   └── SPUM/        — SPUM 생성 유닛 및 스프라이트 (경로 변경 금지)
├── Scenes/          — 프로젝트 씬
├── Settings/        — URP, Input 등 프로젝트 설정
└── [자체 코드]/     — Scripts/, Prefabs/, ScriptableObjects/ 등 직접 생성
```

- `Assets/Assets/` 안의 서드파티 코드는 **수정하지 않는다** (단, 버그 수정이 불가피할 경우 주석으로 명시).
- `Assets/Plugins/MirrorStub/`은 컴파일 스텁이며 건드리지 않는다.
- `Assets/Plugins/DOTweenStub/DOTweenStub.cs`는 빈 파일이며 건드리지 않는다.

---

## 주의사항 요약

| 항목 | 규칙 |
|------|------|
| Mirror / 네트워킹 | 절대 사용 금지 (싱글플레이어) |
| CCGKit 내부 수정 | 금지 — 상속으로 확장 |
| SPUM 캐릭터 편집 | 반드시 SPUM 에디터 씬 사용 |
| 씬 전환 | DOTween.KillAll() 필수 호출 |
| UI 레이아웃 | Unity 기본 Layout 대신 BetterUI 사용 |
| 텍스트 애니메이션 | TMP + Febucci TextAnimator 조합 |
| DOTween 초기화 | 프로젝트 시작 시 DOTween.SetTweensCapacity() 설정 권장 |

---

## 기획서 기준 개발 지침

이 프로젝트의 현재 기준 기획서는 **CardAdventure 기획서 v1.1 (2026.05)** 이다. 모든 구현, 에셋 선택, UI 구성, 데이터 추가는 아래 방향을 우선한다.

- 핵심 목표는 밝은 판타지 세계 **카르타니아**에서 초급 배틀러 자격증을 취득하는 **1챕터 완성형 싱글플레이 RPG**다.
- 장르는 **탑다운 2D 어드벤처 + 턴제 카드 배틀 RPG**이며 로그라이크 요소는 넣지 않는다.
- 현재 범위는 기존 3챕터 장기 구상이 아니라 **챕터 1: 베르데 평원** 완성이다. 항구 도시, 길드 본부, 복잡한 엔딩 분기는 추후 확장 후보로만 남긴다.
- 플레이 우선순위는 **카드 전략 → 수집/덱빌딩 → 탑다운 탐험 → 캐릭터/NPC 대화 → 자격증 성장 서사 → 귀여운 픽셀 감성** 순서다.
- 어드벤처 파트는 초기 포켓몬스터를 떠올리게 하는 한 칸 이동 감각, 밝고 평화로운 루미나 마을, 명확한 시험 루트를 목표로 한다.
- 챕터 구성은 플레이어 집 앞 시작, 루미나 마을, 평원 길, 길드 시험장, 시험관 릴라, 튜토리얼 보스 **매직 크로우**, 초급 배틀러 자격증 엔딩 UI 순서로 진행한다.
- 플레이어 클래스는 전사, 마법사, 도적 3종으로 확정한다. 루미나 마을의 `JobChangerNPC`/길드 전직관을 통해 처음부터 자유 전직 가능하며, 직업별 덱은 별도로 저장한다.
- 카드 배틀 기본값은 전투 시작 5장 드로우, 매 턴 1장 추가 드로우, 손패 유지, 기본 에너지 3, 카드 효과에 의한 에너지 확장 가능, 덱 소진 시 버린 카드 더미 재셔플이다.
- 방어막은 피해를 먼저 흡수하며, 카드/직업 효과에 따라 유지 가능하다. 적 의도는 아이콘과 대략적인 수치로 표시한다.
- 상태이상은 독, 약화, 취약, 힘, 재생을 사용하며 스택 수치와 턴 수를 함께 관리한다.
- 카드 수집은 전투 보상, 상점, NPC 선택지 보상으로 구성한다. 공용 카드는 사용하지 않고 직업별 카드 중심으로 설계한다.
- 일반 전투 보상은 골드와 **카드 3장 중 1장 선택**을 기본으로 한다. 모든 전투는 골드를 지급한다.
- 상점은 첫 전투 후 열리며, 회복 물약은 고정 판매하고 카드 상품은 전투 1회마다 갱신한다. 상점에는 직업별 카드 탭을 제공한다.
- 회복 아이템은 필드에서 언제든 사용하는 작은 회복약/큰 회복약을 기본으로 하며, 전투 중 아이템 사용은 현재 범위에 포함하지 않는다.
- 저장/로드는 일반 RPG 방식으로 시작하며 세이브 슬롯은 3개다. 패배 시 게임오버 화면 후 저장 데이터를 불러오는 흐름을 사용한다.
- 자격증은 성장 시스템이라기보다 1챕터 클리어를 기념하는 결과 UI다. 엔딩 자격증 UI에는 플레이 시간, 최종 직업, 승리 횟수, 수집 카드 수와 직업별 칭호를 표시한다.
- 비주얼은 밝은 도트 픽셀 감성을 중심으로 한다. 기존 v1.0의 노트 필기/스케치 스타일은 사용하지 않고, 현재 제작된 카드 프리팹의 아트풍과 판독성을 기준으로 카드 UI를 다듬는다.
- 새 기능은 1챕터 완성에 직접 필요한지 먼저 확인한다. 대규모 카드 풀, 3챕터 확장, 복잡한 후반 시스템은 현재 우선순위가 아니다.

### 현재 프로젝트 자산 활용 우선순위

- **SPUM**: 사용 가능한 픽셀 캐릭터 에셋 후보로 참고한다. 현재 프로젝트에서는 SPUM이 거의 사용되지 않으므로, 새 캐릭터를 만들기 전에 실제 씬/프리팹 구조와 이미 생성된 에셋을 먼저 확인한다. `Assets/Assets/SPUM/Resources/Addons/Legacy/0_Unit/0_Sprite/`에 인간, 엘프, 오크, 스켈레톤, 의상, 갑옷, 무기 파츠가 있으므로 필요한 경우 조합 가능 여부를 확인한다.
- **탑다운 캐릭터 키 기준**: 플레이어와 모든 NPC의 화면상 키는 테스트 NPC `NPC_BaramIroGun`의 SpriteRenderer 월드 높이 `1.3304521`을 기준으로 통일한다. 새 NPC/플레이어 비주얼을 추가하거나 플레이어 직업을 변경할 때는 `AdventureGridUtility.ReferenceCharacterVisualHeight`와 `GetVisualScaleForReferenceHeight(Sprite)`를 사용해 스프라이트 높이만 기준값에 맞추고, 폭은 원본 비율에 맡긴다. 런타임에서 플레이어 비주얼을 교체하면 `PlayerController.SetVisual(...)` 또는 `RefreshVisualAlignment()`를 호출해 직업 변경 후에도 키 기준을 다시 적용한다.
- **탑다운 발밑 충돌 기준**: 플레이어 실제 충돌은 `BoxCollider2D size=(1,1), offset=(0,-0.5)`로 발 위치가 bounds 중심이 되게 유지한다. NPC 실제 충돌도 월드 bounds `1x1` 기준의 `BoxCollider2D`를 사용하며, 루트 스케일이 1이 아니면 `AdventureGridUtility.ConfigureFootCollider(...)`로 로컬 size를 역보정한다. NPC의 BoxCollider2D 중심은 반드시 해당 NPC `SpriteRenderer.bounds.min.y`(스프라이트 하단/발 위치)에 맞춘다. 루트에 SpriteRenderer가 직접 붙은 NPC도 `offset=0`으로 두지 말고 `AdventureGridUtility.ConfigureFootCollider(collider, transform, spriteRenderer, cellSize)`를 사용해 각 스프라이트의 발 위치로 offset을 계산한다. 모든 NPC는 시작 시 발 콜라이더 중심을 가장 가까운 Grid 셀 중심으로 스냅해야 한다. 움직이는 NPC는 `NpcMovement`, 움직이지 않는 대화/상점/전직 NPC는 `NpcTileAlignment` 또는 동일한 `AdventureGridUtility.SnapOwnerFootToNearestCell(...)` 흐름을 사용한다. 새 NPC에는 `Rigidbody2D(Kinematic, gravityScale=0, freezeRotation=true)` + 솔리드 `BoxCollider2D` + `NpcInteractable`을 기본으로 붙이고, 충돌용 `CircleCollider2D`는 추가하지 않는다.
- **CCGKit**: 카드 게임 구조, 카드 UI 패턴, 효과/트리거 설계 참고용으로 사용한다. 단, 현재 프로젝트에는 자체 `CardAdventure.CardData`, `EnemyData`, `StatusEffectData`가 이미 있으므로 단기 구현에서는 이 구조와 충돌하지 않게 어댑터/변환 계층을 둔다.
- **DOTween PRO**: 카드 이동, 손패 정렬, 데미지 피드백, 팝업, 씬 전환 페이드에 사용한다.
- **Better UI**: 카드 배틀 HUD, 손패, 상점, 덱 편집, 대화창, 저장/로드 UI의 반응형 레이아웃에 사용한다.
- **Febucci Text Animator**: NPC 대화, 시험관 연출 대사, 카드 효과 강조 텍스트에 사용한다.
- **Unity 2D 패키지**: Tilemap, Pixel Perfect, Aseprite Importer, PSD Importer, SpriteShape, Cinemachine, Input System, URP 2D Renderer가 설치되어 있으므로 탑다운 맵과 카메라, 입력은 이 패키지들을 우선 활용한다.

### 호환성 점검 규칙

- 코드 답변 또는 직접 수정 시 `CardAdventure` 네임스페이스, Unity 6/URP 17.3, Input System 1.19, TextMeshPro, BetterUI, DOTween, Febucci, SPUM과의 컴파일 호환성을 확인한다.
- `CardData.CardType`과 CCGKit의 `CardType`처럼 이름이 겹치는 타입은 네임스페이스를 명시해 충돌을 피한다.
- 신규 ScriptableObject 필드는 기존 `.asset` 직렬화가 깨지지 않도록 기본값을 제공하고, 필드 삭제/이름 변경은 피한다.
- 서드파티 폴더(`Assets/Assets/`, `Assets/Plugins/`)는 직접 수정하지 않는다. 필요한 기능은 `Assets/Scripts/` 아래 자체 코드로 감싼다.
- Unity 설정, 씬, 프리팹, ScriptableObject를 변경할 때는 변경 목적과 영향 범위를 함께 기록한다.
- 답변은 간결하게 작성하되, 코드나 설정 변경을 제안할 때는 관련 파일, 의존 패키지, 예상 컴파일 이슈를 빠뜨리지 않는다.

## Claude Desktop 전용 지침

- Claude Desktop에서 작업할 때도 이 `CLAUDE.md`를 최상위 지침으로 사용한다.
- 작업 시작 시 루트의 `PROJECT_STATUS.md`를 먼저 읽고 현재 진행 상황, 최근 변경, 다음 작업, 주의사항을 확인한다.
- 작업 종료 시 `PROJECT_STATUS.md`의 진행 상황, 최근 변경, 다음 작업, 검증 결과를 갱신해 다른 에이전트가 바로 이어서 작업할 수 있게 한다.
- 답변은 한국어로 간결하고 정확하게 작성한다. 구현 세부 설명은 필요한 만큼만 포함하고, 변경한 파일과 검증 결과를 우선 보고한다.
- 긴 작업은 먼저 현재 에셋과 기존 코드 구조를 확인한 뒤 진행한다. 특히 카드/전투/상태이상 관련 답변은 `Assets/Scripts/Data/`와 `Assets/ScriptableObjects/`의 실제 구조를 기준으로 한다.
- 기획서와 충돌하는 제안을 하지 않는다. 범위를 넓히는 아이디어는 “추가 제안”으로 분리하고 기본 구현에는 포함하지 않는다.
- Claude Desktop에서 직접 Unity Editor를 조작할 수 없는 경우, 필요한 Unity 작업을 메뉴 경로, 오브젝트 경로, 컴포넌트명 기준으로 명확히 적는다.

## Antigravity 병행 작업 지침

- Antigravity에서 작업할 때는 루트의 `AGENTS.md`와 이 `CLAUDE.md`를 함께 참조한다.
- 작업 시작 시 `PROJECT_STATUS.md`를 먼저 읽고, 작업 종료 시 같은 파일을 갱신한다.
- Antigravity는 코드 탐색, 리팩터링 후보 정리, 테스트/컴파일 로그 분석에 우선 활용하고, Unity 씬/프리팹/ScriptableObject 변경은 변경 범위를 작게 유지한다.
- Claude Desktop과 Antigravity가 같은 파일을 동시에 수정하지 않도록 작업 단위를 나눈다. 예: Claude Desktop은 기획/데이터 설계, Antigravity는 특정 스크립트 구현 또는 컴파일 오류 수정.
- 작업 전후에는 `Assets/Scripts/`, `Assets/ScriptableObjects/`, `ProjectSettings/`, `Packages/manifest.json` 변경 여부를 확인하고, 예상치 못한 서드파티 에셋 변경은 되돌리지 말고 먼저 원인을 확인한다.

## 에이전트 인수인계 규칙

모든 에이전트(Codex, Claude Desktop, Antigravity)는 작업 전후로 루트의 `PROJECT_STATUS.md`를 사용한다.

- **작업 시작 전**: `PROJECT_STATUS.md`의 현재 목표, 최근 변경, 진행 중 작업, 다음 작업, 주의사항을 확인한다.
- **작업 중**: 작업 범위가 바뀌거나 충돌 가능성이 생기면 `PROJECT_STATUS.md`에 남길 내용을 메모한다.
- **작업 종료 전**: 아래 항목을 반드시 갱신한다.
  - 현재 상태 요약
  - 이번 작업에서 변경한 파일
  - 검증 결과 또는 검증하지 못한 이유
  - 다음 에이전트가 바로 할 수 있는 작업
  - 충돌/주의사항
- **기록 방식**: 길게 쓰지 말고 최신 상태를 위에서 바로 파악할 수 있게 유지한다.
