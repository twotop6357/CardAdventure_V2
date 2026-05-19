using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    [Serializable]
    public class SaveData
    {
        // ── 메타 데이터 ──────────────────────────────────────────
        public string saveDateStr;
        public float playTimeSeconds;
        public string currentSceneName;

        // ── 플레이어 상태 ────────────────────────────────────────
        public int currentHp;
        public int maxHp;
        public int gold;
        public int chapterProgress;
        public int potionCount;

        // ── 직업 및 덱 (참조 문자열) ─────────────────────────────
        public string selectedJobId;
        public List<string> deckCardIds = new List<string>();

        // ── 어드벤처 씬 위치 및 방향 ──────────────────────────────
        public bool hasSavedPosition;
        public float savedPosX;
        public float savedPosY;
        public float savedFacingDirX;
        public float savedFacingDirY;

        // ── 이벤트 및 퀘스트 상태 ────────────────────────────────
        public string pendingChaserNpcId;
        public List<string> completedChaserNpcIds = new List<string>();
        public bool examinerBattleCompleted;
        public bool examinerDialogueCompleted;

        // ── 플레이 기록/통계 ────────────────────────────────────
        public int cardPurchaseGoldSpent;
        public int potionsUsedCount;
        public string maxDamageCardName;
        public int maxDamageCardValue;

        public SaveData()
        {
            saveDateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            playTimeSeconds = 0f;
            currentSceneName = "AdventureScene";
        }
    }
}
