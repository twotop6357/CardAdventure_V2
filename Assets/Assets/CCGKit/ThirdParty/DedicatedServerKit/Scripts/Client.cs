// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;

namespace DedicatedServerKit
{
    /// <summary>
    /// This class wraps the .NET HTTP client used to interact with the REST-based API
    /// of the Go server.
    ///
    /// All the methods are asynchronous to avoid blocking the main thread while waiting
    /// for a response from the server.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// The wrapped .NET HTTP client.
        /// </summary>
        private readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="ipAddress">The IP address the client will connect to.</param>
        /// <param name="port">The port number the client will connect to.</param>
        public Client(string ipAddress, int port)
        {
            httpClient.BaseAddress = new Uri($"{ipAddress}:{port}/");
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
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("email", email), 
                new KeyValuePair<string, string>("username", username), 
                new KeyValuePair<string, string>("password", password) 
            });
            try
            {
                var response = await httpClient.PostAsync("register", form);
                if (response.IsSuccessStatusCode)
                {
                    return new RegistrationResponse { IsOk = true };
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonUtility.FromJson<ErrorResponse>(content);
                    return new RegistrationResponse { IsOk = false, Error = error };
                }
            }
            catch (Exception)
            {
                return new RegistrationResponse 
                { 
                    IsOk = false, 
                    Error = GetServerIsDownErrorResponse() 
                };
            }
        }

        /// <summary>
        /// Logs an existing user in the system.
        /// </summary>
        /// <param name="username">The name of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username), 
                new KeyValuePair<string, string>("password", password) 
            });
            try
            {
                var response = await httpClient.PostAsync("login", form);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var token = JsonUtility.FromJson<LoginOkResponse>(content);
                    PlayerPrefs.SetString("token", token.token);
                    return new LoginResponse { IsOk = true, Token = token.token };
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonUtility.FromJson<ErrorResponse>(content);
                    return new LoginResponse { IsOk = false, Error = error };
                }
            }
            catch (Exception)
            {
                return new LoginResponse 
                { 
                    IsOk = false, 
                    Error = GetServerIsDownErrorResponse() 
                };
            }
        }

        /// <summary>
        /// Creates a new game room.
        /// </summary>
        /// <param name="name">The name of the room.</param>
        /// <param name="maxPlayers">The maximum number of players allowed in the room.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<CreateGameRoomResponse> CreateGameRoomAsync(string name, int maxPlayers)
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", name),
                new KeyValuePair<string, string>("max_players", maxPlayers.ToString())
            });
            try
            {
                var token = PlayerPrefs.GetString("token");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", token);
                var response = await httpClient.PostAsync("create_game_room", form);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var success = JsonUtility.FromJson<CreateGameRoomSuccessResponse>(content);
                    return new CreateGameRoomResponse { IsOk = true, Success = success };
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonUtility.FromJson<ErrorResponse>(content);
                    return new CreateGameRoomResponse { IsOk = false, Error = error };
                }
            }
            catch (Exception)
            {
                return new CreateGameRoomResponse 
                { 
                    IsOk = false, 
                    Error = GetServerIsDownErrorResponse() 
                };
            }
        }

        /// <summary>
        /// Finds the available game rooms.
        /// </summary>
        /// <returns>The task containing the response.</returns>
        public async Task<FindGameRoomsResponse> FindGameRoomsAsync()
        {
            try
            {
                var token = PlayerPrefs.GetString("token");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", token);
                var response = await httpClient.PostAsync("find_game_rooms", null);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var success = JsonUtility.FromJson<FindGameRoomsSuccessResponse>(content);
                    return new FindGameRoomsResponse { IsOk = true, Success = success };
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonUtility.FromJson<ErrorResponse>(content);
                    return new FindGameRoomsResponse { IsOk = false, Error = error };
                }
            }
            catch (Exception)
            {
                return new FindGameRoomsResponse 
                { 
                    IsOk = false, 
                    Error = GetServerIsDownErrorResponse() 
                };
            }
        }

        /// <summary>
        /// Joins an existing game room.
        /// </summary>
        /// <param name="roomId">The public unique identifier of the room to join.</param>
        /// <returns>The task containing the response.</returns>
        public async Task<JoinGameRoomResponse> JoinGameRoomAsync(string roomId)
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("room_id", roomId)
            });
            try
            {
                var token = PlayerPrefs.GetString("token");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", token);
                var response = await httpClient.PostAsync("join_game_room", form);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var success = JsonUtility.FromJson<JoinGameRoomSuccessResponse>(content);
                    return new JoinGameRoomResponse { IsOk = true, Success = success };
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonUtility.FromJson<ErrorResponse>(content);
                    return new JoinGameRoomResponse { IsOk = false, Error = error };
                }
            }
            catch (Exception)
            {
                return new JoinGameRoomResponse 
                { 
                    IsOk = false, 
                    Error = GetServerIsDownErrorResponse() 
                };
            }
        }

        /// <summary>
        /// Utility method that returns an error response. This is used when the Go server is down.
        /// </summary>
        /// <returns>The error response.</returns>
        private ErrorResponse GetServerIsDownErrorResponse()
        {
            return new ErrorResponse 
            {
                title = "Server down",
                detail = "The server is down.",
                status = 500 
            };
        }
    }
}
