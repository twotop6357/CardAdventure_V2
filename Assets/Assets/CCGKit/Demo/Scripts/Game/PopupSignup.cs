// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using DedicatedServerKit;
using System.Text.RegularExpressions;
using TMPro;

namespace CCGKit
{
    public class PopupSignup : Popup
    {
        public TMP_InputField emailInputField;
        public TMP_InputField usernameInputField;
        public TMP_InputField passwordInputField;
        public TMP_InputField passwordRepeatInputField;

        public async void OnSignupButtonPressed()
        {
            var emailText = emailInputField.text;
            var usernameText = usernameInputField.text;
            var passwordText = passwordInputField.text;
            var passwordRepeatText = passwordRepeatInputField.text;

            // Perform some basic validation of the user input locally prior to calling the
            // remote registration method. This is a good way to avoid some unnecessary
            // network traffic.
            if (string.IsNullOrEmpty(emailText))
            {
                OpenAlertDialog("Please enter your email address.");
                return;
            }

            if (!IsValidEmail(emailText))
            {
                OpenAlertDialog("The email address you entered is not valid.");
                return;
            }

            if (string.IsNullOrEmpty(usernameText))
            {
                OpenAlertDialog("Please enter your username.");
                return;
            }

            if (string.IsNullOrEmpty(passwordText))
            {
                OpenAlertDialog("Please enter your password.");
                return;
            }

            if (string.IsNullOrEmpty(passwordRepeatText))
            {
                OpenAlertDialog("Please enter your password again.");
                return;
            }

            if (passwordText != passwordRepeatText)
            {
                OpenAlertDialog("The passwords do not match.");
                return;
            }

            var client = FindFirstObjectByType<ClientObject>();
            var response = await client.RegisterAsync(emailText, usernameText, passwordText);
            if (response.IsOk)
            {
                OpenAlertDialog("Welcome!");
                Close();
            }
            else
            {
                var error = response.Error;
                OpenAlertDialog(error.detail);
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

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
                RegexOptions.IgnoreCase);
        }
    }
}