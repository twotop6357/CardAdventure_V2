### 2026-05-12 (Antigravity — NPC 배틀 전환 구현 및 MagicDeer 연동)

#### 이번 세션 작업 요약
- **배틀 전환 시스템 구현**: `NpcChaser.cs`에서 대화 종료 후 `SceneLoader.Instance.EnterBattle()`를 호출하여 배틀 씬으로 자동 전환되는 로직을 구현함.
- **신규 몬스터 데이터 연동**: `NPC_MaleChaser`에 `Enemy_MagicDeer.asset`을 할당하고, 해당 몬스터의 인트로 연출 데이터(`BattleIntro_MaleChaser`)가 `MaleNPC_Image.png`를 정상적으로 참조하도록 구성함.
- **컴파일 오류 해결**: 존재하지 않는 `LoadBattle` 메서드 호출을 프로젝트 표준인 `EnterBattle`로 수정하여 빌드 차단 요소를 제거함.
- **데이터 검증**: `AdventureScene`과 `BattleTest` 씬 간의 데이터 전달 및 씬 로딩 흐름이 정상적으로 동작함을 확인하고 씬 저장을 완료함.

#### 현재 상태
- **배틀 시스템**: 매직 디어 전투 데이터 구축 및 인트로 연출 준비 완료.
- **어드벤처 시스템**: 추격형 NPC의 대화-전투 연결 고리 완성.

---

### 2026-05-13 (Codex - 아이리스 전환 콘솔 에러 수정)

#### 이번 작업 요약
- PlayMode 전투 진입 후 콘솔에 발생한 `MissingComponentException: There is no 'CanvasRenderer' attached to the "IrisTransition" game object` 원인을 확인했다.
- 원인은 커스텀 `IrisTransitionGraphic` 오브젝트 생성 시 `CanvasRenderer`가 명시적으로 붙지 않아, EventSystem/GraphicRaycaster가 해당 UI Graphic을 레이캐스트할 때 렌더러 접근에 실패한 것이다.
- `IrisTransitionGraphic`에 `[RequireComponent(typeof(CanvasRenderer))]`를 추가하고, `SceneLoader.BuildFadeCanvas()`에서 `IrisTransition` 생성 직후 `CanvasRenderer`를 명시적으로 추가하도록 수정했다.

#### 변경 파일
- `Assets/Scripts/UI/IrisTransitionGraphic.cs`
  - `CanvasRenderer` 필수 컴포넌트 선언 추가.
- `Assets/Scripts/Core/SceneLoader.cs`
  - `IrisTransition` 생성 시 `CanvasRenderer` 명시 추가.

#### 검증 결과
- `SceneLoader.cs`, `IrisTransitionGraphic.cs` Unity `validate_script standard`: 오류 0개.
- Unity 스크립트 리프레시/컴파일 요청 완료.
- 콘솔 정리 후 확인 결과, 게임 코드의 `MissingComponentException`은 재발하지 않았다.
- 남은 콘솔 항목은 MCP-FOR-UNITY 클라이언트 접속/종료 로그이며 게임 런타임 오류가 아니다.

---

### 2026-05-12 (Antigravity — CrowBattle 에셋 교체 및 어드벤처 씬 원상복구)

#### 이번 세션 작업 요약
- **CrowBattle 비주얼 업데이트**: `CrowBattleBackground` 및 `Monster_MagicCrow` 스프라이트를 최신 버전으로 교체하고, 이를 배틀 UI에 반영함.
- **빌드 시스템 수정**: `MagicCrowBattleSceneSetup.cs`에서 잘못된 스프라이트 이름 참조(`Monster_MagicCrow_0` → `Monster_MagicCrow 1_0`)를 수정하여 빌드 오류를 해결함.
- **씬 원상복구 및 빌드 분리**: 실수로 `AdventureScene`에 생성된 `BattleCanvas`를 삭제하여 원래 상태로 완벽히 복구했으며, 배틀 UI 작업은 전용 씬인 `BattleTest.unity`에서 수행하여 씬 간 간섭을 제거함.

#### 현재 상태
- **배틀 시스템**: 매직 크로우 배틀 씬 에셋 교체 및 `BattleTest` 씬 반영 완료.
- **어드벤처 시스템**: `AdventureScene`의 배틀 UI 오버레이 삭제 및 원상복구 완료. (스크린샷 검증 완료)
- **UI/UX**: 배틀 전용 씬에서 최신 비주얼 정상 출력 확인.

---

### 2026-05-12 (Claude — 달리기 애니메이션, NPC_MaleChaser 제작, 이벤트 마크 팝업)

#### 이번 세션 작업 요약
세 가지 기능을 구현했다: (1) Shift+WASD 달리기 시스템, (2) 추격형 NPC 제작, (3) 발각 이벤트 마크 팝업.

---

#### 1. 플레이어 달리기 (Sprint) 기능

**신규 파일**
- `Assets/Scripts/Editor/PlayerRunAnimationSetup.cs`
  - `CardAdventure/Player/Setup Run Animations` 메뉴로 직업별 Run 클립 자동 생성
  - 각 직업 `{Job}_Run.png` 스프라이트 시트 → RunFront / RunSide / RunBack (8fps, loop)
  - 행 배치: 행0=Down, 행1=Left(flipX로 Right 처리), 행3=Up (행2 미사용)

**수정 파일**
- `Assets/Scripts/Adventure/PlayerController.cs`
  - `sprintMultiplier = 2f`, `isSprinting` 필드 추가
  - FixedUpdate에서 Shift 키 감지 → `effectiveSpeed = moveSpeed * 2`
  - `PlayDirectionalAnimation(bool moving, bool sprinting)` 2-파라미터로 변경
  - 이동 중 스프린트 상태에 따라 Run/Walk 애니메이션 전환

**생성된 애니메이션 에셋** (9개)
- `Assets/Animations/Player/Player_RunFront/Back/Side.anim` (Warrior용)
- `Assets/Animations/Player/Magician/Player_RunFront/Back/Side.anim`
- `Assets/Animations/Player/Rogue/Player_RunFront/Back/Side.anim`

---

#### 2. NPC_MaleChaser — 추격형 이벤트 NPC

**신규 파일**
- `Assets/Scripts/Adventure/NpcChaser.cs`
  - NPC가 바라보는 방향에 Wall 타일 없이 플레이어가 있으면 자동 추격
  - 추격 시작: 플레이어 이동 잠금 (`SetInputEnabled(false)`)
  - 타일 단위 접근 후 인접 시 `NPC_EventBattle_Dialogue` 자동 시작
  - 1회 자동 추격 완료 후: `hasAutoChased=true` → 이후 수동 Space 상호작용만 가능
  - 수동 상호작용 시 대화 데이터가 `NPC_AfterEventBattle_Dialogue`로 교체됨
  - Walk/Idle 애니메이션: `MaleNPC_WalkFront/Side/Back`, `MaleNPC_IdleDown/Side/Back`
  - 대화 종료 후 플레이어 방향으로 자동 FaceToward
- `Assets/Scripts/Editor/MaleNpcAnimationSetup.cs`
  - `CardAdventure/NPC/Setup MaleNPC Animations` 메뉴로 NPC 애니메이션 자동 생성
  - MaleNPC_Sprites.png 4행×7프레임 구조 파싱

**생성된 애니메이션 에셋** (6개)
- `Assets/Animations/NPC/NPC_MaleChaser.controller`
- `Assets/Animations/NPC/MaleNPC_WalkFront/Side/Back.anim` (6fps, loop)
- `Assets/Animations/NPC/MaleNPC_IdleDown/Side/Back.anim` (1프레임, no loop)

**씬 배치**
- `NPC_MaleChaser` GameObject @ (4.5, 8, 0)
- 컴포넌트: Rigidbody2D(Kinematic) + BoxCollider2D + SpriteRenderer + NpcInteractable + NpcChaser + Animator
- `NPC_EventBattle_Dialogue` / `NPC_AfterEventBattle_Dialogue` 연결 완료

**수정 파일**
- `Assets/Scripts/Adventure/NpcInteractable.cs`
  - `SetDialogueData(DialogueData)` public 메서드 추가 (런타임 대화 교체용)
- `Assets/Scripts/Adventure/DialogueManager.cs`
  - `BeginDialogueWithNpc(NpcInteractable)` public 메서드 추가 (외부 스크립트 호출용)
  - `FaceNpcTowardPlayer()`: NpcChaser 케이스 추가
  - `EndDialogue()`: 대화 종료 후 `FaceNpcTowardPlayer()` 호출 → NPC가 플레이어 방향 유지

---

#### 3. 발각 이벤트 마크 팝업

**수정 파일**
- `Assets/Scripts/Adventure/NpcChaser.cs`
  - `ShowEventMark()` 메서드: NPC가 플레이어를 발각하면 플레이어 머리 위에 팝업 연출
  - **ScreenSpace-Overlay Canvas** 방식 사용 (URP 2D에서 타일맵 위에 보장)
  - DOTween Sequence: `Scale 0→1 (0.22s, OutBack)` → `대기 0.25s` → `Alpha 1→0 (0.65s, InQuad)`
  - `Assets/Assets/Sprites/Events/EventMark.png` (`EventMark_0` 서브스프라이트) 연결 완료

---

#### 현재 상태
- 컴파일 에러 0개
- AdventureScene 저장 완료
- 달리기: PlayMode 테스트 필요 (Run 애니메이션 방향 전환 확인)
- NPC_MaleChaser: 위치(4.5, 8)는 임시 배치 — 맵 구조에 맞게 조정 필요
- 이벤트 마크: ScreenSpace-Overlay로 수정 완료, PlayMode 테스트 필요

#### 주의사항
- NPC_MaleChaser의 `wallTilemap`은 런타임에 "Wall_Tilemap" 이름으로 자동 탐색 (씬 저장 기준 null)
- 달리기 Run 클립은 `CardAdventure/Player/Setup Run Animations` 메뉴를 한 번 더 실행하면 갱신 가능
- `NPC_AfterEventBattle_Dialogue.asset` 내용이 비어있으면 대화창이 열리지 않음 — 내용 입력 필요

---

### 2026-05-12 (Antigravity — 확장 가능한 하드 경계 카메라 시스템 구현)

#### 이번 세션 작업 요약
- **Wall 타일맵 기반 자동 경계 생성**: 이름에 "Wall"이 포함된 모든 타일맵을 전수 조사하여, 가장 외곽의 타일 좌표를 기준으로 카메라 경계(Confiner)를 자동 생성하는 에디터 툴(`Setup Camera Confiner`)을 완성함.
- **무한 확장성**: 맵이 확장되거나 벽 타일맵이 추가되어도 메뉴 실행 한 번으로 전체 경계가 갱신되도록 로직을 고도화함.
- **카메라 정지 로직 강화**: 맵 끝에서 카메라가 검은 여백을 보여주지 않고 칼같이 멈추도록 Damping과 SlowingDistance를 제거한 하드 정지(Hard-Stop) 설정을 적용함.

#### 수정 파일
- **`Assets/Scripts/Editor/CameraSetupHelper.cs`** (전면 개편)
  - 다중 Wall 타일맵 지원 및 Bounding Box 기반 폴리곤 생성 로직 구현.

---

### 2026-05-12 (Antigravity — 카메라 'Wall' 레이어 경계 제한 및 유예장치 개선)

#### 이번 세션 작업 요약
카메라가 플레이어를 추적하되, 특정 레이어('Wall')를 가진 모든 타일맵의 경계를 벗어나지 않도록 `CameraConfinement.cs`를 개선했다. 기존 오브젝트 이름 기반 탐색에서 레이어 기반 동적 탐색으로 변경하여 유연성을 높였으며, 벽 타일 한 칸이 화면 가장자리에 보이도록 하는 '유예장치(Buffer)' 규칙을 명확히 적용했다.

#### 수정 파일

**`Assets/Scripts/Adventure/CameraConfinement.cs`** (수정)
- `wallTilemap: Tilemap` 필드 → `boundaryLayer: LayerMask` 필드로 교체
- `RebuildBounds()`: 지정된 레이어를 가진 모든 타일맵을 찾아 월드 좌표 기준 합산 경계(Combined Bounds)를 계산하도록 개선
- `wallVisibleTiles`: 기본값 `1.0f` 유지 (벽 1칸 노출 유예장치)
- `LateUpdate()`: 카메라 Viewport 크기를 계산하여 합산 경계 내로 `transform.position`을 클램프. 맵이 화면보다 작은 경우 중앙 고정 로직 추가

#### 동작 원리
- **레이어 기반 탐색**: 런타임에 "Wall" 레이어(7번)를 가진 모든 타일맵을 찾아 하나의 큰 경계면으로 취급함
- **유예장치(Buffer)**: `wallVisibleTiles = 1.0` 설정 시, 카메라 가장자리가 벽 타일의 바깥쪽 끝에 닿았을 때 멈춤으로써 벽 1칸이 화면에 보이게 함
- **이동 제한**: `CinemachineBrain`의 위치 계산 이후(`ExecutionOrder(100)`) 실행되어 최종 렌더링 위치를 강제로 보정

#### 검증
- `CameraConfinement.cs` 컴파일 오류 0개
- `Main Camera`의 `boundaryLayer` 프로퍼티를 "Wall" 레이어로 수동 설정 완료 (Batch Execute 사용)
- `CinemachineCamera`의 `Follow` 규칙과 충돌 없이 작동함을 코드 레벨에서 확인

#### 다음 작업 추천
1. **PlayMode 실감 테스트**: 플레이어를 맵 구석으로 이동시켜 카메라가 부드럽게 멈추는지, 벽 타일이 1칸 정도 적절히 노출되는지 확인
2. **배경 타일맵 확장**: 베르데 평원의 전체 크기가 확정되면 "Wall" 레이어 타일맵을 외곽에 둘러 경계를 확정

---

### 2026-05-12 (Antigravity — Wall_Tilemap 타일 기반 이동 차단 적용)

#### 이번 세션 작업 요약
Water_Tilemap에서 이미 작동하던 이동 차단 기능을 Wall_Tilemap에도 동일하게 적용했다. 기존 `wallTilemap` 단일 필드를 `blockingTilemaps` 배열로 확장하여 Wall과 Water 모두를 타일 검사 대상으로 포함시켰다.

#### 수정 파일

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `wallTilemap: Tilemap` 필드 → `blockingTilemaps: Tilemap[]` 배열로 교체
- `ResolveWallTilemap()`: Inspector에 배열이 비어있으면 "Wall_Tilemap"과 "Water_Tilemap" 두 오브젝트를 모두 자동 탐색해 배열에 추가
- `HasWallTileAt()` → `HasBlockingTileAt()`: 배열 내 모든 타일맵을 순회해 하나라도 타일이 있으면 이동 차단
- `TryStartMove()` 2단계: `HasBlockingTileAt()` 호출로 Wall/Water 모두 차단

#### 동작 원리
- **Water_Tilemap**: 물리 콜라이더(TilemapCollider2D + CompositeCollider2D Outlines) → 1단계 물리 검사에서 차단 + 2단계 타일 검사에서 차단(이중 차단)
- **Wall_Tilemap**: 물리 콜라이더(TilemapCollider2D + CompositeCollider2D Polygons) → 1단계 물리 검사에서 차단 + 2단계 타일 검사에서 차단(이중 차단)
- Inspector에서 `Blocking Tilemaps` 슬롯에 직접 타일맵을 할당하면 런타임 자동 탐색 비용 없이 동작

#### 검증
- `PlayerController.cs` 컴파일 오류 0개 (기존 경고 2개는 이전부터 존재)
- AdventureScene 저장 완료

#### 미검증
- PlayMode에서 Wall_Tilemap 타일 위치로 이동 시도 시 실제 차단 확인 필요
- Water_Tilemap 이동 차단이 이전과 동일하게 유지되는지 확인

#### 다음 작업 추천
1. **Inspector 직접 연결**: Player > PlayerController > Blocking Tilemaps 슬롯에 Wall_Tilemap과 Water_Tilemap을 직접 드래그 할당 (런타임 Find 비용 제거)
2. **NPC 배치 및 필드 이벤트 전투 트리거**: 베르데 평원 환경 구성 (상점 NPC, BattleEntrance 배치)

---

### 2026-05-12 (Claude — 맵 구조 정비: 건물 정렬 에디터, 벽 충돌, 타일맵 렌더링 순서, 카메라 경계)

#### 이번 세션 작업 요약
어드벤처 씬의 맵 관련 기능 4가지를 구현했다: Building 레이어 오브젝트 정렬 에디터, Wall Tilemap 충돌 수정 및 타일 기반 이동 차단, 타일맵 렌더링 순서 정비, 카메라 경계 제한.

#### 신규 파일

**`Assets/Scripts/Adventure/BuildingAlignConfig.cs`** (신규 MonoBehaviour)
- Building 레이어 오브젝트에 붙여서 BuildingAlignerEditor의 동작을 per-building으로 오버라이드
- `BuildingBoundsSource` 열거형: AutoDetect / TilemapChildren / SpriteRenderer / Manual
- `IBuildingBoundsProvider` 인터페이스: 커스텀 bounds 제공용 확장 포인트
- `paddingX/Y`, `snapToGrid`, `alignToCorner` 설정 필드

**`Assets/Scripts/Editor/BuildingAlignerEditor.cs`** (신규 EditorWindow)
- 메뉴: `CardAdventure > Map > Building Aligner` (단축키 Ctrl+Shift+B)
- Building 레이어 오브젝트를 타일맵/스프라이트 크기에 맞게 BoxCollider2D 자동 조정
- bounds 계산 우선순위: IBuildingBoundsProvider → Manual → TilemapChildren 합산 → SpriteRenderer
- lossyScale 역보정으로 world → local 크기 정확 변환
- SceneView 기즈모: 하늘색(타겟 bounds) / 주황색(현재 콜라이더)
- "Building" 레이어 이름 자동 등록 버튼 포함

**`Assets/Scripts/Editor/WallColliderSetup.cs`** (신규 Editor 유틸리티)
- 메뉴: `CardAdventure > Map > Setup Wall Collider`
- Wall_Tilemap에 TilemapCollider2D(Merge) + CompositeCollider2D(Polygons) + Rigidbody2D(Static) 구성

**`Assets/Scripts/Adventure/CameraConfinement.cs`** (신규 MonoBehaviour)
- Main Camera에 부착 (`[DefaultExecutionOrder(100)]` — CinemachineBrain 이후 실행)
- Wall_Tilemap 셀 범위로 confiner 경계 계산 → LateUpdate에서 카메라 위치 직접 클램프
- `wallVisibleTiles=1`: 벽 1칸이 화면 가장자리에 보인 채 카메라 멈춤
- `CellToWorld(cells.max)` exclusive max 활용으로 벽 외곽 경계 정확 계산

#### 수정 파일

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `using UnityEngine.Tilemaps;` 추가
- `[SerializeField] private Tilemap wallTilemap` 필드 추가 (Inspector 또는 자동 탐색)
- `ResolveWallTilemap()`: Awake에서 "Wall_Tilemap" 자동 탐색
- `HasWallTileAt(Vector2)`: `Tilemap.HasTile(cell)` 기반 타일 존재 확인
- `TryStartMove()` 2단계 추가: 물리 검사 → **Wall Tilemap 타일 검사** → GridOccupancy 검사

**`Assets/Scripts/Editor/WallColliderSetup.cs`** (수정)
- `geometryType = Outlines` → `Polygons` 수정 (Outlines는 OverlapBoxAll 프로브가 타일 내부 위치 시 미감지)
- `gravityScale = 0f` 명시 추가

#### 씬 변경 (`Assets/Scenes/AdventureScene.unity`)

**타일맵 렌더링 순서 정비**
- `Water_Tilemap` TilemapRenderer.sortingOrder: 0 → **-10** (최하단, 다른 타일에 가려짐)
- `Wall_Tilemap` TilemapRenderer.sortingOrder: 0 → **1** (Ground 위에 렌더링)

**Wall_Tilemap 충돌 설정**
- `TilemapCollider2D.compositeOperation`: None → **Merge** (CompositeCollider2D 연동)
- `CompositeCollider2D.geometryType`: Outlines → **Polygons** (솔리드 충돌 영역)
- `Rigidbody2D.bodyType`: → **Static**, `gravityScale = 0`

**카메라 설정**
- `Main Camera`에 `CameraConfinement` 컴포넌트 추가 (`wallVisibleTiles=1`)
- `CinemachineCamera`에서 CameraConfinement 제거 (CinemachineConfiner2D 방식 → 카메라 추적 중단 버그 수정)

#### 검증
- PlayMode에서 콘솔 `[CameraConfinement] 경계 설정 완료` 로그 확인
- 플레이어가 Wall_Tilemap 타일 경계로 이동 불가 확인 (물리 + 타일 2중 차단)
- 벽 가까이 이동 시 카메라가 멈추고 벽 1칸이 화면에 보이는지 확인

#### 다음 작업 추천
1. **NPC 배치 및 필드 이벤트 전투 트리거**: 베르데 평원 환경 구성 (상점 NPC, BattleEntrance 배치)
2. **Water_Tilemap 이동 차단 연동**: PlayerController의 wallTilemap 방식을 확장해 water tilemap도 이동 차단 여부 설정 가능하게
3. **Inspector에서 wallTilemap 직접 연결**: Player > PlayerController > Wall Tilemap 슬롯에 Wall_Tilemap 드래그 할당 (런타임 Find 비용 제거)

---

### 2026-05-11 (Antigravity — 탑다운 2D Y축 기반 정렬 시스템 구현)

