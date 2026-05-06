// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

using DG.Tweening;
using FullSerializer;
using TMPro;

#if ENABLE_DEDICATED_SERVER
using System.Collections;
using DedicatedServerKit;
#endif

namespace CCGKit
{
    /// <summary>
    /// For every scene in the demo game, we create a specific game object to handle the user-interface
    /// logic belonging to that scene. The home scene just contains the button delegates that trigger
    /// transitions to other scenes or exit the game.
    /// </summary>
    public class HomeScreen : BaseScreen
    {
#pragma warning disable 649
        [SerializeField] private TextMeshProUGUI versionText;
#pragma warning restore 649

        private fsSerializer serializer = new fsSerializer();

        protected override void Awake()
        {
            base.Awake();

            Assert.IsNotNull(versionText);
            SceneManager.sceneLoaded += (scene, mode) => { DOTween.KillAll(); };
        }

        protected override void Start()
        {
            base.Start();

            Application.targetFrameRate = 60;

            DOTween.SetTweensCapacity(500, 50);

            versionText.text = "Version " + CCGKitInfo.version;

            GameManager.Instance.Initialize();

            var decksPath = Application.persistentDataPath + "/decks.json";
            if (File.Exists(decksPath))
            {
                var file = new StreamReader(decksPath);
                var fileContents = file.ReadToEnd();
                var data = fsJsonParser.Parse(fileContents);
                object deserialized = null;
                serializer.TryDeserialize(data, typeof(List<Deck>), ref deserialized).AssertSuccessWithoutWarnings();
                file.Close();
                GameManager.Instance.playerDecks = deserialized as List<Deck>;
            }

#if ENABLE_DEDICATED_SERVER
        if (!GameManager.Instance.isPlayerLoggedIn)
        {
            OpenPopup<PopupLogin>("PopupLogin", popup =>
            {
            });
        }
#endif
        }

        public void OnPlayButtonPressed()
        {
            SceneManager.LoadScene("Lobby");
        }

        public void OnDecksButtonPressed()
        {
            SceneManager.LoadScene("DeckBuilder");
        }

        public void OnQuitButtonPressed()
        {
            OpenPopup<PopupTwoButtons>("PopupTwoButtons", popup =>
            {
                popup.text.text = "Do you want to quit?";
                popup.buttonText.text = "Yes";
                popup.button2Text.text = "No";
                popup.button.onClickEvent.AddListener(() => { Application.Quit(); });
                popup.button2.onClickEvent.AddListener(() => { popup.Close(); });
            });
        }
    }
}