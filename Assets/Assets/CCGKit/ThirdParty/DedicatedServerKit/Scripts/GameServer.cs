// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

namespace DedicatedServerKit
{
    /// <summary>
    /// This class inherits from Mirror's NetworkManager and represents a spawned game server
    /// instance that is able to communicate with the Go server.
    /// </summary>
    public class GameServer : NetworkManager
    {
        /// <summary>
        /// The client configuration to use by the HTTP client. You set this from the Inspector.
        /// </summary>
        public ClientConfig Config;

        /// <summary>
        /// The public unique identifier of this game server.
        /// </summary>
        private string id;
        /// <summary>
        /// The private unique identifier of this game server. This is used for secure communication
        /// between this game server and the Go server (to prevent any third-party from manipulating
        /// the servers).
        /// </summary>
        private string internalId;

        /// <summary>
        /// The list of current player connections.
        /// </summary>
        private readonly List<NetworkConnection> connectedPlayers = new List<NetworkConnection>(8);

        /// <summary>
        /// The internal HTTP client used to communicate with the Go server.
        /// </summary> 
        private readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// The adapter that is used to communicate with the respective Mirror transport.
        /// </summary> 
        private TransportAdapter transportAdapter;

        /// <summary>
        /// The frequency (in seconds) at which to check if the game server is empty, so that it can
        /// be safely closed.
        /// </summary>
        private const float EmptyServerCheckFrequency = 30.0f;

        /// <summary>
        /// The Awake method caches the transport adapter.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            transportAdapter = GetComponent<TransportAdapter>();
        }

        /// <summary>
        /// The Start method initializes this game server instance based on the passed command-line arguments:
        ///     * Argument 1: Port number.
        ///     * Argument 2: Public unique identifier.
        ///     * Argument 3: Private unique identifier. 
        /// </summary>
        public override void Start()
        {
            base.Start();
            
            var args = Environment.GetCommandLineArgs();
            var port = int.Parse(args[1]);
            transportAdapter.SetPort((ushort)port);
            id = args[2];
            internalId = args[3];

            httpClient.BaseAddress = new Uri($"{Config.IpAddress}:{Config.Port}/");
            
            StartServer();
            StartCoroutine(CheckIfServerIsEmpty());
        }

        /// <summary>
        /// This callback is automatically called when a player joins this game server.
        /// Note how the UpdateMasterServer method is called so that the lobby shows the appropriate
        /// player count for the game room.
        /// </summary>
        /// <param name="conn">The connection of the connecting player.</param>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            if (!connectedPlayers.Contains(conn))
                connectedPlayers.Add(conn);
            
            UpdateMasterServer();
        }

        /// <summary>
        /// This callback is automatically called when a player leaves this game server.
        /// Note how the UpdateMasterServer method is called so that the lobby shows the appropriate
        /// player count for the game room.
        /// </summary>
        /// <param name="conn">The connection of the disconnecting player.</param>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            if (connectedPlayers.Contains(conn))
                connectedPlayers.Remove(conn);
            
            UpdateMasterServer();
        }

        /// <summary>
        /// This coroutine is automatically called every EmptyServerCheckFrequency seconds and is
        /// responsible for destroying this spawned game server instance if there are no players left.
        /// </summary>
        private IEnumerator CheckIfServerIsEmpty()
        {
            while (true)
            {
                yield return new WaitForSeconds(EmptyServerCheckFrequency);
                if (connectedPlayers.Count == 0)
                    Application.Quit();
            }
        }

        /// <summary>
        /// This method updates the Go server's information for this spawned game server instance.
        /// </summary>
        private async void UpdateMasterServer()
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("id", id), 
                new KeyValuePair<string, string>("internal_id", internalId), 
                new KeyValuePair<string, string>("num_players", connectedPlayers.Count.ToString()) 
            });
            var response = await httpClient.PostAsync("update_game_room", form);
            if (response.IsSuccessStatusCode)
            {
            }
        }
    }
}
