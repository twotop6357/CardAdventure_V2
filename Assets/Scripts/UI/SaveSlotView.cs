using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardAdventure.UI
{
    public class SaveSlotView : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI slotNumberText;
        public TextMeshProUGUI dateText;
        public TextMeshProUGUI infoText;
        public Button slotButton;

        private int slotIndex;
        private System.Action<int> onSlotClicked;

        public void Bind(int index, SaveData data, System.Action<int> onClick)
        {
            slotIndex = index;
            onSlotClicked = onClick;

            slotNumberText.text = $"슬롯 {index + 1}";

            if (data == null)
            {
                dateText.text = "빈 슬롯";
                infoText.text = "새로운 모험을 시작하세요.";
            }
            else
            {
                dateText.text = data.saveDateStr;
                string jobName = !string.IsNullOrEmpty(data.selectedJobId) ? data.selectedJobId.Replace("Job_", "") : "전사";
                infoText.text = $"LV.{data.chapterProgress} | {jobName} | {data.gold} G";
            }

            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => onSlotClicked?.Invoke(slotIndex));
        }
    }
}
