using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using CardAdventure;

/// <summary>
/// BattleTest 씬에 배틀 UI Canvas 계층을 자동 생성하는 Editor 빌더.
/// 메뉴: CardAdventure > Build Battle UI Scene
/// </summary>
public static class BattleSceneBuilder
{
    private const string CARD_VIEW_PREFAB_PATH   = "Assets/Prefabs/UI/Card.prefab";
    private const string STATUS_ICON_PREFAB_PATH = "Assets/Prefabs/UI/StatusIcon.prefab";
    private const string BATTLE_BACKGROUND_PATH  = "Assets/Assets/Sprites/BattleBackground/CrowBattleBackground.png";
    private const string MAGIC_CROW_SPRITE_PATH  = "Assets/Assets/Sprites/Enemy/Monster_MagicCrow.png";
    private const string PLAYER_IDLE_SPRITE_PATH = "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleFront.png";

    [MenuItem("CardAdventure/Build Battle UI Scene")]
    public static void BuildBattleUIScene()
    {
        // 기존 Canvas 제거
        var existing = GameObject.Find("BattleCanvas");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[BattleSceneBuilder] 기존 BattleCanvas 제거.");
        }

        // ── 프리팹 먼저 생성 ──────────────────────────────────────
        EnsurePrefabFolders();
        GameObject cardViewPrefab   = EnsureCardViewPrefab();
        GameObject statusIconPrefab = EnsureStatusIconPrefab();

        // ── Canvas 루트 ───────────────────────────────────────────
        GameObject canvasGO = new GameObject("BattleCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Battle UI");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 배경 ──────────────────────────────────────────────────
        GameObject bg = CreatePanel("Background", canvasGO.transform,
            new Color(0.12f, 0.10f, 0.16f, 1f), Vector2.zero, Vector2.one,
            Vector2.zero, new Vector2(0, 0));
        bg.GetComponent<Image>().raycastTarget = false;

        // ── FadeMask ──────────────────────────────────────────────
        GameObject fadeMask = CreatePanel("FadeMask", canvasGO.transform,
            Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fadeMask.AddComponent<CanvasGroup>();
        fadeMask.GetComponent<Image>().raycastTarget = false;

        // ── 적 영역 ───────────────────────────────────────────────
        GameObject enemyArea = CreateEmpty("EnemyArea", canvasGO.transform);
        SetRectAnchored(enemyArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 80), new Vector2(420, 320));
        BattleEnemyView enemyView = enemyArea.AddComponent<BattleEnemyView>();

