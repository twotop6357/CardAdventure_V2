using UnityEngine;
using UnityEditor;
using CardAdventure;

namespace CardAdventure.Editor
{
    public static class FieldPotionCleaner
    {
        [MenuItem("CardAdventure/Remove Field Potions")]
        public static void RemovePotions()
        {
            var potions = Object.FindObjectsByType<FieldPotionItem>(FindObjectsSortMode.None);
            int count = potions.Length;

            foreach (var p in potions)
            {
                Undo.DestroyObjectImmediate(p.gameObject);
            }

            if (count > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log($"[FieldPotionCleaner] {count}개의 필드 포션을 제거하고 씬을 저장했습니다.");
            }
            else
            {
                Debug.Log("[FieldPotionCleaner] 제거할 포션이 없습니다.");
            }
        }
    }
}
