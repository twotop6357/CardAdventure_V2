// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;
using UnityEngine;

namespace DedicatedServerKit
{
    /// <summary>
    /// The adapter for the Telepathy transport.
    /// </summary>
    [RequireComponent(typeof(TelepathyTransport))]
    public class TelepathyTransportAdapter : TransportAdapter
    {
        /// <summary>
        /// The associated transport.
        /// </summary>
        private TelepathyTransport transport;

        /// <summary>
        /// The Awake method caches the associated transport.
        /// </summary>
        private void Awake()
        {
            transport = GetComponent<TelepathyTransport>();
        }

        /// <summary>
        /// Sets the port number of the transport.
        /// </summary>
        /// <param name="port">The port number.</param>
        public override void SetPort(ushort port)
        {
            transport.port = port;
        }
    }
}