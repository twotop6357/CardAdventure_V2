// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

namespace CCGKit
{
    /// <summary>
    /// If you want to have additional, custom network messages in your game, you can handle them
    /// in this subclass.
    /// </summary>
    public class DemoNetworkClient : GameNetworkClient
    {
        /// <summary>
        /// Registers the network handlers for the network messages we are interested in handling.
        /// </summary>
        protected override void RegisterNetworkHandlers()
        {
            base.RegisterNetworkHandlers();
        }

        /// <summary>
        /// Unregisters the network handlers for the network messages we are interested in handling.
        /// </summary>
        protected override void UnregisterNetworkHandlers()
        {
            base.UnregisterNetworkHandlers();
        }
    }
}