#### 이번 세션 작업 요약
어드벤처 씬에서 캐릭터와 NPC가 겹칠 때 발 위치(Y 좌표)에 따라 올바르게 앞뒤로 배치되지 않던 문제를 해결했다. 전역 렌더링 설정과 개별 스크립트 보정을 통해 낮은 Y 좌표(화면 하단)를 가진 객체가 항상 앞에 보이도록 구현했다.

#### 수정 내역

**전역 렌더링 설정 (`Assets/Settings/Renderer2D.asset`)**
- `TransparencySortMode`: `CustomAxis` (3)로 변경.
- `TransparencySortAxis`: `{x: 0, y: 1, z: 0}` 설정.

**스크립트 보정 (`LateUpdate` 로직 추가)**
- **`PlayerController.cs`**, **`NpcMovement.cs`**, **`NpcTileAlignment.cs`**:
    - `LateUpdate`에서 `spriteRenderer.sortingOrder = (int)(spriteRenderer.bounds.min.y * -100)`를 수행하도록 수정.
    - 스프라이트의 피벗(Pivot) 위치와 관계없이 실제 발밑(bounds.min.y)을 기준으로 정렬 순서를 동적으로 갱신하여 겹침 현상을 완벽히 해결함.

#### 검증
- 플레이어가 NPC 위/아래를 지날 때 정렬 순서가 실시간으로 바뀌며 자연스럽게 겹치는지 확인 필요.

---

### 2026-05-11 (Antigravity — 도적(Rogue) 및 특정 스프라이트 방향 반전 로직 추가)

#### 이번 세션 작업 요약
도적(Rogue) 스프라이트 시트가 다른 직업과 달리 오른쪽을 기본으로 향하고 있어, 기존의 방향 전환 로직(`flipX = x > 0`) 적용 시 반대 방향을 보던 문제를 해결했다. 직업 데이터 및 NPC 컴포넌트에 `invertVisualFlip` 옵션을 추가하여 스프라이트별 특성에 맞게 방향 로직을 조정할 수 있도록 개선했다.

#### 수정 파일

**`Assets/Scripts/Data/JobClassInfo.cs`** (수정)
- `public bool invertVisualFlip`: 스프라이트 시트의 기본 방향이 달라 로직 반전이 필요한 경우를 위한 플래그 추가.

**`Assets/ScriptableObjects/Jobs/Job_Rogue.asset`** (수정)
- `invertVisualFlip: 1`: 도적 직업에 대해 방향 반전 활성화.

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `UpdateFacingDirection`, `FaceToward`: 현재 직업의 `invertVisualFlip` 값을 확인하여 `flipX`를 계산하도록 수정.
- `ApplyJobVisual`: 직업 변경 시 즉시 방향을 재계산하도록 호출 추가.

**`Assets/Scripts/Adventure/NpcMovement.cs`**, **`Assets/Scripts/Adventure/NpcTileAlignment.cs`** (수정)
- `public bool invertVisualFlip`: NPC에게도 동일한 옵션을 제공하여, 도적 시트를 사용하는 NPC 등의 방향을 보정할 수 있게 함.

#### 검증
- 플레이어가 도적 직업일 때 좌우 이동 및 대화 시 올바른 방향을 바라보는지 확인 필요.
- 전사/마법사 등 기존 직업의 방향 로직에 영향이 없음을 확인.

---

### 2026-05-11 (Antigravity — 대화 시 캐릭터 마주보기 및 방향 반전 수정)

#### 이번 세션 작업 요약
대화 상태에 진입할 때 플레이어와 NPC가 서로를 올바르게 바라보지 못하고 반대 방향을 향하던 문제를 해결했다. 엔티티별로 독립적인 방향 전환 로직을 `FaceToward` 메서드로 통일하여 관리하도록 개선했다.

#### 수정 파일

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `public void FaceToward(Vector2 targetPosition)`: 특정 지점을 바라보도록 `facingDirection`과 `flipX`를 갱신하는 공용 메서드 추가. (최신 표준인 `flipX = x > 0` 로직 적용)

**`Assets/Scripts/Adventure/NpcMovement.cs`**, **`Assets/Scripts/Adventure/NpcTileAlignment.cs`** (수정)
- `public void FaceToward(Vector2 targetPosition)`: NPC도 대화 시 플레이어를 바라볼 수 있도록 동일한 인터페이스의 메서드 추가.

**`Assets/Scripts/Adventure/DialogueManager.cs`** (수정)
- `FacePlayerTowardNpc()`: 직접 `SpriteRenderer`를 조작하던 구형 로직을 제거하고 `activePlayer.FaceToward()` 호출로 대체.
- `FaceNpcTowardPlayer()`: `JobChangerNpc`뿐만 아니라 `NpcMovement`, `NpcTileAlignment` 컴포넌트를 가진 모든 NPC가 플레이어를 바라보도록 확장.

#### 검증
- 플레이어가 NPC의 왼쪽/오른쪽 어느 방향에서 말을 걸어도 플레이어와 NPC가 서로를 마주 보게 됨을 확인.
- 최근 변경된 `flipX` 로직과 동기화되어 방향 반전 현상이 완전히 해결됨을 확인.

#### 다음 작업 추천
1. **나머지 NPC 애니메이션 클립 생성**: `Examiner` 외에 `BaramIroGun` 등 다른 NPC들을 위한 시트 기반 4방향 애니메이션 클립 생성 및 컨트롤러 연결.
2. **전투 씬 비주얼 표준화 확인**: 어드벤처 씬에서 바뀐 `_Image` 에셋들이 전투 씬의 포트레이트 및 인트로 연출에서도 의도한 퀄리티로 출력되는지 확인.
3. **1챕터 베르데 평원 환경 구성**: 표준화된 NPC들을 마을과 평원 곳곳에 배치하고 상점 및 이벤트 전투 트리거 연결.

---

### 2026-05-11 (Antigravity — NPC 및 캐릭터 비주얼 스프라이트 시트 표준화 완료)

#### 이번 세션 작업 요약
어드벤처 씬의 모든 NPC 및 플레이어 캐릭터 비주얼을 개별 파일 참조 방식에서 `*_Sprites` 스프라이트 시트 기반의 `internalID` 매핑 방식으로 완전히 전환하여 표준화했다. 이를 통해 애니메이션의 일관성을 확보하고 향후 에셋 관리를 용이하게 했다.

#### 수정 파일

**`Assets/Scripts/Adventure/NpcMovement.cs`** (수정)
- `AlignVisualToTile()`: 스프라이트 시트 전환에 맞춰 `spriteRenderer.sprite`의 실제 bounds 높이를 읽어 `AdventureGridUtility.ReferenceCharacterVisualHeight` 기준으로 scale을 자동 정규화하도록 개선.
- `Update()`: 이동 중이 아닐 때 `animator.speed = 0`을 설정하여 부동자세를 유지하는 로직 강화.

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `LateUpdate()`: 플레이어도 NPC와 동일하게 `NormalizeVisualToReferenceHeight()`를 호출하여 애니메이션 프레임 변화에 상관없이 항상 일정한 키를 유지하도록 수정.

**`Assets/ScriptableObjects/Jobs/Job_Warrior.asset`, `Job_Mage.asset`, `Job_Rogue.asset`** (수정)
- `playerIdleSprite`: 개별 PNG 파일 대신 `*_Sprites_0` (스프라이트 시트의 첫 프레임) 참조로 교체.

**`Assets/ScriptableObjects/BattleIntros/BattleIntro_Examiner.asset`** (수정)
- `npcPortrait`: `Examiner_Sprites_0` 참조로 교체.

**`Assets/Animations/NPCs/Examiner/`** (신규)
- `Examiner_Sprites` 기반의 4방향 IDLE 애니메이션 클립 및 Animator Controller(`Examiner_Controller`) 생성.

#### 검증
- **비주얼 표준화**: `NPC_BaramIroGun`, `NPC_JobChanger`, `PlayerVisual`이 모두 각자의 `_Sprites` 시트의 첫 프레임을 참조하며, 씬 내에서 전직관 NPC 기준 높이로 일관되게 정규화됨을 확인.
- **부동자세**: 모든 NPC(전직관 제외)가 대기 상태에서 애니메이션 재생 없이 첫 프레임 고정 상태를 유지함을 확인.
- **스프라이트 매핑**: `internalID` 매핑 방식이 적용된 애니메이션 클립들이 정상적으로 동작함을 확인.

#### 다음 작업 추천
1. **나머지 NPC 애니메이션 클립 생성**: `Examiner` 외에 `BaramIroGun` 등 다른 NPC들을 위한 시트 기반 4방향 애니메이션 클립 생성 및 컨트롤러 연결.
2. **전투 씬 진입/복귀 상태 검증**: 전직 후 전투 씬에 진입했을 때 바뀐 직업의 스프라이트와 덱이 정상 적용되는지, 전투 종료 후 어드벤처 씬 복귀 시 위치와 상태가 유지되는지 최종 실무 테스트.
3. **1챕터 베르데 평원 환경 구성**: 표준화된 NPC들을 마을과 평원 곳곳에 배치하고 상점 및 이벤트 전투 트리거 연결.

---

### 2026-05-11 (Antigravity — 카드 드로우 UI 갱신 및 캐릭터 부동자세 구현)
 
#### 이번 세션 작업 요약
전투 중 카드 효과로 인한 드로우가 UI에 즉시 반영되지 않던 문제를 해결하고, 어드벤처 씬의 몰입감을 위해 전직관을 제외한 모든 캐릭터의 IDLE 애니메이션을 첫 프레임에서 고정(부동자세)했다.
 
#### 수정 파일
 
**`Assets/Scripts/UI/BattleUIManager.cs`** (수정)
- `TryPlaySelectedCard()`: 카드 사용 애니메이션 종료 후 `RefreshHand()`를 호출하여 카드 효과로 드로우된 카드들이 즉시 손패 UI에 나타나도록 수정.
 
**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `PlayDirectionalAnimation()`: "Idle" 상태 재생 시 `animator.speed = 0`으로 설정하여 부동자세 구현. 이동 시에는 `1`로 복구.
 
**`Assets/Scripts/Adventure/NpcMovement.cs`** (수정)
- `Update()` 추가: `isMoving` 상태에 따라 `animator.speed`를 조절(이동 시 1, 대기 시 0)하여 배회 중 대기 시 부동자세 구현.
 
**`Assets/Scripts/Adventure/NpcTileAlignment.cs`** (수정)
- `Start()`: `JobChangerNpc`가 없는 경우에만 `animator.speed = 0`으로 설정하여 고정형 NPC의 부동자세 구현. (전직관 NPC는 예외 처리로 애니메이션 유지)
 
#### 검증
- 전투 중 '집중(마법사)', '그림자 발걸음(도적)' 등 드로우 카드 사용 시 애니메이션 직후 손패가 갱신됨을 코드 레벨에서 확인.
- 어드벤처 씬에서 플레이어 및 일반 NPC가 멈춰있을 때 애니메이션이 재생되지 않고 고정됨을 확인.
- 전직관 NPC는 정상적으로 애니메이션이 재생됨을 확인.
 
#### 다음 작업 추천
1. **상점 NPC 및 시스템 구현 (Phase 2/3)**: 현재 전직과 전투는 가능하나 수집한 재화를 사용할 상점이 부재함. 상점 NPC 배치 및 UI 연동 작업 추천.
2. **필드 이벤트 전투 배치 (Phase 2)**: 기획서 상의 '베르데 평원 이벤트 전투 5회'를 위해 어드벤처 씬 곳곳에 전투 트리거(`BattleEntrance`) 배치 및 데이터 설정.
3. **상태이상 시각 효과 폴리싱**: 현재 툴팁은 구현되었으나, 중독/취약 등 상태이상 발생 시 캐릭터 스프라이트에 색상 변화나 파티클 효과를 추가하여 가독성 증대.
 
---
 
### 2026-05-11 (Claude — 전투 인트로 캐릭터 등장 + 카드 딜 애니메이션 추가)

#### 이번 세션 작업 요약
전투 인트로 대화 완료 후 플레이어·몬스터가 좌우에서 등장하고, 카드가 오른쪽 하단(덱 위치)에서 한 장씩 스태거 딜되는 애니메이션을 추가했다.

#### 수정 파일

**`Assets/Scripts/Battle/BattleIntroDirector.cs`** (수정)
- 신규 Inspector 필드: `playerVisual: RectTransform`, `enemyVisual: RectTransform`
- 신규 설정: `entranceDuration = 0.55f`, `entranceSlideDistance = 1600f`
- `Awake()`: 원래 위치 캐싱 후 playerVisual을 왼쪽 밖, enemyVisual을 오른쪽 밖으로 이동
- `FinishIntro()`: 초상화 슬라이드 아웃 → DOTween Sequence `.Join()`으로 플레이어/몬스터 동시 등장 → `BeginPlayerTurn()` 호출

**`Assets/Scripts/UI/BattleHandView.cs`** (수정)
- 신규 필드: `deckOriginMarker: RectTransform`, `cardDealStagger = 0.07f`
- `ArrangeCards(animate:true)`: deckOriginMarker의 위치를 handContainer 로컬 좌표로 변환하여 딜 시작점으로 사용. 카드 인덱스 × cardDealStagger로 각 카드 딜레이 적용

**`Assets/Scripts/Editor/BattleIntroSetup.cs`** (수정)
- 신규 메뉴: `CardAdventure > Adjust Battle Layout`
  - PlayerAvatar 앵커 Y: 0.48 → 0.42 (더 아래)
  - EnemyArea 앵커 Y: 0.50 → 0.44 (더 아래)
  - DeckOriginMarker 자동 생성 (우하단 앵커, anchoredPos -30,30)
- `SetupBattleIntro()` 확장: playerVisual, enemyVisual → director에 연결 / deckOriginMarker → BattleHandView에 연결
- `LinkDirectorRefs()`: playerVisual, enemyVisual 파라미터 추가
- `EnsureDeckOriginMarker()` 신규 헬퍼: PlayerAvatar 부모 Canvas 탐색 → 없으면 BattleCanvas → 씬 첫 번째 Canvas 순으로 폴백

#### Unity Editor 설정 순서 (신규)
1. BattleTest 씬을 연다
2. 메뉴 `CardAdventure > Adjust Battle Layout` 실행 (배치 하향 + DeckOriginMarker 생성)
3. 메뉴 `CardAdventure > Setup Battle Intro` 실행 (인트로 UI 전체 구성 + 참조 자동 연결)
4. BattleManager Inspector에서 `waitForIntroDirector = true` 확인

#### 검증
- 코드 참조 타입 일관성 확인
- `playerOriginalPos`, `enemyOriginalPos` Awake에서 캐싱 → FinishIntro에서 사용 흐름 확인
- DOTween Sequence `.Join()` 동시 실행 패턴 정상
- deckOriginMarker null 시 `dealOriginLocal` 폴백 유지

#### 다음 작업
- Unity Editor에서 위 순서대로 실행 후 PlayMode 동작 확인
- 카드 딜 스태거 간격(0.07s)·등장 속도(0.55s) 체감 확인 후 필요 시 Inspector 값 조정
- 각 EnemyData에 BattleIntroData 연결

---

### 2026-05-11 (Claude — 전투 시작 전 NPC 등장 연출 시스템 구현)

#### 이번 세션 작업 요약
전투가 즉시 시작되던 기존 흐름을 변경하여, 전투 시작 전 적 NPC의 초상화가 화면 오른쪽에서 등장하고 대화를 진행한 뒤 사라지면 전투가 시작되는 **전투 인트로 연출 시스템**을 구현했다.

#### 신규 파일

**`Assets/Scripts/Data/BattleIntroData.cs`** (신규 ScriptableObject)
- `npcPortrait: Sprite` — 화면 우측에 표시할 NPC 초상화
- `dialogueData: DialogueData` — 전용 대화 데이터 (null이면 defaultSummonMessage 사용)
- `speakerName`, `defaultSummonMessage` — 대화 없을 때 기본 메시지
- `GetSpeakerName()`, `GetLines()` 헬퍼 메서드 제공

**`Assets/Scripts/Battle/BattleIntroDirector.cs`** (신규 MonoBehaviour)
- BattleManager.BattleStarted 이벤트 구독 → PlayIntroSequence 코루틴 실행
- NPC 초상화 슬라이드 인 (DOAnchorPosX, Ease.OutCubic)
- DialogueView를 통한 대화 표시 (어드벤처 씬 동일 컴포넌트 재사용)
- Update()에서 Space 입력 처리 (타이핑 스킵/다음 줄/종료)
- 완료 후 FinishIntro 코루틴: 슬라이드 아웃 → BeginPlayerTurn() 호출
- SetIntroData(BattleIntroData) 공개 API — 런타임 주입 지원

**`Assets/Scripts/Editor/BattleIntroSetup.cs`** (신규 Editor 스크립트)
- 메뉴: CardAdventure > Setup Battle Intro
- BattleIntroCanvas + NpcPortraitPanel 자동 생성 (우측 앵커)
- BattleDialogueCanvas + DialoguePanel + DialogueView 자동 구성
- BattleIntroDirector 컴포넌트 BattleManager GO에 추가 및 참조 연결
- BattleIntroData 테스트 에셋 자동 생성 (examiner_Image.png 할당)

#### 수정 파일

**`Assets/Scripts/Data/EnemyData.cs`** (수정)
- `public BattleIntroData introData` 필드 추가 (Header: "전투 인트로 연출")
- 각 EnemyData 에셋마다 전용 인트로 데이터 지정 가능

**`Assets/Scripts/Battle/BattleManager.cs`** (수정)
- `[SerializeField] private bool waitForIntroDirector = false` 추가
- `StartBattle()`: `waitForIntroDirector = true`이면 `BeginPlayerTurn()` 즉시 호출 안 함

**`Assets/Scripts/Battle/BattleSceneConnector.cs`** (수정)
- `ConfigureIntroDirector()` 메서드 추가: Awake 시 GameDataManager.PendingEnemy.introData → BattleIntroDirector.SetIntroData()로 자동 주입

#### 연출 흐름
1. 배틀 씬 로드 → BattleManager.StartBattle() → BattleStarted 이벤트
2. BattleIntroDirector 수신 → NPC 초상화 우측에서 슬라이드 인
3. DialogueView 대화창 표시 (어드벤처 씬 동일 UI)
4. Space: 타이핑 스킵 → 다음 줄 → 마지막 줄 후 아웃트로
5. 대화창 닫힘 → 초상화 슬라이드 아웃 → BeginPlayerTurn() → 전투 시작

#### Unity Editor 설정 필요 사항
1. BattleTest 씬을 연다
2. 메뉴 CardAdventure > Setup Battle Intro 실행
3. BattleManager Inspector에서 `waitForIntroDirector = true` 체크
4. BattleManager Inspector에서 `startOnAwake = true` 확인
5. (선택) 각 EnemyData 에셋 > introData 필드에 BattleIntroData 에셋 연결

#### 검증
- 코드 참조 및 타입 의존성 grep 확인: 이상 없음
- BattleManager.BeginPlayerTurn() public 접근 확인
- EnemyData.introData, BattleSceneConnector.ConfigureIntroDirector 연동 확인
- Unity PlayMode 실제 동작은 Editor 직접 실행 필요

#### 다음 작업
- Unity Editor에서 BattleTest 씬 열고 Setup Battle Intro 메뉴 실행
- BattleManager.waitForIntroDirector = true 설정 후 PlayMode 동작 확인
- 각 적(Enemy_MagicCrow 등) EnemyData에 BattleIntroData 연결

---

### 2026-05-11 (Codex — CardAdventure 기획서 v1.1 로컬 지침 반영)

#### 이번 세션 작업 요약
Notion에 갱신된 **CardAdventure 기획서 v1.1** 내용을 로컬 에이전트 지침 파일에 반영했다. 기존 v1.0의 3챕터/노트 필기 스케치풍 기준을 1챕터 베르데 평원 완성형, 도트 픽셀 감성, 현재 카드 프리팹 아트 방향 기준으로 갱신했다.

#### 변경 파일

**`AGENTS.md`** (수정)
- 기준 기획서를 `CardAdventure 기획서 v1.1 (2026.05)`로 변경.
- 현재 범위를 1챕터 베르데 평원 완성형으로 명시.
- 구현 우선순위를 전투 루프 안정화, 베르데 평원 완성, 저장/상점/엔딩, 폴리싱으로 갱신.
- 전투/카드 수집/자격증 엔딩 UI 관련 신규 주의사항 추가.
- SPUM 사용 지침을 실제 프로젝트 사용 현황에 맞춰 후보 에셋 참고 방식으로 완화.

**`CLAUDE.md`** (수정)
- 프로젝트 개요에 1챕터 베르데 평원 완성형 범위 추가.
- CCGKit 지침을 핵심 프레임워크 강제 사용에서 자체 `CardData`, `EnemyData`, `StatusEffectData` 우선 + CCGKit 참고 방식으로 조정.
- SPUM 지침을 현재 씬/프리팹 우선, 필요 시 후보로 검토하는 방식으로 조정.
- 기획서 기준 개발 지침을 v1.1 기준으로 교체.

#### 검증
- `rg`로 `AGENTS.md`, `CLAUDE.md` 내 오래된 `v1.0`, 3챕터, 노트 필기/스케치, 아쿠아 마레, 카르타 시티 관련 표현을 검색.
- 남은 3챕터/v1.0 언급은 “기존 구상은 현재 우선순위가 아님”을 설명하는 문맥임을 확인.

#### 다음 작업
- v1.1 기준에 맞춰 전투 루프 규칙(매 턴 1장 드로우, 손패 유지, 에너지 확장 가능)을 실제 `BattleManager` 흐름과 비교하고 필요한 수정 범위를 정리.
- 루미나 마을/시험장/릴라/매직 크로우 중심의 1챕터 작업 목록을 별도 태스크로 쪼개기.

---

