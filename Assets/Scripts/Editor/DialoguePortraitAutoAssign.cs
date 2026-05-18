#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CardAdventure.Editor
{
    /// <summary>
    /// Assets/ScriptableObjects/Dialogues/ 의 모든 DialogueData 에 대해
    /// 파일명에서 NPC 키워드를 추출하고 Assets/Assets/Sprites/FaceImage/ 의 같은 키워드 sprite 를
    /// speakerPortrait 필드에 자동 할당한다.
    /// 메뉴: CardAdventure/UI/Auto Assign Dialogue Portraits
    /// </summary>
    public static class DialoguePortraitAutoAssign
    {
        private const string DialoguesDir = "Assets/ScriptableObjects/Dialogues";
        private const string FaceImageDir = "Assets/Assets/Sprites/FaceImage";

        // DialogueData 파일명 (소문자) → 사용할 FaceImage 파일명(확장자 제외, 소문자)
        // 매칭은 substring으로 한다.
        private static readonly Dictionary<string, string> NameKeywordToFaceFile = new Dictionary<string, string>
        {
            { "jobchanger",  "jobchanger_faceimage" },
            { "shopkeeper",  "shopkeeper_faceimage" },
            { "eventbattle", "examiner_faceimage" },
            { "examiner",    "examiner_faceimage" },
            { "afterjob",    "jobchanger_faceimage" },
            { "warrior",     "warrior_faceimage" },
            { "magician",    "magician_faceimage" },
            { "mage",        "magician_faceimage" },
            { "rogue",       "rogue_faceimage" },
            { "femalenpc",   "femalenpc_faceimage" },
            { "malenpc",     "malenpc_faceimage" },
            { "test",        "malenpc_faceimage" },
        };

        [MenuItem("CardAdventure/UI/Auto Assign Dialogue Portraits")]
        public static void AutoAssign()
        {
            // 1) FaceImage 후보 로드.
            Dictionary<string, Sprite> faces = LoadFaceSprites();
            if (faces.Count == 0)
            {
                Debug.LogWarning($"[DialoguePortraitAutoAssign] {FaceImageDir} 에서 sprite 를 찾지 못했습니다.");
                return;
            }

            // 2) 모든 DialogueData 순회.
            string[] guids = AssetDatabase.FindAssets("t:DialogueData", new[] { DialoguesDir });
            int assigned = 0;
            int skippedAlreadyAssigned = 0;
            int notMatched = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
                if (data == null)
                {
                    continue;
                }

                if (data.speakerPortrait != null)
                {
                    skippedAlreadyAssigned++;
                    Debug.Log($"[DialoguePortraitAutoAssign] 이미 할당됨: {Path.GetFileName(path)} (sprite={data.speakerPortrait.name})");
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                Sprite match = ResolveFace(fileName, faces);
                if (match == null)
                {
                    notMatched++;
                    Debug.LogWarning($"[DialoguePortraitAutoAssign] 매칭 실패: {Path.GetFileName(path)} — 적절한 FaceImage 가 없습니다.");
                    continue;
                }

                SerializedObject so = new SerializedObject(data);
                SerializedProperty prop = so.FindProperty("speakerPortrait");
                if (prop == null)
                {
                    Debug.LogError($"[DialoguePortraitAutoAssign] speakerPortrait 필드를 찾지 못했습니다: {path}");
                    continue;
                }

                prop.objectReferenceValue = match;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                assigned++;
                Debug.Log($"[DialoguePortraitAutoAssign] ✅ {Path.GetFileName(path)} → {match.name}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DialoguePortraitAutoAssign] 완료. 할당 {assigned}, 이미 할당됨 {skippedAlreadyAssigned}, 매칭 실패 {notMatched}");
        }

        private static Dictionary<string, Sprite> LoadFaceSprites()
        {
            Dictionary<string, Sprite> map = new Dictionary<string, Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { FaceImageDir });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    string key = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    map[key] = sprite;
                }
            }
            return map;
        }

        private static Sprite ResolveFace(string dialogueFileNameLower, Dictionary<string, Sprite> faces)
        {
            // 가장 구체적인 키워드부터 매칭한다 (긴 키워드 우선).
            foreach (KeyValuePair<string, string> entry in NameKeywordToFaceFile
                .OrderByDescending(kv => kv.Key.Length))
            {
                if (dialogueFileNameLower.Contains(entry.Key)
                    && faces.TryGetValue(entry.Value, out Sprite sprite))
                {
                    return sprite;
                }
            }

            // fallback: 파일명 단어 중 하나가 FaceImage 키와 직접 일치하는지 검사.
            foreach (KeyValuePair<string, Sprite> face in faces)
            {
                string facePrefix = face.Key.Replace("_faceimage", string.Empty);
                if (!string.IsNullOrEmpty(facePrefix)
                    && dialogueFileNameLower.Contains(facePrefix))
                {
                    return face.Value;
                }
            }

            return null;
        }
    }
}
#endif
