using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using CardAdventure;

namespace CardAdventure.Editor
{
    public static class JobChangeUIWire
    {
        [MenuItem("CardAdventure/Wire Job Change UI")]
        public static void WireJobChangeUI()
        {
            var canvas = GameObject.Find("JobChangeCanvas");
            if (canvas == null) { Debug.LogError("JobChangeCanvas not found"); return; }

            var ctrl = canvas.GetComponent<JobChangeUIController>()
                    ?? canvas.AddComponent<JobChangeUIController>();

            var warrior = AssetDatabase.LoadAssetAtPath<JobClassInfo>("Assets/ScriptableObjects/Jobs/Job_Warrior.asset");
            var mage    = AssetDatabase.LoadAssetAtPath<JobClassInfo>("Assets/ScriptableObjects/Jobs/Job_Mage.asset");
            var rogue   = AssetDatabase.LoadAssetAtPath<JobClassInfo>("Assets/ScriptableObjects/Jobs/Job_Rogue.asset");

            var so = new SerializedObject(ctrl);

            var jp = so.FindProperty("jobs");
            jp.arraySize = 3;
            jp.GetArrayElementAtIndex(0).objectReferenceValue = warrior;
            jp.GetArrayElementAtIndex(1).objectReferenceValue = mage;
            jp.GetArrayElementAtIndex(2).objectReferenceValue = rogue;

            var bp = so.FindProperty("jobButtons");
            bp.arraySize = 3;
            bp.GetArrayElementAtIndex(0).objectReferenceValue = GameObject.Find("JobButton_1")?.GetComponent<Button>();
            bp.GetArrayElementAtIndex(1).objectReferenceValue = GameObject.Find("JobButton_2")?.GetComponent<Button>();
            bp.GetArrayElementAtIndex(2).objectReferenceValue = GameObject.Find("JobButton_3")?.GetComponent<Button>();

            so.FindProperty("characterPreviewImage").objectReferenceValue = GameObject.Find("JobImage")?.GetComponent<Image>();
            so.FindProperty("jobDescriptionText").objectReferenceValue    = GameObject.Find("JobDescriptionText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("yesButton").objectReferenceValue  = GameObject.Find("YesButton")?.GetComponent<Button>();
            so.FindProperty("noButton").objectReferenceValue   = GameObject.Find("NoButton")?.GetComponent<Button>();

            var bg = canvas.transform.Find("BackGround");
            if (bg != null) so.FindProperty("panelRoot").objectReferenceValue = bg.GetComponent<RectTransform>();

            so.ApplyModifiedProperties();
            Debug.Log("[JobChangeUIWire] Done! warrior=" + warrior?.name + " mage=" + mage?.name + " rogue=" + rogue?.name);
        }
    }
}