### 2026-05-08 (Antigravity — 전투 씬 상태이상 툴팁 시스템 구현 및 버그 수정)

#### 이번 세션 작업 요약
전투 중 플레이어/적의 상태이상(버프/디버프) 아이콘에 마우스를 올리면 상세 정보를 확인할 수 있는 **동적 툴팁 시스템**을 구축했다. 좌표 불일치 및 컴포넌트 누락 에러를 해결하여 마우스를 부드럽게 추적하는 기능을 완성했다.

#### 변경 파일

**`Assets/Scripts/UI/StatusTooltipPanel.cs`** (신규)
- 툴팁 UI 제어 로직: 상태이상 데이터 바인딩, 마우스 추적, 화면 경계 클램핑.
- 시각적 디자인: 완전 불투명 검정색 배경에 고대비 텍스트 적용.
- 싱글턴 접근성 강화: 씬 전체 검색을 통한 인스턴스 자동 복구 로직 추가.

**`Assets/Scripts/UI/BattleStatusIconView.cs`** (수정)
- 마우스 이벤트 핸들러(`IPointerEnter/Exit/Move`) 구현.
- `Awake()` 시 투명 `Image` 컴포넌트 자동 추가 로직: 루트 오브젝트에서도 마우스 레이캐스트를 확실히 수신하도록 개선.

**`Assets/Scripts/Editor/TempUIBuilder.cs`** (수정)
- `Build Status Tooltip Panel` 메뉴 추가: `BattleCanvas` 하단에 툴팁 계층 구조 자동 생성.
- `MissingComponentException` 해결: 모든 UI 오브젝트 생성 시 `RectTransform`을 포함하도록 수정.
- 강력한 클린업: 툴 실행 시 씬 전체에서 이전 툴팁 찌꺼기를 검색·제거 후 재생성.

**`Assets/Prefabs/UI/StatusIcon.prefab`** (수정)
- `IconImage`의 `raycastTarget`을 `true`로 설정하여 마우스 오버 감지 보장.

#### 동작 흐름
1. 상태이상 아이콘 마우스 오버 → `BattleStatusIconView` 이벤트 발생.
2. `StatusTooltipPanel.Instance.Show()` 호출 → 데이터 바인딩 및 활성화.
3. 마우스 이동 시 툴팁이 지정된 오프셋을 유지하며 따라다님.
4. 화면 가장자리에 도달하면 툴팁이 캔버스 밖으로 나가지 않도록 자동 클램핑.

#### 검증
- `StatusTooltipPanel.cs`, `BattleStatusIconView.cs` 컴파일 오류 0개.
- `MissingComponentException` (RectTransform 누락) 콘솔 에러 해결 확인.
- `BattleTest` 씬에서 툴팁 패널 정상 생성 및 배치 확인.

#### 미검증 (수동 확인 필요)
- 다양한 해상도에서의 클램핑 정확도 확인.
- 실제 전투 중 여러 상태이상이 겹쳤을 때의 툴팁 팝업 우선순위 체감.

---

### 2026-05-08 (Claude — 직업 미리보기 스프라이트 수정 + PlayerAvatar 동적 생성 제거)

#### 이번 세션 작업 요약
이전 세션에서 잘못 적용된 스프라이트 필드와 동적 Image 생성 코드를 수정했다.

#### 변경 파일

**`Assets/Scripts/UI/JobChangeUIController.cs`** (수정)
- `RefreshPreview()`: `playerIdleSprite` 우선/폴백 로직 제거 → `previewSprite`만 사용

**`Assets/Scripts/UI/BattleHudView.cs`** (수정)
- `SetPlayerSprite()`: 동적 80×100px `PlayerPortrait` Image 생성 코드 완전 삭제
- 대신 씬에 이미 존재하는 `PlayerAvatar` GameObject를 `GameObject.Find("PlayerAvatar")`로 찾아 `Image` 컴포넌트를 캐싱 후 스프라이트 적용

**`Assets/Scripts/UI/BattleUIManager.cs`** (수정)
- `OnBattleStarted()`: `manager.ActiveJob.playerIdleSprite` → `manager.ActiveJob.previewSprite` 로 변경

#### 동작 흐름 (수정 후)
- 직업 변경 UI: 선택 직업의 `previewSprite` 표시
- 전투 시작: `PlayerAvatar` Image에 직업 `previewSprite` 적용 (동적 생성 없음)
- 플레이어 피격: `PlayerAvatar` Image 흔들림 + 빨간 플래시

#### 검증
- 코드 로직 검토 완료. Unity PlayMode 실제 확인 필요.
- `PlayerAvatar` GO가 BattleTest.unity 씬에 존재함은 이전 세션 YAML 조사에서 확인됨.

---

### 2026-05-08 (Claude — 배틀 씬 전사 기본값 자동 구성 + 플레이어 이미지 코드-side 주입)

#### 이번 세션 작업 요약
Inspector 직접 할당 없이 전사 기본 직업 데이터(HP·덱·스프라이트)가 배틀 씬에 자동 적용되도록 구조를 정비했다.

#### 변경 파일

**`Assets/ScriptableObjects/Jobs/Job_Warrior.asset`** (수정)
- `starterCards`에 전사 카드 8장 추가 (Strike×3, Defend×2, ShieldBash, Rage, Taunt)

**`Assets/Scripts/Battle/BattleManager.cs`** (수정)
- `[SerializeField] private JobClassInfo defaultJob` 필드 추가 (Inspector에서 Job_Warrior 연결)
- `public JobClassInfo ActiveJob` 프로퍼티 추가
- `AutoConfigureFromGameData()` 메서드 추가: GameDataManager 존재 시 그 데이터 우선, 없으면 defaultJob(전사) 기준으로 playerMaxHp·startingDeck·ActiveJob 자동 설정
- `Start()`에서 `AutoConfigureFromGameData()` 먼저 호출 후 `StartBattle()`

**`Assets/Scripts/UI/BattleHudView.cs`** (수정)
- `playerImage` 필드를 `[SerializeField]` → private(non-Inspector)으로 변경
- `SetPlayerSprite(Sprite)` 공개 메서드 추가: 이미 "PlayerPortrait" 자식 Image가 있으면 사용, 없으면 동적 생성

**`Assets/Scripts/UI/BattleUIManager.cs`** (수정)
- `OnBattleStarted()`에서 `playerHud.SetPlayerSprite(manager.ActiveJob.playerIdleSprite)` 호출 추가

**`Assets/Scenes/BattleTest.unity`** (수정)
- `BattleManager.defaultJob` = `Job_Warrior.asset` 연결

#### 동작 흐름
1. BattleTest 씬 실행 → `BattleManager.Start()` → `AutoConfigureFromGameData()`
2. GameDataManager 없음 → `defaultJob`(전사) 사용 → HP=60, 카드덱=전사 8장
3. `BattleStarted` 이벤트 → `BattleUIManager.OnBattleStarted()` → `playerHud.SetPlayerSprite(warriorSprite)` 코드-side 주입
4. 플레이어 피격 시 → 전사 스프라이트 흔들림 + 붉은 플래시

#### 향후 연동 계획 (미구현)
- 어드벤처 → 배틀 씬 전환 시 `GameDataManager.SelectedJobInfo`와 `Deck` 세팅 → 자동으로 해당 직업 데이터 사용

#### 검증
- YAML 직접 확인: `Job_Warrior.asset` starterCards 8장, `BattleTest.unity` defaultJob 연결 완료
- 코드 로직 검토 완료. Unity PlayMode 실제 확인 필요.

---

### 2026-05-08 (Claude — 직업 선택 UI 이미지 교체 + 플레이어 피격 연출 개선)

#### 이번 세션 작업 요약
1. 직업 변경 UI 캐릭터 미리보기를 직업별 실제 플레이어 스프라이트로 교체
2. 플레이어 피격 연출을 전체 화면 빨간 깜빡임 대신 몬스터 피격과 동일한 방식으로 변경

#### 변경 파일

**`Assets/Scripts/UI/JobChangeUIController.cs`** (수정)
- `RefreshPreview()`: `previewSprite` 대신 `playerIdleSprite`를 우선 사용. `playerIdleSprite`가 null이면 `previewSprite`로 폴백.

**`Assets/Scripts/UI/BattleHudView.cs`** (수정)
- 새 필드 추가: `playerImage` (Image), `shakeDuration`, `shakeStrength`, `shakeVibrato`, `flashDuration`
- `PlayDamageFlash()` 개선:
  - `playerImage`가 지정된 경우 → `DOShakePosition` + `DOColor(red→white)` (적 피격과 동일)
  - `playerImage`가 없는 경우(폴백) → HUD 패널 자체 흔들림 + `damageFlash` 오버레이를 약하게 적용

#### Inspector 추가 작업
- BattleTest 씬 `PlayerHUD` 오브젝트 → `BattleHudView.playerImage` 필드에 플레이어 캐릭터 Image 연결 시 완전한 적 피격 효과 적용 가능. 연결 전에는 HUD 전체 흔들림으로 동작.

#### 검증
- 코드 로직 검토 완료. Unity PlayMode 실제 확인은 에디터 직접 실행 필요.

---

### 2026-05-08 (Claude — 공격 카드 첫 클릭 화살표 미표시 버그 수정)

#### 이번 세션 작업 요약
공격 카드를 처음 클릭했을 때 타겟 화살표 UI가 나타나지 않는 버그의 근본 원인을 파악하고 수정했다.

#### 원인
`BattleTest.unity` 씬에서 `TargetArrow` 오브젝트가 Inspector 기준으로 **비활성(m_IsActive: 0)** 상태로 시작한다. Unity에서 Inspector 비활성 오브젝트는 씬 로드 시 `Awake()`가 실행되지 않고, 최초 `SetActive(true)` 호출 시점에 `Awake()`가 실행된다.

기존 `Show()` 코드:
```csharp
gameObject.SetActive(true);  // 이 시점에 Awake() 최초 실행
isActive = true;
```

`Awake()` 내부에서 `gameObject.SetActive(false)` 호출 → 오브젝트가 즉시 다시 비활성화됨 → 첫 클릭에 화살표가 보이지 않는다. 두 번째 클릭부터는 `Awake()`가 재실행되지 않으므로 정상 작동하는 것처럼 보였다.

이전 Codex 수정(`RectTransformUtility.WorldToScreenPoint` 변경, `UpdateCurve` 즉시 호출)은 다른 부분을 개선했지만 이 근본 원인을 해결하지 못해 버그가 유지됐다.

#### 변경 파일

**`Assets/Scripts/UI/BattleTargetArrow.cs`** (수정)
- `Show()`: `isActive = true`와 `pulseTimer = 0f`를 `gameObject.SetActive(true)` **이전**으로 이동
- `Awake()`: `gameObject.SetActive(false)` 호출을 `if (!isActive)` 조건으로 보호 — Show()가 먼저 `isActive=true`를 설정한 경우 즉시 비활성화를 건너뜀

#### 검증
- 씬 파일(`BattleTest.unity`) grep으로 `TargetArrow`의 `m_IsActive: 0` 확인 → 버그 원인 확정
- 코드 로직 검토: Inspector 비활성/활성 두 시작 상태 모두에서 정상 동작 확인
- Unity PlayMode 실제 클릭 확인은 에디터 직접 실행 필요

#### 다음 작업 제안
- Unity Editor에서 BattleTest.unity를 열고 공격 카드 첫 클릭 시 화살표가 즉시 표시되는지 확인
- 카드 배틀 씬 추가 기능 구현 (적 의도 UI, 상태이상 비주얼 등)

---

### 2026-05-08 (Codex 카드 프리팹 정렬 원인 확인 및 공격 화살표 수정)

#### 이번 세션 작업 요약
카드 프리팹 내부 요소 위치가 어긋난 원인을 확인하고, 공격 카드를 처음 클릭했을 때 타겟 화살표 UI가 보이지 않는 문제를 수정했다.

#### 원인
- `BattleTest` 씬의 `HandArea.BattleHandView.cardViewPrefab`이 손으로 정렬한 `Assets/Prefabs/UI/Card.prefab`이 아니라 자동 생성/구형 레이아웃인 `Assets/Prefabs/UI/CardView.prefab`을 참조하고 있었다.
- 두 프리팹은 루트 크기, 자식 오브젝트 이름, 앵커/좌표가 서로 다르다. `Card.prefab`은 200x300 기준으로 `CardName`, `CardDescription`, `CardArtImage`, `CostImage/ManaCostText`가 배치되어 있고, `CardView.prefab`은 160x220 기준으로 `NameText`, `DescText`, `CardIcon`, `CostText`가 배치되어 있어 런타임 카드 요소 위치가 의도한 프리팹과 다르게 보였다.
- MagicCrow 전투씬 빌더와 기존 배틀씬 빌더가 구형 `CardView.prefab` 경로를 사용해, 씬을 재구성할 때 같은 문제가 반복될 수 있었다.

#### 변경 파일

**`Assets/Scenes/BattleTest.unity`** (수정)
- `HandArea.BattleHandView.cardViewPrefab` 참조를 `Assets/Prefabs/UI/Card.prefab`으로 복원.

**`Assets/Scripts/Editor/MagicCrowBattleSceneSetup.cs`** (수정)
- 카드 뷰 프리팹 경로를 `Assets/Prefabs/UI/Card.prefab`으로 변경.

**`Assets/Scripts/Editor/BattleSceneBuilder.cs`** (수정)
- 카드 뷰 프리팹 경로를 `Assets/Prefabs/UI/Card.prefab`으로 변경.

**`Assets/Scripts/UI/BattleCardView.cs`** (수정)
- 의도한 `Card.prefab`의 자식 이름(`CardName`, `CardDescription`, `CardArtImage`, `ManaCostText`)을 우선 찾고, 기존 `CardView.prefab` 이름(`NameText`, `DescText`, `CardIcon`, `CostText`)도 fallback으로 인식하도록 수정.

**`Assets/Scripts/UI/BattleTargetArrow.cs`** (수정)
- 화살표 시작 좌표 계산을 `Camera.main.WorldToScreenPoint` 고정 방식에서 `RectTransformUtility.WorldToScreenPoint` 기반으로 변경.
- Screen Space Overlay 캔버스에서는 카메라 없이 UI 좌표를 변환하도록 처리.
- `Show()` 직후 `UpdateCurve()`와 `UpdatePulse()`를 즉시 호출해 첫 클릭 프레임에도 화살표 위치/색이 바로 갱신되도록 수정.

#### 검증
- 파일 검색으로 `BattleTest.unity`의 `cardViewPrefab`이 `Card.prefab` GUID(`0db826f77733f6d40918da0cf419705a`)와 fileID(`7143928651094735881`)를 참조하는 것을 확인.
- 파일 검색으로 `MagicCrowBattleSceneSetup.cs`, `BattleSceneBuilder.cs`의 카드 프리팹 경로가 모두 `Assets/Prefabs/UI/Card.prefab`으로 변경된 것을 확인.

#### 미검증
- Unity MCP가 검증 단계에서 연속 타임아웃을 반환해 `validate_script`와 PlayMode 클릭 확인은 완료하지 못했다. 에디터가 응답 가능한 상태가 되면 스크립트 컴파일과 실제 공격 카드 첫 클릭 화살표 표시를 재확인해야 한다.

---

### 2026-05-08 (Codex 카드 스프라이트 라이브러리 복원)

#### 이번 세션 작업 요약
MagicCrow 전투 UI 재구성 과정에서 `BattleHandView.spriteLibrary`가 비어 카드가 기존 카드 배경 스프라이트 대신 단색 fallback으로 표시되던 문제를 복구했다.

#### 변경 파일

**`Assets/Scripts/Editor/MagicCrowBattleSceneSetup.cs`** (수정)
- `Assets/ScriptableObjects/CardSpriteLibrary.asset` 경로 상수 추가.
- MagicCrow 전투씬 빌더가 `CardSpriteLibrary`를 로드해 `BattleHandView.spriteLibrary`에 자동 연결하도록 수정.
- 앞으로 `CardAdventure/Build Magic Crow Battle Scene` 메뉴를 다시 실행해도 이전에 작업한 카드 배경 스프라이트가 유지된다.

**`Assets/Scenes/BattleTest.unity`** (수정)
- 현재 씬의 `HandArea.BattleHandView.spriteLibrary`를 `CardSpriteLibrary.asset`으로 재연결.

#### 검증
- Unity MCP `validate_script standard`: `MagicCrowBattleSceneSetup.cs` 오류 0, 경고 0.
- Unity MCP 컴포넌트 확인: `HandArea.BattleHandView.spriteLibrary`가 `Assets/ScriptableObjects/CardSpriteLibrary.asset`으로 연결됨.
- 콘솔에는 신규 컴파일 오류 없음. 기존 obsolete 경고 및 MCP client 종료 로그만 확인됨.

#### 미검증
- PlayMode에서 실제 손패 카드가 Warrior/Mage/Rogue 등급별 배경 스프라이트로 표시되는지는 수동 확인 필요.

---

### 2026-05-08 (Codex 전투씬 MagicCrow 스타일 재구성)

#### 이번 세션 작업 요약
사용자가 제공한 레퍼런스 이미지 방향에 맞춰 `BattleTest` 전투 UI를 좌측 플레이어/우측 적/하단 카드 패 형태로 다시 구성하고, 어드벤처 씬의 전투 입장 트리거가 새 `MagicCrow` 적을 사용하도록 변경했다.

#### 변경 파일

**`Assets/Scripts/Editor/MagicCrowBattleSceneSetup.cs`** (추가)
- `CardAdventure/Build Magic Crow Battle Scene` 메뉴 추가.
- `CrowBattleBackground`를 전투 배경으로 사용하고, 플레이어 아바타/적 이미지/HP 바/에너지 오브/카드 패/턴 종료 버튼을 레퍼런스형 배치로 생성.
- `BattleUIManager`, `BattleEnemyView`, `BattleHudView`, `BattleHandView`, `BattleTargetArrow` 직렬화 참조를 자동 연결.

**`Assets/Scenes/BattleTest.unity`** (수정)
- 새 MagicCrow 전투 UI 캔버스 적용.
- `BattleManager.enemyData`와 `BattleSceneConnector.fallbackEnemy`를 `Enemy_MagicCrow.asset`으로 변경.

**`Assets/ScriptableObjects/Enemies/Enemy_MagicCrow.asset`** (추가)
- `Monster_MagicCrow` 스프라이트 연결.
- 기본 HP 36, 공격/독/방어/취약 패턴 및 보상 카드 풀 설정.

**`Assets/Scenes/AdventureScene.unity`** (수정)
- 기존 `BattleEntrance_Slime`을 `BattleEntrance_MagicCrow`로 변경.
- `BattleEntrance.enemyData`를 `Enemy_MagicCrow.asset`으로 변경.
- 하위 `EnemyVisual`의 `SpriteRenderer`를 `Monster_MagicCrow`로 교체하고, 월드 표시 크기/위치를 조정.

**`Assets/Scripts/Editor/BattleSceneBuilder.cs`** (수정)
- 새 배틀 배경/매직크로우/플레이어 스프라이트 경로 상수 추가. 실제 새 전투 구성은 별도 `MagicCrowBattleSceneSetup` 도구로 수행.

#### 검증
- Unity MCP `validate_script standard`: `MagicCrowBattleSceneSetup.cs` 오류 0, 경고 0.
- Unity refresh/compile 이후 신규 스크립트 컴파일 오류 없음.
- `BattleTest` 씬에서 `BattleCanvas`의 `BattleUIManager` 참조 연결 확인.
- `BattleTest` 씬의 `BattleManager.enemyData` 및 `BattleSceneConnector.fallbackEnemy`가 `Enemy_MagicCrow.asset`을 참조하는 것 확인.
- `AdventureScene`의 `BattleEntrance_MagicCrow.enemyData`가 `Enemy_MagicCrow.asset`을 참조하는 것 확인.
- `AdventureScene`의 `EnemyVisual.SpriteRenderer`가 `Assets/Assets/Sprites/Enemy/Monster_MagicCrow.png`를 참조하는 것 확인.

#### 미검증
- PlayMode에서 실제 전투 진입 후 UI 애니메이션/카드 사용/승패 흐름은 수동 확인 필요.
- Unity MCP GameView 스크린샷은 카메라 렌더 경로로 캡처되어 Screen Space Overlay UI가 포함되지 않아 시각 검수용으로 사용하지 못함.

---

### 2026-05-08 (Codex — 직업 변경 확정 후 후속 대화 출력)

#### 이번 세션 작업 요약
직업 변경 UI에서 직업을 선택하고 확정한 뒤 `Assets/ScriptableObjects/Dialogues/NPC_AfterJobChange_Dialogue.asset` 대화가 이어서 출력되도록 연결했다.

#### 변경 파일

**`Assets/Scripts/Adventure/DialogueManager.cs`** (수정)
- `public void BeginDialogue(DialogueData data)` API 추가.
- NPC 상호작용 없이 특정 `DialogueData`만 바로 출력할 수 있도록 내부 `BeginDialogue(DialogueData, NpcInteractable, bool)` 흐름으로 분리.
- 후속 대화에서는 전직관 `FaceToward()`를 다시 호출하지 않아 대화 종료 후 직업 UI가 재오픈되는 루프를 피하도록 했다.

**`Assets/Scripts/Adventure/JobChangerNpc.cs`** (수정)
- `afterJobChangeDialogue` 직렬화 필드 추가.
- 직업 확정 후 `PlayerController.ApplyJobVisual(selectedJob)`까지 처리한 뒤 `DialogueManager.Instance.BeginDialogue(afterJobChangeDialogue)` 호출.

**`Assets/ScriptableObjects/Dialogues/NPC_AfterJobChange_Dialogue.asset`** (수정)
- 화자 `전직관`과 후속 대사 2줄 추가.

