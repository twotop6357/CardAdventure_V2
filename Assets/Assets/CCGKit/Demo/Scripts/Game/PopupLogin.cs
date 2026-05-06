// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using DedicatedServerKit;
using TMPro;

namespace CCGKit
{
    public class PopupLogin : Popup
    {
        public TMP_InputField usernameInputField;
        public TMP_InputField passwordInputField;

        private bool usernameError;
        private bool passwordError;

        public void OnSignupButtonPressed()
        {
            parentScene.OpenPopup<PopupSignup>("PopupSignup", popup => { });
        }

        public async void OnLoginButtonPressed()
        {
            // Perform some basic validation of the user input locally prior to calling the
            // remote login method. This is a good way to avoid some unnecessary network
            // traffic.
            var username = usernameInputField.text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username))
            {
                usernameError = true;
                usernameInputField.text = "Please enter a valid username";
            }

            var password = passwordInputField.text.Trim();
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(password))
            {
                passwordError = true;
                passwordInputField.contentType = TMP_InputField.ContentType.Alphanumeric;
                passwordInputField.text = "Please enter a valid password";
            }

            if (!usernameError && !passwordError)
            {
                var clientObj = FindFirstObjectByType<ClientObject>();
                var response = await clientObj.LoginAsync(username, password);
                if (response.IsOk)
                {
                    GameManager.Instance.isPlayerLoggedIn = true;
                    GameManager.Instance.playerName = username;
                    Close();
                }
                else
                {
                    var error = response.Error;
                    OpenAlertDialog(error.detail);
                }
            }
        }

        private void OpenAlertDialog(string msg)
        {
            parentScene.OpenPopup<PopupOneButton>("PopupOneButton", popup =>
            {
                popup.text.text = msg;
                popup.buttonText.text = "OK";
                popup.button.onClickEvent.AddListener(() => { popup.Close(); });
            });
        }
    }
}