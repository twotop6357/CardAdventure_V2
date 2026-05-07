/*
 * DialogueSceneSetup.cs  (Editor 전용)
 *
 * 메뉴 경로:
 *   CardAdventure > Setup Dialogue System
 *       → 현재 열린 씬에 DialogueCanvas + DialogueManager 자동 구성
 *
 *   CardAdventure > Create NPC (Interactable)
 *       → 선택한 GameObject에 NpcInteractable 컴포넌트 추가,
 *         또는 씬 뿌리에 빈 NPC 오브젝트 생성
 *
 * 생성되는 계층 구조:
 *   DialogueCanvas  (Canvas / CanvasScaler / GraphicRaycaster)
 *     └─ DialoguePanel  (CanvasGroup, RectTransform — 하단 고정)
 *          ├─ PanelBg         (Image — 흰 배경 + 둥근 모서리)
 *          ├─ NameBox         (Image — 이름 박스)
 *          │    └─ NameText   (TextMeshProUGUI)
 *          ├─ DialogueText    (TextMeshProUGUI + TextAnimator_TMP + TypewriterByCharacter)
 *          └─ NextArrow       (TextMeshProUGUI "▼")
 *
 * 씬 내 GameManagers 오브젝트가 있으면 그 아래에 DialogueManager 컴포넌트 추가.
 * 없으면 씬 루트에 "DialogueManager" 오브젝트를 생성한다.
 */

