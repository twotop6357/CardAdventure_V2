using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 전투 시작 전 NPC 등장 연출을 담당한다.
    ///
    /// ┌─ 연출 흐름 ────────────────────────────────────────────────────┐
    /// │ 1. BattleManager.BattleStarted 이벤트 수신                      │
    /// │ 2. NPC 초상화 패널이 화면 오른쪽 밖에서 슬라이드 인              │
    /// │ 3. 대화창 표시                                                    │
    /// │    - BattleIntroData.dialogueData가 있으면 그 대사 사용           │
    /// │    - 없으면 defaultSummonMessage 표시                             │
    /// │ 4. Space 입력 → 대화 진행 (타이핑 중이면 스킵)                   │
    /// │ 5. 마지막 줄 이후 Space → 초상화 슬라이드 아웃 (오른쪽으로)      │
    /// │ 6. BattleManager.BeginPlayerTurn() 호출 → 본격 전투 시작         │
    /// └────────────────────────────────────────────────────────────────┘
    ///
    /// 확장성:
    ///   - SetIntroData()로 런타임에 인트로 데이터 교체 가능
    ///     → 어드벤처 씬의 NPC 대화 후 배틀 씬 진입 시 활용
    ///   - EnemyData.introData가 있으면 자동 우선 사용
    ///   - defaultIntroData 필드로 Inspector 기본값 설정 가능
    ///
    /// 전제 조건:
    ///   - BattleManager.waitForIntroDirector = true 로 설정해야
    ///     BeginPlayerTurn() 호출이 지연된다.
    ///   - npcPortraitPanel과 dialogueView는 Inspector에서 반드시 연결해야 한다.
    /// </summary>
    public class BattleIntroDirector : MonoBehaviour
    {
        // ── Inspector 직렬화 ───────────────────────────────────────

        [Header("핵심 참조")]
        [SerializeField] private BattleManager battleManager;
        [Tooltip("어드벤처 씬과 동일한 스타일의 DialogueView 컴포넌트")]
        [SerializeField] private DialogueView   dialogueView;

        [Header("NPC 초상화 UI")]
        [Tooltip("화면 우측에 등장하는 NPC 초상화 패널 (RectTransform)")]
        [SerializeField] private RectTransform npcPortraitPanel;
        [Tooltip("초상화 이미지가 할당될 Image 컴포넌트")]
        [SerializeField] private Image         npcPortraitImage;

        [Header("캐릭터 등장 UI")]
        [Tooltip("플레이어 캐릭터 RectTransform (왼쪽에서 슬라이드 인)")]
        [SerializeField] private RectTransform playerVisual;
        [Tooltip("몬스터 캐릭터 RectTransform (오른쪽에서 슬라이드 인)")]
        [SerializeField] private RectTransform enemyVisual;

        [Header("연출 설정")]
        [SerializeField] private float slideInDuration      = 0.45f;
        [SerializeField] private float slideOutDuration     = 0.35f;
        [Tooltip("대화창이 닫힌 후 슬라이드 아웃 전 짧은 대기 시간 (초)")]
        [SerializeField] private float afterDialogueDelay   = 0.25f;
        [Tooltip("화면 밖 오른쪽 오프셋 (px). 패널 너비보다 큰 값 권장.")]
        [SerializeField] private float offscreenOffsetX     = 600f;
        [Tooltip("캐릭터 등장 애니메이션 시간 (초)")]
        [SerializeField] private float entranceDuration     = 0.55f;
        [Tooltip("캐릭터 시작 위치 오프셋 (px). 화면 바깥 충분히 먼 거리.")]
        [SerializeField] private float entranceSlideDistance = 1600f;

        [Header("기본 인트로 데이터 (폴백)")]
        [Tooltip("EnemyData.introData와 런타임 주입 데이터가 모두 없을 때 사용한다.")]
        [SerializeField] private BattleIntroData defaultIntroData;

        // ── 런타임 상태 ────────────────────────────────────────────

        private BattleIntroData runtimeIntroData;  // 외부 주입 데이터 (SetIntroData)
        private Vector2 playerOriginalPos;
        private Vector2 enemyOriginalPos;
        private bool   isIntroActive;
        private int    currentLineIndex;
        private string[] currentLines;
        private string currentSpeaker;

        // ── 프로퍼티 ──────────────────────────────────────────────

        /// <summary>
        /// 이번 전투에서 사용할 인트로 데이터.
        /// 우선순위: runtimeIntroData → EnemyData.introData → defaultIntroData
        /// </summary>
        private BattleIntroData ActiveIntroData
        {
            get
            {
                if (runtimeIntroData != null)
                    return runtimeIntroData;

                if (battleManager?.Enemy?.Data?.introData != null)
                    return battleManager.Enemy.Data.introData;

                return defaultIntroData;
            }
        }

        // ── 라이프사이클 ───────────────────────────────────────────

        private void Awake()
        {
            if (battleManager == null)
                battleManager = FindFirstObjectByType<BattleManager>();

            // 초상화 패널을 화면 오른쪽 밖으로 이동 후 비활성화
            if (npcPortraitPanel != null)
            {
                npcPortraitPanel.anchoredPosition = new Vector2(offscreenOffsetX,
                    npcPortraitPanel.anchoredPosition.y);
                npcPortraitPanel.gameObject.SetActive(false);
            }

            // 플레이어·몬스터 원래 위치 캐싱 후 화면 밖으로 이동
            if (playerVisual != null)
            {
                playerOriginalPos = playerVisual.anchoredPosition;
                playerVisual.anchoredPosition = new Vector2(
                    playerOriginalPos.x - entranceSlideDistance,
                    playerOriginalPos.y);
            }

            if (enemyVisual != null)
            {
                enemyOriginalPos = enemyVisual.anchoredPosition;
                enemyVisual.anchoredPosition = new Vector2(
                    enemyOriginalPos.x + entranceSlideDistance,
                    enemyOriginalPos.y);
            }
        }

        private void OnEnable()
        {
            if (battleManager != null)
                battleManager.BattleStarted += OnBattleStarted;
        }

        private void OnDisable()
        {
            if (battleManager != null)
                battleManager.BattleStarted -= OnBattleStarted;
        }

        private void Update()
        {
            if (!isIntroActive) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                HandleSpaceInput();
        }

        // ── 공개 API ──────────────────────────────────────────────

        /// <summary>
        /// 런타임에 인트로 데이터를 주입한다.
        /// 어드벤처 씬 → 배틀 씬 전환 시 BattleSceneConnector 등에서 호출한다.
        /// </summary>
        public void SetIntroData(BattleIntroData data)
        {
            runtimeIntroData = data;
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────

        private void OnBattleStarted(BattleManager manager)
        {
            StartCoroutine(PlayIntroSequence());
        }

        // ── 코루틴 ───────────────────────────────────────────────

        private IEnumerator PlayIntroSequence()
        {
            isIntroActive = true;

            // 1. NPC 초상화 스프라이트 설정
            BattleIntroData data = ActiveIntroData;
            SetupPortrait(data);

            // 2. 초상화 패널 슬라이드 인 (오른쪽 밖 → 화면 오른쪽)
            npcPortraitPanel.gameObject.SetActive(true);
            npcPortraitPanel.anchoredPosition = new Vector2(offscreenOffsetX,
                npcPortraitPanel.anchoredPosition.y);

            yield return npcPortraitPanel
                .DOAnchorPosX(0f, slideInDuration)
                .SetEase(Ease.OutCubic)
                .WaitForCompletion();

            // 3. 대화 내용 준비 및 첫 줄 표시
            PrepareDialogue(data);
            ShowCurrentLine();

            // 이후 진행은 Update() → HandleSpaceInput() → FinishIntro() 가 담당
        }

        private IEnumerator FinishIntro()
        {
            // 대화창 닫기
            dialogueView?.Hide();

            yield return new WaitForSeconds(afterDialogueDelay);

            // 초상화 슬라이드 아웃 (화면 오른쪽 → 밖으로)
            yield return npcPortraitPanel
                .DOAnchorPosX(offscreenOffsetX, slideOutDuration)
                .SetEase(Ease.InCubic)
                .WaitForCompletion();

            npcPortraitPanel.gameObject.SetActive(false);

            // 플레이어·몬스터 동시 등장 애니메이션
            if (playerVisual != null || enemyVisual != null)
            {
                Sequence entranceSeq = DOTween.Sequence();

                if (playerVisual != null)
                    entranceSeq.Join(
                        playerVisual.DOAnchorPos(playerOriginalPos, entranceDuration)
                                    .SetEase(Ease.OutCubic));

                if (enemyVisual != null)
                    entranceSeq.Join(
                        enemyVisual.DOAnchorPos(enemyOriginalPos, entranceDuration)
                                   .SetEase(Ease.OutCubic));

                yield return entranceSeq.WaitForCompletion();
            }

            // 전투 본격 시작 (BattleHandView 카드 딜 포함)
            battleManager?.BeginPlayerTurn();
        }

        // ── 대화 진행 ────────────────────────────────────────────

        private void HandleSpaceInput()
        {
            // 타이핑 진행 중 → 즉시 완성
            if (dialogueView != null && dialogueView.IsTyping)
            {
                dialogueView.SkipTypewriter();
                return;
            }

            // 다음 줄로 이동
            currentLineIndex++;

            if (currentLineIndex < currentLines.Length)
            {
                ShowCurrentLine();
            }
            else
            {
                // 모든 대화 완료 → 아웃트로 시작
                isIntroActive = false;   // Update() 입력 차단
                StartCoroutine(FinishIntro());
            }
        }

        private void PrepareDialogue(BattleIntroData data)
        {
            currentLineIndex = 0;

            if (data != null)
            {
                currentSpeaker = data.GetSpeakerName();
                currentLines   = data.GetLines();
            }
            else
            {
                currentSpeaker = "";
                currentLines   = new[] { "몬스터가 나타났다!" };
            }
        }

        private void ShowCurrentLine()
        {
            if (dialogueView == null) return;

            if (currentLineIndex == 0)
                dialogueView.Show(currentSpeaker, currentLines[0]);
            else
                dialogueView.ShowLine(currentLines[currentLineIndex]);
        }

        // ── 헬퍼 ────────────────────────────────────────────────

        private void SetupPortrait(BattleIntroData data)
        {
            if (npcPortraitImage == null) return;

            if (data?.npcPortrait != null)
                npcPortraitImage.sprite = data.npcPortrait;
        }
    }
}