        // 적 이미지
        GameObject enemyImg = CreateImageObject("EnemyImage", enemyArea.transform,
            Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 40), new Vector2(200, 200));
        enemyImg.GetComponent<Image>().preserveAspect = true;

        // 적 이름
        GameObject enemyName = CreateTMP("NameText", enemyArea.transform,
            "슬라임", 22, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(300, 35));

        // 적 HP 슬라이더
        GameObject enemyHpSlider = CreateHpSlider("EnemyHpSlider", enemyArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(360, 20));

        // 적 HP 텍스트
        GameObject enemyHpText = CreateTMP("EnemyHpText", enemyArea.transform,
            "30 / 30", 18, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(200, 28));

        // 적 방어막 패널
        GameObject enemyBlockPanel = CreatePanel("BlockPanel", enemyArea.transform,
            new Color(0.3f, 0.5f, 0.9f, 0.85f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50, 55), new Vector2(64, 36));
        GameObject enemyBlockText = CreateTMP("BlockText", enemyBlockPanel.transform,
            "0", 18, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 적 상태이상 컨테이너
        GameObject enemyStatusCont = CreateHorizontalGroup("StatusContainer", enemyArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 58), new Vector2(360, 36));

        // 적 의도 패널
        GameObject intentPanel = CreatePanel("IntentPanel", enemyArea.transform,
            new Color(0.9f, 0.2f, 0.2f, 0.85f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(260, 42));
        GameObject intentIcon = CreateImageObject("IntentIcon", intentPanel.transform,
            Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(30, 0), new Vector2(30, 30));
        GameObject intentText = CreateTMP("IntentText", intentPanel.transform,
            "공격 6", 16, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(22, 0), new Vector2(-76, 0));

        // EnemyView 필드 연결
        SerializeEnemyView(enemyView,
            enemyImg.GetComponent<Image>(),
            enemyName.GetComponent<TextMeshProUGUI>(),
            enemyHpSlider.GetComponent<Slider>(),
            enemyHpText.GetComponent<TextMeshProUGUI>(),
            enemyBlockPanel,
            enemyBlockText.GetComponentInChildren<TextMeshProUGUI>(),
            enemyStatusCont.GetComponent<RectTransform>(),
            intentPanel,
            intentIcon.GetComponent<Image>(),
            intentText.GetComponent<TextMeshProUGUI>(),
            statusIconPrefab?.GetComponent<BattleStatusIconView>());

        // ── 플레이어 HUD ──────────────────────────────────────────
        GameObject playerHudGO = CreateEmpty("PlayerHUD", canvasGO.transform);
        SetRectAnchored(playerHudGO,
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(20, 20), new Vector2(380, 180));
        BattleHudView hudView = playerHudGO.AddComponent<BattleHudView>();

        // HUD 배경 패널
        GameObject hudBg = CreatePanel("HudBackground", playerHudGO.transform,
            new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hudBg.GetComponent<Image>().raycastTarget = false;

        // HP 슬라이더
        GameObject playerHpSlider = CreateHpSlider("HpSlider", playerHudGO.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -20), new Vector2(0, 20),
            isStretchX: true);

        // HP 텍스트
        GameObject playerHpText = CreateTMP("HpText", playerHudGO.transform,
            "50 / 50", 18, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10, -44), new Vector2(140, 26));

        // 에너지
        GameObject energyText = CreateTMP("EnergyText", playerHudGO.transform,
            "3 / 3", 22, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10, -75), new Vector2(100, 30));
        energyText.GetComponent<TextMeshProUGUI>().color = new Color(0.9f, 0.85f, 0.2f);

        // 방어막
        GameObject playerBlockPanel = CreatePanel("BlockPanel", playerHudGO.transform,
            new Color(0.3f, 0.5f, 0.9f, 0.85f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-15, -55), new Vector2(64, 36));
        playerBlockPanel.SetActive(false);
        GameObject playerBlockText = CreateTMP("BlockText", playerBlockPanel.transform,
            "0", 18, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 상태이상 컨테이너
        GameObject playerStatusCont = CreateHorizontalGroup("StatusContainer", playerHudGO.transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10, 10), new Vector2(360, 36));

        // 턴 텍스트
        GameObject turnText = CreateTMP("TurnText", playerHudGO.transform,
            "턴 1", 16, TextAlignmentOptions.Right,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10, -10), new Vector2(80, 26));

        // 피격 플래시 (전체화면)
        GameObject damageFlash = CreatePanel("DamageFlash", canvasGO.transform,
            new Color(1f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        damageFlash.GetComponent<Image>().raycastTarget = false;

        // HudView 필드 연결
        SerializeHudView(hudView,
            playerHpSlider.GetComponent<Slider>(),
            playerHpText.GetComponent<TextMeshProUGUI>(),
            playerBlockPanel,
            playerBlockText.GetComponentInChildren<TextMeshProUGUI>(),
            energyText.GetComponent<TextMeshProUGUI>(),
            playerStatusCont.GetComponent<RectTransform>(),
            turnText.GetComponent<TextMeshProUGUI>(),
            damageFlash.GetComponent<Image>(),
            statusIconPrefab?.GetComponent<BattleStatusIconView>());

        // ── 손패 영역 ─────────────────────────────────────────────
        GameObject handAreaGO = CreateEmpty("HandArea", canvasGO.transform);
        SetRectAnchored(handAreaGO,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0, 10), new Vector2(0, 220));
        BattleHandView handView = handAreaGO.AddComponent<BattleHandView>();

        GameObject handContainer = CreateEmpty("HandContainer", handAreaGO.transform);
        SetRectAnchored(handContainer,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1400, 200));

        // HandView 필드 연결
        SerializeHandView(handView,
            cardViewPrefab?.GetComponent<BattleCardView>(),
            handContainer.GetComponent<RectTransform>());

        // ── 카드 날아가는 목표점 ─────────────────────────────────
        GameObject cardTarget = CreateEmpty("CardPlayTarget", canvasGO.transform);
        SetRectAnchored(cardTarget,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 100), new Vector2(10, 10));

        // ── 타게팅 화살표 ─────────────────────────────────────────
        GameObject arrowRoot = CreateEmpty("TargetArrow", canvasGO.transform);
        SetRectAnchored(arrowRoot,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(100, 100));
        BattleTargetArrow targetArrow = arrowRoot.AddComponent<BattleTargetArrow>();

        // 세그먼트 8개 생성
        RectTransform[] segments = new RectTransform[8];
        for (int i = 0; i < 8; i++)
        {
            GameObject seg = CreateImageObject($"Segment_{i}", arrowRoot.transform,
                new Color(0.85f, 0.10f, 0.10f, 0.8f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(80, 28));
            // 세그먼트를 약간 둥글게 표현하기 위해 Image.type = Sliced 하면 좋지만
            // 기본 Simple로도 충분
            segments[i] = seg.GetComponent<RectTransform>();
        }
        // 화살촉
        GameObject head = CreateImageObject("ArrowHead", arrowRoot.transform,
            new Color(0.85f, 0.10f, 0.10f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(36, 36));

        SerializeTargetArrow(targetArrow, segments, head.GetComponent<RectTransform>(), canvas);
        arrowRoot.SetActive(false);

        // ── 턴 종료 버튼 ─────────────────────────────────────────
        GameObject endTurnBtn = CreateButton("EndTurnButton", canvasGO.transform,
            "턴 종료", new Color(0.15f, 0.55f, 0.25f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-80, 0), new Vector2(140, 52));

        // ── 결과 패널 ─────────────────────────────────────────────
        GameObject resultPanel = CreatePanel("ResultPanel", canvasGO.transform,
            new Color(0f, 0f, 0f, 0.85f), Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        resultPanel.SetActive(false);

        GameObject resultText = CreateTMP("ResultText", resultPanel.transform,
            "✨ 승리!", 54, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 60), new Vector2(500, 80));
        resultText.GetComponent<TextMeshProUGUI>().color = Color.yellow;

        GameObject restartBtn = CreateButton("RestartButton", resultPanel.transform,
            "다시 시작", new Color(0.25f, 0.45f, 0.85f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -40), new Vector2(200, 56));

        // ── BattleUIManager ───────────────────────────────────────
        BattleManager battleManager = Object.FindObjectOfType<BattleManager>();
        BattleUIManager uiManager = canvasGO.AddComponent<BattleUIManager>();

        SerializeUIManager(uiManager,
            battleManager,
            hudView,
            enemyView,
            handView,
            endTurnBtn.GetComponent<Button>(),
            resultPanel,
            resultText.GetComponent<TextMeshProUGUI>(),
            restartBtn.GetComponent<Button>(),
            damageFlash.GetComponent<CanvasGroup>() ?? damageFlash.AddComponent<CanvasGroup>(),
            cardTarget.GetComponent<RectTransform>(),
            targetArrow,
            canvas);

        // ── 씬 저장 ───────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[BattleSceneBuilder] ✅ BattleCanvas 생성 완료! Ctrl+S로 씬을 저장하세요.");
        Selection.activeGameObject = canvasGO;
    }

    // ══════════════════════════════════════════════════════════════
    // 프리팹 생성 헬퍼
    // ══════════════════════════════════════════════════════════════

    private static void EnsurePrefabFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    private static GameObject EnsureCardViewPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CARD_VIEW_PREFAB_PATH);
        if (existing != null) return existing;

        // 카드 뷰 프리팹 생성
        GameObject root = new GameObject("CardView");
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 220f);
        root.AddComponent<CanvasRenderer>();

        // 배경
        GameObject bgObj = CreateImageObject("Background", root.transform,
            new Color(0.20f, 0.45f, 0.80f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(160, 220));

        // 카드 아이콘
        GameObject iconObj = CreateImageObject("CardIcon", root.transform,
            Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 30), new Vector2(100, 90));
        iconObj.GetComponent<Image>().preserveAspect = true;

        // 카드 이름
        GameObject nameObj = CreateTMP("NameText", root.transform,
            "카드 이름", 14, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0, -8), new Vector2(0, -130));
        nameObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // 에너지 비용 (좌상단)
        GameObject costObj = CreateTMP("CostText", root.transform,
            "1", 18, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14, -14), new Vector2(28, 28));
        costObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        costObj.GetComponent<TextMeshProUGUI>().color = new Color(0.9f, 0.9f, 0.2f);

        // 설명 텍스트 (하단)
        GameObject descObj = CreateTMP("DescText", root.transform,
            "효과 설명", 11, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0, 12), new Vector2(-8, 60));

        // BattleCardView 컴포넌트
        BattleCardView cardView = root.AddComponent<BattleCardView>();
        SerializeCardView(cardView,
            bgObj.GetComponent<Image>(),
            iconObj.GetComponent<Image>(),
            nameObj.GetComponent<TextMeshProUGUI>(),
            costObj.GetComponent<TextMeshProUGUI>(),
            descObj.GetComponent<TextMeshProUGUI>());

        // 프리팹 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CARD_VIEW_PREFAB_PATH);
        Object.DestroyImmediate(root);
        Debug.Log($"[BattleSceneBuilder] CardView 프리팹 생성: {CARD_VIEW_PREFAB_PATH}");
        return prefab;
    }

    private static GameObject EnsureStatusIconPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(STATUS_ICON_PREFAB_PATH);
        if (existing != null) return existing;

        GameObject root = new GameObject("StatusIcon");
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(36f, 36f);

        GameObject iconObj = CreateImageObject("IconImage", root.transform,
            Color.green, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(36, 36));
        GameObject stacksObj = CreateTMP("StacksText", root.transform,
            "", 11, TextAlignmentOptions.Right,
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-2, 2), new Vector2(20, 16));
        GameObject durObj = CreateTMP("DurationText", root.transform,
            "", 10, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, -2), new Vector2(36, 14));

        BattleStatusIconView iconView = root.AddComponent<BattleStatusIconView>();
        SerializeStatusIconView(iconView,
            iconObj.GetComponent<Image>(),
            stacksObj.GetComponent<TextMeshProUGUI>(),
            durObj.GetComponent<TextMeshProUGUI>());

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, STATUS_ICON_PREFAB_PATH);
        Object.DestroyImmediate(root);
        Debug.Log($"[BattleSceneBuilder] StatusIcon 프리팹 생성: {STATUS_ICON_PREFAB_PATH}");
        return prefab;
    }

    // ══════════════════════════════════════════════════════════════
    // SerializedObject를 통한 필드 연결
    // ══════════════════════════════════════════════════════════════

    private static void SerializeCardView(BattleCardView cv,
        Image bg, Image icon, TextMeshProUGUI name, TextMeshProUGUI cost, TextMeshProUGUI desc)
    {
        SerializedObject so = new SerializedObject(cv);
        so.FindProperty("cardBackground").objectReferenceValue  = bg;
        so.FindProperty("cardTypeIcon").objectReferenceValue    = icon;
        so.FindProperty("cardNameText").objectReferenceValue    = name;
        so.FindProperty("energyCostText").objectReferenceValue  = cost;
        so.FindProperty("descriptionText").objectReferenceValue = desc;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeStatusIconView(BattleStatusIconView sv,
        Image icon, TextMeshProUGUI stacks, TextMeshProUGUI dur)
    {
        SerializedObject so = new SerializedObject(sv);
        so.FindProperty("iconImage").objectReferenceValue   = icon;
        so.FindProperty("stacksText").objectReferenceValue  = stacks;
        so.FindProperty("durationText").objectReferenceValue = dur;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeEnemyView(BattleEnemyView ev,
        Image enemyImg, TextMeshProUGUI nameText,
        Slider hpSlider, TextMeshProUGUI hpText,
        GameObject blockPanel, TextMeshProUGUI blockText,
        RectTransform statusCont,
        GameObject intentPanel, Image intentIcon, TextMeshProUGUI intentText,
        BattleStatusIconView statusIconPrefab)
    {
        SerializedObject so = new SerializedObject(ev);
        so.FindProperty("nameText").objectReferenceValue         = nameText;
        so.FindProperty("hpSlider").objectReferenceValue         = hpSlider;
        so.FindProperty("hpText").objectReferenceValue           = hpText;
        so.FindProperty("blockPanel").objectReferenceValue       = blockPanel;
        so.FindProperty("blockText").objectReferenceValue        = blockText;
        so.FindProperty("statusContainer").objectReferenceValue  = statusCont;
        so.FindProperty("statusIconPrefab").objectReferenceValue = statusIconPrefab;
        so.FindProperty("intentPanel").objectReferenceValue      = intentPanel;
        so.FindProperty("intentIcon").objectReferenceValue       = intentIcon;
        so.FindProperty("intentText").objectReferenceValue       = intentText;
        so.FindProperty("enemyImage").objectReferenceValue       = enemyImg;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeHudView(BattleHudView hv,
        Slider hpSlider, TextMeshProUGUI hpText,
        GameObject blockPanel, TextMeshProUGUI blockText,
        TextMeshProUGUI energyText,
        RectTransform statusCont, TextMeshProUGUI turnText,
        Graphic damageFlash, BattleStatusIconView statusIconPrefab)
    {
        SerializedObject so = new SerializedObject(hv);
        so.FindProperty("hpSlider").objectReferenceValue         = hpSlider;
        so.FindProperty("hpText").objectReferenceValue           = hpText;
        so.FindProperty("blockPanel").objectReferenceValue       = blockPanel;
        so.FindProperty("blockText").objectReferenceValue        = blockText;
        so.FindProperty("energyText").objectReferenceValue       = energyText;
        so.FindProperty("statusContainer").objectReferenceValue  = statusCont;
        so.FindProperty("turnText").objectReferenceValue         = turnText;
        so.FindProperty("damageFlash").objectReferenceValue      = damageFlash;
        so.FindProperty("statusIconPrefab").objectReferenceValue = statusIconPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeHandView(BattleHandView hv,
        BattleCardView cardPrefab, RectTransform container)
    {
        SerializedObject so = new SerializedObject(hv);
        so.FindProperty("cardViewPrefab").objectReferenceValue  = cardPrefab;
        so.FindProperty("handContainer").objectReferenceValue   = container;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeTargetArrow(BattleTargetArrow ta,
        RectTransform[] segs, RectTransform head, Canvas canvas)
    {
        SerializedObject so = new SerializedObject(ta);

        SerializedProperty segsProp = so.FindProperty("segments");
        segsProp.arraySize = segs.Length;
        for (int i = 0; i < segs.Length; i++)
            segsProp.GetArrayElementAtIndex(i).objectReferenceValue = segs[i];

        so.FindProperty("arrowHead").objectReferenceValue = head;
        so.FindProperty("canvas").objectReferenceValue    = canvas;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeUIManager(BattleUIManager um,
        BattleManager bm, BattleHudView hud, BattleEnemyView enemy,
        BattleHandView hand, Button endTurn,
        GameObject resultPanel, TextMeshProUGUI resultText, Button restartBtn,
        CanvasGroup fadeMask, RectTransform cardTarget,
        BattleTargetArrow targetArrow, Canvas canvas)
    {
        SerializedObject so = new SerializedObject(um);
        so.FindProperty("battleManager").objectReferenceValue       = bm;
        so.FindProperty("playerHud").objectReferenceValue           = hud;
        so.FindProperty("enemyView").objectReferenceValue           = enemy;
        so.FindProperty("handView").objectReferenceValue            = hand;
        so.FindProperty("endTurnButton").objectReferenceValue       = endTurn;
        so.FindProperty("resultPanel").objectReferenceValue         = resultPanel;
        so.FindProperty("resultText").objectReferenceValue          = resultText;
        so.FindProperty("resultRestartButton").objectReferenceValue = restartBtn;
        so.FindProperty("fadeMask").objectReferenceValue            = fadeMask;
        so.FindProperty("cardPlayTarget").objectReferenceValue      = cardTarget;
        so.FindProperty("targetArrow").objectReferenceValue         = targetArrow;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ══════════════════════════════════════════════════════════════
    // uGUI 오브젝트 생성 헬퍼
    // ══════════════════════════════════════════════════════════════

    private static GameObject CreateEmpty(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetRectAnchored(GameObject go,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta       = sizeDelta;
    }

    private static GameObject CreatePanel(string name, Transform parent,
        Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateEmpty(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color;
        SetRectAnchored(go, anchorMin, anchorMax, anchoredPos, sizeDelta);
        return go;
    }

    private static GameObject CreateImageObject(string name, Transform parent,
        Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateEmpty(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color;
        SetRectAnchored(go, anchorMin, anchorMax, anchoredPos, sizeDelta);
        return go;
    }

    private static GameObject CreateTMP(string name, Transform parent,
        string text, int fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateEmpty(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = alignment;
        tmp.color     = Color.white;
        SetRectAnchored(go, anchorMin, anchorMax, anchoredPos, sizeDelta);
        return go;
    }

    private static GameObject CreateHpSlider(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        bool isStretchX = false)
    {
        var go = CreateEmpty(name, parent);
        SetRectAnchored(go, anchorMin, anchorMax, anchoredPos, sizeDelta);
        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;

        // Background
        var bgGo = CreatePanel("Background", go.transform,
            new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        // Fill Area
        var fillArea = CreateEmpty("Fill Area", go.transform);
        SetRectAnchored(fillArea, Vector2.zero, Vector2.one, new Vector2(5, 0), new Vector2(-10, 0));
        var fill = CreatePanel("Fill", fillArea.transform,
            new Color(0.2f, 0.75f, 0.3f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = bgGo.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        return go;
    }

    private static GameObject CreateHorizontalGroup(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateEmpty(name, parent);
        SetRectAnchored(go, anchorMin, anchorMax, anchoredPos, sizeDelta);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent,
        string label, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreatePanel(name, parent, bgColor, anchorMin, anchorMax, anchoredPos, sizeDelta);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();

        var textGo = CreateTMP("Text", go.transform,
            label, 20, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        textGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        return go;
    }
}
