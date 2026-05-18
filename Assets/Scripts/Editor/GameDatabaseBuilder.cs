using UnityEngine;
using UnityEditor;
using System.IO;

namespace CardAdventure.EditorTools
{
    public static class GameDatabaseBuilder
    {
        private const string DatabasePath = "Assets/Resources/GameDatabase.asset";

        [MenuItem("CardAdventure/Build Game Database")]
        public static void BuildDatabase()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Find or create GameDatabase
            GameDatabase db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<GameDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            db.allJobs.Clear();
            db.allCards.Clear();

            // Find all JobClassInfo
            string[] jobGuids = AssetDatabase.FindAssets("t:JobClassInfo");
            foreach (string guid in jobGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                JobClassInfo job = AssetDatabase.LoadAssetAtPath<JobClassInfo>(path);
                if (job != null) db.allJobs.Add(job);
            }

            // Find all CardData
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null) db.allCards.Add(card);
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[GameDatabaseBuilder] GameDatabase 갱신 완료. 직업: {db.allJobs.Count}개, 카드: {db.allCards.Count}개");
        }
    }
}