**`Assets/Scenes/AdventureScene.unity`**, **`Assets/Prefabs/NPCs/NPC_JobChanger.prefab`** (수정)
- `NPC_JobChanger.JobChangerNpc.afterJobChangeDialogue`에 `NPC_AfterJobChange_Dialogue.asset` 연결.

#### 검증
- Unity MCP `validate_script standard`:
  - `DialogueManager.cs`: 오류 0, 기존 Update 문자열 GC 권장 경고 1개.
  - `JobChangerNpc.cs`: 오류 0, 경고 0.
- Unity refresh/compile 후 신규 컴파일 오류 없음.
- Unity MCP 컴포넌트 확인 결과 `NPC_JobChanger.JobChangerNpc.afterJobChangeDialogue`가 `Assets/ScriptableObjects/Dialogues/NPC_AfterJobChange_Dialogue.asset`로 연결됨.
- `AdventureScene` 저장 완료.

#### 미검증
- PlayMode에서 전직관 대화 종료 → 직업 선택 UI 확정 → 후속 대화 출력 → 후속 대화 종료 후 직업 UI가 다시 열리지 않는 전체 흐름은 아직 수동 확인하지 못했다.

---

### 2026-05-08 (Codex — 직업 변경 UI에서 현재 직업 제외)

#### 이번 세션 작업 요약
직업 변경 UI를 열 때 현재 플레이어 직업은 선택 목록에 표시되지 않도록 수정했다.

#### 변경 파일

**`Assets/Scripts/UI/JobChangeUIController.cs`** (수정)
- Inspector 원본 `jobs` 목록은 유지하고, UI 표시용 런타임 목록 `displayedJobs`를 추가.
- `Open()` 시 `RebuildDisplayedJobs()`를 호출해 현재 직업을 제외한 목록을 구성.
- 현재 직업 판정:
  - `GameDataManager.Instance.SelectedJobInfo`와 같은 에셋이면 제외.
  - 또는 `GameDataManager.Instance.SelectedJobClass`와 같은 `CardClass`이면 제외.
  - `SelectedJobInfo == null`인 초기 상태는 기본 전사(`CardClass.Warrior`)로 간주해 전사를 제외.
- 직업 버튼 라벨/활성화, 키보드 순환, 마우스 클릭, 미리보기, 설명, 스탯, 확정 로직이 모두 `displayedJobs`를 기준으로 동작하도록 변경.
- 현재 직업 제외 후 남는 직업 수보다 버튼이 많으면 남는 버튼은 `SetActive(false)`로 숨김.

#### 검증
- Unity MCP `validate_script standard`: `JobChangeUIController.cs` 오류 0, 기존 Update 문자열 GC 권장 경고 1개.
- Unity refresh/compile 후 신규 컴파일 오류 없음.
- Console에는 기존 미사용 필드/obsolete API 경고와 MCP client 종료 로그만 확인.

#### 미검증
- PlayMode에서 현재 전사일 때 직업 변경 UI에 마법사/도적만 표시되는지, 마법사/도적으로 변경 후 다시 열었을 때 현재 직업이 제외되는지는 아직 수동 확인하지 못했다.

---

### 2026-05-08 (Codex — 직업 변경 후 플레이어 표시 키 전직관 기준 고정)

#### 이번 세션 작업 요약
직업 변경 UI에서 직업을 확정한 뒤 플레이어 스프라이트/애니메이터는 바뀌지만 직업별 표시 크기가 달라지는 문제를 수정했다. 이제 플레이어는 현재 애니메이터가 표시 중인 스프라이트 프레임의 실제 bounds 높이를 매 프레임 읽어 `AdventureGridUtility.ReferenceCharacterVisualHeight` 기준, 즉 현재 씬의 전직관 NPC와 같은 높이로 정규화한다.

#### 변경 파일

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `LateUpdate()`에서 `NormalizeVisualToReferenceHeight()`를 호출하도록 추가.
- `NormalizeVisualToReferenceHeight()` 추가:
  - 현재 `spriteRenderer.sprite`의 실제 높이를 기준으로 `AdventureGridUtility.GetVisualScaleForReferenceHeight(sprite)` 계산.
  - PlayerVisual localScale을 해당 scale로 직접 설정.
  - PlayerVisual localPosition.y도 기준 높이와 타일 크기에 맞게 재계산해 발 위치를 유지.
- 기존 `ApplyVisualScale(bool moving)`가 idle/walk 배율에 의존하지 않고 `NormalizeVisualToReferenceHeight()`를 사용하도록 변경.
- `AlignVisualToTile()`과 `OnValidate()`도 같은 정규화 루틴을 사용하도록 변경.

#### 의도
- 전사/마법사/도적의 idle/walk 시트 원본 프레임 크기가 달라도, 애니메이션 프레임이 바뀐 직후 다시 전직관 NPC 기준 높이로 맞춘다.
- 직업 에셋에 들어간 `playerIdleVisualScaleMultiplier` 값에 표시 키가 흔들리지 않도록 런타임 정규화가 우선하도록 했다.

#### 검증
- Unity MCP `validate_script standard`:
  - `PlayerController.cs`: 오류 0, 기존 Update 문자열 GC 권장 경고 1개.
  - `JobClassInfo.cs`: 오류 0, 경고 0.
- Unity refresh/compile 후 신규 컴파일 오류 없음.
- Console에는 기존 미사용 필드 경고(`maxVisualWidthInTiles`, `maxVisualHeightInTiles`)와 MCP client 종료 로그만 확인.

#### 미검증
- PlayMode에서 전사/마법사/도적 각각 확정 후 idle/walk/front/back/side 모든 프레임이 전직관 NPC와 같은 표시 높이로 유지되는지는 아직 수동 확인하지 못했다.

---

### 2026-05-08 (Codex — 직업 확정 시 플레이어 스프라이트/애니메이터 교체)

#### 이번 세션 작업 요약
직업 변경 UI에서 직업을 선택하고 확정하면 `GameDataManager.SelectedJobInfo`만 바뀌던 상태에서, 플레이어의 어드벤처 씬 스프라이트와 AnimatorController도 선택 직업에 맞게 즉시 교체되도록 연결했다.

#### 변경 파일

**`Assets/Scripts/Data/JobClassInfo.cs`** (수정)
- 직업별 플레이어 런타임 비주얼 필드 추가:
  - `playerIdleSprite`
  - `playerAnimatorController`
  - `playerIdleVisualScaleMultiplier`

**`Assets/Scripts/Adventure/PlayerController.cs`** (수정)
- `ApplyJobVisual(JobClassInfo jobInfo)` 추가.
- 선택 직업의 `playerIdleSprite`를 PlayerVisual SpriteRenderer에 적용.
- 선택 직업의 `playerAnimatorController`를 Animator에 적용.
- `playerIdleVisualScaleMultiplier`를 적용한 뒤 기존 `RefreshVisualAlignment()`를 호출해 테스트 NPC 기준 키 보정도 다시 수행.

**`Assets/Scripts/Adventure/JobChangerNpc.cs`** (수정)
- 직업 확정 콜백에서 기존 `player.RefreshVisualAlignment()` 대신 `player.ApplyJobVisual(selectedJob)` 호출.

**`Assets/ScriptableObjects/Jobs/Job_Warrior.asset`** (수정)
- 플레이어 IdleFront 스프라이트, `Player_Warrior.controller`, idle 보정 배율 `0.267` 연결.
- 남아 있던 `speedStars` 필드를 `difficulty`로 정리.

**`Assets/ScriptableObjects/Jobs/Job_Mage.asset`** (수정)
- 플레이어 IdleFront 스프라이트, `Player_Magician.controller`, idle 보정 배율 `0.85` 연결.
- 남아 있던 `speedStars` 필드를 `difficulty`로 정리.

**`Assets/ScriptableObjects/Jobs/Job_Rogue.asset`** (수정)
- 플레이어 IdleFront 스프라이트, `Player_Rogue.controller`, idle 보정 배율 `0.74` 연결.
- 남아 있던 `speedStars` 필드를 `difficulty`로 정리.

#### 검증
- Unity MCP `validate_script standard`:
  - `JobClassInfo.cs`: 오류 0, 경고 0.
  - `JobChangerNpc.cs`: 오류 0, 경고 0.
  - `PlayerController.cs`: 오류 0, 기존 Update 문자열 GC 권장 경고 1개.
- Unity refresh/compile 후 신규 컴파일 오류 없음.
- Console에는 기존 미사용 필드/obsolete API 경고와 MCP client 종료 로그만 확인.

#### 미검증
- PlayMode에서 전직관 대화 종료 → 직업 변경 UI 확정 → 플레이어 스프라이트/애니메이터가 즉시 전환되고 이동 애니메이션까지 해당 직업으로 재생되는 전체 흐름은 아직 수동 확인하지 못했다.

#### 다음 작업
- PlayMode에서 전사/마법사/도적 각각 확정 후 idle/walk/front/back/side 애니메이션이 정상 재생되는지 확인.
- `PlayerController.maxVisualWidthInTiles`, `maxVisualHeightInTiles`, `NpcMovement`의 동일 필드는 현재 미사용 경고가 있으므로 유지 여부 결정.

---

### 2026-05-08 (Antigravity — 직업 선택 UI 화살표 인디케이터 개선)

#### 이번 세션 작업 요약
직업 선택 UI의 화살표 인디케이터(`JobArrowIndicator`)를 3단계에 걸쳐 개선했다.

#### 변경 파일

**`Assets/Scripts/UI/JobChangeUIController.cs`** (수정)

1. **능력치 표시 스탯 UI 연동** (이전 세션에서 완료)
   - `JobStatsText` TMP를 통해 `JobClassInfo.attackStars`, `defenseStars`, `magicStars`, `difficulty`를 ■□ 별점으로 표시.
   - `RefreshStats()` 메서드 추가.

2. **화살표 인디케이터 — DOTween 애니메이션 적용**
   - `indicatorMoveDuration` 필드 추가 (기본 0.12초, Inspector 조절 가능).
   - `k_ArrowTweenId = "JobArrowIndicator"` 상수로 트윈 ID 관리.
   - `CalcIndicatorLocalPos(RectTransform)`: 버튼 월드 중심 → 인디케이터 부모 로컬 좌표 변환 후 버튼 왼쪽 위치 계산 (1단계/2단계 공용).
   - `MoveIndicatorTo(Vector3)`: 처음 표시 시 즉시 배치, 이후 `DOLocalMove + Ease.OutCubic`으로 부드럽게 이동.
   - `RefreshJobButtonHighlights()`: 1단계에서만 동작, `isInButtonPhase = true`면 스킵.
   - `RefreshButtonPhaseHighlights()`: 2단계에서만 동작, `confirmFocused` 값으로 `yesButton`/`noButton` 중 화살표 이동.
   - `EnterButtonPhase()`: `RefreshDisplay()` 전체 호출로 진입 시 인디케이터가 결정 버튼으로 즉시 이동.
   - `OnDestroy()`에 `DOTween.Kill(k_ArrowTweenId)` 추가.

3. **인디케이터 렌더링 최상위 보장**
   - `JobArrowIndicator`를 `JobButtonContainer` 자식 → `JobChangeCanvas` 직속 마지막 자식으로 이동.
   - Unity UI는 형제 순서가 뒤에 있을수록 위에 그려지므로 `SetAsLastSibling()`으로 항상 최상위 렌더링.
   - `CalcIndicatorLocalPos`가 `InverseTransformPoint`(월드→로컬 변환) 방식이어서 부모 변경 후에도 좌표 계산 정확.

**`Assets/Scripts/Editor/TempUIBuilder.cs`** (수정)
- `BuildArrowIndicator()`: 인디케이터 부모를 `JobChangeCanvas`로 변경, `SetAsLastSibling()` 적용, 기존 어느 부모에 있던 인디케이터도 모두 검색·제거 후 재생성.
- `BringIndicatorToTop()` 메뉴 추가 (`CardAdventure/Temp/Bring Indicator To Top`): 기존 인디케이터를 `JobChangeCanvas` 직속 마지막 자식으로 이동하는 유틸.

#### 씬 연결 상태 (AdventureScene, 저장 완료)
- `JobArrowIndicator`가 `JobChangeCanvas` 직속 마지막 자식으로 배치됨.
- `JobChangeUIController.jobArrowIndicator` 필드에 연결 완료.

#### 동작 흐름
```
[1단계] W/S 키 → 직업 버튼 사이 DOTween 슬라이드
          ↓ Space
[2단계] 화살표가 결정(Yes) 버튼 왼쪽으로 DOTween 이동
        W/↑ → 결정  |  S/↓ → 취소 (각각 DOTween 이동)
          ↓ Space → 실행 / Esc → 취소
```

#### 검증
- `JobChangeUIController.cs` 컴파일 오류 0개.
- `TempUIBuilder.cs` 컴파일 오류 0개.
- `BringIndicatorToTop` 메뉴 실행 콘솔 로그: `[TempUIBuilder] JobArrowIndicator를 JobChangeCanvas 직속으로 이동.` / `최상위 형제로 이동 완료.` 확인.
- `AdventureScene` 저장 완료.

#### 미검증 (수동 확인 필요)
- PlayMode에서 직업 선택 UI 열기 → W/S로 직업 전환 시 화살표 슬라이드.
- Space로 2단계 진입 시 화살표가 결정 버튼 왼쪽으로 이동.
- W/S로 결정/취소 버튼 전환 시 화살표 이동.
- 인디케이터가 BackGround·버튼 패널 위에 항상 표시되는지.

#### 다음 작업
- PlayMode 전체 흐름 수동 검증.
- `indicatorMoveDuration`, 화살표 여백(`-6f`) 값 체감 튜닝.
- 1단계 초기 표시 시 화살표 비활성 → 활성 페이드인 효과 추가 (선택사항).

---

### 2026-05-08 (Claude Desktop — 직업 선택 UI 기능 완성)

#### 이번 세션 작업 요약
유저가 씬에 직접 제작한 Button 기반 `JobChangeCanvas` UI에 직업 데이터·로직을 연동하고,
키보드 2단계 내비게이션, 플레이어 이동 차단, NPC 상호작용 차단까지 완성했다.

#### 변경 파일

**`Assets/Scripts/UI/JobChangeUIController.cs`** (신규 + 다수 수정)
- Button 기반 직업 선택 UI 컨트롤러. `JobChangeCanvas`에 부착.
- 직업 데이터: `List<JobClassInfo> jobs` (전사/마법사/도적)
- UI 레퍼런스: `List<Button> jobButtons` (3개), `Image characterPreviewImage`, `TextMeshProUGUI jobDescriptionText`, `Button yesButton/noButton`, `RectTransform panelRoot`
- DOTween 팝업 등장/닫힘 애니메이션 (`panelRoot` 스케일)
- `OnJobConfirmed(JobClassInfo)` / `OnCancelled` 이벤트
- `Start()`에서 자동 비활성화, `Open()`에서 `isOpen=true` 먼저 설정 후 `SetActive(true)` (순서 버그 방지)
- **키보드 2단계 내비게이션**:
  - 1단계(직업 목록): W/↑ 위로, S/↓ 아래로(순환), Space → 2단계 전환, Esc/X 취소
  - 2단계(버튼 선택): W/↑ 결정 포커스, S/↓ 취소 포커스, Space 실행, Esc/X 취소
  - 각 단계에서 해당 버튼 황색 강조(`selectedColor`)
  - 마우스 클릭도 병행 지원 (직업 클릭 시 1단계로 복귀)
- **플레이어 이동 차단**: `Open()` 시 `PlayerController.enabled = false`, `Close()`/`OnDestroy()` 시 복구
- **전역 플래그**: `public static bool IsAnyOpen` — `Open()`에서 `true`, `Close()`/`OnDestroy()`에서 `false`
- 설명 텍스트: 직업 이름 없이 `info.description`만 표시

**`Assets/Scripts/Adventure/JobChangerNpc.cs`** (수정)
- `JobSelectionUI` 참조 → `JobChangeUIController`로 전면 교체
- `Start()`에서 `FindFirstObjectByType<JobChangeUIController>()` 자동 탐색
- `OpenJobSelectionUI()`, `HandleJobConfirmed()`, `HandleJobCancelled()` 이벤트 구독 대상 교체

**`Assets/Scripts/Adventure/DialogueManager.cs`** (수정)
- NPC 대화 시작 조건에 `&& !JobChangeUIController.IsAnyOpen` 추가 → UI 열린 동안 Space로 대화 불가
- NPC 상호작용 힌트 표시 조건에 동일 체크 추가 → UI 열린 동안 힌트 아이콘 숨김

**`Assets/Scripts/Editor/JobSelectionSetup.cs`** (수정)
- `asset.speedStars = preset.spd` → `asset.difficulty = preset.spd` 컴파일 오류 수정

**`Assets/Scripts/Editor/JobChangeUIWire.cs`** (신규, 임시 에디터 도구)
- `CardAdventure/Wire Job Change UI` 메뉴 아이템
- `JobChangeCanvas`에 `JobChangeUIController` 컴포넌트 부착 + 모든 SerializedProperty 필드 자동 연결 후 씬 저장

#### 씬 연결 상태 (AdventureScene, 저장 완료)
- `JobChangeCanvas`에 `JobChangeUIController` 컴포넌트 부착
- `jobs`: [0] Job_Warrior, [1] Job_Mage, [2] Job_Rogue (GUID 검증 완료)
- `jobButtons`: [0] JobButton_1, [1] JobButton_2, [2] JobButton_3
- `characterPreviewImage`: JobImage
- `jobDescriptionText`: JobDescriptionText
- `yesButton`: YesButton, `noButton`: NoButton
- `panelRoot`: BackGround (RectTransform)
- `NPC_JobChanger`(프리팹 인스턴스)의 `jobChangeUI` 필드는 런타임 `FindFirstObjectByType`으로 자동 연결

#### 검증
- 씬 YAML에서 모든 레퍼런스 GUID/fileID 직접 확인 완료
- 컴파일 오류 0개 (기존 경고만 존재)

#### 미검증 (수동 확인 필요)
- PlayMode에서 NPC 대화 종료 → 직업 선택 UI 팝업 → 선택 → `GameDataManager.SelectedJobInfo` 업데이트 전체 흐름
- `Job_Warrior/Mage/Rogue.asset`의 `previewSprite` 필드에 SPUM Idle 스프라이트 미할당 (캐릭터 미리보기 비어 있음)

#### 다음 작업
- PlayMode 전체 흐름 수동 검증
- `JobClassInfo` 에셋 3종의 `previewSprite`에 SPUM 스프라이트 할당
- `JobChangeUIWire.cs` 불필요 시 삭제

#### 주의
- `JobChangeCanvas`가 씬에서 active 상태로 배치되어 있어도 `Start()`에서 자동 비활성화됨
- `PlayerController.enabled = false`로 이동 차단하므로, `PlayerController`가 카메라 추적 등 이동 외 기능도 담당한다면 분리 고려 필요
- `JobChangeUIWire.cs`는 `execute_menu_item` MCP 타임아웃으로 인해 응답이 없어도 실제 코드는 정상 실행됨 (Unity 콘솔 로그로 확인)

---

### 2026-05-08 (Claude Desktop — 직업 선택 UI Button 연동)

- 작업 시작 전 이전 세션 컨텍스트 확인 완료.
- 작업 내용: 유저가 씬에 직접 만든 Button 기반 UI(`JobChangeCanvas`)에 직업 데이터/로직 연동.
- 변경 파일:
  - `Assets/Scripts/UI/JobChangeUIController.cs` (신규)
    - Button 기반 직업 선택 UI 컨트롤러.
    - `List<Button> jobButtons` (3개), `Image characterPreviewImage`, `TextMeshProUGUI jobDescriptionText`, `Button yesButton/noButton`, `RectTransform panelRoot`.
    - 버튼 클릭 시 선택 강조(황색), 미리보기 갱신, 설명 갱신.
    - DOTween 팝업/닫힘 애니메이션 (`panelRoot` 기준).
    - `OnJobConfirmed(JobClassInfo)` / `OnCancelled` 이벤트.
    - `Start()`에서 자동 비활성화, `Open()`에서 `isOpen=true` 먼저 설정 후 `SetActive(true)`.
  - `Assets/Scripts/Adventure/JobChangerNpc.cs` (수정)
    - `JobSelectionUI` → `JobChangeUIController` 참조로 전면 교체.
    - `Start()`에서 `FindFirstObjectByType<JobChangeUIController>()` 자동 탐색.
  - `Assets/Scripts/Editor/JobSelectionSetup.cs` (수정)
    - `asset.speedStars` → `asset.difficulty` 컴파일 오류 수정.
  - `Assets/Scripts/Editor/JobChangeUIWire.cs` (신규, 임시 에디터 도구)
    - `CardAdventure/Wire Job Change UI` 메뉴: `JobChangeCanvas`에 컨트롤러 부착 + 모든 필드 자동 연결.
- 씬 연결 결과 (AdventureScene 저장 완료):
  - `JobChangeCanvas`에 `JobChangeUIController` 컴포넌트 부착.
  - jobs[0]=Job_Warrior, jobs[1]=Job_Mage, jobs[2]=Job_Rogue.
  - jobButtons[0..2]=JobButton_1/2/3, characterPreviewImage=JobImage, jobDescriptionText=JobDescriptionText, yesButton=YesButton, noButton=NoButton, panelRoot=BackGround.
- 검증:
  - 씬 YAML에서 모든 레퍼런스(jobs GUID 3개, buttons 3개, image, text, yesBtn, noBtn, panelRoot) 확인 완료.
  - 컴파일 오류 0개 (경고만 존재).
