// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections.Generic;

namespace DedicatedServerKit
{
    /// <summary>
    /// This struct contains the information returned by a call to the registration
    /// entry point of the REST-based API provided by the Go server.
    /// </summary>
    public struct RegistrationResponse
    {
        /// <summary>
        /// True if the call was successful; false otherwise.
        /// </summary>
        public bool IsOk;
        /// <summary>
        /// If the call was not successful, this field contains the details of the error.
        /// </summary>
        public ErrorResponse Error;
    }
    
    /// <summary>
    /// This struct contains the information returned by a call to the login
    /// entry point of the REST-based API provided by the Go server.
    /// </summary>
    public struct LoginResponse
    {
        /// <summary>
        /// True if the call was successful; false otherwise.
        /// </summary>
        public bool IsOk;
        /// <summary>
        /// The JSON web token returned after a successful call.
        /// </summary>
        public string Token;
        /// <summary>
        /// If the call was not successful, this field contains the details of the error.
        /// </summary>
        public ErrorResponse Error;
    }
    
    /// <summary>
    /// This struct contains the information returned by a successful call to the login
    /// entry point of the REST-based API provided by the Go server.
    ///
    /// This one is used internally and not exposed to the client API.
    /// </summary>
    [Serializable]
    public struct LoginOkResponse
    {
        /// <summary>
        /// The JSON web token returned after a successful call.
        /// </summary>
        public string token;
    }
    
    /// <summary>
    /// This struct contains the information of the error returned by an unsuccessful call.
    ///
    /// This type is used internally and not exposed to the client API.
    /// </summary>
    [Serializable]
    public struct ErrorResponse
    {
        /// <summary>
        /// The title of the error.
        /// </summary>
        public string title;
        /// <summary>
        /// The detailed information of the error.
        /// </summary>
        public string detail;
        /// <summary>
        /// The HTTP status code of the error.
        /// </summary>
        public int status;
    }

    /// <summary>
    /// This struct contains the information returned by a call to the room creation
    /// entry point of the REST-based API provided by the Go server.
    /// </summary>
    public struct CreateGameRoomResponse
    {
        /// <summary>
        /// True if the call was successful; false otherwise.
        /// </summary>
        public bool IsOk;
        /// <summary>
        /// The game room information returned after a successful call.
        /// </summary>
        public CreateGameRoomSuccessResponse Success;
        /// <summary>
        /// If the call was not successful, this field contains the details of the error.
        /// </summary>
        public ErrorResponse Error;
    }

    /// <summary>
    /// This struct contains the information returned by a successful call to the room creation
    /// entry point of the REST-based API provided by the Go server.
    ///
    /// This type is used internally and not exposed to the client API.
    /// </summary>
    [Serializable]
    public struct CreateGameRoomSuccessResponse
    {
        /// <summary>
        /// The IP address of the created game room.
        /// </summary>
        public string ip_address;
        /// <summary>
        /// The port number of the created game room.
        /// </summary>
        public int port;
    }

    /// <summary>
    /// This struct contains the information returned by a call to the room search
    /// entry point of the REST-based API provided by the Go server.
    /// </summary>
    public struct FindGameRoomsResponse
    {
        /// <summary>
        /// True if the call was successful; false otherwise.
        /// </summary>
        public bool IsOk;
        /// <summary>
        /// The game room information returned after a successful call.
        /// </summary>
        public FindGameRoomsSuccessResponse Success;
        /// <summary>
        /// If the call was not successful, this field contains the details of the error.
        /// </summary>
        public ErrorResponse Error;
    }

    /// <summary>
    /// This type contains the information of a spawned game room instance.
    /// </summary>
    [Serializable]
    public struct GameRoom
    {
        /// <summary>
        /// The public unique identifier of the game room.
        /// </summary>
        public string id;
        /// <summary>
        /// The name of the game room.
        /// </summary>
        public string name;
        /// <summary>
        /// The maximum number of players allowed in the game room.
        /// </summary>
        public int max_players;
        /// <summary>
        /// The current number of players in the game room.
        /// </summary>
        public int num_players;
    }

    /// <summary>
    /// This struct contains the information returned by a successful call to the room search
    /// entry point of the REST-based API provided by the Go server.
    /// 
    /// This type is used internally and not exposed to the client API.
    /// </summary>
    [Serializable]
    public struct FindGameRoomsSuccessResponse
    {
        /// <summary>
        /// A list containing all the game rooms found in the search.
        /// </summary>
        public List<GameRoom> game_rooms;
    }

    /// <summary>
    /// This struct contains the information returned by a call to the room join
    /// entry point of the REST-based API provided by the Go server.
    /// </summary>
    public struct JoinGameRoomResponse
    {
        /// <summary>
        /// True if the call was successful; false otherwise.
        /// </summary>
        public bool IsOk;
        /// <summary>
        /// The game room information returned after a successful call.
        /// </summary>
        public JoinGameRoomSuccessResponse Success;
        /// <summary>
        /// If the call was not successful, this field contains the details of the error.
        /// </summary>
        public ErrorResponse Error;
    }

    /// <summary>
    /// This struct contains the information returned by a successful call to the room join
    /// entry point of the REST-based API provided by the Go server.
    ///
    /// This type is used internally and not exposed to the client API.
    /// </summary>
    [Serializable]
    public struct JoinGameRoomSuccessResponse
    {
        /// <summary>
        /// The IP address of the game room instance to join.
        /// </summary>
        public string ip_address;
        /// <summary>
        /// The port number of the game room instance to join.
        /// </summary>
        public int port;
    }
}