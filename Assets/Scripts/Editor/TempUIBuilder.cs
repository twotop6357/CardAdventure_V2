using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.U2D.Sprites;

public class TempUIBuilder : EditorWindow
{
    // ── Stats UI 빌드 ───────────────────────────────────────────────
    [MenuItem("CardAdventure/Temp/Build Stats UI")]
    public static void Run()
    {
        var canvasObj = GameObject.Find("JobChangeCanvas");
        if (canvasObj == null) { Debug.LogError("JobChangeCanvas not found"); return; }

        var controller = canvasObj.GetComponent<CardAdventure.JobChangeUIController>();
        if (controller == null) { Debug.LogError("JobChangeUIController not found"); return; }

        var background = canvasObj.transform.Find("BackGround");
        if (background == null) { Debug.LogError("BackGround not found"); return; }

        var oldStats = background.Find("JobStatsText");
        if (oldStats != null) GameObject.DestroyImmediate(oldStats.gameObject);

        var statsObj = new GameObject("JobStatsText");
        statsObj.transform.SetParent(background, false);

        var rt  = statsObj.AddComponent<RectTransform>();
        var tmp = statsObj.AddComponent<TextMeshProUGUI>();

        var descObj = background.Find("JobDescription/JobDescriptionText");
        if (descObj != null)
        {
            var descTmp = descObj.GetComponent<TextMeshProUGUI>();
            if (descTmp != null) { tmp.font = descTmp.font; tmp.color = Color.white; }
        }

        tmp.fontSize  = 26f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.lineSpacing = 15f;
        tmp.text = "공격력 ■■■■□\n방어력 ■■■□□\n마법력 ■□□□□\n난이도 ■■■□□";

        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-310f, -15f);
        rt.sizeDelta       = new Vector2(240f, 280f);

        var so   = new SerializedObject(controller);
        so.Update();
        var prop = so.FindProperty("jobStatsText");
        if (prop != null) { prop.objectReferenceValue = tmp; so.ApplyModifiedProperties(); }

