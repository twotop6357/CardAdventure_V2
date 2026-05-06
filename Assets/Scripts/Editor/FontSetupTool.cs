using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// MaruMinyaHangul TMP Font Asset 생성 + 프로젝트 전체 적용 도구.
/// 메뉴: CardAdventure > Setup Korean Font
/// </summary>
public static class FontSetupTool
{
    private const string FONT_PATH       = "Assets/Fonts/MaruMinyaHangul.ttf";
    private const string FONT_ASSET_DIR  = "Assets/Fonts";
    private const string FONT_ASSET_PATH = "Assets/Fonts/MaruMinyaHangul SDF.asset";
    private const string TMP_SETTINGS_PATH =
        "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    // Dynamic Atlas 설정 — 한글은 Dynamic이 필수
    private const int SAMPLING_SIZE = 90;
    private const int PADDING       = 9;
    private const int ATLAS_W       = 2048;
    private const int ATLAS_H       = 2048;

    [MenuItem("CardAdventure/Setup Korean Font")]
    public static void SetupKoreanFont()
    {
        // ── 1. Font Asset 생성 ────────────────────────────────────
        TMP_FontAsset fontAsset = EnsureFontAsset();
        if (fontAsset == null)
        {
            Debug.LogError("[FontSetupTool] Font Asset 생성 실패. 종료.");
            return;
        }

        // ── 2. TMP Settings 기본 폰트 교체 ───────────────────────
        ApplyToTMPSettings(fontAsset);

        // ── 3. 열려 있는 씬의 모든 TMP 컴포넌트 교체 ─────────────
        int sceneCount = ApplyToOpenScenes(fontAsset);

        // ── 4. 프리팹(Assets/Prefabs/) TMP 컴포넌트 교체 ─────────
        int prefabCount = ApplyToPrefabs(fontAsset);

        // ── 5. 저장 ───────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FontSetupTool] ✅ 완료! " +
                  $"씬 TMP {sceneCount}개, 프리팹 TMP {prefabCount}개 교체.");
    }

    // ══════════════════════════════════════════════════════════════
    // Font Asset 생성
    // ══════════════════════════════════════════════════════════════

    private static TMP_FontAsset EnsureFontAsset()
    {
        // 이미 있으면 재사용
        TMP_FontAsset existing =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (existing != null)
        {
            Debug.Log("[FontSetupTool] 기존 Font Asset 재사용: " + FONT_ASSET_PATH);
            return existing;
        }

        // TTF 로드
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FONT_PATH);
        if (font == null)
        {
            Debug.LogError("[FontSetupTool] 폰트 파일을 찾을 수 없습니다: " + FONT_PATH);
            return null;
        }

        // Dynamic SDF Font Asset 생성
        // AtlasPopulationMode.Dynamic = 런타임에 글리프를 온디맨드 생성
        // → 한글 11,172자 전체를 미리 구울 필요 없음
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            SAMPLING_SIZE,
            PADDING,
            GlyphRenderMode.SDFAA,
            ATLAS_W,
            ATLAS_H,
            TMPro.AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true   // 글리프가 많아지면 추가 Atlas 자동 생성
        );

        if (fontAsset == null)
        {
            Debug.LogError("[FontSetupTool] TMP_FontAsset.CreateFontAsset 실패.");
            return null;
        }

        fontAsset.name = "MaruMinyaHangul SDF";

        // 폴더 확인
        if (!AssetDatabase.IsValidFolder(FONT_ASSET_DIR))
            AssetDatabase.CreateFolder("Assets", "Fonts");

        AssetDatabase.CreateAsset(fontAsset, FONT_ASSET_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log("[FontSetupTool] Font Asset 생성 완료: " + FONT_ASSET_PATH);
        return fontAsset;
    }

    // ══════════════════════════════════════════════════════════════
    // TMP Settings 기본 폰트 교체
    // ══════════════════════════════════════════════════════════════

    private static void ApplyToTMPSettings(TMP_FontAsset fontAsset)
    {
        TMP_Settings settings =
            AssetDatabase.LoadAssetAtPath<TMP_Settings>(TMP_SETTINGS_PATH);

        if (settings == null)
        {
            // Resources 폴더에서 로드 시도
            settings = Resources.Load<TMP_Settings>("TMP Settings");
        }

        if (settings == null)
        {
            Debug.LogWarning("[FontSetupTool] TMP Settings를 찾을 수 없습니다. " +
                             "기본 폰트 교체를 건너뜁니다.");
            return;
        }

        SerializedObject so = new SerializedObject(settings);

        // 기본 폰트 교체
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;

        // 한글 줄바꿈 규칙 — 현대 한글 규칙 사용
        so.FindProperty("m_UseModernHangulLineBreakingRules").boolValue = true;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);

        Debug.Log("[FontSetupTool] TMP Settings 기본 폰트 → MaruMinyaHangul SDF, " +
                  "한글 줄바꿈 규칙 활성화.");
    }

    // ══════════════════════════════════════════════════════════════
    // 열린 씬 전체 적용
    // ══════════════════════════════════════════════════════════════

    private static int ApplyToOpenScenes(TMP_FontAsset fontAsset)
    {
        int total = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded) continue;

            int count = ApplyToScene(scene, fontAsset);
            total += count;

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        return total;
    }

    private static int ApplyToScene(Scene scene, TMP_FontAsset fontAsset)
    {
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            count += ApplyToHierarchy(root, fontAsset);
        }

        return count;
    }

    // ══════════════════════════════════════════════════════════════
    // 프리팹 전체 적용
    // ══════════════════════════════════════════════════════════════

    private static int ApplyToPrefabs(TMP_FontAsset fontAsset)
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

        foreach (string guid in guids)
        {
            string path     = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            if (root == null) continue;

            int count = ApplyToHierarchy(root, fontAsset);

            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                total += count;
                Debug.Log($"[FontSetupTool] {path} — TMP {count}개 교체.");
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        return total;
    }

    // ══════════════════════════════════════════════════════════════
    // 계층 순회 — TMP 컴포넌트 폰트 교체
    // ══════════════════════════════════════════════════════════════

    private static int ApplyToHierarchy(GameObject root, TMP_FontAsset fontAsset)
    {
        int count = 0;

        // TextMeshProUGUI (Canvas)
        TextMeshProUGUI[] uguiTexts =
            root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
        foreach (TextMeshProUGUI tmp in uguiTexts)
        {
            if (tmp.font == fontAsset) continue;
            Undo.RecordObject(tmp, "Apply Korean Font");
            tmp.font = fontAsset;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        // TextMeshPro (3D World)
        TextMeshPro[] worldTexts =
            root.GetComponentsInChildren<TextMeshPro>(includeInactive: true);
        foreach (TextMeshPro tmp in worldTexts)
        {
            if (tmp.font == fontAsset) continue;
            Undo.RecordObject(tmp, "Apply Korean Font");
            tmp.font = fontAsset;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        return count;
    }
}
