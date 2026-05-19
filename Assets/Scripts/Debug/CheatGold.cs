using UnityEngine;
using CardAdventure;

public class CheatGold : MonoBehaviour {
    void Awake() {
        if (GameDataManager.Instance != null) {
            GameDataManager.Instance.EarnGold(1000);
            Debug.Log("[Cheat] 1000 gold added!");
        }
    }
}