        EditorUtility.SetDirty(controller);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasObj.scene);
        Debug.Log("[TempUIBuilder] JobStatsText 생성 및 연결 완료.");
    }

    [MenuItem("CardAdventure/Temp/Check Stats Link")]
    public static void CheckLink()
    {
        var canvasObj = GameObject.Find("JobChangeCanvas");
        var ctrl = canvasObj?.GetComponent<CardAdventure.JobChangeUIController>();
        if (ctrl == null) { Debug.LogError("Controller not found"); return; }
        var field = typeof(CardAdventure.JobChangeUIController)
            .GetField("jobStatsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var val = field?.GetValue(ctrl);
        Debug.Log("[TempUIBuilder] jobStatsText = " + (val == null ? "NULL" : val.ToString() + " (연결됨)"));
    }

    // ── 상태이상 툴팁 패널 빌드 ─────────────────────────────────────
    /// <summary>
    /// BattleTest 씬에 StatusTooltipPanel을 자동 생성한다.
    /// BattleCanvas(또는 씬의 루트 Canvas)의 마지막 자식으로 배치되어 항상 최상위 렌더링.
    /// </summary>
    [MenuItem("CardAdventure/Temp/Build Status Tooltip Panel")]
    public static void BuildStatusTooltipPanel()
    {
        // ── 1. 부모 Canvas 탐색 ──
        Canvas targetCanvas = null;

        // BattleCanvas 우선
        var canvasObj = GameObject.Find("BattleCanvas");
        if (canvasObj != null)
            targetCanvas = canvasObj.GetComponent<Canvas>();

        // 없으면 씬의 루트 Canvas 중 첫 번째
        if (targetCanvas == null)
        {
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
                if (c.transform.parent == null) { targetCanvas = c; break; }
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[TempUIBuilder] Canvas를 찾을 수 없습니다. 씬에 Canvas가 있는지 확인하세요.");
            return;
        }

        // ── 2. 기존 패널 제거 (씬 전체 검색) ──
        var allExisting = Object.FindObjectsByType<CardAdventure.StatusTooltipPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in allExisting)
            Object.DestroyImmediate(p.gameObject);

        // 이름으로도 검색하여 제거 (스크립트가 없는 찌꺼기 방지)
        var oldByName = GameObject.Find("StatusTooltipPanel");
        if (oldByName != null) Object.DestroyImmediate(oldByName);

        // ── 3. 루트 패널 GO 생성 ──
        var panelGo = new GameObject("StatusTooltipPanel", typeof(RectTransform));
        panelGo.transform.SetParent(targetCanvas.transform, false);
        panelGo.transform.SetAsLastSibling(); // 항상 최상위

        var canvasGroup = panelGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        var panelRt        = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin  = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax  = new Vector2(0.5f, 0.5f);
        panelRt.pivot      = new Vector2(0f, 1f); // 위치 계산은 좌상단 기준
        panelRt.sizeDelta  = new Vector2(240f, 150f); // 약간 더 크게
        panelRt.anchoredPosition = Vector2.zero;

        // 배경 이미지 (불투명 검정)
        var bgImg         = panelGo.AddComponent<Image>();
        bgImg.color       = Color.black;
        bgImg.raycastTarget = false;

        // ── 4. 아이콘 배경 ──
        var iconBgGo      = new GameObject("IconBackground", typeof(RectTransform));
        iconBgGo.transform.SetParent(panelGo.transform, false);
        var iconBgRt      = iconBgGo.GetComponent<RectTransform>();
        iconBgRt.anchorMin        = new Vector2(0f, 1f);
        iconBgRt.anchorMax        = new Vector2(0f, 1f);
        iconBgRt.pivot            = new Vector2(0f, 1f);
        iconBgRt.anchoredPosition = new Vector2(8f, -8f);
        iconBgRt.sizeDelta        = new Vector2(36f, 36f);
        var iconBgImg             = iconBgGo.AddComponent<Image>();
        iconBgImg.color           = Color.gray;
        iconBgImg.raycastTarget   = false;

        // ── 5. 아이콘 이미지 ──
        var iconGo        = new GameObject("IconImage", typeof(RectTransform));
        iconGo.transform.SetParent(iconBgGo.transform, false);
        var iconRt        = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin  = Vector2.zero;
        iconRt.anchorMax  = Vector2.one;
        iconRt.offsetMin  = new Vector2(4f, 4f);
        iconRt.offsetMax  = new Vector2(-4f, -4f);
        var iconImg       = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.enabled        = false; // 스프라이트 없을 때 숨김

        // ── 6. 이름 텍스트 ──
        var nameGo        = new GameObject("NameText", typeof(RectTransform));
        nameGo.transform.SetParent(panelGo.transform, false);
        var nameRt        = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin        = new Vector2(0f, 1f);
        nameRt.anchorMax        = new Vector2(1f, 1f);
        nameRt.pivot            = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(52f, -8f);
        nameRt.sizeDelta        = new Vector2(-60f, 32f);
        var nameTmp       = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = "상태이상";
        nameTmp.fontSize  = 16f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color     = Color.white;
        nameTmp.alignment = TextAlignmentOptions.BottomLeft;

        // ── 7. 구분선 ──
        var divGo         = new GameObject("Divider", typeof(RectTransform));
        divGo.transform.SetParent(panelGo.transform, false);
        var divRt         = divGo.GetComponent<RectTransform>();
        divRt.anchorMin        = new Vector2(0f, 1f);
        divRt.anchorMax        = new Vector2(1f, 1f);
        divRt.pivot            = new Vector2(0f, 1f);
        divRt.anchoredPosition = new Vector2(8f, -52f);
        divRt.sizeDelta        = new Vector2(-16f, 1f);
        var divImg        = divGo.AddComponent<Image>();
        divImg.color      = new Color(1f, 1f, 1f, 0.18f);
        divImg.raycastTarget = false;

        // ── 8. 설명 텍스트 ──
        var descGo        = new GameObject("DescriptionText", typeof(RectTransform));
        descGo.transform.SetParent(panelGo.transform, false);
        var descRt        = descGo.GetComponent<RectTransform>();
        descRt.anchorMin        = new Vector2(0f, 1f);
        descRt.anchorMax        = new Vector2(1f, 1f);
        descRt.pivot            = new Vector2(0f, 1f);
        descRt.anchoredPosition = new Vector2(8f, -58f);
        descRt.sizeDelta        = new Vector2(-16f, 60f);
        var descTmp       = descGo.AddComponent<TextMeshProUGUI>();
        descTmp.text      = "상태이상 설명";
        descTmp.fontSize  = 12f;
        descTmp.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
        descTmp.alignment = TextAlignmentOptions.TopLeft;
        descTmp.textWrappingMode = TextWrappingModes.Normal;

        // ── 9. 스택/지속시간 텍스트 행 ──
        var stacksGo      = new GameObject("StacksText", typeof(RectTransform));
        stacksGo.transform.SetParent(panelGo.transform, false);
        var stacksRt      = stacksGo.GetComponent<RectTransform>();
        stacksRt.anchorMin        = new Vector2(0f, 0f);
        stacksRt.anchorMax        = new Vector2(0.5f, 0f);
        stacksRt.pivot            = new Vector2(0f, 0f);
        stacksRt.anchoredPosition = new Vector2(8f, 8f);
        stacksRt.sizeDelta        = new Vector2(-4f, 20f);
        var stacksTmp     = stacksGo.AddComponent<TextMeshProUGUI>();
        stacksTmp.text    = string.Empty;
        stacksTmp.fontSize = 11f;
        stacksTmp.color   = new Color(0.95f, 0.85f, 0.50f, 1f); // 골드색
        stacksTmp.alignment = TextAlignmentOptions.BottomLeft;

        var durationGo    = new GameObject("DurationText", typeof(RectTransform));
        durationGo.transform.SetParent(panelGo.transform, false);
        var durationRt    = durationGo.GetComponent<RectTransform>();
        durationRt.anchorMin        = new Vector2(0.5f, 0f);
        durationRt.anchorMax        = new Vector2(1f, 0f);
        durationRt.pivot            = new Vector2(0f, 0f);
        durationRt.anchoredPosition = new Vector2(4f, 8f);
        durationRt.sizeDelta        = new Vector2(-12f, 20f);
        var durationTmp   = durationGo.AddComponent<TextMeshProUGUI>();
        durationTmp.text  = string.Empty;
        durationTmp.fontSize = 11f;
        durationTmp.color = new Color(0.70f, 0.90f, 1.00f, 1f); // 하늘색
        durationTmp.alignment = TextAlignmentOptions.BottomRight;

        // ── 10. StatusTooltipPanel 컴포넌트 부착 및 필드 연결 ──
        var tooltip = panelGo.AddComponent<CardAdventure.StatusTooltipPanel>();

        var so = new SerializedObject(tooltip);
        so.Update();
        so.FindProperty("panelRect"       ).objectReferenceValue = panelRt;
        so.FindProperty("iconImage"       ).objectReferenceValue = iconImg;
        so.FindProperty("iconBackground"  ).objectReferenceValue = iconBgImg;
        so.FindProperty("nameText"        ).objectReferenceValue = nameTmp;
        so.FindProperty("descriptionText" ).objectReferenceValue = descTmp;
        so.FindProperty("stacksText"      ).objectReferenceValue = stacksTmp;
        so.FindProperty("durationText"    ).objectReferenceValue = durationTmp;
        so.FindProperty("canvasGroup"     ).objectReferenceValue = canvasGroup;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(panelGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetCanvas.gameObject.scene);

        Debug.Log("[TempUIBuilder] StatusTooltipPanel 생성 완료. 부모: " + targetCanvas.gameObject.name);
    }
}
