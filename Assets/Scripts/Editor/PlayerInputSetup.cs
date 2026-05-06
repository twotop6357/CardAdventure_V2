using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PlayerInputSetup
{
    [MenuItem("CardAdventure/Setup Player Input")]
    public static void Setup()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        if (asset == null) { Debug.LogError("InputSystem_Actions not found"); return; }

        var go = GameObject.FindWithTag("Player");
        if (go == null) { Debug.LogError("Player not found"); return; }

        var pi = go.GetComponent<PlayerInput>();
        if (pi == null) { Debug.LogError("PlayerInput not found"); return; }

        pi.actions = asset;
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;

        EditorUtility.SetDirty(go);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[PlayerInputSetup] Done: " + asset.name);
    }
}
