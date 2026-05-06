// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace CCGKit
{
    public class GameListItem : MonoBehaviour
    {
        public TextMeshProUGUI gameNameText;
        public Image lockIcon;

        public BaseScreen scene;

        private string roomId;
        private int numPlayers;
        private int maxPlayers;

        private void Awake()
        {
            Assert.IsNotNull(gameNameText);
            Assert.IsNotNull(lockIcon);
        }

        public void SetInfo(string roomId, int numPlayers, int maxPlayers)
        {
            this.roomId = roomId;
            this.numPlayers = numPlayers;
            this.maxPlayers = maxPlayers;
        }

        public void OnJoinButtonPressed()
        {
            if (lockIcon.IsActive())
            {
                scene.OpenPopup<PopupPassword>("PopupPassword", popup =>
                {
                    popup.button.onClickEvent.AddListener(() =>
                    {
                        scene.ClosePopup();
                        JoinMatch(popup.inputField.text);
                    });
                });
            }
            else
            {
                JoinMatch();
            }
        }

        private void JoinMatch(string password = "")
        {
            var lobby = FindFirstObjectByType<LobbyScreen>();
            lobby.OnRoomButtonPressed(roomId);
        }

        private void OpenAlertDialog(string msg)
        {
            scene.OpenPopup<PopupOneButton>("PopupOneButton", popup =>
            {
                popup.text.text = msg;
                popup.buttonText.text = "OK";
                popup.button.onClickEvent.AddListener(() => { popup.Close(); });
            });
        }
    }
}