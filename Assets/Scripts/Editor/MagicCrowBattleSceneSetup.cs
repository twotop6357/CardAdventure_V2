using CardAdventure;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MagicCrowBattleSceneSetup
{
    private const string CardViewPrefabPath = "Assets/Prefabs/UI/Card.prefab";
    private const string CardSpriteLibraryPath = "Assets/ScriptableObjects/CardSpriteLibrary.asset";
    private const string StatusIconPrefabPath = "Assets/Prefabs/UI/StatusIcon.prefab";
    private const string BattleBackgroundPath = "Assets/Assets/Sprites/BattleBackground/CrowBattleBackground.png";
    private const string MagicCrowSpritePath = "Assets/Assets/Sprites/Enemy/Monster_MagicCrow.png";
    private const string PlayerIdleSpritePath = "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleFront.png";

    [MenuItem("CardAdventure/Build Magic Crow Battle Scene")]
    public static void Build()
    {
        GameObject existing = GameObject.Find("BattleCanvas");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject cardViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardViewPrefabPath);
        CardSpriteLibrary cardSpriteLibrary = AssetDatabase.LoadAssetAtPath<CardSpriteLibrary>(CardSpriteLibraryPath);
        GameObject statusIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StatusIconPrefabPath);
        Sprite backgroundSprite = LoadSprite(BattleBackgroundPath, "CrowBattleBackground_0");
        Sprite crowSprite = LoadSprite(MagicCrowSpritePath, "Monster_MagicCrow 1_0");
        Sprite playerSprite = LoadSprite(PlayerIdleSpritePath, "Warrior_IdleFront_0");

        GameObject canvasGo = new GameObject("BattleCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Magic Crow Battle Scene");

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject background = CreatePanel("Background", canvasGo.transform, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = backgroundSprite;
        backgroundImage.raycastTarget = false;

        CreatePanel("BattlefieldShade", canvasGo.transform, new Color(0.02f, 0.025f, 0.035f, 0.25f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
            .GetComponent<Image>().raycastTarget = false;

        GameObject topBar = CreatePanel("TopBar", canvasGo.transform, new Color(0.04f, 0.055f, 0.065f, 0.86f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -26), new Vector2(0, 52));
        topBar.GetComponent<Image>().raycastTarget = false;
        CreateTMP("Title", topBar.transform, "CardAdventure", 22, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(22, 0), new Vector2(260, 0))
            .GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        TextMeshProUGUI battleLabel = CreateTMP("BattleLabel", topBar.transform, "Magic Crow", 20, TextAlignmentOptions.Right,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-28, 0), new Vector2(260, 0))
            .GetComponent<TextMeshProUGUI>();
        battleLabel.color = new Color(0.78f, 0.92f, 1f);

        CreatePanel("PlayerGroundShadow", canvasGo.transform, new Color(0f, 0f, 0f, 0.34f),
            new Vector2(0.28f, 0.43f), new Vector2(0.28f, 0.43f), new Vector2(0, -18), new Vector2(310, 34))
            .GetComponent<Image>().raycastTarget = false;
        GameObject playerAvatar = CreateImage("PlayerAvatar", canvasGo.transform, Color.white,
            new Vector2(0.28f, 0.48f), new Vector2(0.28f, 0.48f), Vector2.zero, new Vector2(250, 250));
        Image playerAvatarImage = playerAvatar.GetComponent<Image>();
        playerAvatarImage.sprite = playerSprite;
        playerAvatarImage.preserveAspect = true;
        playerAvatarImage.raycastTarget = false;

        CreatePanel("EnemyGroundShadow", canvasGo.transform, new Color(0f, 0f, 0f, 0.38f),
            new Vector2(0.69f, 0.42f), new Vector2(0.69f, 0.42f), new Vector2(0, -24), new Vector2(360, 40))
            .GetComponent<Image>().raycastTarget = false;

        GameObject enemyArea = CreateEmpty("EnemyArea", canvasGo.transform);
        SetRect(enemyArea, new Vector2(0.69f, 0.50f), new Vector2(0.69f, 0.50f), new Vector2(0, 10), new Vector2(520, 430));
        BattleEnemyView enemyView = enemyArea.AddComponent<BattleEnemyView>();

        GameObject enemyImageGo = CreateImage("EnemyImage", enemyArea.transform, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(290, 290));
        Image enemyImage = enemyImageGo.GetComponent<Image>();
        enemyImage.sprite = crowSprite;
        enemyImage.preserveAspect = true;

        TextMeshProUGUI enemyName = CreateTMP("NameText", enemyArea.transform, "Magic Crow", 24, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(340, 36))
            .GetComponent<TextMeshProUGUI>();
        enemyName.fontStyle = FontStyles.Bold;

        GameObject enemyHpSlider = CreateHpSlider("EnemyHpSlider", enemyArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(340, 18), out Image enemyHpFill);
        GameObject enemyHpText = CreateTMP("EnemyHpText", enemyArea.transform, "30 / 30", 18, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 61), new Vector2(200, 28));
        GameObject enemyBlockPanel = CreatePanel("BlockPanel", enemyArea.transform, new Color(0.35f, 0.75f, 0.95f, 0.92f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150, 86), new Vector2(58, 34));
        GameObject enemyBlockText = CreateTMP("BlockText", enemyBlockPanel.transform, "0", 18, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject enemyStatus = CreateHorizontalGroup("StatusContainer", enemyArea.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60, 87), new Vector2(240, 36));
        GameObject intentPanel = CreatePanel("IntentPanel", enemyArea.transform, new Color(0.08f, 0.08f, 0.10f, 0.78f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(260, 42));
        GameObject intentIcon = CreateImage("IntentIcon", intentPanel.transform, Color.white,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(30, 30));
        GameObject intentText = CreateTMP("IntentText", intentPanel.transform, "공격 6", 16, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(22, 0), new Vector2(-76, 0));

        SerializeEnemyView(enemyView, enemyImage, enemyName, enemyHpSlider.GetComponent<Slider>(),
            enemyHpText.GetComponent<TextMeshProUGUI>(), enemyHpFill, enemyBlockPanel,
            enemyBlockText.GetComponent<TextMeshProUGUI>(), enemyStatus.GetComponent<RectTransform>(),
            intentPanel, intentIcon.GetComponent<Image>(), intentText.GetComponent<TextMeshProUGUI>(),
            statusIconPrefab != null ? statusIconPrefab.GetComponent<BattleStatusIconView>() : null);

        GameObject playerHudGo = CreateEmpty("PlayerHUD", canvasGo.transform);
        SetRect(playerHudGo, new Vector2(0.28f, 0.30f), new Vector2(0.28f, 0.30f), Vector2.zero, new Vector2(360, 112));
        BattleHudView hudView = playerHudGo.AddComponent<BattleHudView>();
        CreatePanel("HudBackground", playerHudGo.transform, new Color(0.02f, 0.025f, 0.03f, 0.58f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
            .GetComponent<Image>().raycastTarget = false;
        GameObject playerHpSlider = CreateHpSlider("HpSlider", playerHudGo.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -20), new Vector2(0, 20), out _);
        GameObject playerHpText = CreateTMP("HpText", playerHudGo.transform, "50 / 50", 18, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10, -44), new Vector2(140, 26));
        GameObject energyOrb = CreatePanel("EnergyOrb", playerHudGo.transform, new Color(0.82f, 0.26f, 0.08f, 0.92f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-118, -46), new Vector2(78, 78));
        energyOrb.transform.SetAsFirstSibling();
        TextMeshProUGUI energyText = CreateTMP("EnergyText", playerHudGo.transform, "3 / 3", 24, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-118, -46), new Vector2(74, 34))
            .GetComponent<TextMeshProUGUI>();
        energyText.color = new Color(0.9f, 0.85f, 0.2f);
        GameObject playerBlockPanel = CreatePanel("BlockPanel", playerHudGo.transform, new Color(0.35f, 0.75f, 0.95f, 0.92f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18, -56), new Vector2(58, 34));
        playerBlockPanel.SetActive(false);
        GameObject playerBlockText = CreateTMP("BlockText", playerBlockPanel.transform, "0", 18, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject playerStatus = CreateHorizontalGroup("StatusContainer", playerHudGo.transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(84, 12), new Vector2(250, 36));
        GameObject turnText = CreateTMP("TurnText", playerHudGo.transform, "Turn 1", 16, TextAlignmentOptions.Right,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10, -10), new Vector2(80, 26));
        GameObject damageFlash = CreatePanel("DamageFlash", canvasGo.transform, new Color(1f, 0f, 0f, 0f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        damageFlash.GetComponent<Image>().raycastTarget = false;

        SerializeHudView(hudView, playerHpSlider.GetComponent<Slider>(), playerHpText.GetComponent<TextMeshProUGUI>(),
            playerBlockPanel, playerBlockText.GetComponent<TextMeshProUGUI>(), energyText,
            playerStatus.GetComponent<RectTransform>(), turnText.GetComponent<TextMeshProUGUI>(),
            damageFlash.GetComponent<Image>(), statusIconPrefab != null ? statusIconPrefab.GetComponent<BattleStatusIconView>() : null);

        GameObject handArea = CreateEmpty("HandArea", canvasGo.transform);
        SetRect(handArea, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 8), new Vector2(0, 270));
        BattleHandView handView = handArea.AddComponent<BattleHandView>();
        GameObject handContainer = CreateEmpty("HandContainer", handArea.transform);
        SetRect(handContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(1420, 235));
        SerializeHandView(handView, cardViewPrefab != null ? cardViewPrefab.GetComponent<BattleCardView>() : null,
            handContainer.GetComponent<RectTransform>(), cardSpriteLibrary);

        GameObject cardTarget = CreateEmpty("CardPlayTarget", canvasGo.transform);
        SetRect(cardTarget, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 128), new Vector2(10, 10));
        BattleTargetArrow targetArrow = CreateTargetArrow(canvasGo.transform, canvas);

        GameObject endTurnButton = CreateButton("EndTurnButton", canvasGo.transform, "End Turn", new Color(0.16f, 0.38f, 0.48f),
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-128, 218), new Vector2(150, 54));

        GameObject resultPanel = CreatePanel("ResultPanel", canvasGo.transform, new Color(0f, 0f, 0f, 0.85f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        resultPanel.SetActive(false);
        GameObject resultText = CreateTMP("ResultText", resultPanel.transform, "Victory!", 54, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(500, 80));
        resultText.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        GameObject restartButton = CreateButton("RestartButton", resultPanel.transform, "Restart", new Color(0.25f, 0.45f, 0.85f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(200, 56));

        GameObject fadeMask = CreatePanel("FadeMask", canvasGo.transform, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CanvasGroup fadeGroup = fadeMask.AddComponent<CanvasGroup>();
        fadeMask.GetComponent<Image>().raycastTarget = false;
        fadeMask.transform.SetAsLastSibling();

        BattleUIManager uiManager = canvasGo.AddComponent<BattleUIManager>();
        SerializeUIManager(uiManager, Object.FindFirstObjectByType<BattleManager>(), hudView, enemyView, handView,
            endTurnButton.GetComponent<Button>(), resultPanel, resultText.GetComponent<TextMeshProUGUI>(),
            restartButton.GetComponent<Button>(), fadeGroup, cardTarget.GetComponent<RectTransform>(), targetArrow);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGo;
        Debug.Log("[MagicCrowBattleSceneSetup] Built Magic Crow battle scene UI.");
    }

    private static BattleTargetArrow CreateTargetArrow(Transform parent, Canvas canvas)
    {
        GameObject arrowRoot = CreateEmpty("TargetArrow", parent);
        SetRect(arrowRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
        BattleTargetArrow arrow = arrowRoot.AddComponent<BattleTargetArrow>();
        RectTransform[] segments = new RectTransform[8];
        for (int i = 0; i < segments.Length; i++)
        {
            GameObject segment = CreateImage("Segment_" + i, arrowRoot.transform, new Color(0.85f, 0.10f, 0.10f, 0.8f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 28));
            segments[i] = segment.GetComponent<RectTransform>();
        }

        GameObject head = CreateImage("ArrowHead", arrowRoot.transform, new Color(0.85f, 0.10f, 0.10f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36));

        SerializedObject so = new SerializedObject(arrow);
        SerializedProperty segmentsProp = so.FindProperty("segments");
        segmentsProp.arraySize = segments.Length;
        for (int i = 0; i < segments.Length; i++)
            segmentsProp.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
        so.FindProperty("arrowHead").objectReferenceValue = head.GetComponent<RectTransform>();
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.ApplyModifiedPropertiesWithoutUndo();
        arrowRoot.SetActive(false);
        return arrow;
    }

    private static void SerializeEnemyView(BattleEnemyView view, Image enemyImage, TextMeshProUGUI nameText,
        Slider hpSlider, TextMeshProUGUI hpText, Image hpFill, GameObject blockPanel, TextMeshProUGUI blockText,
        RectTransform statusContainer, GameObject intentPanel, Image intentIcon, TextMeshProUGUI intentText,
        BattleStatusIconView statusIconPrefab)
    {
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("hpSlider").objectReferenceValue = hpSlider;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("hpFillImage").objectReferenceValue = hpFill;
        so.FindProperty("blockPanel").objectReferenceValue = blockPanel;
        so.FindProperty("blockText").objectReferenceValue = blockText;
        so.FindProperty("statusContainer").objectReferenceValue = statusContainer;
        so.FindProperty("statusIconPrefab").objectReferenceValue = statusIconPrefab;
        so.FindProperty("intentPanel").objectReferenceValue = intentPanel;
        so.FindProperty("intentIcon").objectReferenceValue = intentIcon;
        so.FindProperty("intentText").objectReferenceValue = intentText;
        so.FindProperty("enemyImage").objectReferenceValue = enemyImage;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeHudView(BattleHudView view, Slider hpSlider, TextMeshProUGUI hpText,
        GameObject blockPanel, TextMeshProUGUI blockText, TextMeshProUGUI energyText,
        RectTransform statusContainer, TextMeshProUGUI turnText, Graphic damageFlash,
        BattleStatusIconView statusIconPrefab)
    {
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("hpSlider").objectReferenceValue = hpSlider;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("blockPanel").objectReferenceValue = blockPanel;
        so.FindProperty("blockText").objectReferenceValue = blockText;
        so.FindProperty("energyText").objectReferenceValue = energyText;
        so.FindProperty("statusContainer").objectReferenceValue = statusContainer;
        so.FindProperty("turnText").objectReferenceValue = turnText;
        so.FindProperty("damageFlash").objectReferenceValue = damageFlash;
        so.FindProperty("statusIconPrefab").objectReferenceValue = statusIconPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeHandView(BattleHandView view, BattleCardView cardPrefab, RectTransform container,
        CardSpriteLibrary spriteLibrary)
    {
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("cardViewPrefab").objectReferenceValue = cardPrefab;
        so.FindProperty("handContainer").objectReferenceValue = container;
        so.FindProperty("spriteLibrary").objectReferenceValue = spriteLibrary;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SerializeUIManager(BattleUIManager manager, BattleManager battleManager, BattleHudView hud,
        BattleEnemyView enemy, BattleHandView hand, Button endTurn, GameObject resultPanel,
        TextMeshProUGUI resultText, Button restartButton, CanvasGroup fadeMask, RectTransform cardTarget,
        BattleTargetArrow targetArrow)
    {
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("battleManager").objectReferenceValue = battleManager;
        so.FindProperty("playerHud").objectReferenceValue = hud;
        so.FindProperty("enemyView").objectReferenceValue = enemy;
        so.FindProperty("handView").objectReferenceValue = hand;
        so.FindProperty("endTurnButton").objectReferenceValue = endTurn;
        so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        so.FindProperty("resultText").objectReferenceValue = resultText;
        so.FindProperty("resultRestartButton").objectReferenceValue = restartButton;
        so.FindProperty("fadeMask").objectReferenceValue = fadeMask;
        so.FindProperty("cardPlayTarget").objectReferenceValue = cardTarget;
        so.FindProperty("targetArrow").objectReferenceValue = targetArrow;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateEmpty(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = CreateEmpty(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        if (name.Contains("Panel") || name.Contains("Bar") || name.Contains("Button") || name.Contains("Background") || name.Contains("Block"))
        {
            ClassicPixelUiTheme.ApplyPanel(image, name.Contains("Intent") || name.Contains("Top"));
        }
        SetRect(go, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        return go;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        return CreatePanel(name, parent, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = CreateEmpty(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        ClassicPixelUiTheme.ApplyText(tmp);
        SetRect(go, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        return go;
    }

    private static GameObject CreateHpSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta, out Image fillImage)
    {
        GameObject go = CreateEmpty(name, parent);
        SetRect(go, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        GameObject background = CreatePanel("Background", go.transform, new Color(0.10f, 0.10f, 0.10f, 0.92f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject fillArea = CreateEmpty("Fill Area", go.transform);
        SetRect(fillArea, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-8, 0));
        GameObject fill = CreatePanel("Fill", fillArea.transform, new Color(0.78f, 0.12f, 0.12f, 0.96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fillImage = fill.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = background.GetComponent<Image>();
        slider.interactable = false;
        return go;
    }

    private static GameObject CreateHorizontalGroup(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = CreateEmpty(name, parent);
        SetRect(go, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = CreatePanel(name, parent, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        TextMeshProUGUI text = CreateTMP("Text", go.transform, label, 20, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<TextMeshProUGUI>();
        text.fontStyle = FontStyles.Bold;
        ClassicPixelUiTheme.ApplyButton(button);
        return go;
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
