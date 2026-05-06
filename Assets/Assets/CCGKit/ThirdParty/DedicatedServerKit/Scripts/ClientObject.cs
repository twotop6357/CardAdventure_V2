// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;
using System.Threading.Tasks;
using UnityEngine;

namespace DedicatedServerKit
{
    /// <summary>
    /// This class wraps the Client class and its methods into a MonoBehaviour component
    /// that can be used from within a Unity scene.
    /// </summary>
    public class ClientObject : MonoBehaviour
    {
        /// <summary>
        /// The client configuration to use by the client. You set this from the Inspector.
        /// </summary>
        public ClientConfig Config;

        /// <summary>
        /// The Network Manager to use by the client. You set this from the Inspector.
        /// </summary>
        public NetworkManager NetworkManager;

        /// <summary>
        /// The wrapped client object.
        /// </summary> 
        private Client client;

        /// <summary>
        /// The wrapped client object is instantiated when Unity's Start method is called.
        /// </summary>
        private void Awake()
        {
            client = new Client(Config.IpAddress, Config.Port);
            var networkManager = FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Instantiate(NetworkManager);
            }
        }

        /// <summary>
        /// Registers a new user with the specified information in the system.
        /// </summary>
        /// <param name="email">The email of the user.</param>
        /// <param name="username">The name of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<RegistrationResponse> RegisterAsync(string email, string username, string password)
        {
            return await client.RegisterAsync(email, username, password);
        }
        
        /// <summary>
        /// Logs an existing user in the system.
        /// </summary>
        /// <param name="username">The name of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            return await client.LoginAsync(username, password);
        }
        
        /// <summary>
        /// Creates a new game room.
        /// </summary>
        /// <param name="name">The name of the room.</param>
        /// <param name="maxPlayers">The maximum number of players allowed in the room.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<CreateGameRoomResponse> CreateGameRoomAsync(string name, int maxPlayers)
        {
            return await client.CreateGameRoomAsync(name, maxPlayers);
        }
        
        /// <summary>
        /// Finds the available game rooms.
        /// </summary>
        /// <returns>The task containing the response.</returns>
        public async Task<FindGameRoomsResponse> FindGameRoomsAsync()
        {
            return await client.FindGameRoomsAsync();
        }

        /// <summary>
        /// Joins an existing game room.
        /// </summary>
        /// <param name="roomId">The public unique identifier of the room.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<JoinGameRoomResponse> JoinGameRoomAsync(string id)
        {
            return await client.JoinGameRoomAsync(id);
        }
    }
}
