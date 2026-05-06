// Mirror Networking stub — single-player project용 컴파일 에러 해소 스텁
// 실제 네트워크 기능 없음. CCGKit이 Mirror에 의존하나 이 프로젝트는 싱글플레이어이므로
// 스텁으로 컴파일만 통과시킵니다.
#pragma warning disable CS0067, CS0649

using System;
using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────
// Mirror namespace
// ──────────────────────────────────────────
namespace Mirror
{
    public interface NetworkMessage { }

    public abstract class NetworkBehaviour : MonoBehaviour
    {
        public bool isServer => false;
        public bool isClient => false;
        public bool isLocalPlayer => false;
        public bool hasAuthority => false;
        public NetworkIdentity netIdentity => null;
        public uint netId => 0;

        public virtual void OnStartServer() { }
        public virtual void OnStopServer() { }
        public virtual void OnStartClient() { }
        public virtual void OnStopClient() { }
        public virtual void OnStartLocalPlayer() { }
        public virtual void OnStopLocalPlayer() { }
        public virtual void OnStartAuthority() { }
        public virtual void OnStopAuthority() { }
        protected bool SyncVarHookGuard(uint dirtyBit) => false;
        protected void SetSyncVarHookGuard(uint dirtyBit, bool value) { }
        protected void SetSyncVar<T>(T value, ref T fieldValue, uint dirtyBit) { fieldValue = value; }
        protected void SetSyncVarGameObject(GameObject newGameObject, ref GameObject gameObjectField, uint dirtyBit, ref uint netIdField) { }
        protected void SetSyncVarNetworkIdentity(NetworkIdentity ni, ref NetworkIdentity field, uint dirtyBit, ref uint netIdField) { }
    }

    public class NetworkIdentity : MonoBehaviour
    {
        public uint netId;
        public bool isServer => false;
        public bool isClient => false;
        public bool isLocalPlayer => false;
        public NetworkConnection connectionToClient;
    }

    public class NetworkConnection
    {
        public int connectionId;   // stub: int for CCGKit compatibility
        public NetworkIdentity identity;
        public bool isReady;
        public void Send<T>(T msg) where T : struct, NetworkMessage { }
    }

    public class NetworkConnectionToClient : NetworkConnection { }

    public abstract class NetworkManager : MonoBehaviour
    {
        public string networkAddress = "localhost";
        public ushort maxConnections = 100;
        public Transport transport;

        public static NetworkManager singleton;

        public virtual void Awake() { }
        public virtual void Start() { }
        public virtual void StartServer() { }
        public virtual void StartClient() { }
        public virtual void StartHost() { }
        public virtual void StopServer() { }
        public virtual void StopClient() { }
        public virtual void StopHost() { }

        public virtual void OnServerConnect(NetworkConnectionToClient conn) { }
        public virtual void OnServerDisconnect(NetworkConnectionToClient conn) { }
        public virtual void OnServerAddPlayer(NetworkConnectionToClient conn) { }
        public virtual void OnStartServer() { }
        public virtual void OnStopServer() { }
        public virtual void OnStartHost() { }
        public virtual void OnStopHost() { }
        public virtual void OnStartClient() { }
        public virtual void OnStopClient() { }
        public virtual void OnClientConnect() { }
        public virtual void OnClientDisconnect() { }
        public virtual void OnClientError(Exception exception) { }

        // Older Mirror API compat
        public bool runInBackground { get; set; }
        public int maxDelay { get; set; }
    }

    public abstract class Transport : MonoBehaviour
    {
        public static Transport activeTransport;
    }

    public static class NetworkServer
    {
        public static bool active => false;
        public static Dictionary<uint, NetworkIdentity> spawned = new Dictionary<uint, NetworkIdentity>();

        public static void RegisterHandler<T>(Action<NetworkConnection, T> handler, bool requireAuthentication = true)
            where T : struct, NetworkMessage { }
        public static void UnregisterHandler<T>() where T : struct, NetworkMessage { }
        public static void SendToAll<T>(T msg, int channelId = 0) where T : struct, NetworkMessage { }
        public static void SendToReadyObservers<T>(NetworkIdentity identity, T msg, bool includeOwner = true, int channelId = 0)
            where T : struct, NetworkMessage { }
        public static bool Spawn(GameObject obj, NetworkConnection ownerConnection = null) => false;
        public static void Destroy(GameObject obj) { }
        public static void Shutdown() { }
    }

    public static class NetworkClient
    {
        public static bool isConnected => false;
        public static bool active => false;
        public static NetworkBehaviour localPlayer => null;

        public static void RegisterHandler<T>(Action<T> handler, bool requireAuthentication = false)
            where T : struct, NetworkMessage { }
        public static void UnregisterHandler<T>() where T : struct, NetworkMessage { }
        public static void Send<T>(T msg, int channelId = 0) where T : struct, NetworkMessage { }
        public static void Connect(string address) { }
        public static void Disconnect() { }
        public static void Shutdown() { }
    }

    public class TelepathyTransport : Transport
    {
        public int port = 7777;
    }
}

// ──────────────────────────────────────────
// kcp2k namespace
// ──────────────────────────────────────────
namespace kcp2k
{
    public class KcpTransport : Mirror.Transport
    {
        public ushort Port = 7777;
    }
}