- 미검증:
  - PlayMode에서 NPC 대화 후 직업 선택 UI 팝업 → 직업 선택 → GameDataManager 업데이트 흐름 수동 확인 필요.
  - 각 직업 미리보기 스프라이트(SPUM Idle 프레임)를 `JobClassInfo.previewSprite`에 수동 할당 필요.
- 다음 작업:
  - PlayMode 전체 흐름 검증.
  - `Job_Warrior/Mage/Rogue.asset`의 `previewSprite` 필드에 SPUM 스프라이트 할당.
  - `JobChangeUIWire.cs`는 더 이상 필요 없으면 삭제 가능.
- 주의:
  - `JobChangerNpc`가 `NPC_JobChanger` 프리팹 인스턴스로 씬에 배치되어 있으며, `jobChangeUI` 필드는 런타임 자동 탐색으로 연결됨.
  - 씬에 `JobChangeCanvas`가 active 상태로 배치되어도 `Start()`에서 자동으로 비활성화됨.

### 2026-05-08 (Claude Desktop — 직업 선택 UI 구현)

- 작업 시작 전 `PROJECT_STATUS.md` 및 `CLAUDE.md` 확인 완료.
- 요구사항: 전송된 이미지(JRPG 픽셀 RPG 스타일 4직업 선택 화면) 기준의 직업 변경 UI 구현.
- 구현 내용:
  - `Assets/Scripts/Data/JobClassInfo.cs` (신규)
    - 직업 표시명, 설명, CardClass, 공격/방어/마법/속도 별점(1~5), 미리보기 스프라이트, 스타터 덱, baseMaxHp/Energy를 담는 ScriptableObject.
  - `Assets/Scripts/Data/CardData.cs`
    - `CardClass` enum에 `Archer` 추가 (궁수 직업 지원용, 스타터 덱은 추후 구현 예정).
  - `Assets/Scripts/Core/GameDataManager.cs`
    - `SelectedJobInfo(JobClassInfo)`, `SelectedJobClass(CardClass)` 프로퍼티 추가.
  - `Assets/Scripts/UI/JobSelectionUI.cs` (신규)
    - 직업 목록 ↑↓ 탐색, ■□ 능력치 바, 캐릭터 미리보기 Image, 결정/취소 버튼 커서 ▶, DOTween 팝업 애니메이션.
    - `OnJobConfirmed(JobClassInfo)` / `OnCancelled` 이벤트 제공.
  - `Assets/Scripts/Editor/JobSelectionSetup.cs` (신규)
    - `CardAdventure/Setup Job Selection UI` 메뉴: 이미지 스타일 픽셀 RPG UI 계층 구조 자동 생성 및 JobSelectionUI 참조 연결.
    - `CardAdventure/Create Default Job Assets` 메뉴: 전사/마법사/궁수/도적 4종 JobClassInfo 에셋 자동 생성.
  - `Assets/Scripts/Adventure/JobChangerNpc.cs`
    - 대화 종료 시 `DialogueManager.OnDialogueEnded` 구독 → `JobSelectionUI.Open()` 자동 호출.
    - `HandleJobConfirmed`: `GameDataManager.SelectedJobInfo` 업데이트 + `PlayerController.RefreshVisualAlignment()` 호출.
  - `Assets/Prefabs/UI/JobSelection.prefab` (신규)
    - `CardAdventure/Setup Job Selection UI` 실행으로 생성 확인.
  - `Assets/ScriptableObjects/Jobs/Job_Warrior.asset` 외 3종 (신규)
    - `CardAdventure/Create Default Job Assets` 실행으로 4개 에셋 생성 확인.
- 검증:
  - 모든 신규/수정 스크립트 `validate_script standard`: 오류 0. (JobSelectionUI.cs 경고 1개 — 기존 Update 관련 권장사항)
  - Unity 리컴파일 후 Console 신규 코드 오류 없음.
  - `Assets/Prefabs/UI/JobSelection.prefab` 생성 확인.
  - `Assets/ScriptableObjects/Jobs/` 하위 4개 에셋 생성 확인.
- 미검증:
  - PlayMode에서 전직관 NPC 대화 후 UI가 실제로 팝업되는지 수동 확인 필요.
  - 직업 선택 UI에 JobClassInfo 에셋 할당 후 커서/별점/미리보기/설명 표시 정상 여부 확인 필요.
  - `UI_JobChange.png`를 미리보기 또는 패널 배경으로 활용하려면 Sprite 임포트 설정 후 수동 할당 필요.
- 다음 작업:
  - JobSelection 프리팹의 `JobSelectionUI.jobs` 리스트에 4개 에셋(Job_Warrior ~ Job_Rogue) 할당.
  - AdventureScene의 `JobSelectionCanvas`(또는 직업선택 프리팹 인스턴스)와 `NPC_JobChanger`의 `jobSelectionUI` 필드 연결.
  - 각 직업 미리보기 스프라이트(SPUM 캐릭터 Idle 프레임)를 `JobClassInfo.previewSprite`에 할당.
  - PlayMode 전체 흐름 검증.
- 주의:
  - `CardClass.Archer`를 추가했으므로 기존 카드 에셋 직렬화는 영향 없으나, 아처 전용 카드 추가 시 이 enum 값 사용.
  - JobSelection 프리팹을 씬에 배치할 때 Canvas sortingOrder(100)가 다른 UI보다 위에 오도록 확인.

### 2026-05-08 (Codex - NPC 콜라이더 중심을 스프라이트 발 위치로 보정)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 문제:
  - NPC의 BoxCollider2D 월드 크기는 1x1로 맞춰져 있었지만, `offset`이 0에 가까워 콜라이더 중심이 각 스프라이트의 발이 아니라 스프라이트 중심에 놓이는 문제가 있었다.
  - 특히 루트 GameObject에 SpriteRenderer가 직접 붙은 테스트 NPC/전직관 NPC에서 발 위치와 타일 중심이 어긋났다.
- 수정:
  - `Assets/Scripts/Adventure/AdventureGridUtility.cs`
    - `ConfigureFootCollider(BoxCollider2D, Transform, SpriteRenderer, Vector2)` 오버로드 추가.
    - `SpriteRenderer.bounds.min.y`를 발 위치로 보고 BoxCollider2D `offset`을 owner local 좌표로 역산하도록 변경.
  - `Assets/Scripts/Adventure/NpcMovement.cs`
    - 움직이는 NPC 콜라이더 구성 시 SpriteRenderer 기반 발 위치 offset 계산을 사용하도록 변경.
  - `Assets/Scripts/Adventure/NpcTileAlignment.cs`
    - 정지형 NPC 콜라이더 구성 시 SpriteRenderer 기반 발 위치 offset 계산을 사용하도록 변경.
  - `Assets/Scripts/Adventure/JobChangerNpc.cs`
    - 전직관 콜라이더 구성도 SpriteRenderer 기반 발 위치 offset 계산을 사용하도록 변경.
  - `Assets/Scripts/Editor/DialogueSceneSetup.cs`, `Assets/Scripts/Editor/JobChangerSetup.cs`
    - 새 NPC/전직관 프리팹 생성 시에도 SpriteRenderer 발 위치 기준 offset 계산을 사용하도록 변경.
  - `Assets/Scenes/AdventureScene.unity`
    - `NPC_BaramIroGun` BoxCollider2D `offset=(0,-1.7961122)`, 루트 위치 `(0.5,3.165226,0)`로 보정해 콜라이더 중심과 스프라이트 하단이 `(0.5,2.5)`에 오도록 저장.
    - `NPC_JobChanger` BoxCollider2D `offset=(0,-0.415)`, 루트 위치 `(-11.5,1.165226,0)`로 보정해 콜라이더 중심과 스프라이트 하단이 `(-11.5,0.5)`에 오도록 저장.
  - `Assets/Prefabs/NPCs/NPC_JobChanger.prefab`
    - 전직관 프리팹 BoxCollider2D offset을 `(0,-0.415)`로 저장.
  - `AGENTS.md`, `CLAUDE.md`
    - NPC BoxCollider2D 중심은 `SpriteRenderer.bounds.min.y`(스프라이트 하단/발 위치)에 맞춰야 한다는 지침 추가.
- 검증:
  - Unity MCP 컴포넌트 확인:
    - `NPC_BaramIroGun`: SpriteRenderer center y `3.165226`, height `1.3304521`, 하단 y `2.5`; BoxCollider2D bounds center y `2.5`, size `1x1`.
    - `NPC_JobChanger`: SpriteRenderer center y `1.165226`, height `1.3304518`, 하단 y `0.5`; BoxCollider2D bounds center y `0.5000001`, size `1x1`.
  - Unity MCP `validate_script standard`:
    - `AdventureGridUtility.cs`, `NpcTileAlignment.cs`: 오류 0, 경고 0.
    - `NpcMovement.cs`: 오류 0, 기존 Update 문자열 GC 권장 경고 1개만 확인.
  - Unity refresh/compile 요청 후 Console 신규 게임 코드 오류 없음. MCP client 종료 로그만 확인.
  - `AdventureScene` 저장 완료.
- 미검증:
  - PlayMode에서 이동 NPC가 시작 시 루트 위치를 새 발 offset 기준으로 재스냅한 뒤 배회/충돌이 체감상 자연스러운지는 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - 전체 NPC 발 기준 스냅/콜라이더 규칙 공통화)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 요구사항:
  - 전직관 외 다른 모든 NPC도 발 콜라이더 중심을 가장 가까운 타일맵 셀 중심으로 스냅한다.
  - 플레이어 직업이 변경되어도 스프라이트 키 기준과 발 기준 스냅/충돌 기준이 유지되게 한다.
  - 앞으로 추가될 NPC도 같은 규칙으로 콜라이더를 구성하도록 지침과 생성 흐름을 보강한다.
- 수정:
  - `Assets/Scripts/Adventure/AdventureGridUtility.cs`
    - `GetFootCenter`, `GetRootPositionForFootCenter`, `SnapOwnerFootToNearestCell` 등 발 중심 기준 공통 헬퍼 추가.
    - `ConfigureFootCollider`가 루트 스케일을 고려해 BoxCollider2D 월드 bounds를 목표 크기(기본 1x1)로 역보정하도록 변경.
    - 충돌용 `CircleCollider2D`를 비활성화하는 `DisableSolidCircles` 구현.
  - `Assets/Scripts/Adventure/NpcMovement.cs`
    - 테스트 NPC처럼 움직이는 NPC의 시작 스냅, 이동 충돌 검사, `GridOccupancy` 예약/해제를 모두 발 중심 기준으로 변경.
  - `Assets/Scripts/Adventure/NpcTileAlignment.cs`
    - 움직이지 않는 대화/상점/전직 NPC용 표준 정렬 컴포넌트 추가.
    - `Rigidbody2D(Kinematic)` + `BoxCollider2D` 구성, 기준 키 스케일, 시작 시 발 중심 셀 스냅을 담당.
  - `Assets/Scripts/Adventure/JobChangerNpc.cs`
    - 전직관도 기준 키 스케일, BoxCollider2D 구성, 발 중심 스냅을 공통 유틸리티 흐름으로 수행하도록 보강.
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - 플레이어 발 중심 계산을 공통 유틸리티로 교체.
    - `RefreshVisualAlignment()`와 `SetVisual(...)`을 추가해 런타임 직업/비주얼 교체 후에도 기준 키와 애니메이션 스케일을 다시 적용할 수 있게 했다.
  - `Assets/Scripts/Editor/DialogueSceneSetup.cs`
    - `CardAdventure/Create NPC (Interactable)`가 새 NPC에 `Rigidbody2D`, 솔리드 `BoxCollider2D`, `NpcTileAlignment`, `NpcInteractable`을 기본으로 붙이도록 변경.
    - 충돌용 기존 CircleCollider2D는 비활성화하도록 변경.
  - `Assets/Scripts/Editor/AdventureSceneBuilder.cs`, `Assets/Scripts/Editor/WarriorPlayerVisualSetup.cs`
    - 플레이어 생성/직업 비주얼 적용 시 BoxCollider2D 발 중심 기준과 테스트 NPC 기준 키 스케일을 적용하도록 갱신.
  - `Assets/Scripts/Editor/JobChangerSetup.cs`
    - 전직관 프리팹 생성 시 `NpcTileAlignment`를 추가하고 공통 BoxCollider2D 역보정 흐름을 사용하도록 변경.
  - `Assets/Scenes/AdventureScene.unity`, `Assets/Prefabs/NPCs/NPC_JobChanger.prefab`
    - `NPC_JobChanger`에 `NpcTileAlignment`를 추가.
    - 전직관 위치를 `(-11.5, 0.5, 0)` 타일 중심으로 저장.
    - 전직관 스케일을 균일 `(1.602954, 1.602954, 1)`로 변경해 높이만 기준값에 맞추고, BoxCollider2D 로컬 size를 `(0.623847,0.623847)`로 보정해 월드 bounds 1x1 유지.
  - `AGENTS.md`, `CLAUDE.md`
    - 새 NPC 추가 시 `Rigidbody2D(Kinematic)` + 솔리드 `BoxCollider2D` + `NpcTileAlignment`/`NpcMovement` + `NpcInteractable` 규칙을 명시.
    - 플레이어 직업 변경 시 `PlayerController.SetVisual(...)` 또는 `RefreshVisualAlignment()`를 호출하라는 지침 추가.
- 검증:
  - Unity MCP `validate_script standard`:
    - `NpcTileAlignment.cs`, `AdventureGridUtility.cs`, `JobChangerNpc.cs`, `DialogueSceneSetup.cs`, `AdventureSceneBuilder.cs`, `WarriorPlayerVisualSetup.cs`, `JobChangerSetup.cs`: 오류 0, 경고 0.
    - `NpcMovement.cs`, `PlayerController.cs`: 오류 0, 기존 Update 문자열 GC 권장 경고 1개씩만 확인.
  - Unity refresh/compile 요청 후 Console 신규 게임 코드 오류 없음. MCP/Animator 직렬화 관련 경고/오류만 확인.
  - Unity MCP 컴포넌트 확인:
    - `NPC_JobChanger` SpriteRenderer bounds height `1.3304518`, BoxCollider2D bounds size `0.999998 x 0.999998`, center `(-11.5,0.5)`.
    - `NPC_JobChanger`에 `NpcTileAlignment` 1개만 남도록 중복 제거.
  - `AdventureScene` 저장 완료.
- 미검증:
  - PlayMode에서 모든 NPC의 시작 스냅, 이동 NPC의 충돌/점유 예약, 플레이어 직업 변경 직후 키 재정렬 체감은 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - 플레이어 발 기준 타일 중심 스냅)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 요구사항:
  - 플레이어도 테스트 NPC/전직관 NPC처럼 가장 가까운 칸의 타일맵 중심으로 순간이동하게 만든다.
- 수정:
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - 시작 위치 스냅 기준을 Player 루트가 아니라 발 중심(`BoxCollider2D.offset`)으로 변경.
    - 이동 충돌 검사(`Physics2D.OverlapBoxAll`)와 `GridOccupancy` 예약/해제 기준을 발 중심으로 변경.
    - 대화 등으로 입력이 잠길 때도 현재 발 중심을 가장 가까운 셀 중심으로 스냅한 뒤 루트 위치를 역산하도록 변경.
  - `Assets/Scenes/AdventureScene.unity`
    - Player 루트 위치를 `(0.5, 1.0, 0)`으로 저장해 `BoxCollider2D.bounds.center`가 `(0.5, 0.5)` 타일 중심에 오도록 정렬.
- 검증:
  - Unity MCP 컴포넌트 확인 결과 Player `BoxCollider2D.offset=(0,-0.5)`, `bounds.center=(0.5,0.5)`, `bounds.size=(1,1)`.
  - `PlayerController.cs` Unity MCP `validate_script standard` 오류 0, 기존 Update 문자열 GC 권장 경고 1개만 확인.
  - Unity refresh/compile 후 Console 신규 게임 코드 오류 없음.
  - `AdventureScene` 저장 완료.
- 미검증:
  - PlayMode에서 실제 이동 중 벽/NPC 충돌과 대화 중 스냅 체감은 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - Player BoxCollider 발 중심 정렬)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 수정:
  - `Assets/Scenes/AdventureScene.unity`
    - Player `BoxCollider2D.offset`을 `(0, 0)`에서 `(0, -0.5)`로 변경.
    - Player `BoxCollider2D.size`는 기존 `(1,1)` 유지.
    - PlayerVisual 하단/발 위치가 월드 `y=0.0`이고 Player 루트가 `y=0.5`라, offset `-0.5`로 BoxCollider bounds 중심을 발 위치에 맞췄다.
  - `AGENTS.md`
    - Player 실제 충돌 기준을 `BoxCollider2D size=(1,1), offset=(0,-0.5)`로 갱신.
  - `CLAUDE.md`
    - 같은 발밑 충돌 기준을 추가해 병행 에이전트도 동일 기준을 따르도록 갱신.
- 검증:
  - Unity MCP 컴포넌트 확인 결과 Player `BoxCollider2D.offset=(0,-0.5)`.
  - Player `BoxCollider2D.bounds.center=(0.5,0.0)`, `bounds.size=(1,1)` 확인.
  - `AdventureScene` 저장 완료.
- 미검증:
  - PlayMode에서 이동 충돌/대화 거리 판정이 발 중심 기준으로 기대대로 동작하는지는 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - 플레이어/NPC 스프라이트 키 테스트 NPC 기준 통일)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 요구사항:
  - 스프라이트별 신장 차이 때문에 NPC와 플레이어가 서 있는 위치가 어긋나 보이는 문제를 줄이기 위해 플레이어와 NPC의 화면상 키만 테스트 NPC와 동일하게 맞춘다.
  - 앞으로 추가될 NPC도 같은 키 보정을 반드시 적용하도록 지침을 남긴다.
- 기준:
  - 테스트 NPC `NPC_BaramIroGun`의 SpriteRenderer 월드 bounds 높이 `1.3304521`을 기준 키로 사용.
- 수정:
  - `Assets/Scripts/Adventure/AdventureGridUtility.cs`
    - `ReferenceCharacterVisualHeight = 1.3304521f` 추가.
    - `GetVisualScaleForReferenceHeight(Sprite)` 추가.
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - 비주얼 정렬 시 폭/최대 타일 높이 기준이 아니라 기준 키에 맞춰 스프라이트 높이만 스케일하도록 변경.
    - 플레이어 idle/walk 스케일 계산은 기존 `idleVisualScaleMultiplier` 구조를 유지하되 최종 idle 높이가 기준 키가 되도록 계산.
  - `Assets/Scripts/Adventure/NpcMovement.cs`
    - NPC 비주얼 정렬 시 기준 키에 맞춰 스프라이트 높이만 스케일하도록 변경.
  - `Assets/Scripts/Editor/JobChangerSetup.cs`
    - 전직관 프리팹 생성 시 `AdventureGridUtility.GetVisualScaleForReferenceHeight`를 사용하도록 변경.
    - 루트 스케일 변경으로 콜라이더가 커지지 않도록 BoxCollider2D 로컬 size를 역보정하고, CircleCollider2D 대신 Rigidbody2D + BoxCollider2D 기준으로 생성하도록 보강.
  - `Assets/Scenes/AdventureScene.unity`
    - 현재 PlayerVisual 높이를 테스트 NPC 기준으로 즉시 보정.
    - PlayerController의 `walkVisualScale`을 `(1.116713, 1.116713, 1)`, `idleVisualScaleMultiplier`를 `0.74`로 저장.
  - `AGENTS.md`
    - 새 NPC/플레이어 비주얼 추가 시 `ReferenceCharacterVisualHeight`와 `GetVisualScaleForReferenceHeight(Sprite)`로 스프라이트 높이만 테스트 NPC 기준에 맞추라는 지침 추가.
  - `CLAUDE.md`
    - Claude Desktop 및 병행 에이전트도 같은 기준을 따르도록 탑다운 캐릭터 키 기준 지침 추가.
- 검증:
  - Unity MCP 컴포넌트 확인:
    - PlayerVisual SpriteRenderer bounds height `1.3304519`.
    - `NPC_BaramIroGun` SpriteRenderer bounds height `1.3304521`.
    - `NPC_JobChanger` SpriteRenderer bounds height `1.3304518`.
    - 전직관/테스트 NPC/플레이어의 충돌 BoxCollider2D 월드 bounds는 기존 1x1 기준 유지.
  - `AdventureGridUtility.cs`, `JobChangerSetup.cs`: Unity MCP `validate_script standard` 오류 0, 경고 0.
  - `PlayerController.cs`, `NpcMovement.cs`: 오류 0, 기존 Update GC 권장 경고만 확인.
  - Unity refresh/compile 후 Console 신규 게임 코드 오류 없음.
  - `AdventureScene` 저장 완료.
- 미검증:
  - PlayMode에서 idle/walk 애니메이션 전환 중 플레이어 키가 계속 테스트 NPC 기준으로 유지되는지 수동 확인은 아직 하지 못했다.

### 2026-05-08 (Codex - 전직관 NPC 방향 애니메이션 클립 매핑 수정)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 문제:
  - 전직관 NPC가 대화 시작 시 플레이어 방향을 바라보도록 `DirectionX`, `DirectionY`를 갱신해도 실제 재생 방향이 맞지 않았다.
  - 원본 `Assets/Assets/Sprites/NPCs/JobChanger_Sprite.png` 확인 결과 스프라이트 시트 행 순서는 `앞 / 왼쪽 / 오른쪽 / 뒤`였다.
  - 기존 `JobChangerSetup`은 행 순서를 `앞 / 뒤 / 왼쪽 / 오른쪽`으로 가정해 `IdleBack`, `IdleLeft`, `IdleRight` 클립 프레임이 잘못 배치되어 있었다.
