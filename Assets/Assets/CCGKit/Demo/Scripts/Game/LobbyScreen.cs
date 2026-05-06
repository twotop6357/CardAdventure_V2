// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using CCGKit;
using DG.Tweening;
using FullSerializer;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_DEDICATED_SERVER
using DedicatedServerKit;
using Mirror;
using System.Collections;
#endif

namespace CCGKit
{
    public class LobbyScreen : BaseScreen
    {
#pragma warning disable 649
        [SerializeField] private CanvasGroup settingsGroup;

        [SerializeField] private CanvasGroup createGameGroup;

        [SerializeField] private CanvasGroup findGamesGroup;

        [SerializeField] private GameObject playOnlineText;

        [SerializeField] private GameObject createOnlineGameButton;

        [SerializeField] private GameObject findOnlineGamesButton;

        [SerializeField] private TMP_InputField playerNameInputField;

        [SerializeField] private TextMeshProUGUI currentDeckNameText;

        [SerializeField] private TextMeshProUGUI currentAIDeckNameText;

        [SerializeField] private TMP_InputField gameNameInputField;

        [SerializeField] private Toggle passwordToggle;

        [SerializeField] private TMP_InputField passwordInputField;

        [SerializeField] private TextMeshProUGUI numGamesText;

        [SerializeField] private GameObject gameListContent;

        [SerializeField] private GameObject gameListItemPrefab;
#pragma warning restore 649

        private List<Deck> decks;
        private int currentDeckIndex;

        private fsSerializer serializer = new fsSerializer();

        private const float LoadingTime = 5.0f;

#if ENABLE_DEDICATED_SERVER
    private ClientObject client;
    private TransportAdapter transportAdapter;
#endif

        protected override void Awake()
        {
            base.Awake();

            Assert.IsNotNull(createGameGroup);
            Assert.IsNotNull(findGamesGroup);
            Assert.IsNotNull(playerNameInputField);
            Assert.IsNotNull(currentDeckNameText);
            Assert.IsNotNull(gameNameInputField);
            Assert.IsNotNull(passwordToggle);
            Assert.IsNotNull(passwordInputField);
            Assert.IsNotNull(numGamesText);
            Assert.IsNotNull(gameListContent);
            Assert.IsNotNull(gameListItemPrefab);
        }

        protected override void Start()
        {
            base.Start();

#if ENABLE_DEDICATED_SERVER
        client = FindObjectOfType<ClientObject>();
        transportAdapter = FindObjectOfType<TransportAdapter>();
#endif

            var defaultDeckTextAsset = Resources.Load<TextAsset>("DefaultDeck");
            if (defaultDeckTextAsset != null)
            {
                GameManager.Instance.defaultDeck = JsonUtility.FromJson<Deck>(defaultDeckTextAsset.text);
            }

            var decksPath = Application.persistentDataPath + "/decks.json";
            if (File.Exists(decksPath))
            {
                var file = new StreamReader(decksPath);
                var fileContents = file.ReadToEnd();
                var data = fsJsonParser.Parse(fileContents);
                object deserialized = null;
                serializer.TryDeserialize(data, typeof(List<Deck>), ref deserialized).AssertSuccessWithoutWarnings();
                file.Close();
                decks = deserialized as List<Deck>;
            }

            playerNameInputField.text = PlayerPrefs.GetString("player_name");
            if (decks != null && decks.Count > 0)
            {
                currentDeckIndex = PlayerPrefs.GetInt("default_deck");
                if (currentDeckIndex < decks.Count)
                {
                    currentDeckNameText.text = decks[currentDeckIndex].name;
                    PlayerPrefs.SetInt("default_deck", currentDeckIndex);
                }
                else
                {
                    currentDeckNameText.text = decks[0].name;
                    PlayerPrefs.SetInt("default_deck", 0);
                }
            }

            passwordInputField.gameObject.SetActive(passwordToggle.isOn);

            OnSettingsButtonPressed();

#if ENABLE_DEDICATED_SERVER
    playOnlineText.SetActive(true);
    createOnlineGameButton.SetActive(true);
    findOnlineGamesButton.SetActive(true);
#endif
        }

        public void OnBackButtonPressed()
        {
            SceneManager.LoadScene("Home");
        }

        public void OnSettingsButtonPressed()
        {
            HideGroup(findGamesGroup);
            HideGroup(createGameGroup);
            ShowGroup(settingsGroup);
        }

        public void OnSetPlayerName()
        {
            PlayerPrefs.SetString("player_name", playerNameInputField.text);
        }

        public void OnPrevDeckButtonPressed()
        {
            if (decks != null && decks.Count > 0)
            {
                --currentDeckIndex;
                if (currentDeckIndex < 0)
                {
                    currentDeckIndex = 0;
                }

                if (currentDeckIndex < decks.Count)
                {
                    currentDeckNameText.text = decks[currentDeckIndex].name;
                    PlayerPrefs.SetInt("default_deck", currentDeckIndex);
                }
            }
        }

        public void OnNextDeckButtonPressed()
        {
            if (decks != null && decks.Count > 0)
            {
                ++currentDeckIndex;
                if (currentDeckIndex == decks.Count)
                {
                    currentDeckIndex = currentDeckIndex - 1;
                }

                if (currentDeckIndex < decks.Count)
                {
                    currentDeckNameText.text = decks[currentDeckIndex].name;
                    PlayerPrefs.SetInt("default_deck", currentDeckIndex);
                }
            }
        }

