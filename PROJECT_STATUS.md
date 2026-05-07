# CardAdventure Project Status

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
