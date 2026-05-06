// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections;
using UnityEngine;

namespace CCGKit
{
    public class BotLauncher : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(2.0f);

            var defaultDeckTextAsset = Resources.Load<TextAsset>("DefaultDeck");
            if (defaultDeckTextAsset != null)
            {
                GameManager.Instance.defaultDeck = JsonUtility.FromJson<Deck>(defaultDeckTextAsset.text);
            }

            GameManager.Instance.Initialize();
            GameNetworkManager.Instance.IsSinglePlayer = true;
            GameNetworkManager.Instance.StartClient();
        }
    }
}