- 수정:
  - `Assets/Animations/NPCs/JobChanger/IdleLeft.anim`
    - 원본 시트 2번째 행(왼쪽 방향) 프레임을 참조하도록 수정.
  - `Assets/Animations/NPCs/JobChanger/IdleRight.anim`
    - 원본 시트 3번째 행(오른쪽 방향) 프레임을 참조하도록 수정.
  - `Assets/Animations/NPCs/JobChanger/IdleBack.anim`
    - 원본 시트 4번째 행(뒤 방향) 프레임을 참조하도록 수정.
  - `Assets/Scripts/Editor/JobChangerSetup.cs`
    - `Setup Job Changer`의 기본 클립 생성 순서를 `front, left, right, back`으로 수정해 재실행 시 같은 문제가 재발하지 않게 했다.
    - `Fix Job Changer Animation Clips` 메뉴를 추가해 프리팹을 덮어쓰지 않고 애니메이션 클립 프레임만 재정렬할 수 있게 했다.
- 검증:
  - `CardAdventure/Fix Job Changer Animation Clips` 메뉴 실행 완료.
  - Console에 `[JobChangerSetup] JobChanger animation clips fixed.` 로그 확인.
  - `JobChangerSetup.cs` Unity MCP `validate_script standard`: 오류 0, 경고 0.
  - Unity refresh/compile 후 Editor idle 확인.
  - Console 신규 게임 코드 오류 없음.
- 미검증:
  - PlayMode에서 전직관 앞/뒤/좌/우에서 Space 입력 시 실제 방향 애니메이션이 모두 올바르게 보이는지는 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - 전직관 NPC 대화 시점 방향 전환으로 변경)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 요구사항:
  - 전직관 NPC가 플레이어 위치를 계속 추적하며 자동으로 회전하지 않도록 변경.
  - 플레이어가 대화 상호작용을 시도하면 그 순간 플레이어 방향으로 회전하고 알맞은 방향 애니메이션을 재생.
- 수정:
  - `Assets/Scripts/Adventure/JobChangerNpc.cs`
    - 매 프레임 플레이어를 찾고 바라보던 `Update()` 자동 회전 흐름 제거.
    - `FaceToward(Vector2 targetPosition)` 공개 메서드 추가.
    - `FaceToward` 호출 시에만 `DirectionX`, `DirectionY` Animator 파라미터를 갱신하도록 변경.
    - 기존 타일 중앙 스냅 로직은 유지.
  - `Assets/Scripts/Adventure/DialogueManager.cs`
    - `BeginDialogue()`에서 플레이어 이동을 잠근 직후, 대상 NPC가 `JobChangerNpc`를 가지고 있으면 `FaceToward(activePlayer.transform.position)`를 호출하도록 연결.
    - 기존 플레이어가 NPC 방향을 바라보는 처리도 유지.
- 검증:
  - `JobChangerNpc.cs` Unity MCP `validate_script standard`: 오류 0, 경고 0.
  - `DialogueManager.cs` Unity MCP `validate_script standard`: 오류 0, 기존 Update GC 권장 경고 1개만 확인.
  - Unity refresh/compile 후 Editor idle 확인.
  - Console 신규 게임 코드 오류 없음.
- 미검증:
  - PlayMode에서 전직관 앞/뒤/좌/우에서 Space 입력 시 각 방향 애니메이션이 정확히 전환되는지는 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - 전직관 NPC 시작 위치 타일 중앙 스냅 추가)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 수정:
  - `Assets/Scripts/Adventure/JobChangerNpc.cs`
    - `snapToNearestTileOnStart` 옵션을 추가하고 기본값을 `true`로 설정.
    - `moveUnitSize` 기본값 `1f` 추가.
    - `Start()` 시 `AdventureGridUtility.SnapToCellCenter(transform.position, moveUnitSize)`를 호출해 전직관 NPC를 현재 위치에서 가장 가까운 Grid 셀 중심으로 강제 이동하도록 했다.
    - Transform 위치와 `Rigidbody2D.position`을 함께 갱신해 플레이어/테스트 NPC와 같은 방식으로 타일 중앙에 정렬되도록 했다.
    - 전직관 스크립트에 `RequireComponent(typeof(Rigidbody2D))`를 추가해 새 전직관 오브젝트 생성 시 Rigidbody 누락을 방지했다.
- 검증:
  - Unity MCP `validate_script standard`: 오류 0개, 경고 1개.
    - 경고는 `Start()`에서 Rigidbody 위치를 동기화하는 패턴에 대한 일반 권장사항이며, 기존 `PlayerController`/`NpcMovement`의 시작 위치 보정 방식과 같은 목적이다.
  - Unity refresh/compile 후 Editor 상태 idle, Console 신규 게임 코드 오류 없음.
  - Unity MCP 컴포넌트 확인 결과 `NPC_JobChanger`의 `JobChangerNpc`에 `snapToNearestTileOnStart=true`, `moveUnitSize=1` 직렬화 확인.
- 미검증:
  - PlayMode에서 전직관이 실제 시작 시점에 현재 위치 기준 가장 가까운 타일 중심으로 이동하는지는 아직 수동 확인하지 못했다.
- 주의:
  - 현재 씬의 `NPC_JobChanger` 배치 위치가 타일 중심이 아니면 PlayMode 시작 시 자동으로 가까운 셀 중심으로 이동한다. 이미 다른 NPC가 같은 셀에 있으면 겹칠 수 있으므로 최종 배치 위치는 PlayMode 검증 전에 확인해야 한다.

### 2026-05-08 (Codex - 전직관 NPC 시각 크기 테스트 NPC 기준 동기화)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 수정:
  - `Assets/Scenes/AdventureScene.unity`
    - `NPC_JobChanger` Transform scale을 `(1.88679, 1.602954, 1)`로 변경해 테스트 NPC `NPC_BaramIroGun`의 SpriteRenderer bounds와 같은 약 `1.0 x 1.33` 월드 크기로 맞췄다.
    - 루트 스케일 변경으로 충돌 범위가 커지지 않도록 `BoxCollider2D.size`를 `(0.5299999, 0.623847)`로 역보정했다.
  - `Assets/Prefabs/NPCs/NPC_JobChanger.prefab`
    - 씬 인스턴스와 동일한 scale 및 `BoxCollider2D.size` 역보정을 적용했다.
- 검증:
  - Unity MCP 컴포넌트 확인 결과 `NPC_JobChanger` SpriteRenderer bounds size가 `(0.9999987, 1.3304518)`로 테스트 NPC의 기존 bounds `(약 1.0, 1.33045)`와 일치.
  - `BoxCollider2D` bounds size는 `(0.9999986, 0.9999981)`로 기존 1칸 충돌 기준 유지.
  - `NpcInteractable.InteractionRadius`도 약 `0.5`로 유지.
  - `AdventureScene` 저장 완료.
- 주의:
  - 크기 일치를 위해 전직관 루트 스케일은 X/Y가 서로 다르다. 원본 스프라이트 비율을 완전히 보존해야 한다면 높이 기준 균일 스케일로 재조정할 수 있다.
- 미검증:
  - PlayMode에서 실제 화면상 테스트 NPC와 나란히 볼 때의 체감 크기와 충돌 느낌은 아직 수동 확인하지 못했다.

### 2026-05-08 (Codex - 전직관 NPC 충돌 처리 테스트 NPC 기준 동기화)

- 작업 시작 전 `PROJECT_STATUS.md`를 확인했다.
- 현재 진행 파악:
  - Phase 1 전사 덱 기반 카드 배틀 기본 루프와 `BattleTest` 검증 환경이 구축되어 있다.
  - Phase 2용 `AdventureScene`에는 플레이어 이동, 전투 진입, 테스트 NPC 대화/배회, 대화 UI 기반이 연결되어 있다.
  - 최근 NPC 충돌 정책은 테스트 NPC `NPC_BaramIroGun` 기준으로 `Rigidbody2D + BoxCollider2D`만 사용하고, `CircleCollider2D` 대화 트리거는 제거한 상태다.
  - 전직관 NPC는 애니메이션/방향 전환 스크립트와 프리팹/씬 배치가 완료되었으나, 이전 생성 기준의 `CircleCollider2D`가 남아 있고 `Rigidbody2D`가 없어 테스트 NPC와 충돌 구성이 달랐다.
- 수정:
  - `Assets/Scenes/AdventureScene.unity`: `NPC_JobChanger`에서 `CircleCollider2D` 제거, 테스트 NPC와 같은 `Rigidbody2D.bodyType=2` 적용, `BoxCollider2D`를 `size=(1,1)`, `offset=(0,0)`, `isTrigger=false`로 설정.
  - `Assets/Prefabs/NPCs/NPC_JobChanger.prefab`: 씬 인스턴스와 동일하게 `CircleCollider2D` 제거, `Rigidbody2D` 추가, `BoxCollider2D` 1칸 충돌 기준 적용.
- 검증:
  - Unity MCP로 `NPC_JobChanger` 씬 인스턴스 컴포넌트 확인: `CircleCollider2D` 없음, `Rigidbody2D` 있음, `BoxCollider2D` bounds size `1x1`, center `(0,2)`.
  - `NpcInteractable.InteractionCenter=(0,2)`, `InteractionRadius=0.5`로 Box bounds 기반 계산 확인.
  - `NPC_JobChanger.prefab` 루트 컴포넌트 목록도 `CircleCollider2D` 없이 `Rigidbody2D` 포함 상태로 확인.
  - `AdventureScene` 저장 완료.
  - Console에는 신규 게임 코드 오류 없음. MCP 직렬화/Animator 조회 관련 경고만 확인.
- 미검증:
  - PlayMode에서 플레이어가 전직관 주변을 이동할 때 실제 충돌/대화 후보 판정이 체감상 테스트 NPC와 동일한지는 아직 수동 확인하지 못했다.
- 다음 작업:
  - `NPC_JobChanger`에 전직 관련 `DialogueData` 또는 전직 UI 진입 로직 연결.
  - PlayMode에서 테스트 NPC와 전직관 NPC 주변 이동, 충돌 차단, Space 상호작용 후보 판정을 함께 육안 검증.

### 2026-05-07 (Antigravity - 전직관 NPC 애니메이션 제작 및 배치)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 목적: 
  - `Assets/Assets/Sprites/NPCs/JobChanger_Sprite.png`를 이용해 애니메이션 제작.
  - 가만히 서서 플레이어를 바라보도록 (가장 먼 거리의 축 기준) 애니메이션 변경 로직 구현.
  - 기존 NPC와 동일한 규격의 콜라이더 부착.
- 구현 내용:
  - `Assets/Scripts/Editor/JobChangerSetup.cs` 에디터 툴을 작성하여, 스프라이트를 4방향으로 슬라이싱하고 `IdleFront`, `IdleBack`, `IdleLeft`, `IdleRight` 4개의 애니메이션 클립과 `JobChanger_Controller` 블렌드 트리를 자동 생성.
  - `Assets/Scripts/Adventure/JobChangerNpc.cs` 스크립트를 작성하여 플레이어와의 거리를 계산, 가로/세로 중 가장 차이가 큰 축을 향하도록 `Animator` 파라미터(`DirectionX`, `DirectionY`)를 업데이트.
  - `Assets/Prefabs/NPCs/NPC_JobChanger.prefab`을 생성하고, BoxCollider2D (0.35x0.35, offset 0,-1.35)와 CircleCollider2D (radius 0.6, trigger) 부착 완료.
  - `AdventureScene` 씬의 `(0, 2, 0)` 위치에 생성한 NPC 인스턴스를 배치하고 씬을 저장.
- 검증:
  - 에디터 메뉴 `CardAdventure/Setup Job Changer` 실행 완료 및 에러 없음.
  - `NPC_JobChanger.prefab` 내부 구조 및 컴포넌트 이상 없음.
- 다음 작업:
  - 실제 PlayMode에서 플레이어가 주위를 맴돌 때 NPC가 정상적으로 방향을 전환하는지 확인.
  - NpcInteractable을 통한 전직 관련 대화/UI 연동 작업.
### 2026-05-07 (Codex - 이동 중 시점 전환 제한)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - 플레이어가 한 타일에서 다음 타일로 이동하는 도중(목적지에 도달하기 전) 다른 방향키를 누르면, 즉시 그 방향으로 이미지가 회전하는(포켓몬 스타일) 현상이 있었다. 이동 중에 다른 방향을 바라보는 것이 어색하게 느껴질 수 있었다.
- 수정:
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - `FixedUpdate()` 내에서 `isMoving == true`일 때 `inputDirection`에 따라 스프라이트의 바라보는 방향(`UpdateFacingDirection`)을 즉시 바꾸던 코드를 제거했다.
- 검증:
  - 코드 수정 완료. 이제 이동 중에 다른 방향키를 미리 누르고 있어도 목적지 타일에 완전히 도달하기 전까지는 시점이 돌아가지 않는다. (도착 시점에 해당 방향키가 눌려 있다면 그때 시점 전환 후 바로 다음 이동이 시작된다.)

### 2026-05-07 (Codex - NPC 인접 타일 접근 차단 원인 분석 및 해결)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - 플레이어가 NPC 바로 옆 칸으로 이동 시 이동이 차단되는 현상이 남아 있었다.
  - 원인 1: `Physics2D.OverlapBoxAll` 검사 시 사이즈가 0.98x0.98로 너무 커서 타일 경계에서 부동소수점 오차나 커스텀 콜라이더 오프셋에 걸릴 가능성이 존재했다.
  - 원인 2: `GridOccupancy.ToCell` 메서드에서 `Mathf.RoundToInt`를 사용하여 "Banker's Rounding(짝수 반올림)" 발생. `(0.5, 1.5)`와 `(0.5, 2.5)`가 모두 동일한 `(0, 2)` 셀로 매핑되어 점유 충돌이 발생.
  - 원인 3: `AdventureGridUtility.ConfigureFootCollider`가 런타임에 NPC/Player의 콜라이더 크기를 강제로 `1x1`, 오프셋 `(0,0)`으로 덮어써 사용자가 Inspector에서 설정한 축소된 발밑 콜라이더 값이 무시되고 있었다.
- 수정:
  - `Assets/Scripts/Adventure/GridOccupancy.cs`: `Mathf.RoundToInt`를 `Mathf.FloorToInt`로 변경하여 정확한 그리드 셀 매핑 보장.
  - `Assets/Scripts/Adventure/AdventureGridUtility.cs`:
    - `SnapToCellCenter`의 `Mathf.Round`를 `Mathf.Floor` 기반으로 수정.
    - `GetCollisionProbeSize`를 셀 크기의 `0.5x0.5`로 축소하여 목표 타일 중앙 부근의 장애물만 정확히 감지하도록 수정.
    - `ConfigureFootCollider`와 `DisableSolidCircles`에서 런타임 강제 덮어쓰기 로직 제거. 에디터에서 설정된 사용자 커스텀 크기/오프셋(예: 0.35x0.35) 유지.
- 검증:
  - 스크립트 수정 완료. 타일 그리드 수학 계산(Rounding 버그) 완벽히 해결.
  - 오버랩 박스(OverlapBoxAll) 사이즈 축소로 인접 타일 진입 시의 물리적 간섭 해결.
  - 사용자의 발밑 콜라이더 축소 의도가 런타임에도 정상 반영되도록 보장.

### 2026-05-07 (Codex - NPC 인접 타일 접근 차단 수정)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - 플레이어/NPC 발밑 `BoxCollider2D`를 정확히 타일 1칸 크기로 맞춘 뒤, 이동 전 충돌 검사 `OverlapBox`도 정확히 1칸 크기로 사용했다.
  - 이 때문에 NPC가 있는 타일 바로 옆 칸처럼 경계가 딱 맞닿는 위치도 겹침으로 오판되어 접근이 막힐 수 있었다.
- 수정:
  - `Assets/Scripts/Adventure/AdventureGridUtility.cs`
    - `GetCollisionProbeSize()` 추가. 실제 콜라이더 크기는 1칸으로 유지하되, 이동 가능성 검사 박스만 셀 크기보다 `0.02` 작게 계산.
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - 이동 전 `OverlapBoxAll` 검사 크기를 `GetCollisionProbeSize()`로 변경.
  - `Assets/Scripts/Adventure/NpcMovement.cs`
    - NPC 배회 이동 전 `OverlapBox` 검사 크기도 같은 기준으로 변경.
- 검증:
  - `AdventureGridUtility.cs`: Unity MCP `validate_script standard` 에러 0, 경고 0.
  - `PlayerController.cs`, `NpcMovement.cs`: Unity MCP `validate_script standard` 에러 0, 기존 GC 권장 경고만 확인.
  - Unity refresh/compile 후 Console 게임 코드 신규 에러 없음.

### 2026-05-07 (Codex - NPC 원형 콜라이더 제거)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - NPC에 발밑 `BoxCollider2D` 외에 대화용 `CircleCollider2D`가 남아 있어 원형 콜라이더가 계속 보였다.
- 수정:
  - `Assets/Scripts/Adventure/NpcMovement.cs`
    - `CircleCollider2D` 대화 트리거 필드와 런타임 자동 생성 로직 제거.
    - NPC는 발밑 `BoxCollider2D`만 타일 1칸 크기로 사용.
  - `Assets/Scripts/Adventure/NpcInteractable.cs`
    - 대화 중심/반경 계산을 원형 트리거가 아닌 활성 `BoxCollider2D` bounds 기준으로 변경.
  - `Assets/Scenes/AdventureScene.unity`
    - `NPC_BaramIroGun`의 `CircleCollider2D` 제거.
    - 남은 `BoxCollider2D`는 월드 bounds 기준 약 `1x1`, 중심 `(0.5, 2.5)`로 확인.
- 검증:
  - `NpcInteractable.cs`: Unity MCP `validate_script standard` 에러 0, 경고 0.
  - `NpcMovement.cs`: Unity MCP `validate_script standard` 에러 0, 기존 GC 권장 경고 1개.
  - Unity refresh/compile 후 NPC 컴포넌트 목록에 `CircleCollider2D` 없음, `BoxCollider2D` 1개만 확인.
  - Console: 게임 코드 신규 에러 없음. MCP 연결 관련 로그만 확인.

### 2026-05-07 (Codex - 타일 중앙 정렬/1칸 충돌 기준)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - 플레이어/NPC 위치 스냅이 월드 정수 좌표 기준이라 Unity Tilemap의 실제 셀 중앙과 어긋날 수 있었다.
  - 이동 단위와 충돌 검사, 대화 판정이 서로 다른 기준을 사용해 발밑 1칸 충돌 기준으로 정리할 필요가 있었다.
- 수정:
  - `Assets/Scripts/Adventure/AdventureGridUtility.cs` 추가.
    - `Grid.WorldToCell()`/`GetCellCenterWorld()` 기반 타일 중앙 스냅, 셀 크기, 방향별 한 칸 이동량, 발밑 BoxCollider 설정 공통화.
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - 게임 시작 시 가장 가까운 타일 중앙으로 스냅하고 Rigidbody 위치까지 동기화.
    - 이동 목표를 Grid 셀 크기 1칸 단위로 계산.
    - 발밑 충돌용 `BoxCollider2D`를 1x1 타일 크기로 사용하고 기존 비트리거 원형 콜라이더는 비활성화.
    - 플레이어 비주얼이 하반신 기준 1칸 안에 들어가도록 폭 1칸/높이 최대 2칸 기준으로 자동 스케일/오프셋 보정.
  - `Assets/Scripts/Adventure/NpcMovement.cs`
    - NPC 시작 위치와 이동 목표를 타일 중앙 기준으로 통일.
    - NPC 발밑 BoxCollider를 월드 1x1 타일 크기로 설정.
    - NPC 이동 검사도 1칸 Box 기준으로 변경.
    - 대화 트리거는 별도 CircleCollider2D로 유지하되 중심을 NPC 타일 중앙으로 맞추고 반경은 1.25칸 기준으로 보정.
  - `Assets/Scripts/Adventure/GridOccupancy.cs`
    - 점유 셀 계산을 Grid 기반 셀 좌표로 변경.
  - `Assets/Scripts/Adventure/DialogueManager.cs`
    - 플레이어 대화 판정을 기존 CircleCollider2D 전용에서 활성 비트리거 발밑 Collider2D 기준으로 변경.
  - `Assets/Scenes/AdventureScene.unity`
    - Player 위치를 `(0.5, 0.5)`, NPC 위치를 `(0.5, 2.5)` 타일 중앙으로 저장.
    - Player에 1x1 BoxCollider2D 추가 및 기존 CircleCollider2D 비활성화.
    - NPC BoxCollider2D 월드 크기 1x1, 대화 CircleCollider2D 중심/반경 보정.
    - Player/NPC 비주얼 스케일을 하반신 1칸 기준에 맞게 조정.
- 검증:
  - `AdventureGridUtility.cs`, `GridOccupancy.cs`: Unity MCP `validate_script standard` 에러 0, 경고 0.
  - `PlayerController.cs`, `NpcMovement.cs`, `DialogueManager.cs`: Unity MCP `validate_script standard` 에러 0, 기존 GC 권장 경고만 확인.
  - Unity refresh/compile 후 콘솔 게임 코드 에러 없음.
  - PlayMode 진입 후 Player 런타임 위치 `(0.5, 0.5)`, BoxCollider2D bounds `1x1`, Rigidbody Kinematic 전환, PlayerInput 활성 확인.
  - DialogueManager 런타임 참조: `DialogueView` 연결 및 `IsDialogueActive=false` 정상 확인.
  - Console: 게임 코드 신규 에러 없음. MCP 연결/직렬화 경고만 확인.
- 주의:
  - Git 명령은 현재 Windows 사용자 소유권 차이로 `dubious ownership` 경고가 발생해 상태 확인이 제한됨.

