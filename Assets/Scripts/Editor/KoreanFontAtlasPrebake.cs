#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CardAdventure.Editor
{
    /// <summary>
    /// MaruMinyaHangul SDF 폰트의 Dynamic Atlas에 한글 음절(가-힣) 전체와 ASCII, 자모, 자주 쓰는 기호를
    /// 사전 등록해 런타임 글리프 누락으로 인한 한글 깨짐을 방지한다.
    /// 메뉴: CardAdventure/UI/Prebake Korean Glyphs
    /// </summary>
    public static class KoreanFontAtlasPrebake
    {
        private const string FontAssetPath = "Assets/Fonts/MaruMinyaHangul SDF.asset";

        [MenuItem("CardAdventure/UI/Prebake Korean Glyphs")]
        public static void Prebake()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                Debug.LogError($"[KoreanFontAtlasPrebake] 폰트 에셋이 없습니다: {FontAssetPath}");
                return;
            }

            StringBuilder sb = new StringBuilder(12000);
            // ASCII 0x20 ~ 0x7E
            for (int i = 0x20; i <= 0x7E; i++) sb.Append((char)i);
            // Hangul Syllables (가 ~ 힣)
            for (int i = 0xAC00; i <= 0xD7A3; i++) sb.Append((char)i);
            // Hangul Compatibility Jamo
            for (int i = 0x3130; i <= 0x318F; i++) sb.Append((char)i);
            // Hangul Jamo
            for (int i = 0x1100; i <= 0x11FF; i++) sb.Append((char)i);
            // 자주 쓰는 기호 (■□★☆ 등)
            sb.Append("■□▶◀▲▼◆●○→←↑↓·…—–★☆♥♦♠♣⊕⊗℃℉°×÷±≠≤≥√√∞〈〉《》「」『』【】〔〕");

            string allChars = sb.ToString();

            int charBefore = font.characterTable?.Count ?? 0;
            int glyphBefore = font.glyphTable?.Count ?? 0;

            string missing;
            bool addedAll = font.TryAddCharacters(allChars, out missing);

            int charAfter = font.characterTable?.Count ?? 0;
            int glyphAfter = font.glyphTable?.Count ?? 0;

            EditorUtility.SetDirty(font);
            if (font.atlasTextures != null)
            {
                foreach (Texture2D t in font.atlasTextures)
                {
                    if (t != null) EditorUtility.SetDirty(t);
                }
            }
            if (font.material != null) EditorUtility.SetDirty(font.material);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);

            int missingCount = string.IsNullOrEmpty(missing) ? 0 : missing.Length;
            Debug.Log(
                $"[KoreanFontAtlasPrebake] 한글/ASCII 글리프 사전 등록 완료. " +
                $"요청 문자 {allChars.Length}자, 추가 후 character {charAfter} (이전 {charBefore}), " +
                $"glyph {glyphAfter} (이전 {glyphBefore}), 누락 {missingCount}자, 전부 추가됨={addedAll}");
        }
    }
}
#endif
