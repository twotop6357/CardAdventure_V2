using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// JobSelection UI 프리팹을 자동 생성하는 에디터 도구.
    ///
    /// 메뉴: CardAdventure/Setup Job Selection UI
    ///
    /// 생성되는 계층 구조:
    ///   JobSelectionCanvas (Canvas, CanvasScaler 1280x720, GraphicRaycaster)
    ///     └─ Blocker (Image, 반투명 오버레이, 전체 화면 스트레치)
    ///          └─ JobSelectionPanel (CanvasGroup + Image, 720x500 중앙 정렬)
    ///               └─ PanelInner (VerticalLayoutGroup — 내부 채움)
    ///                    ├─ TitleBox  ("직업 선택")
    ///                    ├─ ContentArea (HorizontalLayoutGroup, 직업 목록 + 미리보기/스탯)
    ///                    └─ BottomArea  (HorizontalLayoutGroup, 설명 + 버튼)
    /// </summary>
    public static class JobSelectionSetup
    {
        // ── 경로 ───────────────────────────────────────────────
        private const string PREFAB_DIR  = "Assets/Prefabs/UI";
        private const string PREFAB_PATH = "Assets/Prefabs/UI/JobSelection.prefab";
        private const string FONT_PATH   = "Assets/Fonts/MaruMinyaHangul SDF.asset";

        // ── 색상 ───────────────────────────────────────────────
        // 기준 이미지 스타일: 검은 창 + 금색 테두리 + 흰 텍스트
        private static readonly Color32 ColBorder   = new Color32(219, 191, 20, 255);
        private static readonly Color32 ColFill     = new Color32(4, 3, 3, 245);
        private static readonly Color32 ColOverlay  = new Color32(0,   0,   0,   140);
        private static readonly Color32 ColText     = new Color32(245, 245, 235, 255);
        private static readonly Color32 ColTransp   = new Color32(0,   0,   0,   0);

        // ── 폰트 크기 ──────────────────────────────────────────
        private const float FS_TITLE = 26f;
        private const float FS_JOB   = 21f;
        private const float FS_STAT  = 17f;
        private const float FS_DESC  = 17f;
        private const float FS_BTN   = 21f;

        // ── 직업 이름 (이미지 기준 4종) ────────────────────────
        private static readonly string[] JOB_NAMES = { "전사", "마법사", "궁수", "도적" };

        // ══════════════════════════════════════════════════════
        //  메뉴 진입점
        // ══════════════════════════════════════════════════════

        // ── 기본 직업 에셋 데이터 ──────────────────────────────
        private struct JobPreset
        {
            public string   name;
            public string   desc;
            public CardClass cls;
            public int atk, def, mag, spd, hp;
        }

        private static readonly JobPreset[] JOB_PRESETS =
        {
            new JobPreset { name = "전사",  cls = CardClass.Warrior, atk = 4, def = 3, mag = 1, spd = 3, hp = 60,
                desc = "검을 사용한 근접 전투에 특화된 직업.\n강인한 체력과 높은 방어력을 바탕으로 전선에서 싸운다." },
            new JobPreset { name = "마법사", cls = CardClass.Mage,    atk = 2, def = 1, mag = 5, spd = 2, hp = 40,
                desc = "마법을 구사하는 원거리 전투 직업.\n낮은 체력이지만 압도적인 마법 화력을 자랑한다." },
            new JobPreset { name = "궁수",  cls = CardClass.Archer,  atk = 3, def = 2, mag = 2, spd = 5, hp = 45,
                desc = "빠른 속도와 활을 이용한 원거리 공격 직업.\n민첩성을 살려 적의 빈틈을 노린다." },
            new JobPreset { name = "도적",  cls = CardClass.Rogue,   atk = 3, def = 2, mag = 1, spd = 4, hp = 50,
                desc = "재빠른 몸놀림과 독을 이용한 공격 직업.\n치명적인 일격으로 적을 처리한다." },
        };

        [MenuItem("CardAdventure/Create Default Job Assets")]
        public static void CreateDefaultJobAssets()
        {
            const string dir = "Assets/ScriptableObjects/Jobs";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            foreach (var preset in JOB_PRESETS)
            {
                string path = $"{dir}/Job_{preset.cls}.asset";
                JobClassInfo existing = AssetDatabase.LoadAssetAtPath<JobClassInfo>(path);
                if (existing != null)
                {
                    Debug.Log($"[JobSelectionSetup] 이미 존재: {path} (건너뜀)");
                    continue;
                }

                JobClassInfo asset = ScriptableObject.CreateInstance<JobClassInfo>();
                asset.displayName  = preset.name;
                asset.description  = preset.desc;
                asset.cardClass    = preset.cls;
                asset.attackStars  = preset.atk;
                asset.defenseStars = preset.def;
                asset.magicStars   = preset.mag;
                asset.difficulty   = preset.spd;
                asset.baseMaxHp    = preset.hp;
                asset.baseMaxEnergy = 3;

                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[JobSelectionSetup] ✅ 직업 에셋 생성: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[JobSelectionSetup] 기본 직업 에셋 생성 완료.");
        }

        [MenuItem("CardAdventure/Setup Job Selection UI")]
        public static void Build()
        {
            // 1. 폰트 로드
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                Debug.LogWarning(
                    $"[JobSelectionSetup] 한국어 폰트({FONT_PATH})를 찾지 못했습니다. " +
                    "기본 폰트로 진행합니다. 먼저 'CardAdventure > Setup Korean Font'를 실행하세요.");
            }

            // 2. 출력 디렉토리 생성
            if (!Directory.Exists(PREFAB_DIR))
            {
                Directory.CreateDirectory(PREFAB_DIR);
                AssetDatabase.Refresh();
            }

            // 3. 계층 구조 생성 및 참조 수집
            GameObject root = BuildHierarchy(font,
                out CanvasGroup             cg,
                out RectTransform           panelRt,
                out List<TextMeshProUGUI>   cursorTexts,
                out List<TextMeshProUGUI>   labelTexts,
                out Image                   previewImage,
                out TextMeshProUGUI         attackStars,
                out TextMeshProUGUI         defenseStars,
                out TextMeshProUGUI         magicStars,
                out TextMeshProUGUI         speedStars,
                out TextMeshProUGUI         jobNameText,
                out TextMeshProUGUI         descText,
                out TextMeshProUGUI         confirmCursor,
                out TextMeshProUGUI         cancelCursor);

            // 4. JobSelectionUI 컴포넌트 연결
            WireJobSelectionUI(root, cg, panelRt,
                cursorTexts, labelTexts, previewImage,
                attackStars, defenseStars, magicStars, speedStars,
                jobNameText, descText, confirmCursor, cancelCursor);

            // 5. 프리팹 저장
            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            string action = existed ? "갱신" : "생성";
            Debug.Log($"[JobSelectionSetup] ✅ JobSelection UI 프리팹 {action} 완료: {PREFAB_PATH}");
            EditorGUIUtility.PingObject(saved);
        }

        // ══════════════════════════════════════════════════════
        //  계층 구조 빌드
        // ══════════════════════════════════════════════════════

        private static GameObject BuildHierarchy(TMP_FontAsset font,
            out CanvasGroup             canvasGroup,
            out RectTransform           panelRoot,
            out List<TextMeshProUGUI>   cursorTexts,
            out List<TextMeshProUGUI>   labelTexts,
            out Image                   previewImage,
            out TextMeshProUGUI         attackStars,
            out TextMeshProUGUI         defenseStars,
            out TextMeshProUGUI         magicStars,
            out TextMeshProUGUI         speedStars,
            out TextMeshProUGUI         jobNameText,
            out TextMeshProUGUI         descText,
            out TextMeshProUGUI         confirmCursor,
            out TextMeshProUGUI         cancelCursor)
        {
            // ── Canvas ────────────────────────────────────────
            GameObject canvasGo = new GameObject("JobSelectionCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1280f, 720f);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Blocker (반투명 오버레이, 전체화면) ─────────────
            GameObject blocker = CreateImage(canvasGo.transform, "Blocker", ColOverlay);
            StretchFull(blocker.GetComponent<RectTransform>());

            // ── JobSelectionPanel (외곽 테두리, 720x500 중앙) ───
            GameObject panelOuter = CreateImage(blocker.transform, "JobSelectionPanel", ColBorder);
            RectTransform panelOuterRt = panelOuter.GetComponent<RectTransform>();
            SetAnchoredCenter(panelOuterRt, 0f, 0f, 720f, 500f);
            panelRoot    = panelOuterRt;
            canvasGroup  = panelOuter.AddComponent<CanvasGroup>();

            // ── PanelInner (내부 채움 + VerticalLayoutGroup) ──
            GameObject panelInner = CreateImage(panelOuter.transform, "PanelInner", ColFill);
            StretchWithOffset(panelInner.GetComponent<RectTransform>(), 4, 4, 4, 4);

            VerticalLayoutGroup mainVlg = panelInner.AddComponent<VerticalLayoutGroup>();
            mainVlg.padding               = new RectOffset(12, 12, 10, 10);
            mainVlg.spacing               = 8f;
            mainVlg.childForceExpandWidth  = true;
            mainVlg.childForceExpandHeight = false;
            mainVlg.childAlignment         = TextAnchor.UpperCenter;
            mainVlg.childControlWidth      = true;
            mainVlg.childControlHeight     = false;

            // ── 제목 박스 ──────────────────────────────────────
            BuildTitleBox(panelInner.transform, font);

            // ── 콘텐츠 영역 ────────────────────────────────────
            BuildContentArea(panelInner.transform, font,
                out cursorTexts, out labelTexts,
                out previewImage,
                out attackStars, out defenseStars, out magicStars, out speedStars);

            // ── 하단 영역 ──────────────────────────────────────
            BuildBottomArea(panelInner.transform, font,
                out jobNameText, out descText,
                out confirmCursor, out cancelCursor);

            return canvasGo;
        }

        // ── 제목 박스 ──────────────────────────────────────────

        private static void BuildTitleBox(Transform parent, TMP_FontAsset font)
        {
            // 외곽 테두리
            GameObject outer = CreateImage(parent, "TitleBox", ColBorder);
            LayoutElement outerLe = outer.AddComponent<LayoutElement>();
            outerLe.minHeight      = 46f;
            outerLe.preferredHeight = 46f;
            outerLe.flexibleWidth  = 1f;

            // 내부 채움
            GameObject inner = CreateImage(outer.transform, "TitleBox_Inner", ColFill);
            StretchWithOffset(inner.GetComponent<RectTransform>(), 3, 3, 3, 3);

            HorizontalLayoutGroup hlg = inner.AddComponent<HorizontalLayoutGroup>();
            hlg.padding              = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment       = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;

            // 제목 텍스트
            GameObject titleGo = MakeTmp(inner.transform, "TitleText",
                "직업 선택", font, FS_TITLE, TextAlignmentOptions.Center, ColText);
            titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            LayoutElement titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.flexibleWidth  = 1f;
            titleLe.flexibleHeight = 1f;
        }

        // ── 콘텐츠 영역 (직업 목록 | 미리보기 + 능력치) ──────────

        private static void BuildContentArea(Transform parent, TMP_FontAsset font,
            out List<TextMeshProUGUI> cursorTexts,
            out List<TextMeshProUGUI> labelTexts,
            out Image previewImage,
            out TextMeshProUGUI attackStars,
            out TextMeshProUGUI defenseStars,
            out TextMeshProUGUI magicStars,
            out TextMeshProUGUI speedStars)
        {
            GameObject area = new GameObject("ContentArea");
            area.transform.SetParent(parent, false);
            LayoutElement areaLe = area.AddComponent<LayoutElement>();
            areaLe.minHeight      = 262f;
            areaLe.preferredHeight = 262f;
            areaLe.flexibleWidth  = 1f;

            HorizontalLayoutGroup hlg = area.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 8f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment         = TextAnchor.UpperLeft;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = true;

            // ── 왼쪽: 직업 목록 ────────────────────────────────
            BuildLeftJobList(area.transform, font, out cursorTexts, out labelTexts);

            // ── 오른쪽: 미리보기 + 능력치 ──────────────────────
            BuildRightPreviewStats(area.transform, font,
                out previewImage,
                out attackStars, out defenseStars, out magicStars, out speedStars);
        }

        private static void BuildLeftJobList(Transform parent, TMP_FontAsset font,
            out List<TextMeshProUGUI> cursorTexts,
            out List<TextMeshProUGUI> labelTexts)
        {
            cursorTexts = new List<TextMeshProUGUI>();
            labelTexts  = new List<TextMeshProUGUI>();

            // 외곽 테두리
            GameObject outer = CreateImage(parent, "LeftPanel", ColBorder);
            LayoutElement outerLe = outer.AddComponent<LayoutElement>();
            outerLe.minWidth      = 220f;
            outerLe.preferredWidth = 220f;
            outerLe.flexibleHeight = 1f;

            // 내부 채움
            GameObject inner = CreateImage(outer.transform, "LeftPanel_Inner", ColFill);
            StretchWithOffset(inner.GetComponent<RectTransform>(), 3, 3, 3, 3);

            VerticalLayoutGroup vlg = inner.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(12, 8, 16, 8);
            vlg.spacing               = 12f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;

            foreach (string jobName in JOB_NAMES)
            {
                BuildJobRow(inner.transform, jobName, font,
                    out TextMeshProUGUI cursor,
                    out TextMeshProUGUI label);
                cursorTexts.Add(cursor);
                labelTexts.Add(label);
            }
        }

        private static void BuildJobRow(Transform parent, string jobName, TMP_FontAsset font,
            out TextMeshProUGUI cursor, out TextMeshProUGUI label)
        {
            GameObject row = new GameObject($"JobRow_{jobName}");
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 4f;
            hlg.childAlignment        = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = false;

            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight      = 32f;
            rowLe.preferredHeight = 32f;
            rowLe.flexibleWidth  = 1f;

            // 커서 ( ▶ 또는 공백 )
            GameObject cursorGo = MakeTmp(row.transform, "Cursor", "  ", font,
                FS_JOB, TextAlignmentOptions.Left, ColText);
            cursor = cursorGo.GetComponent<TextMeshProUGUI>();
            LayoutElement cursorLe = cursorGo.AddComponent<LayoutElement>();
            cursorLe.minWidth      = 22f;
            cursorLe.preferredWidth = 22f;

            // 직업 이름
            GameObject labelGo = MakeTmp(row.transform, "Label", jobName, font,
                FS_JOB, TextAlignmentOptions.Left, ColText);
            label = labelGo.GetComponent<TextMeshProUGUI>();
            LayoutElement labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
        }

        private static void BuildRightPreviewStats(Transform parent, TMP_FontAsset font,
            out Image previewImage,
            out TextMeshProUGUI attackStars,
            out TextMeshProUGUI defenseStars,
            out TextMeshProUGUI magicStars,
            out TextMeshProUGUI speedStars)
        {
            // 외곽 테두리
            GameObject outer = CreateImage(parent, "RightPanel", ColBorder);
            LayoutElement outerLe = outer.AddComponent<LayoutElement>();
            outerLe.flexibleWidth  = 1f;
            outerLe.flexibleHeight = 1f;

            // 내부 채움
            GameObject inner = CreateImage(outer.transform, "RightPanel_Inner", ColFill);
            StretchWithOffset(inner.GetComponent<RectTransform>(), 3, 3, 3, 3);

            HorizontalLayoutGroup hlg = inner.AddComponent<HorizontalLayoutGroup>();
            hlg.padding               = new RectOffset(14, 14, 14, 14);
            hlg.spacing               = 14f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = true;

            // ── 캐릭터 미리보기 이미지 ──
            GameObject previewGo = new GameObject("CharacterPreview");
            previewGo.transform.SetParent(inner.transform, false);
            previewImage                  = previewGo.AddComponent<Image>();
            previewImage.color            = Color.white;
            previewImage.preserveAspect   = true;
            LayoutElement previewLe = previewGo.AddComponent<LayoutElement>();
            previewLe.minWidth      = 110f;
            previewLe.preferredWidth = 110f;
            previewLe.flexibleHeight = 1f;

            // ── 능력치 스탯 그리드 ─────────────────────────────
            GameObject statsGo = new GameObject("StatsGrid");
            statsGo.transform.SetParent(inner.transform, false);
            LayoutElement statsLe = statsGo.AddComponent<LayoutElement>();
            statsLe.flexibleWidth  = 1f;
            statsLe.flexibleHeight = 1f;

            GridLayoutGroup grid = statsGo.AddComponent<GridLayoutGroup>();
            grid.cellSize        = new Vector2(118f, 28f);
            grid.spacing         = new Vector2(6f, 10f);
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.padding         = new RectOffset(4, 4, 20, 4);
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.UpperLeft;

            // 능력치 4종: 레이블 + ■□ 텍스트 (각 2개 셀 점유)
            attackStars  = BuildStatEntry(statsGo.transform, "공격",  font, "■■■■□");
            defenseStars = BuildStatEntry(statsGo.transform, "방어",  font, "■■■□□");
            magicStars   = BuildStatEntry(statsGo.transform, "마법",  font, "■□□□□");
            speedStars   = BuildStatEntry(statsGo.transform, "속도",  font, "■■■□□");
        }

        private static TextMeshProUGUI BuildStatEntry(Transform parent, string statName,
            TMP_FontAsset font, string defaultStars)
        {
            // 레이블 셀
            MakeTmp(parent, $"Stat_{statName}_Label", statName, font,
                FS_STAT, TextAlignmentOptions.Left, ColText);

            // 별 텍스트 셀
            GameObject starsGo = MakeTmp(parent, $"Stat_{statName}_Stars", defaultStars, font,
                FS_STAT, TextAlignmentOptions.Left, ColText);
            return starsGo.GetComponent<TextMeshProUGUI>();
        }

        // ── 하단 영역 (설명 | 버튼) ──────────────────────────────

        private static void BuildBottomArea(Transform parent, TMP_FontAsset font,
            out TextMeshProUGUI jobNameText,
            out TextMeshProUGUI descText,
            out TextMeshProUGUI confirmCursor,
            out TextMeshProUGUI cancelCursor)
        {
            GameObject area = new GameObject("BottomArea");
            area.transform.SetParent(parent, false);
            LayoutElement areaLe = area.AddComponent<LayoutElement>();
            areaLe.minHeight      = 112f;
            areaLe.preferredHeight = 112f;
            areaLe.flexibleWidth  = 1f;

            HorizontalLayoutGroup hlg = area.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 8f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment         = TextAnchor.UpperLeft;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = true;

            // ── 설명 박스 ──────────────────────────────────────
            BuildDescriptionBox(area.transform, font, out jobNameText, out descText);

            // ── 버튼 박스 ──────────────────────────────────────
            BuildButtonBox(area.transform, font, out confirmCursor, out cancelCursor);
        }

        private static void BuildDescriptionBox(Transform parent, TMP_FontAsset font,
            out TextMeshProUGUI jobNameText, out TextMeshProUGUI descText)
        {
            // 외곽 테두리
            GameObject outer = CreateImage(parent, "DescriptionBox", ColBorder);
            LayoutElement outerLe = outer.AddComponent<LayoutElement>();
            outerLe.flexibleWidth  = 3f;
            outerLe.flexibleHeight = 1f;

            // 내부 채움
            GameObject inner = CreateImage(outer.transform, "DescriptionBox_Inner", ColFill);
            StretchWithOffset(inner.GetComponent<RectTransform>(), 3, 3, 3, 3);

            VerticalLayoutGroup vlg = inner.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(12, 8, 10, 8);
            vlg.spacing               = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;

            // 직업 이름
            GameObject nameGo = MakeTmp(inner.transform, "JobNameText", "전사", font,
                FS_DESC, TextAlignmentOptions.TopLeft, ColText);
            nameGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            LayoutElement nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.minHeight      = 26f;
            nameLe.preferredHeight = 26f;
            nameLe.flexibleWidth  = 1f;
            jobNameText = nameGo.GetComponent<TextMeshProUGUI>();

            // 직업 설명
            GameObject descGo = MakeTmp(inner.transform, "DescriptionText",
                "직업 설명이 여기에 표시됩니다.", font, FS_DESC, TextAlignmentOptions.TopLeft, ColText);
            descGo.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
            LayoutElement descLe = descGo.AddComponent<LayoutElement>();
            descLe.flexibleWidth  = 1f;
            descLe.flexibleHeight = 1f;
            descText = descGo.GetComponent<TextMeshProUGUI>();
        }

        private static void BuildButtonBox(Transform parent, TMP_FontAsset font,
            out TextMeshProUGUI confirmCursor, out TextMeshProUGUI cancelCursor)
        {
            // 외곽 테두리
            GameObject outer = CreateImage(parent, "ButtonBox", ColBorder);
            LayoutElement outerLe = outer.AddComponent<LayoutElement>();
            outerLe.minWidth      = 152f;
            outerLe.preferredWidth = 152f;
            outerLe.flexibleHeight = 1f;

            // 내부 채움
            GameObject inner = CreateImage(outer.transform, "ButtonBox_Inner", ColFill);
            StretchWithOffset(inner.GetComponent<RectTransform>(), 3, 3, 3, 3);

            VerticalLayoutGroup vlg = inner.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(12, 8, 20, 8);
            vlg.spacing               = 14f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;

            BuildButtonRow(inner.transform, "ConfirmRow", "결정", font, out confirmCursor);
            BuildButtonRow(inner.transform, "CancelRow",  "취소", font, out cancelCursor);
        }

        private static void BuildButtonRow(Transform parent, string rowName, string btnText,
            TMP_FontAsset font, out TextMeshProUGUI cursor)
        {
            GameObject row = new GameObject(rowName);
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 4f;
            hlg.childAlignment        = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = false;

            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight      = 32f;
            rowLe.preferredHeight = 32f;
            rowLe.flexibleWidth  = 1f;

            // 커서
            GameObject cursorGo = MakeTmp(row.transform, "Cursor", "  ", font,
                FS_BTN, TextAlignmentOptions.Left, ColText);
            cursor = cursorGo.GetComponent<TextMeshProUGUI>();
            LayoutElement cursorLe = cursorGo.AddComponent<LayoutElement>();
            cursorLe.minWidth      = 22f;
            cursorLe.preferredWidth = 22f;

            // 버튼 레이블
            GameObject labelGo = MakeTmp(row.transform, "Label", btnText, font,
                FS_BTN, TextAlignmentOptions.Left, ColText);
            LayoutElement labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
        }

        // ══════════════════════════════════════════════════════
        //  JobSelectionUI 컴포넌트 연결
        // ══════════════════════════════════════════════════════

        private static void WireJobSelectionUI(
            GameObject  canvasRoot,
            CanvasGroup canvasGroup,
            RectTransform panelRoot,
            List<TextMeshProUGUI> cursorTexts,
            List<TextMeshProUGUI> labelTexts,
            Image previewImage,
            TextMeshProUGUI attackStars,
            TextMeshProUGUI defenseStars,
            TextMeshProUGUI magicStars,
            TextMeshProUGUI speedStars,
            TextMeshProUGUI jobNameText,
            TextMeshProUGUI descText,
            TextMeshProUGUI confirmCursor,
            TextMeshProUGUI cancelCursor)
        {
            // JobSelectionUI는 JobSelectionPanel에 붙인다
            GameObject panelGo = panelRoot.gameObject;
            JobSelectionUI ui  = panelGo.AddComponent<JobSelectionUI>();

            SerializedObject so = new SerializedObject(ui);

            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("panelRoot").objectReferenceValue   = panelRoot;

            // 직업 커서 리스트
            SetSerializedList(so.FindProperty("jobCursorTexts"), cursorTexts);
            // 직업 레이블 리스트
            SetSerializedList(so.FindProperty("jobLabelTexts"), labelTexts);

            so.FindProperty("characterPreviewImage").objectReferenceValue = previewImage;
            so.FindProperty("attackStarsText").objectReferenceValue       = attackStars;
            so.FindProperty("defenseStarsText").objectReferenceValue      = defenseStars;
            so.FindProperty("magicStarsText").objectReferenceValue        = magicStars;
            so.FindProperty("speedStarsText").objectReferenceValue        = speedStars;
            so.FindProperty("jobNameInDescription").objectReferenceValue  = jobNameText;
            so.FindProperty("descriptionText").objectReferenceValue       = descText;
            so.FindProperty("confirmCursorText").objectReferenceValue     = confirmCursor;
            so.FindProperty("cancelCursorText").objectReferenceValue      = cancelCursor;

            so.ApplyModifiedProperties();
        }

        private static void SetSerializedList(SerializedProperty prop,
            List<TextMeshProUGUI> items)
        {
            prop.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        // ══════════════════════════════════════════════════════
        //  헬퍼 메서드
        // ══════════════════════════════════════════════════════

        /// <summary>Image 컴포넌트가 달린 새 GameObject를 생성한다.</summary>
        private static GameObject CreateImage(Transform parent, string name, Color32 color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        /// <summary>TextMeshProUGUI 컴포넌트가 달린 새 GameObject를 생성한다.</summary>
        private static GameObject MakeTmp(Transform parent, string name, string text,
            TMP_FontAsset font, float fontSize,
            TextAlignmentOptions alignment, Color32 color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = fontSize;
            tmp.alignment          = alignment;
            tmp.color              = color;
            tmp.overflowMode       = TextOverflowModes.Ellipsis;
            tmp.enableWordWrapping = false;
            if (font != null) tmp.font = font;
            return go;
        }

        /// <summary>RectTransform을 부모에 전체 스트레치로 설정한다.</summary>
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>RectTransform을 지정 오프셋만큼 안쪽으로 스트레치한다.</summary>
        private static void StretchWithOffset(RectTransform rt,
            float left, float right, float bottom, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>RectTransform을 중앙 앵커로 고정 크기 설정한다.</summary>
        private static void SetAnchoredCenter(RectTransform rt,
            float x, float y, float width, float height)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(width, height);
        }
    }
}