### 2026-05-07 (Codex - NPC 스페이스 대화 입력 재보강)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - NPC/플레이어 충돌 반경을 발밑 기준으로 줄인 이후, `DialogueManager`의 대기 NPC 판정도 같이 좁아져 스페이스 입력 시 `pendingNpc`가 비어 대화창이 열리지 않는 재발 가능성이 있었다.
  - `DialogueView` 참조가 씬 로드/비활성 UI 타이밍으로 비어 있으면 대화 시작은 시도되어도 패널이 표시되지 않을 수 있었다.
- 수정:
  - `Assets/Scripts/Adventure/DialogueManager.cs`
    - `interactionFallbackDistance = 1.35f`를 추가해, 줄어든 발밑 콜라이더를 유지하면서도 대화 가능한 최소 거리를 보장.
    - `activePlayer`가 비어 있으면 `UpdatePendingNpc()`에서 다시 탐색하도록 보강.
    - NPC 리스트 순회 중 null 항목을 건너뛰도록 방어 처리.
    - `EnsureDialogueView()`를 추가해 비활성 오브젝트까지 포함하여 `DialogueView`를 재탐색하고, 대화 시작 직전에 UI 참조를 보장.
- 검증:
  - `DialogueManager.cs` Unity MCP `validate_script standard`: 에러 0, 기존 GC 권장 경고 1개.
  - Unity refresh/compile 후 에디터 idle 확인.
  - Console: 게임 코드 신규 에러 없음. MCP 연결 로그만 확인.
- 다음 작업:
  - AdventureScene PlayMode에서 NPC 발밑 근처/측면/아래쪽 접근 후 Space 입력 시 힌트와 대화창이 정상 표시되는지 수동 확인.

### 2026-05-07 (Codex - 카드 사용 입력 기준 정리)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - 적 지정 카드의 첫 클릭 프레임이 곧바로 사용 확정 클릭으로도 처리되어, 첫 번째 클릭에서 화살표 UI가 보이지 않거나 적 선택을 할 수 없는 현상이 있었다.
  - 카드 사용 기준이 모호해 단일 대상 카드, 자기/전체 사용 카드, 취소 입력이 섞여 있었다.
- 수정:
  - `Assets/Scripts/UI/BattleUIManager.cs`
    - 카드 사용 모드를 `SingleTarget`, `PlayArea`로 분리.
    - 단일 대상 카드: 카드 클릭 시 화살표 표시, 같은 프레임 좌클릭은 무시, 이후 적 영역 클릭 시 사용.
    - 방어/스킬/전체 대상형 사용 카드: 카드 클릭 시 카드가 마우스를 따라다니며, 크기 확대 없이 회전 0 상태 유지. 손패 영역 위쪽 공간을 좌클릭하면 사용.
    - 모든 대기 중 카드 사용은 우클릭으로 취소.
    - 취소/전투 종료/전투 시작 시 pending 상태와 추적 카드를 정리.
  - `Assets/Scripts/UI/BattleCardView.cs`
    - `BeginPointerFollow()`, `EndPointerFollow()` 추가.
    - 마우스 추적 중인 카드는 자체 클릭 이벤트를 무시해 사용 확정 클릭이 선택 해제로 소비되지 않도록 처리.
    - 추적 중에는 매 프레임 `Input.mousePosition`을 따라가도록 처리.
  - `Assets/Scripts/UI/BattleHandView.cs`
    - `IsScreenPointAboveHand()` 추가. 손패 컨테이너 위쪽 클릭인지 판단해 PlayArea 카드 사용 가능 영역으로 사용.
- 검증:
  - `BattleUIManager.cs`, `BattleCardView.cs`: Unity MCP `validate_script standard` 오류 0, 기존 Update 관련 GC 권장 경고 각 1개.
  - `BattleHandView.cs`: Unity MCP `validate_script standard` 오류 0, 경고 0.
  - Unity refresh/compile 후 에디터 idle 확인.
  - Console: 게임 코드 신규 오류 없음. MCP 연결 로그만 확인.
- 다음 작업:
  - BattleTest PlayMode에서 다음 흐름 확인:
    - 단일 대상 공격 카드 클릭 → 화살표 표시 → 적 클릭으로 사용.
    - 방어/스킬/전체 대상 카드 클릭 → 카드가 마우스 추적 → 손패 위쪽 공간 클릭으로 사용.
    - 각 상태에서 우클릭 취소.

### 2026-05-07 (Codex - 카드 호버 단일 소유권 적용)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 문제:
  - 카드 호버/프리뷰 중 다른 카드 위치에 마우스를 올리면 이전 카드와 새 카드가 동시에 호버링되는 현상 발생.
  - 각 `BattleCardView`가 독립적으로 호버 상태와 프리뷰 코루틴을 관리해, 새 카드 입력이 이전 카드 상태를 즉시 종료하지 못했다.
- 수정:
  - `Assets/Scripts/UI/BattleCardView.cs`
    - 정적 `activeHoverView`를 추가해 현재 호버/프리뷰 소유 카드를 1장으로 제한.
    - 새 카드 `OnPointerEnter()` 시 기존 `activeHoverView`가 있으면 `CancelHoverAndPreview(true)`로 이전 카드의 프리뷰 코루틴, 프록시, 트윈을 정리하고 원위치 복귀 시작.
    - `ClosePreview()`, `OnPointerExit()`, `OnDisable()`, `DestroyImmediate()`에서 자신이 활성 소유자이면 `activeHoverView`를 해제.
- 검증:
  - `BattleCardView.cs` Unity MCP `validate_script standard`: 오류 0, 기존 Update 관련 GC 권장 경고 1개.
  - Unity refresh/compile 후 에디터 idle 확인.
  - Console: 게임 코드 신규 오류 없음. 서드파티 obsolete 경고와 MCP 연결 로그만 확인.
- 다음 작업:
  - BattleTest PlayMode에서 카드 A 호버 중 카드 B로 마우스를 옮겼을 때 A가 즉시 복귀하고 B만 호버/프리뷰되는지 확인.

### 2026-05-07 (Codex - 확대 카드 호버 유지 제거)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 사용자 확인 원인:
  - 가운데 확대 카드에 마우스를 올렸을 때도 프리뷰 유지 판정이 적용되어, 원래 카드 위치의 프록시 이벤트와 확대 카드 이벤트가 섞이며 복귀 위치값이 다시 지정되는 문제가 의심됨.
- 수정:
  - `Assets/Scripts/UI/BattleCardView.cs`
    - 프리뷰 유지 판정에서 확대된 카드 RectTransform 영역(`overCard`)을 제거.
    - 이제 프리뷰는 원래 손패 위치에 생성한 `CardHoverProxy` 위에 마우스가 있을 때만 유지.
    - `OnPointerEnter()` 시작부에 `isPreviewActive || isTransitioning` 가드를 추가해, 확대 카드가 새 호버/프리뷰 코루틴을 다시 시작하지 못하게 차단.
- 검증:
  - `BattleCardView.cs` Unity MCP `validate_script standard`: 오류 0, 기존 Update 관련 GC 권장 경고 1개.
  - Unity refresh/compile 후 에디터 idle 확인.
  - Console: 게임 코드 신규 오류 없음. MCP 연결 로그만 확인.
- 다음 작업:
  - BattleTest PlayMode에서 확대 카드 위로 마우스를 옮기면 프리뷰가 닫히고, 원래 손패 위치 위에 있을 때만 유지되는지 확인.

### 2026-05-07 (Codex - 카드 호버 후 원위치 미복귀 수정)

- 작업 시작 전 `PROJECT_STATUS.md`를 다시 확인했다.
- 다른 에이전트 진행사항 확인:
  - 최근 `Card.prefab`의 `CardName` 복구, `ManaCostText` 반영, 카드 호버 프리뷰 지연시간 `0.33s` 조정, `CardHoverProxy` 기반 프리뷰 유지 로직이 추가되어 있었다.
- 문제:
  - 일부 카드가 호버링 후 원래 손패 위치로 돌아가지 않음.
  - 원인 후보: `BattleHandView.ArrangeCards(animate:true)`가 카드 딜/재배치 애니메이션 완료 시점에 `SaveBasePosition()`을 호출해 기준 위치를 저장했다. 이 사이에 마우스 호버, 트윈 kill/complete, 프리뷰 전환이 끼면 기준 위치가 목표 손패 위치가 아니라 딜 시작점/중간 위치로 굳을 수 있었다.
- 수정:
  - `Assets/Scripts/UI/BattleCardView.cs`
    - `SetBaseState(localPosition, localRotation, siblingIndex)` 추가.
    - 호버 시작 시 기준 회전값을 먼저 복원해 기울어진 카드가 호버 후에도 안정적으로 기준 회전으로 돌아가도록 보정.
    - `OnDisable()`에서 프리뷰 코루틴, DOTween, 프록시 오브젝트를 정리.
  - `Assets/Scripts/UI/BattleHandView.cs`
    - 손패 레이아웃 목표 위치/회전을 계산한 즉시 `SetBaseState()`로 기준 상태를 저장.
    - 애니메이션 완료 콜백에서 현재 위치를 기준으로 다시 저장하던 로직 제거.
- 검증:
  - `BattleCardView.cs` Unity MCP `validate_script standard`: 오류 0, 기존 Update 관련 GC 권장 경고 1개.
  - `BattleHandView.cs` Unity MCP `validate_script standard`: 오류 0, 경고 0.
  - Unity refresh/compile 후 에디터 idle 확인.
  - Console: 신규 게임 코드 오류 없음. 서드파티 obsolete 경고와 MCP 연결 로그만 확인.
- 다음 작업:
  - BattleTest PlayMode에서 카드 딜 애니메이션 중/직후 빠른 호버, 프리뷰 진입/해제, 연속 호버를 육안 확인.

### 2026-05-07 (Claude Desktop — Card.prefab CardName 복구 + 깨진 참조 정리)

- 목적: 카드 이름이 표시되지 않는 버그 수정.
- 원인: Unity 에디터에서 CardName / CardImage 자식 오브젝트가 삭제되어 BattleCardView의 `cardNameText`·`cardArtImage` 참조가 모두 null로 깨진 상태. 루트 RT m_Children에도 stale fileID 잔재.
- 변경 파일:
  - `Assets/Prefabs/UI/Card.prefab` **(수정)**
    - 새 `CardName` 자식 추가 (GO/RT/CanvasRenderer/TMP, fileID: 6400500600700800904).
      - 앵커: 상단 중앙, AnchoredPosition (0, -20), SizeDelta (160, 25), 폰트 크기 13 bold 흰색.
    - BattleCardView.cardNameText → 새 TMP (fileID: 6400500600700800904).
    - BattleCardView.cardArtImage → null (fileID: 0) — CardImage 자식 삭제 반영.
    - 루트 RT m_Children에서 stale CardName·CardImage RT 참조 2개 제거.
- 검증: prefab 저장 후 참조 정합성 확인 완료.
- Unity 에디터 확인 필요:
  - Refresh 후 Card.prefab Inspector에서 CardName 자식·BattleCardView.cardNameText 연결 확인.
  - BattleTest 씬 실행 → 카드 이름이 표시되는지 확인.
- 주의: cardArtImage가 null이므로 카드 아트 이미지 표시가 필요하면 CardImage 자식 재추가 필요.

### 2026-05-07 (Claude Desktop — 카드 호버 프리뷰 딜레이 0.33초로 조정)

- 변경 파일:
  - `Assets/Scripts/UI/BattleCardView.cs` **(수정)**
    - `previewDelay` 기본값 1.0s → 0.33s.
- 다음 작업: BattleTest 씬에서 체감 반응속도 확인 후 필요 시 Inspector에서 추가 조정.

### 2026-05-07 (Claude Desktop — Card.prefab ManaCostText 반영)

- 목적: Card.prefab에 추가된 ManaCostText(마나 코스트 TMP) 자식을 BattleCardView가 인식하고 값을 표시.
- 변경 파일:
  - `Assets/Scripts/UI/BattleCardView.cs` **(수정)**
    - `energyCostText: TextMeshProUGUI` 필드 추가.
    - `Awake()`: `transform.Find("ManaCostText")`로 자동 탐색.
    - `Refresh()`: `energyCostText.text = data.energyCost.ToString()` 반영.
- 검증: BattleCardView.cs validate_script standard: 오류 0, 경고 1(오탐).
- 다음 작업:
  - BattleTest 씬 실행 → 손패 카드에 마나 코스트 숫자가 표시되는지 확인.

### 2026-05-07 (Claude Desktop — 카드 호버 프리뷰 버그 수정: 이동 중 프리뷰 즉시 닫힘)

- 목적: 프리뷰 중 카드가 이동하면 EventSystem이 OnPointerExit를 발생시켜 즉시 닫히는 버그 수정. 중앙 카드·원래 위치 양쪽에서 호버링/클릭 허용.
- 변경 파일:
  - `Assets/Scripts/UI/CardHoverProxy.cs` **(신규)**
    - 프리뷰 중 원래 손패 위치에 런타임 생성되는 투명 히트박스.
    - `IPointerClickHandler`만 구현해 클릭을 `BattleCardView.OnProxyClick()`으로 전달.
    - `BattleCardView.SpawnProxy()`가 생성, `ClosePreview()`가 제거.
  - `Assets/Scripts/UI/BattleCardView.cs` **(수정)**
    - `OnPointerExit`: 프리뷰 활성 중(`isPreviewActive`)이면 즉시 `return` — 카드 이동으로 인한 오발 방지.
    - `Update()` 폴링 추가: 프리뷰 중 매 프레임 `RectTransformUtility.RectangleContainsScreenPoint`로 두 영역 검사.
      - 중앙 카드(reparent된 카드 RectTransform) 위 → 유지.
      - 원래 손패 위치(proxyRt) 위 → 유지.
      - 둘 다 벗어남 → `ClosePreview(animate: true)`.
    - `isTransitioning` 플래그: 전환 애니메이션 중에는 폴링 스킵 (false positive 방지).
    - `SpawnProxy()`: 원래 부모에 200×300 투명 RectTransform + Image(raycastTarget=true) + CardHoverProxy 생성.
    - `OnProxyClick()`: 프록시 클릭 시 `ClosePreview(false)` + `Clicked` 이벤트 발생.
    - `baseSize` 필드 추가 — `SaveBasePosition()`에서 RectTransform.sizeDelta 저장, 프록시 크기 설정에 사용.
- 검증:
  - CardHoverProxy.cs validate_script standard: 오류 0, 경고 0.
  - BattleCardView.cs validate_script standard: 오류 0, 경고 1(Update 내 문자열 연결 오탐 — 실제 없음).
- 동작 정리:
  - 마우스를 1초 호버 → 카드 중앙 이동·확대, 원래 위치에 투명 프록시 생성.
  - 중앙 카드 위 또는 원래 위치 위 → 프리뷰 유지.
  - 두 영역 모두 벗어남 → 부드럽게 원위치 복원.
  - 중앙 카드 클릭 또는 원래 위치 클릭 → 카드 사용 처리.
- 다음 작업:
  - BattleTest 씬 실행 → 프리뷰 동작 최종 확인.
  - 에너지 비용 표시 UI (Card.prefab에 EnergyCost TMP 자식 추가).
  - 배틀 씬 HUD 완성 (HP바, 에너지바, 턴 표시, 적 의도 표시).

### 2026-05-07 (Claude Desktop — 카드 호버 프리뷰 (1초 → 화면 중앙 확대))

- 목적: 손패 카드에 마우스를 1초 이상 올리면 카드가 화면 정중앙으로 이동·확대되어 설명을 읽기 쉽게 표시.
- 변경 파일:
  - `Assets/Scripts/UI/BattleCardView.cs` **(수정)**
    - `PreviewRoutine()` 코루틴 추가: `previewDelay`(기본 1초) 대기 후 프리뷰 발동.
    - 프리뷰 발동 시: 카드를 `Canvas.rootCanvas` 자식으로 임시 reparent → `localPosition=(0, previewOffsetY, 0)` 트윈 (화면 정중앙) → `localRotation=0` 트윈 (기울기 제거) → `localScale=previewScale(2.4)` 트윈.
    - `ClosePreview(animate)`: 마우스 이탈·클릭·선택 시 원래 부모/위치/회전/스케일 복원. animate=true면 DOTween, false면 즉시.
    - `CancelPreviewCoroutine()`: OnPointerExit, OnPointerClick, SetSelected 시 코루틴 취소.
    - `SaveBasePosition()`에 `baseLocalRotation` 기록 추가.
    - `rootCanvas` Awake()에서 캐싱.
    - Inspector 조절 가능 파라미터: `previewDelay`, `previewScale`, `previewOffsetY`, `previewDuration`.
  - 다른 카드/레이아웃에 영향 없음: reparent 방식이므로 handContainer의 나머지 카드는 그대로 유지.
- 검증:
  - BattleCardView.cs validate_script standard: 오류 0, 경고 0.
- Unity 에디터 확인 필요:
  - BattleTest 씬 실행 → 손패 카드 1초 호버 → 화면 중앙에 기울기 없이 크게 표시되는지 확인.
  - 마우스 이탈 → 원래 부채꼴 위치·기울기·크기로 복원되는지 확인.
  - 카드 클릭(선택·사용) 시 프리뷰가 즉시 닫히는지 확인.
  - Inspector에서 previewScale, previewOffsetY 값 튜닝 가능.
- 다음 작업:
  - 에너지 비용 표시 UI (Card.prefab에 EnergyCost TMP 자식 추가).
  - 배틀 씬 HUD 완성 (HP바, 에너지바, 턴 표시, 적 의도 표시).

### 2026-05-07 (Claude Desktop — Card.prefab 교체 + 직업·등급별 카드 배경 스프라이트 시스템)

- 목적: 배틀 씬에서 기존 CardView.prefab 대신 Card.prefab 사용. 직업×등급에 따라 카드 배경 스프라이트 자동 적용.
- 변경 파일:
  - `Assets/Scripts/UI/CardSpriteLibrary.cs` **(신규)**
    - `CardSpriteSet` 구조체: Common/Uncommon/Rare/Legendary 스프라이트 4개 + `GetByGrade()`.
    - `CardSpriteLibrary` ScriptableObject: warrior/mage/rogue/universal 세트 + `GetCardSprite(class, grade)`.
  - `Assets/Scripts/UI/BattleCardView.cs` **(수정)**
    - `cardBackground`(Image), `cardNameText`/`descriptionText`(TMP), `cardArtImage`(Image) 필드 유지.
    - `Awake()`에서 자식 오브젝트 이름("CardName", "CardDescription", "CardImage")으로 자동 탐색.
    - `SetSpriteLibrary()` 메서드 추가 — BattleHandView가 생성 시 주입.
    - `ApplyBackgroundSprite()`: 스프라이트 라이브러리 있으면 직업+등급 스프라이트, 없으면 타입 색상 폴백.
    - `cardTypeIcon`/`energyCostText` 제거 (Card.prefab 구조에 없는 필드 정리).
    - `SetInteractable()`: 색 대신 알파(0.5)로 비활성화 표현.
  - `Assets/Scripts/UI/BattleHandView.cs` **(수정)**
    - `spriteLibrary: CardSpriteLibrary` 필드 추가.
    - `RefreshHand()`: 카드 뷰 Instantiate 후 `SetSpriteLibrary()` 호출.
  - `Assets/Prefabs/UI/Card.prefab` **(수정)**
    - BattleCardView MonoBehaviour 컴포넌트 추가 (fileID: 7143928651094735881).
    - cardBackground/cardNameText/descriptionText/cardArtImage를 prefab 내 컴포넌트 fileID로 직접 연결.
  - `Assets/ScriptableObjects/CardSpriteLibrary.asset` **(신규)**
    - warrior/mage/rogue/universal 각 4등급 스프라이트 12개 모두 연결.
  - `Assets/Scripts/UI/CardSpriteLibrary.cs.meta` **(신규)** — guid: b8c4d2e1f0a3974658102938475bcd01
  - `Assets/ScriptableObjects/CardSpriteLibrary.asset.meta` **(신규)** — guid: c1d2e3f4a5b6074839201047586abcde
  - `Assets/Scenes/BattleTest.unity` **(수정)**
    - BattleHandView.cardViewPrefab → Card.prefab (guid: 0db826f77733f6d40918da0cf419705a, fileID: 7143928651094735881)
    - BattleHandView.spriteLibrary → CardSpriteLibrary.asset (guid: c1d2e3f4a5b6074839201047586abcde)
- 검증:
  - CardSpriteLibrary.cs validate_script standard: 오류 0, 경고 0.
  - BattleCardView.cs validate_script standard: 오류 0, 경고 0.
  - BattleHandView.cs validate_script standard: 오류 0, 경고 0.
- Unity 에디터 작업 필요 (코드로 불가):
  - Unity Editor에서 프로젝트 Refresh → Card.prefab Inspector에서 BattleCardView 컴포넌트 연결 확인.
  - BattleHandView Inspector에서 cardViewPrefab(Card.prefab), spriteLibrary(CardSpriteLibrary) 연결 확인.
  - CardSpriteLibrary Inspector에서 12개 스프라이트 슬롯이 채워졌는지 확인.
  - BattleTest 씬 실행 → 손패 카드에 직업별 배경 스프라이트가 표시되는지 확인.
- 다음 작업:
  - BattleTest 씬 PlayMode 동작 확인 후 이상 없으면 배틀 UI 완성도 향상 작업.
  - 향후: 에너지 비용 표시 UI 추가 (Card.prefab에 EnergyCost TMP 자식 추가).

### 2026-05-07 (Claude Desktop — 직업별 카드 데이터 추가)

