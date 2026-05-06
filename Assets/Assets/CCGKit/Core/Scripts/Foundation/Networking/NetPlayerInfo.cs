// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;

namespace CCGKit
{
    public struct NetPlayerInfo
    {
        public int id;
        public NetworkIdentity netId;
        public int currentCardInstanceId;
        public NetStat[] stats;
        public NetStaticZone[] staticZones;
        public NetDynamicZone[] dynamicZones;
    }
}