        public void OnPlayNowButtonPressed()
        {
            OpenPopup<PopupLoading>("PopupLoading", popup => { popup.text.text = "Preparing to play..."; });
#if ENABLE_DEDICATED_SERVER
#endif
        }

        public void OnOpenCreateGameButtonPressed()
        {
            HideGroup(settingsGroup);
            HideGroup(findGamesGroup);
            ShowGroup(createGameGroup);
        }

        public void OnCreateGameButtonPressed()
        {
            CreateGame();
        }

        public void OnPasswordToggleValueChanged()
        {
            passwordInputField.gameObject.SetActive(passwordToggle.isOn);
        }

        public void OnOpenFindGamesButtonPressed()
        {
            HideGroup(settingsGroup);
            HideGroup(createGameGroup);
            ShowGroup(findGamesGroup);
            FindGames();
        }

        public void OnFindGamesButtonPressed()
        {
            FindGames();
        }

        public void OnSinglePlayerGameButtonPressed()
        {
            GameNetworkManager.Instance.StartHost();
        }

        public void OnCreateLANGameButtonPressed()
        {
            GameNetworkManager.Instance.StartHost();
        }

        public void OnJoinLANGameButtonPressed()
        {
            GameNetworkManager.Instance.StartClient();
        }

        private void ShowGroup(CanvasGroup group)
        {
            group.DOFade(1.0f, 0.4f);
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        private void HideGroup(CanvasGroup group)
        {
            group.DOFade(0.0f, 0.2f);
            group.interactable = false;
            group.blocksRaycasts = false;
        }

#if ENABLE_DEDICATED_SERVER
    private async void CreateGame()
#else
        private void CreateGame()
#endif
        {
            OpenPopup<PopupLoading>("PopupLoading", popup => { popup.text.text = "Creating new game..."; });
            var gameName = gameNameInputField.text;
            if (string.IsNullOrEmpty(gameName))
            {
                gameName = "New game";
            }

            var password = passwordInputField.text;
#if ENABLE_DEDICATED_SERVER
        var response = await client.CreateGameRoomAsync(gameName, 2);
        if (response.IsOk)
        {
            StartCoroutine(JoinGameServerCoroutine(response.Success.ip_address, response.Success.port));
        }
        else
        {
            ClosePopup();
            OpenPopup<PopupOneButton>("PopupOneButton", popup =>
            {
                popup.text.text = response.Error.detail;
                popup.buttonText.text = "OK";
                popup.button.onClickEvent.AddListener(() => { popup.Close(); });
            });
        }
#endif
        }

#if ENABLE_DEDICATED_SERVER
        private IEnumerator JoinGameServerCoroutine(string ipAddress, int port)
        {
            yield return new WaitForSeconds(LoadingTime);
            ClosePopup();
            while (!NetworkClient.isConnected)
            {
                JoinGameServer(ipAddress, port);
                yield return new WaitForSeconds(1.0f);
            }
        }

        private void JoinGameServer(string ipAddress, int port)
        {
            var networkManager = NetworkManager.singleton;
            transportAdapter.SetPort((ushort)port);
            networkManager.networkAddress = ipAddress;
            networkManager.StartClient();
        }
#endif

#if ENABLE_DEDICATED_SERVER
    private async void FindGames()
#else
        private void FindGames()
#endif
        {
            OpenPopup<PopupLoading>("PopupLoading", popup => { popup.text.text = "Finding games..."; });
#if ENABLE_DEDICATED_SERVER
        var response = await client.FindGameRoomsAsync();

        ClosePopup();

        if (response.IsOk)
        {
            foreach (Transform child in gameListContent.transform)
            {
                Destroy(child.gameObject);
            }

            var success = response.Success;
            numGamesText.text = success.game_rooms.Count.ToString();
            foreach (var room in success.game_rooms)
            {
                var go = Instantiate(gameListItemPrefab) as GameObject;
                go.transform.SetParent(gameListContent.transform, false);
                var gameListItem = go.GetComponent<GameListItem>();
                gameListItem.gameNameText.text = room.name;
                gameListItem.lockIcon.gameObject.SetActive(false);
                gameListItem.scene = this;
                gameListItem.SetInfo(room.id, room.num_players, room.max_players);
            }
        }
        else
        {
            OpenPopup<PopupOneButton>("PopupOneButton", popup =>
            {
                popup.text.text = response.Error.detail;
                popup.buttonText.text = "OK";
                popup.button.onClickEvent.AddListener(() => { popup.Close(); });
            });
        }
#endif
        }

#if ENABLE_DEDICATED_SERVER
    public async void OnRoomButtonPressed(string roomId)
#else
        public void OnRoomButtonPressed(string roomId)
#endif
        {
#if ENABLE_DEDICATED_SERVER
        OpenPopup<PopupLoading>("PopupLoading", popup =>
        {
            popup.text.text = "Finding games...";
        });

        var clientObj = FindObjectOfType<ClientObject>();
        var response = await clientObj.JoinGameRoomAsync(roomId);

        if (response.IsOk)
        {
            var roomInfo = response.Success;
            JoinGameServer(roomInfo.ip_address, roomInfo.port);
        }
        else
        {
            ClosePopup();
            OpenPopup<PopupOneButton>("PopupOneButton", popup =>
            {
                popup.text.text = response.Error.detail;
                popup.buttonText.text = "OK";
                popup.button.onClickEvent.AddListener(() => { popup.Close(); });
            });
        }
#endif
        }
    }
}