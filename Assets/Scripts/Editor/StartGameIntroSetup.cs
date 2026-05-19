using System.Collections.Generic;
using CardAdventure.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardAdventure.EditorTools
{
    public static class StartGameIntroSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";

        [MenuItem("CardAdventure/Setup/Start Game Intro")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != LobbyScenePath)
            {
                scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            }

            LobbyUIController lobby = Object.FindFirstObjectByType<LobbyUIController>(FindObjectsInactive.Include);
            if (lobby == null)
            {
                Debug.LogError("[StartGameIntroSetup] LobbyUIController를 찾지 못했습니다.");
                return;
            }

            StartGameIntroController intro = Object.FindFirstObjectByType<StartGameIntroController>(FindObjectsInactive.Include);
            if (intro == null)
            {
                GameObject introGo = new GameObject("StartGameIntroController");
                intro = introGo.AddComponent<StartGameIntroController>();
            }

            SerializedObject introSo = new SerializedObject(intro);
            introSo.FindProperty("lobbyBackground").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/BattleBackground/LobbyBackground.png");
            introSo.FindProperty("jobChangerImage").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/CharacterImages/JobChanger_Image.png");
            introSo.FindProperty("startDialogue").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/ScriptableObjects/Dialogues/NPC_StartGame_Dialogue.asset");
            introSo.FindProperty("afterJobSelectDialogue").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/ScriptableObjects/Dialogues/NPC_StartGameAfterJobSelect_Dialogue.asset");
            introSo.FindProperty("dialogueCanvasPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/DialogueCanvas.prefab");

            SerializedProperty jobsProp = introSo.FindProperty("jobs");
            jobsProp.ClearArray();
            List<JobClassInfo> jobs = new List<JobClassInfo>
            {
                AssetDatabase.LoadAssetAtPath<JobClassInfo>("Assets/ScriptableObjects/Jobs/Job_Warrior.asset"),
                AssetDatabase.LoadAssetAtPath<JobClassInfo>("Assets/ScriptableObjects/Jobs/Job_Mage.asset"),
                AssetDatabase.LoadAssetAtPath<JobClassInfo>("Assets/ScriptableObjects/Jobs/Job_Rogue.asset")
            };

            for (int i = 0; i < jobs.Count; i++)
            {
                jobsProp.InsertArrayElementAtIndex(i);
                jobsProp.GetArrayElementAtIndex(i).objectReferenceValue = jobs[i];
            }

            introSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject lobbySo = new SerializedObject(lobby);
            lobbySo.FindProperty("startGameIntroController").objectReferenceValue = intro;
            lobbySo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(intro);
            EditorUtility.SetDirty(lobby);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[StartGameIntroSetup] 새 게임 인트로 설정을 완료했습니다.");
        }
    }
}