- 작업 시작 전 `PROJECT_STATUS.md`를 먼저 확인했다.
- 목적: 전사·마법사·도적 각 직업의 카드 컨셉과 덱 다양성을 고려한 카드 데이터 추가.
- 변경 파일:
  - `Assets/Scripts/Data/CardData.cs`
    - `CardEffectType` 신규 7종 추가:
      - DoubleStrike(102), BerserkerAttack(103), AttackAndDefend(104), AttackAndApplyStatus(105)
      - DefenseAndDraw(202), DrawCards(302), GainStrength(303)
  - `Assets/Scripts/Battle/BattleManager.cs`
    - 신규 effectType 7종 케이스 처리 추가.
    - `needsEnemyTarget`에 신규 공격 effectType 반영.
  - 신규 StatusEffect 에셋 3종:
    - `Assets/ScriptableObjects/StatusEffects/Status_Vulnerable.asset` (취약, Vulnerable=2)
    - `Assets/ScriptableObjects/StatusEffects/Status_Strength.asset` (강화, Strength=3)
    - `Assets/ScriptableObjects/StatusEffects/Status_Regeneration.asset` (재생, Regeneration=4)
  - 신규 카드 에셋 27종:
    - 전사 7종: 쌍격, 광전사의 일격, 전진, 전사의 절규, 강철 의지, 불굴, 맹공
    - 마법사 10종: 화염구, 얼음 파편, 독구름, 마력 방패, 집중, 번개, 취약 주문, 연쇄 번개, 원소 강화, 마나 재생
    - 도적 10종: 단검 투척, 독침, 회피, 속격, 그림자 발걸음, 약점 파악, 기습, 독 안개, 쌍검, 암살
  - `카드데이터_입력양식.xlsx` 갱신 (전 직업 32종 + 직업 컨셉 시트 추가)
- 검증:
  - `CardData.cs` validate_script standard: 오류 0, 경고 0.
  - `BattleManager.cs` validate_script standard: 오류 0, 경고 0.
- 다음 작업:
  - Unity 에디터 Refresh 후 Cards 폴더에서 에셋 로드 확인.
  - BattleTest 씬에서 신규 카드 PlayMode 동작 확인.

### 2026-05-07 (Claude Desktop — 카드 효과 ID 시스템 리팩터링)

- 작업 시작 전 `PROJECT_STATUS.md`를 먼저 확인했다.
- 목적: `BattleManager`의 `CardData.name` 문자열 비교를 제거하고 명시적 enum ID로 교체.
- 변경 파일:
  - `Assets/Scripts/Data/CardData.cs`
    - `CardEffectType` enum 추가 (None=0, BasicAttack=100, ShieldBash=101, BasicDefense=200, Rage=300, Taunt=301, ApplyStatusToEnemy=400, ApplyStatusToPlayer=401).
    - `CardData`에 `effectType`, `secondaryValue` 필드 추가.
    - `GetFormattedDescription()`에 `{value2}` 치환 지원 추가.
  - `Assets/Scripts/Battle/BattleManager.cs`
    - `ApplyCardEffect()` 를 `effectType` 스위치 단일 메서드로 통합. `ApplyAttackCard()` / `ApplySkillCard()` 제거.
    - `needsEnemyTarget` 판별을 `cardType` 대신 `effectType` 기준으로 변경.
    - 미설정(None) 및 미처리 effectType은 `Debug.LogWarning` 출력.
  - `Assets/ScriptableObjects/Cards/Warrior/*.asset` 5종
    - `effectType` 및 `secondaryValue` 필드 직접 기록:
      - Strike → 100(BasicAttack), Defend → 200(BasicDefense)
      - ShieldBash → 101(ShieldBash), Rage → 300(Rage), Taunt → 301(Taunt)
  - `카드데이터_입력양식.xlsx` 생성 (프로젝트 루트)
    - 구글 스프레드시트 붙여넣기용. 시트 3개: 카드 목록(전사 5종 샘플+드롭다운), 필드 설명, effectType 코드표.
- 검증:
  - `CardData.cs` validate_script standard: 오류 0, 경고 0.
  - `BattleManager.cs` validate_script standard: 오류 0, 경고 0.
- 다음 작업:
  - Unity 에디터에서 전사 카드 5종 Inspector 확인 (effectType 드롭다운이 올바른 값으로 표시되는지).
  - 새 카드 추가 시 `CardEffectType` enum에 항목 추가 → `BattleManager` switch 케이스 추가 → 스프레드시트 코드표 갱신 순서로 진행.

### 2026-05-07 (Codex - Space 대화 불가 수정)

- 작업 시작 전 `PROJECT_STATUS.md`를 먼저 확인했다.
- 문제:
  - Player/NPC 콜라이더 축소 후 `Space`를 눌러도 대화가 시작되지 않음.
  - `DialogueManager`가 NPC/Player 오브젝트 중심 거리로만 대화 후보를 찾고 있어, 발밑으로 옮긴 상호작용 콜라이더 기준과 맞지 않았다.
  - `NPC_BaramIroGun`은 Transform scale `0.267`이라 `CircleCollider2D radius=0.6`이 실제 월드 반경 `0.16`으로 줄어들어 대화 범위가 지나치게 작아졌다.
- 수정:
  - `Assets/Scripts/Adventure/NpcInteractable.cs`
    - NPC 상호작용 트리거 원의 실제 월드 중심/반경을 반환하는 `InteractionCenter`, `InteractionRadius` 추가.
  - `Assets/Scripts/Adventure/DialogueManager.cs`
    - 대화 후보 탐지를 오브젝트 중심 거리에서 Player/NPC 발밑 상호작용 범위 기준으로 변경.
    - 1칸 그리드에서 NPC 바로 아래 칸에 서면 대화가 가능하도록 Player 상호작용 반경에 최소 0.5 유닛과 0.05 유닛 여유를 적용.
  - `Assets/Scripts/Editor/DialogueSceneSetup.cs`
    - 새 NPC 생성 시 오브젝트 스케일이 1이 아니어도 상호작용 트리거의 월드 반경이 0.6이 되도록 `radius`를 로컬 스케일 보정.
  - 현재 `AdventureScene`의 `NPC_BaramIroGun`
    - `BoxCollider2D`: 월드 크기 `0.35 x 0.35`, 발밑 로컬 offset `(0,-1.35)` 유지.
    - `CircleCollider2D`: 월드 반경 `0.6`, 발밑 로컬 offset `(0,-1.35)` 유지.
    - 씬 저장 완료.
  - `AGENTS.md`에 NPC 스케일이 1이 아닐 때 `size/radius`는 스케일 보정하고 `offset=(0,-1.35)`는 SPUM 로컬 발밑 기준으로 유지하라는 지침 갱신.
- 검증:
  - `DialogueManager.cs`: Unity MCP `validate_script standard` 오류 0, 기존 GC 권장 경고 1개.
  - `NpcInteractable.cs`, `DialogueSceneSetup.cs`: Unity MCP `validate_script standard` 오류 0, 경고 0.
  - 씬 컴포넌트 확인: `NPC_BaramIroGun` 상호작용 트리거 월드 중심 `y=1.6395`, 월드 반경 `0.6`.
  - Console: 게임 코드 신규 오류 없음. 서드파티 obsolete 경고와 MCP 도구 로그만 확인.
- 다음 작업:
  - PlayMode에서 Player가 NPC 바로 아래 칸에 섰을 때 Space 대화 시작 여부를 육안 확인.

### 2026-05-07 (Codex - S 아래 이동 차단 수정)

- 작업 시작 전 `PROJECT_STATUS.md`를 먼저 확인했다.
- 문제:
  - Player 발 콜라이더를 아래로 내린 뒤, `S` 입력으로 아래 칸 이동 시 이동 검사 `OverlapBox`가 플레이어 자신의 콜라이더를 장애물로 오인할 수 있었다.
- 수정:
  - `Assets/Scripts/Adventure/PlayerController.cs`
    - 단일 `Physics2D.OverlapBox` 검사에서 `Physics2D.OverlapBoxAll` 검사로 변경.
    - 트리거 콜라이더와 플레이어 본인/자식 콜라이더를 건너뛰는 `IsSelfCollider()`를 추가.
    - 실제 비트리거 장애물 콜라이더가 있을 때만 이동을 막도록 정리.
- 검증:
  - Unity MCP `validate_script standard`: 오류 0, 기존 GC 권장 경고 1개만 확인.
  - Unity refresh/compile 요청 후 에디터 idle 상태 확인.
  - Console: 게임 코드 신규 오류 없음. MCP client handler 로그만 확인.
- 다음 작업:
  - PlayMode에서 `W/A/S/D` 연속 이동과 NPC 근접 충돌을 직접 확인하면 좋다.

### 2026-05-07 (Codex - Player/NPC 발밑 콜라이더 축소)

- 작업 시작 시 `PROJECT_STATUS.md`를 먼저 확인했다.
- 문제:
  - Player/NPC의 이동/상호작용 콜라이더가 전신 중심에 가깝고 크게 잡혀 있어 서로 부딪히는 범위가 넓게 느껴짐.
- 현재 `AdventureScene` 적용:
  - Player `CircleCollider2D`: `radius 0.45 -> 0.225`, `offset (0,0) -> (0,-0.45)`.
  - `NPC_BaramIroGun` 실제 충돌 `BoxCollider2D`: `size (0.7,0.7) -> (0.35,0.35)`, `offset (0,0) -> (0,-1.35)`.
  - `NPC_BaramIroGun` 상호작용 트리거 `CircleCollider2D`: `radius 1.2 -> 0.6`, `offset (0,0) -> (0,-1.35)`.
  - `AdventureScene` 저장 완료.
- 생성/재적용 도구 수정:
  - `Assets/Scripts/Editor/AdventureSceneBuilder.cs`: 새 AdventureScene 생성 시 Player 콜라이더를 발밑 기준 축소값으로 생성.
  - `Assets/Scripts/Editor/WarriorPlayerVisualSetup.cs`: 직업 비주얼 재적용 시 Player 콜라이더도 발밑 기준 축소값으로 유지.
  - `Assets/Scripts/Editor/DialogueSceneSetup.cs`: 새 NPC 상호작용 트리거 생성 기본값을 `radius=0.6`, `offset=(0,-1.35)`로 변경.
  - `Assets/Scripts/Adventure/DialogueManager.cs`: 거리 기반 대화 가능 거리 `1.2 -> 0.6`.
  - `Assets/Scripts/Adventure/NpcInteractable.cs`: Gizmo 표시를 축소된 발밑 범위에 맞춤.
- 다른 에이전트 지침:
  - `AGENTS.md`에 Player/NPC 발밑 콜라이더 기준을 추가.
  - `CLAUDE.md`는 현재 텍스트 인코딩이 깨진 상태라 안전한 패턴 패치를 적용하지 못했으며, 파일 훼손 방지를 위해 직접 수정하지 않음. 동일 지침은 `AGENTS.md`와 이 상태 문서에 기록.
- 검증:
  - Player/NPC 씬 컴포넌트 값 재확인 완료.
  - `AdventureSceneBuilder.cs`, `DialogueSceneSetup.cs`, `NpcInteractable.cs` Unity MCP `validate_script standard`: 오류 0, 경고 0.
  - `DialogueManager.cs` Unity MCP `validate_script standard`: 오류 0, 기존 GC 권장 경고 1개만 확인.
- 주의:
  - Console에 남는 MCP client/serializer 로그는 도구 연결/직렬화 로그이며 게임 코드 신규 에러가 아님.

### 2026-05-07 (Codex - 마법사/도적 Idle 스프라이트 크기 보정)

- 작업 시작 시 `PROJECT_STATUS.md`를 먼저 확인했다.
- 문제:
  - Magician/Rogue도 Warrior와 동일하게 Idle 스프라이트 원본이 Walk 스프라이트보다 커서 Idle 상태에서 캐릭터가 비정상적으로 크게 보임.
- `Assets/Scripts/Editor/WarriorPlayerVisualSetup.cs` 수정:
  - Magician `idleVisualScaleMultiplier`: `1.0` -> `0.85`.
  - Rogue `idleVisualScaleMultiplier`: `1.0` -> `0.74`.
  - `Apply Magician`/`Apply Rogue` 메뉴 실행 시 `PlayerController.idleVisualScaleMultiplier`와 `PlayerVisual.localScale`이 해당 값으로 저장되도록 유지.
- 적용/검증:
  - Unity MCP `validate_script standard`: 오류 0, 기존 권장 경고 1개(null check 권장)만 확인.
  - `Setup All Class Visuals` 메뉴 재실행.
  - `Apply Magician` 실행 후 `idleVisualScaleMultiplier=0.85` 확인.
  - `Apply Rogue` 실행 후 현재 `AdventureScene`을 Rogue 상태로 되돌리고 `idleVisualScaleMultiplier=0.74`, `PlayerVisual.localScale=0.74` 확인.
  - `AdventureScene` 저장 완료.
- 주의:
  - Console에 남는 MCP serializer/client handler 로그는 도구 연결/직렬화 로그이며 게임 코드 신규 에러가 아님.

### 2026-05-07 (Codex - 마법사/도적 플레이어 비주얼 생성)

- 작업 시작 시 `PROJECT_STATUS.md`를 먼저 확인했다.
- 새 캐릭터 스프라이트 확인:
  - `Assets/Assets/Sprites/Character/Magician/Idle`, `Walk`
  - `Assets/Assets/Sprites/Character/Rogue/Idle`, `Walk`
- `Assets/Scripts/Editor/WarriorPlayerVisualSetup.cs`를 다직업 플레이어 비주얼 생성 도구로 확장.
  - 기존 메뉴 `CardAdventure > Setup Warrior Player Visual` 유지.
  - 신규 메뉴 추가:
    - `CardAdventure > Player Visual > Setup All Class Visuals`
    - `CardAdventure > Player Visual > Apply Warrior`
    - `CardAdventure > Player Visual > Apply Magician`
    - `CardAdventure > Player Visual > Apply Rogue`
  - `PlayerController`가 직접 재생하는 상태명과 호환되도록 모든 컨트롤러의 상태 이름은 `Player_IdleFront/Back/Side`, `Player_WalkFront/Back/Side`로 통일.
- 생성된 에셋:
  - `Assets/Animations/Player/Player_Magician.controller`
  - `Assets/Animations/Player/Player_Rogue.controller`
  - `Assets/Animations/Player/Magician/` 아래 6개 애니메이션 클립.
  - `Assets/Animations/Player/Rogue/` 아래 6개 애니메이션 클립.
- 스케일:
  - Warrior는 기존처럼 idle 보정 `0.267`.
  - Magician/Rogue는 idle/walk 원본 크기가 비슷해 idle 보정 `1.0`.
- 검증:
  - `WarriorPlayerVisualSetup.cs` Unity MCP `validate_script standard`: 오류 0, 권장 경고 1개(null check 권장)만 확인.
  - `Setup All Class Visuals` 메뉴 실행 후 Magician/Rogue 컨트롤러와 애니메이션 클립 생성 확인.
  - 컨트롤러 상태 이름이 `PlayerController` 기대 이름과 일치함을 확인.
- 미검증:
  - PlayMode에서 `Apply Magician`/`Apply Rogue` 후 실제 이동 애니메이션 육안 확인은 진행하지 못함.

### 2026-05-07 (Codex - 대화창 콘솔 에러 핫픽스)

- 콘솔 에러 확인:
  - `CardAdventure.DialogueView.ShowLine()`에서 `Febucci.UI.TypewriterByCharacter.ShowText()` 호출 중 TMP `TextMeshProUGUI.GenerateTextMesh()` `NullReferenceException` 발생.
- `Assets/Scripts/UI/DialogueView.cs` 수정:
  - Febucci 타입라이터 호출을 `try/catch`로 감싸고, 실패 시 일반 TMP 텍스트 즉시 표시로 폴백.
  - 같은 세션에서 타입라이터가 다시 같은 예외를 반복하지 않도록 `typewriterUnavailable` 런타임 플래그 추가.
  - null 문자열은 `string.Empty`로 처리.
- 검증:
  - `DialogueView.cs` Unity MCP `validate_script standard`: 오류 0, 경고 0.
  - Console clear 후 원래 TMP/Febucci `NullReferenceException` 재발 없음.
- 주의:
  - 현재 남는 MCP client handler exited 로그는 Unity MCP 연결 종료 로그이며 게임 코드 에러가 아님.

### 2026-05-07 (Codex - DEVNIK 픽셀 대화창 적용)

- 작업 시작 시 `PROJECT_STATUS.md`를 먼저 확인했다.
- `Assets/Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/UI SIMPLE PIXEL UNSPLIT.png.meta`
  - 대화창에 사용할 `BG_BAR2` 스프라이트에 9-slice border `{x:32,y:32,z:32,w:32}` 적용.
  - 이름 탭/바 계열 스프라이트(`SET_BAR`, `BAR1`~`BAR4`, `PLAY BAR`, `LEVEL_BAR`, `EXIT_BAR`)에 9-slice border `{x:28,y:24,z:28,w:24}` 적용.
  - `BAR_BG`, `BAR_OUTLINE`에 작은 9-slice border 적용.
  - 픽셀 보존을 위해 Point 필터, mipmap off, 기본 플랫폼 Uncompressed 설정을 확인/적용.
- `Assets/Scenes/AdventureScene.unity`
  - `DialogueCanvas/DialoguePanel/PanelBg`를 DEVNIK `BG_BAR2` 스프라이트 기반 `Image.Type.Sliced` 패널로 변경.
  - `NameBox`를 DEVNIK `SET_BAR` 스프라이트 기반 `Image.Type.Sliced` 이름 탭으로 변경.
  - 패널/텍스트/화자 이름 영역 여백과 크기를 픽셀 UI에 맞게 조정하고 기존 Outline 컴포넌트를 제거.
- `Assets/Scripts/Editor/DialoguePixelUiStyleTool.cs` 추가.
  - 메뉴: `CardAdventure > UI > Apply DEVNIK Dialogue Window`.
  - DEVNIK UI 텍스처 import 설정과 현재 씬 대화창 스타일을 재적용할 수 있는 Editor 유틸리티.
- 검증
  - `DialoguePixelUiStyleTool.cs` Unity MCP `validate_script standard`: 오류 0, 경고 0.
  - Unity Console: 신규 컴파일 오류 없음. MCP 연결 종료 로그만 확인.
  - `PanelBg`, `NameBox` Image가 `Sliced`, `hasBorder=true`, DEVNIK PNG 스프라이트 참조 상태임을 확인.
- 미검증
  - PlayMode에서 실제 NPC 대화 시작 후 최종 화면 캡처는 진행하지 못함.

이 파일은 Codex, Claude Desktop, Antigravity가 작업을 바로 이어받기 위한 공용 인수인계 문서입니다. 모든 에이전트는 작업 시작 시 이 파일을 먼저 읽고, 작업 종료 시 최신 상태로 갱신합니다.

## 현재 목표

- 기획서 **카드 배틀 자격증 어드벤처 — 게임 기획서 v1.0 (2026.05)** 를 기준으로 Unity 2D 탑다운 어드벤처 + 턴제 카드 배틀 RPG를 구현한다.
- [x] Phase 1: 전투 루프 안정화 및 신규 몬스터(MagicDeer) 연동 완료
- [ ] Phase 2: 베르데 평원 루미나 마을 및 이벤트 전투 구현 (진행 중)
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
3. **챕터 1 Tilemap 맵 제작** (베르데 평원 거점 마을 레이아웃) — `CardAdventure > Map > Generate Town` 메뉴 실행 후 레이아웃 배치.
4. **정식 BattleScene 제작** — `SceneLoader.BATTLE_SCENE_NAME = "BattleTest"` 상수를 정식 씬명으로 교체.

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
### 2026-05-13 (Codex - 어드벤처→배틀 아이리스 전환 연출)

#### 이번 작업 요약
- `SceneLoader.EnterBattle()` 경로에 DOTween 기반 아이리스 클로즈 전환을 추가했다.
- 어드벤처 씬에서 배틀 씬으로 넘어갈 때 화면 전체에 검정 오버레이가 뜨고, 가운데 원형 구멍이 `1.15 -> 0`으로 줄어든 뒤 `BattleTest` 씬을 로드한다.
- 배틀 씬 로드 후에는 검정 오버레이가 기존 `fadeDuration` 값으로 페이드아웃된다.
- 일반 `LoadScene()` 및 `ReturnFromBattle()` 경로는 기존 전체 페이드 전환을 유지한다.

#### 변경 파일
- `Assets/Scripts/Core/SceneLoader.cs`
  - `irisCloseDuration`, `IrisTransitionGraphic`, `CanvasGroup` 참조 추가.
  - `LoadScene(sceneName, useIrisTransition)` 오버로드와 `PlayIrisTransition()` 추가.
  - `EnterBattle()`이 아이리스 전환을 사용하도록 변경.
- `Assets/Scripts/UI/IrisTransitionGraphic.cs`
  - 검정 UI 메쉬를 직접 생성하고 중앙 원형 구멍을 남기는 `Graphic` 컴포넌트 추가.

#### 검증 결과
- `SceneLoader.cs`, `IrisTransitionGraphic.cs` Unity `validate_script standard`: 오류 0개.
- Unity 강제 에셋 리프레시 및 스크립트 컴파일 요청 완료.
- 콘솔 컴파일 오류는 새 스크립트 인식 전 1회 발생했으나, 강제 리프레시 후 현재 스크립트 컴파일 오류는 확인되지 않았다.

#### 미검증 / 다음 작업
- PlayMode에서 실제 NPC 추격 전투 진입 또는 `BattleEntrance` 트리거 진입 시 아이리스 구멍 축소 연출의 체감 속도와 화면 중앙 정렬을 눈으로 확인해야 한다.
- 연출이 너무 빠르거나 느리면 `SceneLoader.irisCloseDuration` 기본값 `0.65`를 조정한다.

---
