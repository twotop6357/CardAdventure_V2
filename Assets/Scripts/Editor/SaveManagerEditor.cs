using UnityEngine;
using UnityEditor;
using System.IO;

namespace CardAdventure.EditorTools
{
    public class SaveManagerEditor : EditorWindow
    {
        [MenuItem("CardAdventure/Save Data Manager")]
        public static void ShowWindow()
        {
            GetWindow<SaveManagerEditor>("Save Data Manager");
        }

        private void OnGUI()
        {
            GUILayout.Label("세이브 데이터 관리", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            if (GUILayout.Button("모든 세이브 데이터 삭제", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("세이브 데이터 삭제", 
                    "정말로 모든 세이브 데이터를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", 
                    "삭제", "취소"))
                {
                    DeleteAllSaveData();
                }
            }

            EditorGUILayout.Space();
            
            GUILayout.Label("현재 세이브 데이터 경로:", EditorStyles.label);
            EditorGUILayout.SelectableLabel(Application.persistentDataPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private void DeleteAllSaveData()
        {
            bool deletedAny = false;
            for (int i = 0; i < SaveManager.MAX_SAVE_SLOTS; i++)
            {
                if (SaveManager.HasSaveData(i))
                {
                    SaveManager.DeleteSaveData(i);
                    deletedAny = true;
                }
            }

            // 추가적으로 혹시 모를 다른 save_slot_*.json 파일들이 있다면 함께 삭제
            string[] saveFiles = Directory.GetFiles(Application.persistentDataPath, "save_slot_*.json");
            foreach (string file in saveFiles)
            {
                try
                {
                    File.Delete(file);
                    deletedAny = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveData Manager] 파일 삭제 실패: {file} - {e.Message}");
                }
            }

            if (deletedAny)
            {
                Debug.Log("[SaveData Manager] 모든 세이브 데이터가 성공적으로 삭제되었습니다.");
                EditorUtility.DisplayDialog("완료", "모든 세이브 데이터가 성공적으로 삭제되었습니다.", "확인");
            }
            else
            {
                Debug.Log("[SaveData Manager] 삭제할 세이브 데이터가 존재하지 않습니다.");
                EditorUtility.DisplayDialog("알림", "삭제할 세이브 데이터가 존재하지 않습니다.", "확인");
            }
        }
    }
}
