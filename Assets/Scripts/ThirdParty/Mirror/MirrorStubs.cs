// Mirror Networking Stubs
// These provide compile-time type definitions so the project builds
// without Mirror installed. Replace with the real Mirror package:
//   Unity Package Manager → Add package from git URL:
//   https://github.com/MirrorNetworking/Mirror.git?path=/Assets/Mirror
// Then delete this file.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    // ── Attributes ────────────────────────────────────────────────────────────

    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVarAttribute : Attribute
    {
        public string hook;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public bool requiresAuthority = true;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : Attribute
    {
        public int channel = 0;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ServerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : Attribute { }

    // ── Core Types ────────────────────────────────────────────────────────────

    public class NetworkConnectionToClient
    {
        public int connectionId;
        public NetworkIdentity identity;
    }

    public class NetworkIdentity : MonoBehaviour
    {
        public uint netId;
        public NetworkConnectionToClient connectionToClient;
        public bool isServer;
        public bool isClient;
        public bool isLocalPlayer;
    }

    public class NetworkBehaviour : MonoBehaviour
    {
        public uint netId
        {
            get
            {
                var id = GetComponent<NetworkIdentity>();
                return id != null ? id.netId : 0;
            }
        }
        public bool isServer
        {
            get
            {
                var id = GetComponent<NetworkIdentity>();
                return id != null && id.isServer;
            }
        }
        public bool isClient
        {
            get
            {
                var id = GetComponent<NetworkIdentity>();
                return id != null && id.isClient;
            }
        }
        public bool isLocalPlayer
        {
            get
            {
                var id = GetComponent<NetworkIdentity>();
                return id != null && id.isLocalPlayer;
            }
        }
        public NetworkConnectionToClient connectionToClient
        {
            get
            {
                var id = GetComponent<NetworkIdentity>();
                return id != null ? id.connectionToClient : null;
            }
        }

        public virtual void OnStartServer() { }
        public virtual void OnStartClient() { }
        public virtual void OnStartLocalPlayer() { }
        public virtual void OnStopServer() { }
        public virtual void OnStopClient() { }
    }

    // ── NetworkServer ─────────────────────────────────────────────────────────

    public static class NetworkServer
    {
        public static readonly Dictionary<uint, NetworkIdentity> spawned =
            new Dictionary<uint, NetworkIdentity>();

        public static readonly Dictionary<int, NetworkConnectionToClient> connections =
            new Dictionary<int, NetworkConnectionToClient>();

        public static bool active => false;

        public static void Spawn(GameObject obj) { }
        public static void Spawn(GameObject obj, NetworkConnectionToClient conn) { }
        public static void Destroy(GameObject obj) { UnityEngine.Object.Destroy(obj); }
        public static void AddPlayerForConnection(NetworkConnectionToClient conn, GameObject player) { }
    }

    // ── NetworkClient ─────────────────────────────────────────────────────────

    public static class NetworkClient
    {
        public static NetworkConnectionToClient connection;
        public static NetworkIdentity localPlayer;
        public static bool active => false;
        public static bool isConnected => false;

        public static readonly Dictionary<uint, NetworkIdentity> spawned =
            new Dictionary<uint, NetworkIdentity>();

        public static bool TryGetValue(uint netId, out NetworkIdentity identity)
        {
            return spawned.TryGetValue(netId, out identity);
        }
    }

    // ── NetworkManager ────────────────────────────────────────────────────────

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager singleton;

        public int maxConnections = 4;
        public GameObject playerPrefab;
        public List<GameObject> spawnPrefabs = new List<GameObject>();
        public Transport transport;

        public string networkAddress = "localhost";

        protected virtual void Awake()
        {
            if (singleton == null) singleton = this;
        }

        public virtual void OnServerAddPlayer(NetworkConnectionToClient conn) { }
        public virtual void OnServerDisconnect(NetworkConnectionToClient conn) { }
        public virtual void OnClientConnect() { }
        public virtual void OnClientDisconnect() { }
        public virtual void OnStartServer() { }
        public virtual void OnStopServer() { }
        public virtual void OnStartClient() { }
        public virtual void OnStopClient() { }
        public virtual void OnServerReady(NetworkConnectionToClient conn) { }

        public void StartHost() { }
        public void StartServer() { }
        public void StartClient() { }
        public void StopHost() { }
        public void StopServer() { }
        public void StopClient() { }

        public virtual void ServerChangeScene(string newSceneName) { }
    }

    // ── Transport ─────────────────────────────────────────────────────────────

    public abstract class Transport : MonoBehaviour
    {
        public static Transport active;
    }

    public class KcpTransport : Transport { }

    // ── Sync Collections ──────────────────────────────────────────────────────

    [Serializable]
    public class SyncList<T> : List<T>
    {
        public delegate void SyncListChanged(Operation op, int itemIndex, T oldItem, T newItem);
        public event SyncListChanged Callback;

        public enum Operation { OP_ADD, OP_CLEAR, OP_INSERT, OP_REMOVEAT, OP_SET }

        public new void Add(T item)
        {
            base.Add(item);
            Callback?.Invoke(Operation.OP_ADD, Count - 1, default, item);
        }

        public new void RemoveAt(int index)
        {
            T old = this[index];
            base.RemoveAt(index);
            Callback?.Invoke(Operation.OP_REMOVEAT, index, old, default);
        }

        public new void Clear()
        {
            base.Clear();
            Callback?.Invoke(Operation.OP_CLEAR, 0, default, default);
        }
    }

    public class SyncIDictionary<TKey, TValue>
    {
        public enum Operation { OP_ADD, OP_CLEAR, OP_REMOVE, OP_SET }
    }

    public class SyncDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public delegate void SyncDictionaryChanged(SyncIDictionary<TKey, TValue>.Operation op, TKey key, TValue item);
        public event SyncDictionaryChanged Callback;

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            Callback?.Invoke(SyncIDictionary<TKey, TValue>.Operation.OP_ADD, key, value);
        }

        public new bool Remove(TKey key)
        {
            if (TryGetValue(key, out TValue val))
            {
                base.Remove(key);
                Callback?.Invoke(SyncIDictionary<TKey, TValue>.Operation.OP_REMOVE, key, val);
                return true;
            }
            return false;
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                bool existed = ContainsKey(key);
                base[key] = value;
                Callback?.Invoke(existed ? SyncIDictionary<TKey, TValue>.Operation.OP_SET
                    : SyncIDictionary<TKey, TValue>.Operation.OP_ADD, key, value);
            }
        }

        public new void Clear()
        {
            base.Clear();
            Callback?.Invoke(SyncIDictionary<TKey, TValue>.Operation.OP_CLEAR, default, default);
        }
    }

    // ── Channels ──────────────────────────────────────────────────────────────

    public static class Channels
    {
        public const int Reliable = 0;
        public const int Unreliable = 1;
    }
}