#if UNITY_EDITOR
using Febucci.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure.Editor
{
    public static class DialogueSceneSetup
    {
        // ──────────────────────────────────────────────────────
        //  대화 시스템 전체 설정
        // ──────────────────────────────────────────────────────
        [MenuItem("CardAdventure/Setup Dialogue System")]
        public static void SetupDialogueSystem()
        {
            // 1) DialogueCanvas 생성 또는 기존 재사용
            GameObject canvasGo = EnsureDialogueCanvas();

            // 2) DialoguePanel 및 하위 계층 구성
            GameObject panelGo = BuildDialoguePanel(canvasGo);

            // 3) DialogueView 컴포넌트 연결
            DialogueView view = ConnectDialogueView(panelGo);

            // 4) DialogueManager 배치
            DialogueManager manager = EnsureDialogueManager();

            // 5) DialogueManager ↔ DialogueView 연결
            if (manager != null && view != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("dialogueView").objectReferenceValue = view;
                so.ApplyModifiedProperties();
            }

            // 씬 더티 마크
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[DialogueSceneSetup] ✅ 대화 시스템 구성 완료. 씬을 저장하세요.");
        }

        // ──────────────────────────────────────────────────────
        //  NPC 오브젝트 생성
        // ──────────────────────────────────────────────────────
        [MenuItem("CardAdventure/Create NPC (Interactable)")]
        public static void CreateNpcInteractable()
        {
            GameObject target = Selection.activeGameObject;

            if (target != null)
            {
                // 선택된 오브젝트에 NpcInteractable 추가
                if (target.GetComponent<NpcInteractable>() == null)
                {
                    EnsureNpcCollider(target);
                    Undo.AddComponent<NpcInteractable>(target);
                    Debug.Log($"[DialogueSceneSetup] '{target.name}'에 NpcInteractable 추가 완료.");
                }
                else
                {
                    Debug.Log($"[DialogueSceneSetup] '{target.name}'에 이미 NpcInteractable이 있습니다.");
                }
            }
            else
            {
                // 선택 없으면 씬 루트에 새 NPC 생성
                GameObject npcGo = new GameObject("NPC_New");
                Undo.RegisterCreatedObjectUndo(npcGo, "Create NPC");
                EnsureNpcCollider(npcGo);
                npcGo.AddComponent<NpcInteractable>();
                Selection.activeGameObject = npcGo;
                Debug.Log("[DialogueSceneSetup] 새 NPC 오브젝트 생성 완료. Inspector에서 DialogueData를 연결하세요.");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ══════════════════════════════════════════════════════
        //  내부 빌더 메서드
        // ══════════════════════════════════════════════════════

        private static GameObject EnsureDialogueCanvas()
        {
            // 이미 있으면 재사용
            var existing = GameObject.Find("DialogueCanvas");
            if (existing != null) return existing;

            GameObject go = new GameObject("DialogueCanvas");
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueCanvas");

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // 대화창은 최상위

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject BuildDialoguePanel(GameObject canvasGo)
        {
            // 기존 패널 재사용
            Transform existing = canvasGo.transform.Find("DialoguePanel");
            if (existing != null) return existing.gameObject;

            // ── DialoguePanel ──────────────────────────────────
            GameObject panel = new GameObject("DialoguePanel");
            Undo.RegisterCreatedObjectUndo(panel, "Create DialoguePanel");
            panel.transform.SetParent(canvasGo.transform, false);

            RectTransform panelRt = panel.AddComponent<RectTransform>();
            // 하단 전체 너비 고정, 높이 140
            panelRt.anchorMin        = new Vector2(0f, 0f);
            panelRt.anchorMax        = new Vector2(1f, 0f);
            panelRt.pivot            = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta        = new Vector2(0f, 140f);

            panel.AddComponent<CanvasGroup>();

            // ── PanelBg (흰 배경) ──────────────────────────────
            GameObject bg = new GameObject("PanelBg");
            bg.transform.SetParent(panel.transform, false);
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin        = Vector2.zero;
            bgRt.anchorMax        = Vector2.one;
            bgRt.offsetMin        = Vector2.zero;
            bgRt.offsetMax        = Vector2.zero;

            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.96f, 0.95f, 0.90f, 0.97f); // 크림 화이트
            // 외곽 테두리처럼 보이도록 Outline 컴포넌트 추가
            Outline outline = bg.AddComponent<Outline>();
            outline.effectColor    = new Color(0.15f, 0.15f, 0.25f, 1f);
            outline.effectDistance = new Vector2(2f, 2f);

            // ── NameBox ────────────────────────────────────────
            GameObject nameBox = new GameObject("NameBox");
            nameBox.transform.SetParent(panel.transform, false);
            RectTransform nameBoxRt = nameBox.AddComponent<RectTransform>();
            nameBoxRt.anchorMin        = new Vector2(0f, 1f);
            nameBoxRt.anchorMax        = new Vector2(0f, 1f);
            nameBoxRt.pivot            = new Vector2(0f, 0f);
            nameBoxRt.anchoredPosition = new Vector2(14f, 0f);
            nameBoxRt.sizeDelta        = new Vector2(120f, 28f);

            Image nameBoxImg = nameBox.AddComponent<Image>();
            nameBoxImg.color = new Color(0.18f, 0.38f, 0.62f, 1f); // 파란 이름 박스

            // NameText
            GameObject nameTextGo = new GameObject("NameText");
            nameTextGo.transform.SetParent(nameBox.transform, false);
            RectTransform nameTextRt = nameTextGo.AddComponent<RectTransform>();
            nameTextRt.anchorMin = Vector2.zero;
            nameTextRt.anchorMax = Vector2.one;
            nameTextRt.offsetMin = new Vector2(6f, 2f);
            nameTextRt.offsetMax = new Vector2(-6f, -2f);

            TextMeshProUGUI nameTmp = nameTextGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text      = "화자 이름";
            nameTmp.fontSize  = 14f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color     = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // ── DialogueText ───────────────────────────────────
            GameObject textGo = new GameObject("DialogueText");
            textGo.transform.SetParent(panel.transform, false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin        = Vector2.zero;
            textRt.anchorMax        = Vector2.one;
            textRt.offsetMin        = new Vector2(20f, 14f);
            textRt.offsetMax        = new Vector2(-36f, -14f);

            TextMeshProUGUI dialogueTmp = textGo.AddComponent<TextMeshProUGUI>();
            dialogueTmp.text      = "";
            dialogueTmp.fontSize  = 16f;
            dialogueTmp.color     = new Color(0.1f, 0.08f, 0.06f, 1f);
            dialogueTmp.alignment = TextAlignmentOptions.TopLeft;
            dialogueTmp.enableWordWrapping = true;

            // Febucci: TextAnimator_TMP + TypewriterByCharacter
            textGo.AddComponent<TextAnimator_TMP>();
            TypewriterByCharacter tw = textGo.AddComponent<TypewriterByCharacter>();
            tw.waitForNormalChars = 0.04f;
            tw.waitLong           = 0.5f;
            tw.waitMiddle         = 0.18f;

            // ── NextArrow (▼) ──────────────────────────────────
            GameObject arrowGo = new GameObject("NextArrow");
            arrowGo.transform.SetParent(panel.transform, false);
            RectTransform arrowRt = arrowGo.AddComponent<RectTransform>();
            arrowRt.anchorMin        = new Vector2(1f, 0f);
            arrowRt.anchorMax        = new Vector2(1f, 0f);
            arrowRt.pivot            = new Vector2(1f, 0f);
            arrowRt.anchoredPosition = new Vector2(-10f, 10f);
            arrowRt.sizeDelta        = new Vector2(20f, 20f);

            TextMeshProUGUI arrowTmp = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowTmp.text      = "▼";
            arrowTmp.fontSize  = 14f;
            arrowTmp.color     = new Color(0.15f, 0.15f, 0.25f, 1f);
            arrowTmp.alignment = TextAlignmentOptions.Center;
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
            Transform textGo   = panelGo.transform.Find("DialogueText");
            Transform arrowGo  = panelGo.transform.Find("NextArrow");

            if (nameBox != null)
                so.FindProperty("nameBoxRoot").objectReferenceValue = nameBox.gameObject;

            if (nameText != null)
                so.FindProperty("speakerNameText").objectReferenceValue =
                    nameText.GetComponent<TextMeshProUGUI>();

            if (textGo != null)
            {
                so.FindProperty("dialogueText").objectReferenceValue =
                    textGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("typewriter").objectReferenceValue =
                    textGo.GetComponent<TypewriterByCharacter>();
            }

            if (arrowGo != null)
                so.FindProperty("nextArrow").objectReferenceValue = arrowGo.gameObject;

            so.ApplyModifiedProperties();
            return view;
        }

        private static DialogueManager EnsureDialogueManager()
        {
            // 이미 씬에 있으면 반환
            DialogueManager existing = Object.FindFirstObjectByType<DialogueManager>();
            if (existing != null) return existing;

            // GameManagers 오브젝트가 있으면 그 아래에 추가
            GameObject managers = GameObject.Find("GameManagers");
            if (managers != null)
            {
                DialogueManager dm = Undo.AddComponent<DialogueManager>(managers);
                Debug.Log("[DialogueSceneSetup] DialogueManager를 GameManagers에 추가했습니다.");
                return dm;
            }

            // 없으면 새 오브젝트 생성
            GameObject go = new GameObject("DialogueManager");
            Undo.RegisterCreatedObjectUndo(go, "Create DialogueManager");
            return go.AddComponent<DialogueManager>();
        }

        private static void EnsureNpcCollider(GameObject go)
        {
            if (go.GetComponent<Collider2D>() != null) return;

            CircleCollider2D col = Undo.AddComponent<CircleCollider2D>(go);
            Vector3 scale = go.transform.lossyScale;
            float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 0.001f);
            col.radius   = 0.6f / radiusScale;
            col.offset   = new Vector2(0f, -1.35f);
            col.isTrigger = true;
        }
    }
}
#endif
