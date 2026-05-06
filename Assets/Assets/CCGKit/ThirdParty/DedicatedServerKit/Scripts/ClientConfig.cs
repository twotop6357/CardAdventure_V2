// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;

namespace DedicatedServerKit
{
    /// <summary>
    /// This class defines the client configuration asset that can be created by
    /// right-clicking in the Project window and selecting the appropriate entry.
    ///
    /// A client configuration asset contains the essential information used to
    /// connect to the Go server. It can be useful to store different configurations
    /// (e.g., demo, production, etc.) and easily switch between them.
    /// </summary>
    [CreateAssetMenu(menuName = "Dedicated Server Kit/Client config", fileName = "ClientConfig", order = 0)]
    public class ClientConfig : ScriptableObject
    {
        /// <summary>
        /// The IP address of the Go server to connect to.
        /// </summary>
        public string IpAddress;

        /// <summary>
        /// The port number of the Go server to connect to.
        /// </summary>
        public int Port;
    }
}
