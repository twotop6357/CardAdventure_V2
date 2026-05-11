/*
 * BattleIntroSetup.cs  (Editor 전용)
 *
 * 메뉴 경로:
 *   CardAdventure > Setup Battle Intro
 *       → 현재 열린 씬에 전투 인트로 연출에 필요한 UI 요소를 자동으로 구성한다.
 *
 * 생성/구성 대상:
 *   1. BattleIntroCanvas
 *         └─ NpcPortraitPanel  (화면 오른쪽 앵커, 초상화 이미지 패널)
 *                └─ NpcPortraitImage  (Image 컴포넌트)
 *
 *   2. BattleDialogueCanvas
 *         → Assets/Prefabs/UI/DialogueCanvas.prefab 을 인스턴스화
 *           (어드벤처 씬 대화창과 완전히 동일한 외형 — 스프라이트/폰트/레이아웃 공유)
 *
 *   3. BattleIntroDirector 컴포넌트 → BattleManager 오브젝트에 추가
 *       (이미 있으면 재사용)
 *
 *   4. BattleIntroData 에셋 → Assets/ScriptableObjects/BattleIntros/ 자동 생성
 *       (examiner_Image.png 스프라이트를 자동 할당, 이미 있으면 재사용)
 *
 * 사용법:
 *   1. BattleTest 씬을 Unity Editor에서 연다.
 *   2. 메뉴: CardAdventure > Setup Battle Intro 실행.
 *   3. BattleManager Inspector에서 waitForIntroDirector = true 체크.
 *   4. BattleManager.startOnAwake = true 확인.
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure.Editor
{
    public static class BattleIntroSetup
    {
        // ── 상수 ─────────────────────────────────────────────────
        private const string INTRO_CANVAS_NAME    = "BattleIntroCanvas";
        private const string PORTRAIT_PANEL_NAME  = "NpcPortraitPanel";
        private const string PORTRAIT_IMG_NAME    = "NpcPortraitImage";
        private const string DIALOGUE_CANVAS_NAME = "BattleDialogueCanvas";
        private const string DECK_MARKER_NAME     = "DeckOriginMarker";

        // 어드벤처 씬 대화창 프리팹 경로
        private const string DIALOGUE_PREFAB_PATH = "Assets/Prefabs/UI/DialogueCanvas.prefab";

        // 초상화 패널 너비 (px)
        private const float PORTRAIT_WIDTH = 460f;

        private const string TEST_INTRO_ASSET_PATH =
            "Assets/ScriptableObjects/BattleIntros/BattleIntro_Examiner.asset";
        private const string EXAMINER_SPRITE_PATH =
            "Assets/Assets/Sprites/Images/examiner_Image.png";

        // 레이아웃 조정: 대상 오브젝트 이름
        private const string PLAYER_AVATAR_NAME = "PlayerAvatar";
        private const string ENEMY_AREA_NAME    = "EnemyArea";

        // ── 메뉴 진입점 ──────────────────────────────────────────

        [MenuItem("CardAdventure/Adjust Battle Layout")]
        public static void AdjustBattleLayout()
        {
            bool changed = false;

            // ── PlayerAvatar 앵커 Y 하향 ─────────────────────────
            GameObject playerAvatarGo = GameObject.Find(PLAYER_AVATAR_NAME);
            if (playerAvatarGo != null)
            {
                RectTransform rt = playerAvatarGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(rt.anchorMin.x, 0.42f);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, 0.42f);
                    EditorUtility.SetDirty(playerAvatarGo);
                    changed = true;
                    Debug.Log($"[BattleIntroSetup] {PLAYER_AVATAR_NAME} 앵커 Y → 0.42");
                }
            }
            else
            {
                Debug.LogWarning($"[BattleIntroSetup] '{PLAYER_AVATAR_NAME}' 오브젝트를 찾을 수 없습니다.");
            }

            // ── EnemyArea 앵커 Y 하향 ────────────────────────────
            GameObject enemyAreaGo = GameObject.Find(ENEMY_AREA_NAME);
            if (enemyAreaGo != null)
            {
                RectTransform rt = enemyAreaGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(rt.anchorMin.x, 0.44f);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, 0.44f);
                    EditorUtility.SetDirty(enemyAreaGo);
                    changed = true;
                    Debug.Log($"[BattleIntroSetup] {ENEMY_AREA_NAME} 앵커 Y → 0.44");
                }
            }
            else
            {
                Debug.LogWarning($"[BattleIntroSetup] '{ENEMY_AREA_NAME}' 오브젝트를 찾을 수 없습니다.");
            }

            // ── DeckOriginMarker 생성 ─────────────────────────────
            EnsureDeckOriginMarker(playerAvatarGo);

            if (changed) MarkSceneDirty();
            Debug.Log("[BattleIntroSetup] ✅ 레이아웃 조정 완료!");
        }

        [MenuItem("CardAdventure/Setup Battle Intro")]
        public static void SetupBattleIntro()
        {
            // 1) NPC 초상화 캔버스 + 패널
            GameObject portraitCanvas = EnsurePortraitCanvas();
            GameObject portraitPanel  = BuildPortraitPanel(portraitCanvas);

            // 2) 대화 캔버스 — 어드벤처 씬 프리팹 인스턴스화
            GameObject dialogueCanvas = EnsureDialogueCanvasFromPrefab();
            DialogueView dialogueView = dialogueCanvas != null
                ? dialogueCanvas.GetComponentInChildren<DialogueView>(true)
                : null;

            // 3) BattleIntroDirector 컴포넌트 설정
            BattleIntroDirector director = EnsureIntroDirector();

            // 캐릭터 비주얼 참조
            RectTransform playerVis = GameObject.Find(PLAYER_AVATAR_NAME)
                ?.GetComponent<RectTransform>();
            RectTransform enemyVis  = GameObject.Find(ENEMY_AREA_NAME)
                ?.GetComponent<RectTransform>();

            // DeckOriginMarker 확보 (없으면 생성)
            RectTransform deckMarker = EnsureDeckOriginMarker(
                GameObject.Find(PLAYER_AVATAR_NAME));

            LinkDirectorRefs(director, dialogueView,
                portraitPanel?.GetComponent<RectTransform>(),
                portraitPanel?.GetComponentInChildren<Image>(),
                playerVis, enemyVis);

            // BattleHandView 에 DeckOriginMarker 연결
            BattleHandView handView = Object.FindFirstObjectByType<BattleHandView>();
            if (handView != null && deckMarker != null)
            {
                var soHand = new SerializedObject(handView);
                soHand.FindProperty("deckOriginMarker").objectReferenceValue = deckMarker;
                soHand.ApplyModifiedProperties();
                EditorUtility.SetDirty(handView);
            }

            // 4) 테스트용 BattleIntroData 에셋 생성 및 디렉터에 연결
            BattleIntroData introAsset = EnsureTestIntroData();
            if (introAsset != null && director != null)
            {
                var so = new SerializedObject(director);
                so.FindProperty("defaultIntroData").objectReferenceValue = introAsset;
                so.ApplyModifiedProperties();
            }

            // 씬 저장 마킹
            MarkSceneDirty();

            Debug.Log("[BattleIntroSetup] ✅ 설정 완료! " +
                      "BattleManager Inspector에서 waitForIntroDirector = true 로 설정해 주세요.");
        }

        // ── 1. 초상화 캔버스 ──────────────────────────────────────

        private static GameObject EnsurePortraitCanvas()
        {
            GameObject existing = GameObject.Find(INTRO_CANVAS_NAME);
            if (existing != null) return existing;

            GameObject go = new GameObject(INTRO_CANVAS_NAME);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // BattleCanvas(0)보다 위, DialogueCanvas(50)보다 아래

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);   // 프리팹과 동일
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject BuildPortraitPanel(GameObject canvasGo)
        {
            if (canvasGo == null) return null;

            Transform existing = canvasGo.transform.Find(PORTRAIT_PANEL_NAME);
            if (existing != null) return existing.gameObject;

            // ── NpcPortraitPanel ──────────────────────────────────
            GameObject panelGo = new GameObject(PORTRAIT_PANEL_NAME);
            panelGo.transform.SetParent(canvasGo.transform, false);

            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(1f, 0f);
            panelRt.anchorMax        = new Vector2(1f, 1f);
            panelRt.pivot            = new Vector2(1f, 0.5f);
            panelRt.anchoredPosition = new Vector2(PORTRAIT_WIDTH + 50f, 0f); // 초기 숨김
            panelRt.sizeDelta        = new Vector2(PORTRAIT_WIDTH, 0f);       // 높이 스트레치

            // ── NpcPortraitImage ──────────────────────────────────
            GameObject imgGo = new GameObject(PORTRAIT_IMG_NAME);
            imgGo.transform.SetParent(panelGo.transform, false);

            RectTransform imgRt = imgGo.AddComponent<RectTransform>();
            imgRt.anchorMin        = Vector2.zero;
            imgRt.anchorMax        = Vector2.one;
            imgRt.sizeDelta        = Vector2.zero;
            imgRt.anchoredPosition = Vector2.zero;

            Image img = imgGo.AddComponent<Image>();
            img.preserveAspect = true;

            return panelGo;
        }

        // ── 2. 대화 캔버스 (어드벤처 씬 프리팹 인스턴스화) ──────

        /// <summary>
        /// Assets/Prefabs/UI/DialogueCanvas.prefab 을 인스턴스화하여
        /// 어드벤처 씬과 완전히 동일한 외형의 대화창을 배틀 씬에 생성한다.
        ///
        /// 이미 BattleDialogueCanvas가 씬에 있고 프리팹 연결이 유효하면 재사용.
        /// 수동으로 생성된 기존 오브젝트가 있으면 삭제 후 프리팹으로 교체.
        /// </summary>
        private static GameObject EnsureDialogueCanvasFromPrefab()
        {
            // ── 기존 오브젝트 확인 ────────────────────────────────
            GameObject existing = GameObject.Find(DIALOGUE_CANVAS_NAME);
            if (existing != null)
            {
                // 프리팹 연결이 살아있으면 그대로 재사용
                if (PrefabUtility.GetPrefabAssetType(existing) != PrefabAssetType.NotAPrefab)
                {
                    Debug.Log("[BattleIntroSetup] 기존 BattleDialogueCanvas(프리팹) 재사용.");
                    return existing;
                }

                // 수동 생성본은 제거 후 프리팹으로 교체
                Debug.Log("[BattleIntroSetup] 기존 BattleDialogueCanvas(수동)를 삭제하고 프리팹으로 교체합니다.");
                Object.DestroyImmediate(existing);
            }

            // ── 프리팹 로드 ───────────────────────────────────────
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DIALOGUE_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[BattleIntroSetup] DialogueCanvas 프리팹을 찾을 수 없습니다: {DIALOGUE_PREFAB_PATH}\n" +
                    "어드벤처 씬에서 대화창을 먼저 프리팹으로 저장해 주세요.");
                return null;
            }

            // ── 프리팹 인스턴스화 ────────────────────────────────
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = DIALOGUE_CANVAS_NAME;

            // SortingOrder: 배틀 씬에서도 최상위 (프리팹 기본값 50 유지)
            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 50;

            Debug.Log($"[BattleIntroSetup] {DIALOGUE_PREFAB_PATH} 인스턴스화 완료 → {DIALOGUE_CANVAS_NAME}");
            return instance;
        }

        // ── 3. BattleIntroDirector ────────────────────────────────

        private static BattleIntroDirector EnsureIntroDirector()
        {
            BattleIntroDirector existing =
                Object.FindFirstObjectByType<BattleIntroDirector>();
            if (existing != null) return existing;

            BattleManager bm = Object.FindFirstObjectByType<BattleManager>();
            GameObject target = bm != null
                ? bm.gameObject
                : new GameObject("BattleIntroDirector");

            return target.AddComponent<BattleIntroDirector>();
        }

        private static void LinkDirectorRefs(BattleIntroDirector director,
            DialogueView dialogueView,
            RectTransform portraitPanel,
            Image portraitImage,
            RectTransform playerVisual  = null,
            RectTransform enemyVisual   = null)
        {
            if (director == null) return;

            var so = new SerializedObject(director);

            BattleManager bm = Object.FindFirstObjectByType<BattleManager>();
            if (bm != null)
                so.FindProperty("battleManager").objectReferenceValue = bm;

            if (dialogueView != null)
                so.FindProperty("dialogueView").objectReferenceValue = dialogueView;

            if (portraitPanel != null)
                so.FindProperty("npcPortraitPanel").objectReferenceValue = portraitPanel;

            if (portraitImage != null)
                so.FindProperty("npcPortraitImage").objectReferenceValue = portraitImage;

            if (playerVisual != null)
                so.FindProperty("playerVisual").objectReferenceValue = playerVisual;

            if (enemyVisual != null)
                so.FindProperty("enemyVisual").objectReferenceValue = enemyVisual;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(director);
        }

        // ── DeckOriginMarker ──────────────────────────────────────

        /// <summary>
        /// BattleCanvas(또는 PlayerAvatar의 부모 캔버스) 아래에
        /// DeckOriginMarker RectTransform을 생성한다 (우하단 앵커, -30,30).
        /// </summary>
        private static RectTransform EnsureDeckOriginMarker(GameObject hintGo)
        {
            // 이미 존재하면 재사용
            GameObject existing = GameObject.Find(DECK_MARKER_NAME);
            if (existing != null) return existing.GetComponent<RectTransform>();

            // 부모 캔버스 탐색: hintGo 조상 Canvas → 없으면 BattleCanvas 이름으로
            Canvas parentCanvas = null;
            if (hintGo != null)
                parentCanvas = hintGo.GetComponentInParent<Canvas>(true);

            if (parentCanvas == null)
            {
                // 씬 전체에서 BattleCanvas 이름으로 탐색
                GameObject bc = GameObject.Find("BattleCanvas");
                if (bc != null) parentCanvas = bc.GetComponent<Canvas>();
            }

            if (parentCanvas == null)
            {
                // 마지막 폴백: 씬 첫 번째 Canvas
                parentCanvas = Object.FindFirstObjectByType<Canvas>();
            }

            if (parentCanvas == null)
            {
                Debug.LogWarning("[BattleIntroSetup] DeckOriginMarker를 붙일 Canvas를 찾지 못했습니다.");
                return null;
            }

            GameObject markerGo = new GameObject(DECK_MARKER_NAME);
            markerGo.transform.SetParent(parentCanvas.transform, false);

            RectTransform rt = markerGo.AddComponent<RectTransform>();
            // 우하단 앵커, 화면 모서리에서 (30, 30) 안쪽
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-30f, 30f);
            rt.sizeDelta        = Vector2.zero;

            Debug.Log($"[BattleIntroSetup] DeckOriginMarker 생성 → {parentCanvas.name}");
            return rt;
        }

        // ── 4. 테스트용 BattleIntroData 에셋 ─────────────────────

        private static BattleIntroData EnsureTestIntroData()
        {
            BattleIntroData existing =
                AssetDatabase.LoadAssetAtPath<BattleIntroData>(TEST_INTRO_ASSET_PATH);
            if (existing != null) return existing;

            const string folder = "Assets/ScriptableObjects/BattleIntros";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "BattleIntros");

            BattleIntroData data = ScriptableObject.CreateInstance<BattleIntroData>();
            data.speakerName          = "릴라";
            data.defaultSummonMessage = "어디 한번 실력을 보여줘 봐!";

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EXAMINER_SPRITE_PATH);
            if (sprite != null)
                data.npcPortrait = sprite;
            else
                Debug.LogWarning(
                    $"[BattleIntroSetup] 스프라이트를 찾을 수 없습니다: {EXAMINER_SPRITE_PATH}");

            AssetDatabase.CreateAsset(data, TEST_INTRO_ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BattleIntroSetup] BattleIntroData 에셋 생성: {TEST_INTRO_ASSET_PATH}");
            return data;
        }

        // ── 헬퍼 ─────────────────────────────────────────────────

        private static void MarkSceneDirty()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
