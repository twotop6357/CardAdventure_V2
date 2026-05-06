// Copyright (C) 2020 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;

namespace DedicatedServerKit
{
    /// <summary>
    /// This class provides a generic way to set the port of any Mirror transport
    /// used with the kit. We need to do this because the base Transport class in
    /// Mirror does not expose a 'port number' property (every transport provides
    /// its own one instead).
    /// </summary>
    public abstract class TransportAdapter : MonoBehaviour
    {
        /// <summary>
        /// Sets the port number of the transport.
        /// </summary>
        /// <param name="port">The port number.</param>
        public abstract void SetPort(ushort port);
    }
}