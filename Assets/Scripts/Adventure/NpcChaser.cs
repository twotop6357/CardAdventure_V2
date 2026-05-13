using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// NPC가 바라보는 방향에 Wall 타일이 없고 플레이어가 그 방향에 위치하면
    /// 플레이어에게 타일 단위로 접근하여 인접 시 대화를 자동 시작합니다.
    ///
    /// 상태 머신:
    ///   IDLE    → 주기적으로 전방 시야 검사
    ///   CHASING → 플레이어를 향해 한 칸씩 이동
    ///   IDLE    ← 대화 완료 후 복귀 (또는 추격 실패)
    ///
    /// 필수 컴포넌트: Rigidbody2D, BoxCollider2D, NpcInteractable
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(NpcInteractable))]
    public class NpcChaser : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────
        [Header("감지 설정")]
        [Tooltip("초기 바라보는 방향 (down = 아래, up = 위, left/right)")]
        [SerializeField] private Vector2 initialFacingDir = Vector2.down;
        [Tooltip("시야 감지 최대 거리 (타일 단위)")]
        [SerializeField] private int detectionRange = 8;
        [Tooltip("시야 검사 주기 (초)")]
        [SerializeField] private float checkInterval = 0.25f;
        [Tooltip("벽 타일 판정에 사용할 Wall Tilemap. 비워두면 'Wall_Tilemap' 자동 탐색")]
        [SerializeField] private Tilemap wallTilemap;

        [Header("이동 설정")]
        [SerializeField] private float moveSpeed    = 3.5f;
        [SerializeField] private float moveUnitSize = 1f;
        [Tooltip("장애물 레이어 (기본: Ignore Raycast 제외 전체)")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("비주얼")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("스프라이트 기본 방향이 오른쪽이면 true로 설정")]
        public bool invertVisualFlip = false;

        [Header("1회 자동 추격 후 대화 교체")]
        [Tooltip("자동 추격 대화 완료 후 이 대화로 교체됩니다")]
        [SerializeField] private DialogueData afterChaseDialogueData;

        [Header("이벤트 연출")]
        [Tooltip("플레이어 발각 시 머리 위에 표시할 스프라이트")]
        [SerializeField] private Sprite eventMarkSprite;
        [Tooltip("자동 추격 대화가 끝난 후 시작할 전투 데이터. 비워두면 전투를 시작하지 않습니다.")]
        public EnemyData battleEnemyData;

        // ─── 애니메이션 상태 이름 ────────────────────────────────────
        private const string AnimWalkFront = "MaleNPC_WalkFront";
        private const string AnimWalkSide  = "MaleNPC_WalkSide";
        private const string AnimWalkBack  = "MaleNPC_WalkBack";
        private const string AnimIdleDown  = "MaleNPC_IdleDown";
        private const string AnimIdleSide  = "MaleNPC_IdleSide";
        private const string AnimIdleBack  = "MaleNPC_IdleBack";

        // ─── 런타임 상태 ─────────────────────────────────────────────
        private Rigidbody2D      rb;
        private BoxCollider2D    footCollider;
        private NpcInteractable  npcInteractable;
        private PlayerController player;
        private Animator         animator;

        private Vector2 targetPosition;
        private Vector2 currentFacingDir;
        private bool    isMoving;
        private bool    isChasing;
        private bool    hasAutoChased;

        // ══════════════════════════════════════════════════════════════
        //  Unity 생명주기
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            rb              = GetComponent<Rigidbody2D>();
            footCollider    = GetComponent<BoxCollider2D>();
            npcInteractable = GetComponent<NpcInteractable>();
            spriteRenderer  = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            animator        = GetComponent<Animator>();

            rb.gravityScale   = 0f;
            rb.freezeRotation = true;
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.interpolation  = RigidbodyInterpolation2D.Interpolate;

            if (obstacleLayer.value == 0)
                obstacleLayer = ~LayerMask.GetMask("Ignore Raycast");

            currentFacingDir = initialFacingDir.sqrMagnitude > 0.01f ? initialFacingDir.normalized : Vector2.down;

            ResolveMoveUnitSize();
            ConfigureFootCollider();
            AlignVisualToTile();
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            ResolveWallTilemap();

            // 그리드 스냅
            Vector2 snappedFoot = AdventureGridUtility.SnapToCellCenter(
                GetFootCenter(transform.position), moveUnitSize);
            targetPosition = GetRootPositionForFootCenter(snappedFoot);
            transform.position = targetPosition;
            rb.position        = targetPosition;
            GridOccupancy.TryReserve(snappedFoot, moveUnitSize);

            PlayIdleAnim();
            StartCoroutine(DetectionLoop());
        }

        private void OnDestroy()
        {
            GridOccupancy.Release(GetFootCenter(targetPosition), moveUnitSize);
        }

        private void FixedUpdate()
        {
            if (!isMoving) return;

            Vector2 next = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);

            if (Vector2.Distance(next, targetPosition) < 0.001f)
            {
                rb.MovePosition(targetPosition);
                isMoving = false;
                // 추격 중이 아닐 때만 idle 전환 (추격 중엔 walk가 타일 간 계속 재생)
                if (!isChasing)
                    PlayIdleAnim();
            }
        }

        private void LateUpdate()
        {
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = 10000 + (int)(spriteRenderer.bounds.min.y * -100);
        }

        // ══════════════════════════════════════════════════════════════
        //  감지 루프
        // ══════════════════════════════════════════════════════════════

        private IEnumerator DetectionLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);

                // 대화 중·추격 중·이미 1회 추격 완료면 검사 생략
                if (isChasing || hasAutoChased || IsDialogueActive()) continue;

                if (PlayerInFacingDirection())
                {
                    isChasing = true;
                    StartCoroutine(ChaseAndTalk());
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  추격 및 대화 코루틴
        // ══════════════════════════════════════════════════════════════

        private IEnumerator ChaseAndTalk()
        {
            // 혹시 대화 중이라면 종료까지 대기
            while (IsDialogueActive())
                yield return null;

            // 추격 시작: 이벤트 마크 연출 → 플레이어 이동 잠금 → walk 시작
            ShowEventMark();
            player?.SetInputEnabled(false);
            PlayWalkAnim(currentFacingDir);

            bool dialogueTriggered = false;

            // 플레이어 인접까지 이동
            while (!IsAdjacentToPlayer())
            {
                if (IsDialogueActive()) { yield return null; continue; }

                while (isMoving)
                    yield return null;

                Vector2 myFoot     = GetFootCenter(targetPosition);
                Vector2 playerFoot = GetPlayerFootCenter();
                Vector2 dir        = BestDirectionToward(myFoot, playerFoot);
                if (dir != Vector2.zero)
                    TryStep(dir);
                else
                    break; // 이동 불가

                yield return null;
            }

            while (isMoving)
                yield return null;

            // 플레이어와 인접하면 대화 시작
            if (IsAdjacentToPlayer())
            {
                PlayIdleAnim();    // 플레이어 앞에 멈춰 idle 전환
                FaceToward(GetPlayerFootCenter());
                yield return new WaitForSeconds(0.05f);
                TriggerDialogue();
                dialogueTriggered = true;

                // 대화 끝날 때까지 대기 (DialogueManager가 플레이어 이동을 복원함)
                while (IsDialogueActive())
                    yield return null;

                // 1회 자동 추격 완료 처리
                hasAutoChased = true;
                if (afterChaseDialogueData != null)
                    npcInteractable.SetDialogueData(afterChaseDialogueData);

                // 전투 데이터가 있으면 배틀 로드
                if (battleEnemyData != null)
                {
                    yield return new WaitForSeconds(0.2f); // 연출 유예
                    if (SceneLoader.Instance != null)
                    {
                        SceneLoader.Instance.EnterBattle(battleEnemyData);
                    }
                    else
                    {
                        // SceneLoader가 없으면 GameDataManager 세팅 후 씬 매뉴얼 로드 (폴백)
                        if (GameDataManager.Instance != null)
                            GameDataManager.Instance.PrepareBattle(battleEnemyData, "AdventureScene");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleTest");
                    }
                }
            }

            // 대화 없이 추격이 실패했으면 idle 전환 + 플레이어 이동 수동 복원
            if (!dialogueTriggered)
            {
                PlayIdleAnim();
                player?.SetInputEnabled(true);
            }

            isChasing = false;
        }

        // ══════════════════════════════════════════════════════════════
        //  시야 감지
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// 현재 바라보는 방향 직선에 벽 없이 플레이어가 있으면 true.
        /// </summary>
        private bool PlayerInFacingDirection()
        {
            if (player == null || wallTilemap == null) return false;

            Vector2 myFoot     = GetFootCenter(targetPosition);
            Vector2 playerFoot = GetPlayerFootCenter();
            Vector2 dir        = currentFacingDir;

            // 플레이어가 바라보는 축 방향에 정렬되어 있는지 확인
            bool isVertical   = Mathf.Abs(dir.y) > Mathf.Abs(dir.x);
            Vector2 toPlayer  = playerFoot - myFoot;

            if (isVertical)
            {
                // 세로 방향 감지: X 좌표가 비슷해야 함
                if (Mathf.Abs(toPlayer.x) > moveUnitSize * 0.6f) return false;
                // 바라보는 방향과 같은 Y 방향이어야 함
                if (Mathf.Sign(toPlayer.y) != Mathf.Sign(dir.y)) return false;
            }
            else
            {
                // 가로 방향 감지: Y 좌표가 비슷해야 함
                if (Mathf.Abs(toPlayer.y) > moveUnitSize * 0.6f) return false;
                if (Mathf.Sign(toPlayer.x) != Mathf.Sign(dir.x)) return false;
            }

            // 거리 검사
            if (toPlayer.magnitude > detectionRange * moveUnitSize + 0.5f) return false;

            // 방향을 따라 셀별로 Wall 확인
            for (int i = 1; i <= detectionRange; i++)
            {
                Vector2 checkPos = myFoot + dir * (i * moveUnitSize);

                // 벽 검사
                Vector3Int cell = wallTilemap.WorldToCell(new Vector3(checkPos.x, checkPos.y, 0f));
                if (wallTilemap.HasTile(cell)) return false; // 벽에 막힘

                // 플레이어 도달 검사
                if (Vector2.Distance(checkPos, playerFoot) < moveUnitSize * 0.6f)
                    return true;
            }

            return false;
        }

        // ══════════════════════════════════════════════════════════════
        //  이동
        // ══════════════════════════════════════════════════════════════

        private void TryStep(Vector2 dir)
        {
            Vector2 currentFoot   = GetFootCenter(targetPosition);
            Vector2 candidateFoot = currentFoot + AdventureGridUtility.GetCardinalStep(dir, moveUnitSize);

            // 플레이어 타일로는 진입하지 않음 (인접에서 멈춤)
            Vector2 playerFoot = GetPlayerFootCenter();
            if (Vector2.Distance(candidateFoot, playerFoot) < moveUnitSize * 0.5f) return;

            // 물리 검사 (자신 콜라이더 잠시 끔)
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols) c.enabled = false;
            Collider2D hit = Physics2D.OverlapBox(
                candidateFoot,
                AdventureGridUtility.GetCollisionProbeSize(moveUnitSize),
                0f, obstacleLayer);
            foreach (var c in cols) c.enabled = true;

            if (hit != null && !hit.isTrigger) return;

            // 타일 예약 검사
            if (!GridOccupancy.TryReserve(candidateFoot, moveUnitSize)) return;

            GridOccupancy.Release(currentFoot, moveUnitSize);
            targetPosition   = GetRootPositionForFootCenter(candidateFoot);
            isMoving         = true;
            currentFacingDir = dir;
            UpdateFacing(dir);
            PlayWalkAnim(dir);
        }

        /// <summary>목표 방향으로 최적 1축 방향 반환 (X 우선)</summary>
        private Vector2 BestDirectionToward(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0f ? Vector2.right : Vector2.left;
            else
                return delta.y > 0f ? Vector2.up : Vector2.down;
        }

        // ══════════════════════════════════════════════════════════════
        //  대화 트리거
        // ══════════════════════════════════════════════════════════════

        private void TriggerDialogue()
        {
            if (DialogueManager.Instance == null) return;
            if (!npcInteractable.CanInteract()) return;
            if (IsDialogueActive()) return;

            DialogueManager.Instance.BeginDialogueWithNpc(npcInteractable);
        }

        // ══════════════════════════════════════════════════════════════
        //  비주얼 / 방향
        // ══════════════════════════════════════════════════════════════

        private void UpdateFacing(Vector2 dir)
        {
            if (spriteRenderer == null || dir.x == 0f) return;
            bool flip = dir.x > 0f;
            if (invertVisualFlip) flip = !flip;
            spriteRenderer.flipX = flip;
        }

        public void FaceToward(Vector2 worldPos)
        {
            Vector2 delta = worldPos - (Vector2)transform.position;

            // 주 이동축 결정
            Vector2 cardinalDir;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                cardinalDir = delta.x >= 0f ? Vector2.right : Vector2.left;
            else
                cardinalDir = delta.y >= 0f ? Vector2.up : Vector2.down;

            currentFacingDir = cardinalDir;
            UpdateFacing(cardinalDir);
            PlayIdleAnim();
        }

        /// <summary>
        /// 플레이어 머리 위에 이벤트 마크를 빠르게 팝업한 뒤 서서히 페이드아웃합니다.
        /// </summary>
        private void ShowEventMark()
        {
            if (eventMarkSprite == null || player == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // 플레이어 머리 위 월드 좌표 → 스크린 좌표
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            float headWorldY = playerSr != null
                ? playerSr.bounds.max.y + 0.05f
                : player.transform.position.y + 1.2f;
            Vector3 headWorld  = new Vector3(player.transform.position.x, headWorldY, 0f);
            Vector2 screenPos  = cam.WorldToScreenPoint(headWorld);

            // ScreenSpace-Overlay Canvas → 타일맵·스프라이트 위에 무조건 표시됨
            var canvasGo = new GameObject("EventMark_Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGo.AddComponent<CanvasScaler>();

            // Image 생성
            var imgGo = new GameObject("EventMark_Image");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var img = imgGo.AddComponent<Image>();
            img.sprite             = eventMarkSprite;
            img.preserveAspect     = true;
            img.color              = Color.white;
            img.raycastTarget      = false;

            // 화면 높이 기준 크기 설정 (8%)
            float size = Screen.height * 0.08f;
            var rt     = imgGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            // 스크린 좌표 → 캔버스 로컬 좌표 (ScreenSpace-Overlay 는 camera=null)
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, null, out Vector2 localPos);
            rt.anchoredPosition = localPos;
            rt.localScale       = Vector3.zero;

            // 빠르게 팝업 (OutBack) → 서서히 페이드아웃 (InQuad)
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.25f);
            seq.Append(img.DOFade(0f, 0.65f).SetEase(Ease.InQuad));
            seq.OnComplete(() => { if (canvasGo != null) Object.Destroy(canvasGo); });
        }

        private void PlayWalkAnim(Vector2 dir)
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) return;
            if      (dir.y < 0f) animator.Play(AnimWalkFront, 0);
            else if (dir.y > 0f) animator.Play(AnimWalkBack,  0);
            else                  animator.Play(AnimWalkSide,  0);
        }

        private void PlayIdleAnim()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) return;
            if      (currentFacingDir.y < 0f) animator.Play(AnimIdleDown, 0);
            else if (currentFacingDir.y > 0f) animator.Play(AnimIdleBack, 0);
            else                               animator.Play(AnimIdleSide, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  초기화 헬퍼
        // ══════════════════════════════════════════════════════════════

        private void ResolveMoveUnitSize()
        {
            Grid grid = FindFirstObjectByType<Grid>();
            if (grid == null) return;
            float size = Mathf.Abs(grid.cellSize.x) > 0.001f ? Mathf.Abs(grid.cellSize.x) : Mathf.Abs(grid.cellSize.y);
            if (size > 0.001f) moveUnitSize = size;
        }

        private void ResolveWallTilemap()
        {
            if (wallTilemap != null) return;
            var obj = GameObject.Find("Wall_Tilemap");
            if (obj != null) wallTilemap = obj.GetComponent<Tilemap>();
        }

        private void ConfigureFootCollider()
        {
            Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
            AdventureGridUtility.ConfigureFootCollider(footCollider, transform, spriteRenderer, cellSize);
            AdventureGridUtility.DisableSolidCircles(gameObject);
        }

        private void AlignVisualToTile()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            float scale = AdventureGridUtility.GetVisualScaleForReferenceHeight(spriteRenderer.sprite);
            if (spriteRenderer.transform == transform)
            {
                transform.localScale = new Vector3(scale, scale, transform.localScale.z);
            }
            else
            {
                spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
                Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
                float finalH = spriteRenderer.sprite.bounds.size.y * scale;
                Vector3 lp = spriteRenderer.transform.localPosition;
                lp.y = (finalH - cellSize.y) * 0.5f;
                spriteRenderer.transform.localPosition = lp;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  좌표 헬퍼
        // ══════════════════════════════════════════════════════════════

        private Vector2 GetFootCenter(Vector2 root) =>
            AdventureGridUtility.GetFootCenter(root, transform, footCollider);

        private Vector2 GetRootPositionForFootCenter(Vector2 foot) =>
            AdventureGridUtility.GetRootPositionForFootCenter(foot, transform, footCollider);

        private Vector2 GetPlayerFootCenter()
        {
            if (player == null) return transform.position;
            BoxCollider2D playerBox = player.GetComponent<BoxCollider2D>();
            return AdventureGridUtility.GetFootCenter(player.transform.position, player.transform, playerBox);
        }

        private bool IsAdjacentToPlayer()
        {
            if (player == null) return false;
            return Vector2.Distance(GetFootCenter(targetPosition), GetPlayerFootCenter()) < moveUnitSize + 0.15f;
        }

        private static bool IsDialogueActive() =>
            DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = isChasing ? Color.red : new Color(1f, 0.8f, 0f, 0.6f);
            Vector2 foot = footCollider != null
                ? GetFootCenter(transform.position)
                : (Vector2)transform.position;

            for (int i = 1; i <= detectionRange; i++)
            {
                Gizmos.DrawWireCube(foot + currentFacingDir * (i * moveUnitSize), Vector3.one * 0.3f);
            }
        }
#endif
    }
}
