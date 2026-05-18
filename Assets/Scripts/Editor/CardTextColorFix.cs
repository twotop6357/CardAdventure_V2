using UnityEditor;
using UnityEngine;
using TMPro;

namespace CardAdventure
{
    public static class CardTextColorFix
    {
        [MenuItem("CardAdventure/Fix/Apply Card Text Colors")]
        public static void ApplyCardTextColors()
        {
            string[] prefabPaths = {
                "Assets/Prefabs/UI/Card.prefab",
                "Assets/Prefabs/UI/CardView.prefab"
            };
            int changed = 0;

            foreach (string path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogWarning("[CardTextColorFix] Not found: " + path); continue; }

                // CardName → 검정
                Transform cardName = prefab.transform.Find("CardName");
                if (cardName != null)
                {
                    var tmp = cardName.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) { tmp.color = Color.black; changed++; Debug.Log($"[CardTextColorFix] {path}/CardName -> black"); }
                }

                // CardDescription → 흰색
                Transform cardDesc = prefab.transform.Find("CardDescription");
                if (cardDesc != null)
                {
                    var tmp = cardDesc.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) { tmp.color = Color.white; changed++; Debug.Log($"[CardTextColorFix] {path}/CardDescription -> white"); }
                }

                EditorUtility.SetDirty(prefab);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CardTextColorFix] Done. {changed} text component(s) updated.");
        }
    }
}
