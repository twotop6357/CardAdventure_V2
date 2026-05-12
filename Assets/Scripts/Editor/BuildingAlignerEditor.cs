using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CardAdventure.Editor
{
    /// <summary>
    /// Building 레이어 오브젝트를 타일맵/스프라이트 크기에 맞게 정렬하는 에디터 윈도우.
    /// Menu: CardAdventure > Map > Building Aligner  (단축키 Ctrl+Shift+B)
    /// </summary>
    public class BuildingAlignerEditor : EditorWindow
    {
        // ─── 전역 설정 ──────────────────────────────────────────────────
        private int    targetLayerIndex = 6;
        private float  globalPaddingX;
        private float  globalPaddingY;
        private bool   globalSnapToGrid  = true;
        private bool   globalAlignCorner;

        // ─── 내부 상태 ──────────────────────────────────────────────────
        private readonly List<GameObject> buildings = new();
        private Vector2     scrollPos;
        private bool        settingsFold = true;
        private string      statusMsg    = string.Empty;
        private MessageType statusType;

        // SceneView 기즈모 색상
        private static readonly Color ColTarget   = new(0.2f, 0.85f, 1f,  0.07f);
        private static readonly Color ColOutline  = new(0.2f, 0.85f, 1f,  0.90f);
        private static readonly Color ColCollider = new(1.0f, 0.60f, 0.1f, 0.90f);

        // ─── 메뉴 / 창 ──────────────────────────────────────────────────
        [MenuItem("CardAdventure/Map/Building Aligner %#b")]
        public static void ShowWindow()
        {
            var win = GetWindow<BuildingAlignerEditor>("Building Aligner");
            win.minSize = new Vector2(360, 500);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Refresh();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ─── GUI ────────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawToolbar();
            DrawBuildingList();
            DrawStatus();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Building Aligner", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Building 레이어 오브젝트를 타일맵/스프라이트 크기에 맞춰 콜라이더를 조정하고 그리드에 정렬합니다.",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawSettings()
        {
            settingsFold = EditorGUILayout.BeginFoldoutHeaderGroup(settingsFold, "Settings");
            if (settingsFold)
            {
                EditorGUI.indentLevel++;

                // ── 레이어 선택 ──
                BuildLayerPopup();

                string layerName = LayerMask.LayerToName(targetLayerIndex);
                if (string.IsNullOrEmpty(layerName))
                {
                    EditorGUILayout.HelpBox(
                        $"Layer {targetLayerIndex} 에 이름이 없습니다. 아래 버튼으로 \"Building\" 이름을 등록할 수 있습니다.",
                        MessageType.Warning);
                    if (GUILayout.Button("  \"Building\" 레이어로 등록  "))
                        RegisterBuildingLayer();
                }

                EditorGUILayout.Space(4);

                // ── 콜라이더 패딩 ──
                globalPaddingX    = EditorGUILayout.FloatField(
                    new GUIContent("Padding X", "계산된 bounds에 추가할 가로 여백 (월드 단위)"), globalPaddingX);
                globalPaddingY    = EditorGUILayout.FloatField(
                    new GUIContent("Padding Y", "계산된 bounds에 추가할 세로 여백 (월드 단위)"), globalPaddingY);

                // ── 그리드 스냅 ──
                globalSnapToGrid  = EditorGUILayout.Toggle(
                    new GUIContent("Snap to Grid", "bounds 중앙을 가장 가까운 Grid 셀에 스냅"), globalSnapToGrid);
                globalAlignCorner = EditorGUILayout.Toggle(
                    new GUIContent("Align to Corner", "짝수 칸(2x2, 4x4 등) 건물: 셀 중앙 대신 셀 코너에 정렬"),
                    globalAlignCorner);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void BuildLayerPopup()
        {
            // 32개 레이어 전부 표시 (unnamed 포함)
            var labels  = new string[32];
            var indices = new int[32];
            for (int i = 0; i < 32; i++)
            {
                string n  = LayerMask.LayerToName(i);
                labels[i] = string.IsNullOrEmpty(n) ? $"[{i}] (unnamed)" : $"[{i}] {n}";
                indices[i] = i;
            }

            int sel = Mathf.Clamp(targetLayerIndex, 0, 31);
            sel = EditorGUILayout.Popup("Target Layer", sel, labels);
            targetLayerIndex = indices[sel];
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Height(28)))
                {
                    Refresh();
                    SetStatus($"Refresh 완료: {buildings.Count}개 건물 발견", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(buildings.Count == 0))
                {
                    if (GUILayout.Button($"Apply All  ({buildings.Count})", GUILayout.Height(28)))
                        ApplyAll();
                }
            }
            EditorGUILayout.Space(2);
        }

        private void DrawBuildingList()
        {
            string lname = LayerMask.LayerToName(targetLayerIndex);
            string header = string.IsNullOrEmpty(lname)
                ? $"Layer {targetLayerIndex} Buildings ({buildings.Count})"
                : $"Layer \"{lname}\" Buildings ({buildings.Count})";
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            if (buildings.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "해당 레이어에 오브젝트가 없습니다.\nLayer 설정을 확인하고 Refresh를 눌러주세요.",
                    MessageType.Warning);
            }
            else
            {
                for (int i = buildings.Count - 1; i >= 0; i--)
                {
                    if (buildings[i] == null) { buildings.RemoveAt(i); continue; }
                    DrawEntry(buildings[i]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(GameObject go)
        {
            bool hasConfig = go.TryGetComponent<BuildingAlignConfig>(out var config);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // ── 헤더 행 ──
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = hasConfig ? $"★ {go.name}" : go.name;
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Select", GUILayout.Width(56)))
                    {
                        Selection.activeGameObject = go;
                        SceneView.FrameLastActiveSceneView();
                    }
                    if (GUILayout.Button("Apply", GUILayout.Width(50)))
                    {
                        Apply(go);
                        SetStatus($"Applied → {go.name}", MessageType.Info);
                    }
                }

                // ── 정보 ──
                Vector3 pos = go.transform.position;
                Vector3 sc  = go.transform.localScale;
                EditorGUILayout.LabelField(
                    $"Pos: ({pos.x:F2}, {pos.y:F2})    Scale: ({sc.x:F3}, {sc.y:F3})",
                    EditorStyles.miniLabel);

                if (go.TryGetComponent<BoxCollider2D>(out var bc))
                    EditorGUILayout.LabelField(
                        $"BoxCollider2D: size({bc.size.x:F2}, {bc.size.y:F2})  off({bc.offset.x:F2}, {bc.offset.y:F2})",
                        EditorStyles.miniLabel);

                Bounds b = CalculateBounds(go, config);
                if (b.size.sqrMagnitude > 0.001f)
                    EditorGUILayout.LabelField(
                        $"Bounds: center({b.center.x:F2}, {b.center.y:F2})  size({b.size.x:F2}, {b.size.y:F2})",
                        EditorStyles.miniLabel);

                int tmCount = go.GetComponentsInChildren<Tilemap>(true).Length;
                if (tmCount > 0)
                    EditorGUILayout.LabelField($"Child Tilemaps: {tmCount}", EditorStyles.miniLabel);

                bool hasSr = go.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null;
                if (hasSr && tmCount == 0)
                    EditorGUILayout.LabelField("Source: SpriteRenderer", EditorStyles.miniLabel);

                if (hasConfig)
                    EditorGUILayout.LabelField("[BuildingAlignConfig attached]",
                        EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.Space(2);
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(statusMsg)) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(statusMsg, statusType);
        }

        // ─── 핵심 로직 ──────────────────────────────────────────────────

        private void Refresh()
        {
            buildings.Clear();
            var all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
                if (go.layer == targetLayerIndex)
                    buildings.Add(go);
        }

        private void ApplyAll()
        {
            int n = 0;
            foreach (var go in buildings)
            {
                if (go == null) continue;
                Apply(go);
                n++;
            }
            SetStatus($"Apply All 완료: {n}개 적용", MessageType.Info);
        }

        private void Apply(GameObject go)
        {
            go.TryGetComponent<BuildingAlignConfig>(out var config);

            float padX   = config != null ? config.paddingX       : globalPaddingX;
            float padY   = config != null ? config.paddingY       : globalPaddingY;
            bool  snap   = config != null ? config.snapToGrid     : globalSnapToGrid;
            bool  corner = config != null ? config.alignToCorner  : globalAlignCorner;

            // 1. Bounds 계산
            Bounds worldBounds = CalculateBounds(go, config);
            if (worldBounds.size.sqrMagnitude < 0.001f)
            {
                Debug.LogWarning($"[BuildingAligner] {go.name}: bounds 계산 실패. Tilemap/Sprite/Manual 설정 확인.");
                return;
            }

            Undo.RecordObject(go.transform, $"BuildingAligner: {go.name}");

            // 2. 중앙 → 그리드 스냅: bounds center를 Grid 셀에 맞추고 GO를 이동
            if (snap)
            {
                var grid = FindFirstObjectByType<Grid>();
                if (grid != null)
                {
                    Vector3 snappedCenter = SnapToGrid(worldBounds.center, grid, corner);
                    snappedCenter.z       = go.transform.position.z;

                    Vector3 delta     = snappedCenter - worldBounds.center;
                    go.transform.position += delta;
                    worldBounds.center    += delta;   // 갱신된 bounds로 콜라이더 계산
                }
            }

            // 3. 콜라이더 조정
            AdjustColliders(go, worldBounds, padX, padY);

            EditorUtility.SetDirty(go);
        }

        // ─── Bounds 계산 ────────────────────────────────────────────────

        /// <summary>
        /// 우선순위: IBuildingBoundsProvider → Manual → TilemapChildren → SpriteRenderer
        /// </summary>
        private static Bounds CalculateBounds(GameObject go, BuildingAlignConfig config)
        {
            // (a) 커스텀 확장: IBuildingBoundsProvider 컴포넌트
            var provider = go.GetComponent<IBuildingBoundsProvider>();
            if (provider != null)
            {
                Bounds custom = provider.GetBuildingBounds();
                if (custom.size.sqrMagnitude > 0.001f)
                    return custom;
            }

            var src = config != null ? config.boundsSource : BuildingBoundsSource.AutoDetect;

            // (b) Manual
            if (src == BuildingBoundsSource.Manual && config != null)
                return config.GetBuildingBounds();

            // (c) 자식 Tilemap 합산
            if (src == BuildingBoundsSource.AutoDetect || src == BuildingBoundsSource.TilemapChildren)
            {
                var tilemaps = go.GetComponentsInChildren<Tilemap>(true);
                if (tilemaps.Length > 0)
                {
                    Bounds tb = GetCombinedTilemapBounds(tilemaps);
                    if (tb.size.sqrMagnitude > 0.001f)
                        return tb;
                }
            }

            // (d) SpriteRenderer
            if (src == BuildingBoundsSource.AutoDetect || src == BuildingBoundsSource.SpriteRenderer)
            {
                if (go.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
                    return sr.bounds;
            }

            return default;
        }

        /// <summary>여러 Tilemap의 월드 bounds를 하나로 합산합니다.</summary>
        private static Bounds GetCombinedTilemapBounds(Tilemap[] tilemaps)
        {
            bool  init     = false;
            Bounds combined = default;

            foreach (var tm in tilemaps)
            {
                if (tm == null) continue;
                tm.CompressBounds();
                if (tm.cellBounds.size == Vector3Int.zero) continue;

                // localBounds → world (TransformPoint min/max로 음수 스케일도 처리)
                Bounds local = tm.localBounds;
                Vector3 wMin = tm.transform.TransformPoint(local.min);
                Vector3 wMax = tm.transform.TransformPoint(local.max);

                var wb = new Bounds();
                wb.SetMinMax(Vector3.Min(wMin, wMax), Vector3.Max(wMin, wMax));

                if (!init) { combined = wb; init = true; }
                else        combined.Encapsulate(wb);
            }

            return init ? combined : default;
        }

        // ─── 그리드 스냅 ────────────────────────────────────────────────

        private static Vector3 SnapToGrid(Vector3 worldPos, Grid grid, bool toCorner)
        {
            Vector3Int cell = grid.WorldToCell(worldPos);
            return toCorner
                ? grid.CellToWorld(cell)          // 셀 코너 (정수 좌표)
                : grid.GetCellCenterWorld(cell);   // 셀 중앙
        }

        // ─── 콜라이더 조정 ──────────────────────────────────────────────

        private static void AdjustColliders(GameObject go, Bounds worldBounds, float padX, float padY)
        {
            if (go.TryGetComponent<BoxCollider2D>(out var bc))
                AdjustBoxCollider2D(bc, go.transform, worldBounds, padX, padY);

            // 확장 포인트: PolygonCollider2D / CompositeCollider2D 등은 여기에 추가
        }

        private static void AdjustBoxCollider2D(
            BoxCollider2D bc, Transform owner, Bounds worldBounds, float padX, float padY)
        {
            Undo.RecordObject(bc, "BuildingAligner: BoxCollider2D");

            // lossyScale 역보정으로 world → local 크기 계산
            Vector3 ls = owner.lossyScale;
            float sx = Mathf.Abs(ls.x) > 0.001f ? Mathf.Abs(ls.x) : 1f;
            float sy = Mathf.Abs(ls.y) > 0.001f ? Mathf.Abs(ls.y) : 1f;

            // world bounds center → 로컬 offset
            Vector3 localCenter = owner.InverseTransformPoint(worldBounds.center);

            bc.offset = new Vector2(localCenter.x, localCenter.y);
            bc.size   = new Vector2(
                worldBounds.size.x / sx + padX,
                worldBounds.size.y / sy + padY);

            EditorUtility.SetDirty(bc);
        }

        // ─── SceneView 기즈모 ────────────────────────────────────────────

        private void OnSceneGUI(SceneView sv)
        {
            foreach (var go in buildings)
            {
                if (go == null) continue;

                go.TryGetComponent<BuildingAlignConfig>(out var config);
                Bounds b = CalculateBounds(go, config);
                if (b.size.sqrMagnitude < 0.001f) continue;

                // 타겟 bounds (하늘색 영역)
                Handles.DrawSolidRectangleWithOutline(BoundsToCorners(b), ColTarget, ColOutline);
                Handles.Label(
                    new Vector3(b.min.x, b.max.y + 0.1f, 0f),
                    go.name,
                    EditorStyles.miniLabel);

                // 현재 BoxCollider2D (주황색 와이어)
                if (go.TryGetComponent<BoxCollider2D>(out var bc))
                {
                    Handles.color = ColCollider;
                    Vector3 ls  = go.transform.lossyScale;
                    // offset은 로컬 → 월드
                    Vector3 wOff = go.transform.TransformVector(
                        new Vector3(bc.offset.x, bc.offset.y, 0f));
                    Vector3 center = go.transform.position + wOff;
                    Vector3 size   = new Vector3(
                        bc.size.x * Mathf.Abs(ls.x),
                        bc.size.y * Mathf.Abs(ls.y),
                        0.05f);
                    Handles.DrawWireCube(center, size);
                }
            }
        }

        private static Vector3[] BoundsToCorners(Bounds b) => new[]
        {
            new Vector3(b.min.x, b.min.y, 0f),
            new Vector3(b.max.x, b.min.y, 0f),
            new Vector3(b.max.x, b.max.y, 0f),
            new Vector3(b.min.x, b.max.y, 0f),
        };

        // ─── 유틸리티 ───────────────────────────────────────────────────

        /// <summary>TagManager에서 Layer 6을 "Building"으로 등록합니다.</summary>
        private static void RegisterBuildingLayer()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null) { Debug.LogError("[BuildingAligner] TagManager layers 프로퍼티를 찾을 수 없습니다."); return; }

            var elem = layersProp.GetArrayElementAtIndex(6);
            if (!string.IsNullOrEmpty(elem.stringValue))
            {
                Debug.LogWarning($"[BuildingAligner] Layer 6이 이미 \"{elem.stringValue}\"로 등록되어 있습니다.");
                return;
            }

            elem.stringValue = "Building";
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildingAligner] Layer 6 → \"Building\" 등록 완료. Unity를 재시작하거나 레이어 목록을 새로고침하세요.");
        }

        private void SetStatus(string msg, MessageType type)
        {
            statusMsg  = msg;
            statusType = type;
            Repaint();
        }
    }
}
