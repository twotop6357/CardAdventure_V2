// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;
using UnityEngine;

namespace CCGKit
{
    /// <summary>
    /// Create a NetworkManager subclass with an automatically-managed lifetime. Having a subclass will also
    /// come in handy if we need to extend the capabilities of the vanilla NetworkManager in the future.
    /// </summary>
    public class GameNetworkManager : NetworkManager
    {
        public bool IsSinglePlayer;

        private static GameNetworkManager instance;

        public static GameNetworkManager Instance
        {
            get { return instance ?? new GameObject("GameNetworkManager").AddComponent<GameNetworkManager>(); }
        }

        public void Initialize()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // UNET currently crashes on iOS if the runInBackground property is set to true.
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
            {
                runInBackground = false;
            }
        }

        private void OnEnable()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);

            var server = GameObject.Find("Server");
            if (server != null)
            {
                server.GetComponent<Server>().OnPlayerConnected(conn.connectionId);
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            var server = GameObject.Find("Server");
            if (server != null)
            {
                server.GetComponent<Server>().OnPlayerDisconnected(conn.connectionId);
            }
        }
    }
}