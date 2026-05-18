using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace CardAdventure
{
    public static class SaveManager
    {
        public const int MAX_SAVE_SLOTS = 3;
        public static int CurrentSlotIndex { get; private set; } = 0;

        public static void SetCurrentSlotIndex(int slotIndex)
        {
            CurrentSlotIndex = Mathf.Clamp(slotIndex, 0, MAX_SAVE_SLOTS - 1);
        }

        private static string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.json");
        }

        public static bool HasSaveData(int slotIndex)
        {
            return File.Exists(GetSaveFilePath(slotIndex));
        }

        public static bool HasAnySaveData()
        {
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                if (HasSaveData(i)) return true;
            }
            return false;
        }

        public static bool HasCurrentSaveData()
        {
            return HasSaveData(CurrentSlotIndex);
        }

        public static SaveData LoadCurrentSaveData()
        {
            return LoadSaveData(CurrentSlotIndex);
        }

        public static SaveData LoadSaveData(int slotIndex)
        {
            string path = GetSaveFilePath(slotIndex);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slotIndex} 불러오기 실패: {e.Message}");
                return null;
            }
        }

        public static void SaveGame(int slotIndex)
        {
            if (GameDataManager.Instance == null)
            {
                Debug.LogError("[SaveManager] GameDataManager 인스턴스가 존재하지 않습니다.");
                return;
            }

            SaveData data = GameDataManager.Instance.CreateSaveData();
            
            // 기존 플레이 타임 누적 처리 등은 필요하다면 여기서 추가 (지금은 간단히 저장 시간만 기록)
            data.saveDateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(GetSaveFilePath(slotIndex), json);
                SetCurrentSlotIndex(slotIndex);
                Debug.Log($"[SaveManager] 슬롯 {slotIndex} 저장 성공: {GetSaveFilePath(slotIndex)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slotIndex} 저장 실패: {e.Message}");
            }
        }

        public static void DeleteSaveData(int slotIndex)
        {
            string path = GetSaveFilePath(slotIndex);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
