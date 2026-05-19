/*
 * DialogueSceneSetup.cs  (Editor ?꾩슜)
 *
 * 硫붾돱 寃쎈줈:
 *   CardAdventure > Setup Dialogue System
 *       ???꾩옱 ?대┛ ?ъ뿉 DialogueCanvas + DialogueManager ?먮룞 援ъ꽦
 *
 *   CardAdventure > Create NPC (Interactable)
 *       ???좏깮??GameObject??NpcInteractable 而댄룷?뚰듃 異붽?,
 *         ?먮뒗 ??肉뚮━??鍮?NPC ?ㅻ툕?앺듃 ?앹꽦
 *
 * ?앹꽦?섎뒗 怨꾩링 援ъ“:
 *   DialogueCanvas  (Canvas / CanvasScaler / GraphicRaycaster)
 *     ?붴? DialoguePanel  (CanvasGroup, RectTransform ???섎떒 怨좎젙)
 *          ?쒋? PanelBg         (Image ??寃? ?쎌? 李?
 *          ?쒋? PortraitFrame   (Image ??珥덉긽???꾨젅??
 *          ??   ?붴? PortraitImage
 *          ?쒋? NameBox         (Image ???대쫫 諛뺤뒪)
 *          ??   ?붴? NameText   (TextMeshProUGUI)
 *          ?쒋? DialogueText    (TextMeshProUGUI)
 *          ?붴? NextArrow       (TextMeshProUGUI "??)
 *
 * ????GameManagers ?ㅻ툕?앺듃媛 ?덉쑝硫?洹??꾨옒??DialogueManager 而댄룷?뚰듃 異붽?.
 * ?놁쑝硫???猷⑦듃??"DialogueManager" ?ㅻ툕?앺듃瑜??앹꽦?쒕떎.
 */

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure.Editor
{
    public static class DialogueSceneSetup
    {
        // ??????????????????????????????????????????????????????
        //  ????쒖뒪???꾩껜 ?ㅼ젙
        // ??????????????????????????????????????????????????????
        [MenuItem("CardAdventure/Setup Dialogue System")]
        public static void SetupDialogueSystem()
        {
            // 1) DialogueCanvas ?앹꽦 ?먮뒗 湲곗〈 ?ъ궗??
            GameObject canvasGo = EnsureDialogueCanvas();

            // 2) DialoguePanel 諛??섏쐞 怨꾩링 援ъ꽦
            GameObject panelGo = BuildDialoguePanel(canvasGo);

            // 3) DialogueView 而댄룷?뚰듃 ?곌껐
            DialogueView view = ConnectDialogueView(panelGo);

            // 4) DialogueManager 諛곗튂
            DialogueManager manager = EnsureDialogueManager();

            // 5) DialogueManager ??DialogueView ?곌껐
            if (manager != null && view != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("dialogueView").objectReferenceValue = view;
                so.ApplyModifiedProperties();
            }

            // ???뷀떚 留덊겕
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[DialogueSceneSetup] ??????쒖뒪??援ъ꽦 ?꾨즺. ?ъ쓣 ??ν븯?몄슂.");
        }

        // ??????????????????????????????????????????????????????
        //  NPC ?ㅻ툕?앺듃 ?앹꽦
        // ??????????????????????????????????????????????????????
        [MenuItem("CardAdventure/Create NPC (Interactable)")]
        public static void CreateNpcInteractable()
        {
            GameObject target = Selection.activeGameObject;

            if (target != null)
            {
                // ?좏깮???ㅻ툕?앺듃??NpcInteractable 異붽?
                if (target.GetComponent<NpcInteractable>() == null)
                {
                    EnsureNpcCollider(target);
                    Undo.AddComponent<NpcInteractable>(target);
                    Debug.Log($"[DialogueSceneSetup] '{target.name}'??NpcInteractable 異붽? ?꾨즺.");
                }
                else
                {
                    Debug.Log($"[DialogueSceneSetup] '{target.name}'???대? NpcInteractable???덉뒿?덈떎.");
                }
            }
            else
            {
                // ?좏깮 ?놁쑝硫???猷⑦듃????NPC ?앹꽦
                GameObject npcGo = new GameObject("NPC_New");
                Undo.RegisterCreatedObjectUndo(npcGo, "Create NPC");
                EnsureNpcCollider(npcGo);
                npcGo.AddComponent<NpcInteractable>();
                Selection.activeGameObject = npcGo;
                Debug.Log("[DialogueSceneSetup] ??NPC ?ㅻ툕?앺듃 ?앹꽦 ?꾨즺. Inspector?먯꽌 DialogueData瑜??곌껐?섏꽭??");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧
        //  ?대? 鍮뚮뜑 硫붿꽌??
        // ?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧?먥븧

        private static GameObject EnsureDialogueCanvas()
        {
            // ?대? ?덉쑝硫??ъ궗??
            var existing = GameObject.Find("DialogueCanvas");
            if (existing != null) return existing;

            GameObject go = new GameObject("DialogueCanvas");
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueCanvas");

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // ??붿갹? 理쒖긽??

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject BuildDialoguePanel(GameObject canvasGo)
        {
            // 湲곗〈 ?⑤꼸 ?ъ궗??
            Transform existing = canvasGo.transform.Find("DialoguePanel");
            if (existing != null) return existing.gameObject;

            // ?? DialoguePanel ??????????????????????????????????
            GameObject panel = new GameObject("DialoguePanel");
            Undo.RegisterCreatedObjectUndo(panel, "Create DialoguePanel");
            panel.transform.SetParent(canvasGo.transform, false);

            RectTransform panelRt = panel.AddComponent<RectTransform>();
            // ?섎떒 ?꾩껜 ?덈퉬 怨좎젙, ?믪씠 140
            panelRt.anchorMin        = new Vector2(0f, 0f);
            panelRt.anchorMax        = new Vector2(1f, 0f);
            panelRt.pivot            = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta        = new Vector2(0f, 156f);

            panel.AddComponent<CanvasGroup>();

            // ?? PanelBg (??諛곌꼍) ??????????????????????????????
            GameObject bg = new GameObject("PanelBg");
            bg.transform.SetParent(panel.transform, false);
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin        = Vector2.zero;
            bgRt.anchorMax        = Vector2.one;
            bgRt.offsetMin        = new Vector2(10f, 6f);
            bgRt.offsetMax        = new Vector2(-10f, -6f);

            Image bgImg = bg.AddComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(bgImg, ClassicPixelUiTheme.ShopPanelAccent.Blue, false);

            // ?? PortraitFrame ?????????????????????????????????
            GameObject portraitFrame = new GameObject("PortraitFrame");
            portraitFrame.transform.SetParent(panel.transform, false);
            RectTransform portraitFrameRt = portraitFrame.AddComponent<RectTransform>();
            portraitFrameRt.anchorMin = new Vector2(0f, 0.5f);
            portraitFrameRt.anchorMax = new Vector2(0f, 0.5f);
            portraitFrameRt.pivot = new Vector2(0f, 0.5f);
            portraitFrameRt.anchoredPosition = new Vector2(24f, 0f);
            portraitFrameRt.sizeDelta = new Vector2(126f, 126f);

            Image portraitFrameImg = portraitFrame.AddComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(portraitFrameImg, ClassicPixelUiTheme.ShopPanelAccent.Gold, false);

            GameObject portraitImageGo = new GameObject("PortraitImage");
            portraitImageGo.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitImageRt = portraitImageGo.AddComponent<RectTransform>();
            portraitImageRt.anchorMin = Vector2.zero;
            portraitImageRt.anchorMax = Vector2.one;
            portraitImageRt.offsetMin = new Vector2(6f, 6f);
            portraitImageRt.offsetMax = new Vector2(-6f, -6f);
            Image portraitImage = portraitImageGo.AddComponent<Image>();
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;

            // ?? NameBox ????????????????????????????????????????
            GameObject nameBox = new GameObject("NameBox");
            nameBox.transform.SetParent(panel.transform, false);
            RectTransform nameBoxRt = nameBox.AddComponent<RectTransform>();
            nameBoxRt.anchorMin        = new Vector2(0f, 1f);
            nameBoxRt.anchorMax        = new Vector2(0f, 1f);
            nameBoxRt.pivot            = new Vector2(0f, 0f);
            nameBoxRt.anchoredPosition = new Vector2(14f, 0f);
            nameBoxRt.sizeDelta        = new Vector2(120f, 28f);

            Image nameBoxImg = nameBox.AddComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(nameBoxImg, ClassicPixelUiTheme.ShopPanelAccent.Blue, false);

            // NameText
            GameObject nameTextGo = new GameObject("NameText");
            nameTextGo.transform.SetParent(nameBox.transform, false);
            RectTransform nameTextRt = nameTextGo.AddComponent<RectTransform>();
            nameTextRt.anchorMin = Vector2.zero;
            nameTextRt.anchorMax = Vector2.one;
            nameTextRt.offsetMin = new Vector2(6f, 2f);
            nameTextRt.offsetMax = new Vector2(-6f, -2f);

            TextMeshProUGUI nameTmp = nameTextGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text      = "?붿옄 ?대쫫";
            nameTmp.fontSize  = 14f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            ClassicPixelUiTheme.ApplyText(nameTmp);

            // ?? DialogueText ???????????????????????????????????
            GameObject textGo = new GameObject("DialogueText");
            textGo.transform.SetParent(panel.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin        = Vector2.zero;
            textRt.anchorMax        = Vector2.one;
            textRt.offsetMin        = new Vector2(174f, 26f);
            textRt.offsetMax        = new Vector2(-44f, -24f);

            TextMeshProUGUI dialogueTmp = textGo.AddComponent<TextMeshProUGUI>();
            dialogueTmp.text      = "";
            dialogueTmp.fontSize  = 17f;
            dialogueTmp.alignment = TextAlignmentOptions.TopLeft;
            dialogueTmp.textWrappingMode = TextWrappingModes.Normal;
            ClassicPixelUiTheme.ApplyText(dialogueTmp);

            // ?? NextArrow (?? ??????????????????????????????????
            GameObject arrowGo = new GameObject("NextArrow");
            arrowGo.transform.SetParent(panel.transform, false);
            RectTransform arrowRt = arrowGo.AddComponent<RectTransform>();
            arrowRt.anchorMin        = new Vector2(1f, 0f);
            arrowRt.anchorMax        = new Vector2(1f, 0f);
            arrowRt.pivot            = new Vector2(1f, 0f);
            arrowRt.anchoredPosition = new Vector2(-42f, 18f);
            arrowRt.sizeDelta        = new Vector2(20f, 20f);

            TextMeshProUGUI arrowTmp = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowTmp.text      = "▼";
            arrowTmp.fontSize  = 14f;
            arrowTmp.alignment = TextAlignmentOptions.Center;
            arrowTmp.color     = ClassicPixelUiTheme.Cyan;
            arrowGo.SetActive(false);

            return panel;
        }

        private static DialogueView ConnectDialogueView(GameObject panelGo)
        {
            DialogueView view = panelGo.GetComponent<DialogueView>();
            if (view == null)
                view = Undo.AddComponent<DialogueView>(panelGo);

            var so = new SerializedObject(view);

            so.FindProperty("canvasGroup").objectReferenceValue =
                panelGo.GetComponent<CanvasGroup>();

            so.FindProperty("dialoguePanel").objectReferenceValue =
                panelGo.GetComponent<RectTransform>();

            Transform nameBox  = panelGo.transform.Find("NameBox");
            Transform nameText = nameBox?.Find("NameText");
            Transform portrait = panelGo.transform.Find("PortraitFrame");
            Transform portraitImage = portrait?.Find("PortraitImage");
            Transform textGo   = panelGo.transform.Find("DialogueText");
            Transform arrowGo  = panelGo.transform.Find("NextArrow");

            if (nameBox != null)
                so.FindProperty("nameBoxRoot").objectReferenceValue = nameBox.gameObject;

            if (nameText != null)
                so.FindProperty("speakerNameText").objectReferenceValue =
                    nameText.GetComponent<TextMeshProUGUI>();

            if (portrait != null)
                so.FindProperty("portraitRoot").objectReferenceValue = portrait.gameObject;

            if (portraitImage != null)
                so.FindProperty("portraitImage").objectReferenceValue = portraitImage.GetComponent<Image>();

            if (textGo != null)
            {
                so.FindProperty("dialogueText").objectReferenceValue =
                    textGo.GetComponent<TextMeshProUGUI>();
            }

            if (arrowGo != null)
                so.FindProperty("nextArrow").objectReferenceValue = arrowGo.gameObject;

            so.ApplyModifiedProperties();
            return view;
        }

        private static DialogueManager EnsureDialogueManager()
        {
            // ?대? ?ъ뿉 ?덉쑝硫?諛섑솚
            DialogueManager existing = Object.FindFirstObjectByType<DialogueManager>();
            if (existing != null) return existing;

            // GameManagers ?ㅻ툕?앺듃媛 ?덉쑝硫?洹??꾨옒??異붽?
            GameObject managers = GameObject.Find("GameManagers");
            if (managers != null)
            {
                DialogueManager dm = Undo.AddComponent<DialogueManager>(managers);
                Debug.Log("[DialogueSceneSetup] DialogueManager瑜?GameManagers??異붽??덉뒿?덈떎.");
                return dm;
            }

            // ?놁쑝硫????ㅻ툕?앺듃 ?앹꽦
            GameObject go = new GameObject("DialogueManager");
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueManager");
            return go.AddComponent<DialogueManager>();
        }

        private static void EnsureNpcCollider(GameObject go)
        {
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody2D>(go);
            }

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            EditorUtility.SetDirty(rb);

            CircleCollider2D legacyCircle = go.GetComponent<CircleCollider2D>();
            if (legacyCircle != null && !legacyCircle.isTrigger)
            {
                legacyCircle.enabled = false;
                EditorUtility.SetDirty(legacyCircle);
            }

            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = Undo.AddComponent<BoxCollider2D>(go);
            }

            box.offset = Vector2.zero;
            SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
            AdventureGridUtility.ConfigureFootCollider(
                box,
                go.transform,
                spriteRenderer,
                AdventureGridUtility.GetCellSize(1f));
            EditorUtility.SetDirty(box);

            if (go.GetComponent<NpcMovement>() == null && go.GetComponent<NpcTileAlignment>() == null)
            {
                Undo.AddComponent<NpcTileAlignment>(go);
            }
        }
    }
}
#endif

