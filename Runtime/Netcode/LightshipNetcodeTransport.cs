// Copyright 2022-2024 Niantic.

using System;
using System.Collections.Generic;
using Niantic.Lightship.AR.Utilities.Logging;
using Niantic.Lightship.SharedAR.Networking;
using Niantic.Lightship.SharedAR.Rooms;
using Niantic.Lightship.AR.Utilities;
using Unity.Netcode;
using System.Threading;
using UnityEngine;

namespace Niantic.Lightship.SharedAR.Netcode
{
    /// <summary>
    /// Lightship's Netcode for GameObjects compatibility layer. Implemented using the Room and
    /// INetworking apis.
    /// </summary>
    [PublicAPI]
    public class LightshipNetcodeTransport : NetworkTransport
    {
        private struct CachedEvent
        {
            public ulong clientId;
            public ArraySegment<byte> payload;
            public float receiveTime;
            public NetworkEvent eventType;
        }

        private byte[] _emptyMessage = new byte[1] { 0 };
        private Queue<CachedEvent> _eventCache = new Queue<CachedEvent>();

        private IRoom _room;
        private INetworking _networking;

        private System.Diagnostics.Stopwatch _networkWatch = new System.Diagnostics.Stopwatch();

        private ulong _serverClient;
        private bool _serverClientHasValidId;
        private bool _isHost = false;
        private Dictionary<uint, bool> _clientPeers;

        private const uint kNetcodeDataTag = 1;
        private const uint kClientAnnouncementTag = 2;
        private const uint kServerAckTag = 3;
        private const uint kServerForceDisconnectTag = 4;

        private const ushort kMaxDataSize = 1500;

        private NetcodeSessionStats _statsCache;

        private uint _lastNetworkError;

        /// <summary>
        /// Session stats describing network usage.
        /// </summary>
        [PublicAPI]
        public struct NetcodeSessionStats
        {
            public ulong TotalBytesSent;
            public ulong TotalBytesReceived;
            public uint TotalMessagesSent;
            public uint TotalMessagesReceived;
            public int PeerCount;
            public float Timestamp;

            /// <summary>
            /// Compare two NetcodeSessionStats to calculate bandwidth usage. Order is determined
            /// automatically.
            /// </summary>
            /// <param name="stats1">First stat snapshot.</param>
            /// <param name="stats2">Second stat snapshot.</param>
            /// <param name="bytesSentPerSec">Outgoing bytes per second.</param>
            /// <param name="messagesSentPerSec">Outgoing messages per second.</param>
            /// <param name="bytesReceivedPerSec">Incoming bytes per second.</param>
            /// <param name="messagesReceivedPerSec">Incoming messages per second.</param>
            public static void GetPerSecondStats(NetcodeSessionStats stats1,
                NetcodeSessionStats stats2,
                out float bytesSentPerSec,
                out float messagesSentPerSec,
                out float bytesReceivedPerSec,
                out float messagesReceivedPerSec)
            {
                NetcodeSessionStats A, B;
                if (stats1.Timestamp > stats2.Timestamp)
                {
                    A = stats1;
                    B = stats2;
                }
                else
                {
                    A = stats2;
                    B = stats1;
                }

                var deltaT = (A.Timestamp - B.Timestamp) / 1000.0f;
                bytesSentPerSec = (A.TotalBytesSent - B.TotalBytesSent) / deltaT;
                messagesSentPerSec = (A.TotalMessagesSent - B.TotalMessagesSent) / deltaT;
                bytesReceivedPerSec = (A.TotalBytesReceived - B.TotalBytesReceived) / deltaT;
                messagesReceivedPerSec = (A.TotalMessagesReceived - B.TotalMessagesReceived) / deltaT;
            }
        }

        /// <summary>
        /// Poll the current stats of the active netcode session.
        /// </summary>
        [PublicAPI]
        public NetcodeSessionStats GetNetcodeSessionStats()
        {
            var stats = _statsCache;
            if (_networking != null)
            {
                stats.PeerCount = _networking.PeerIDs.Count;
                stats.Timestamp = GetTimeMS();
            }
            return stats;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        /// <summary>
        /// Send a payload to the specified clientId, data and networkDelivery.
        /// </summary>
        /// <param name="clientId">The clientId to send to</param>
        /// <param name="payload">The data to send</param>
        /// <param name="networkDelivery">The delivery type (QoS) to send data with</param>
        [PublicAPI]
        public override void Send(ulong clientId, ArraySegment<byte> data,
            NetworkDelivery delivery = NetworkDelivery.Reliable)
        {
            if (_networking == null)
            {
                return;
            }

            if (clientId == ServerClientId)
            {
                clientId = _serverClient;
            }

            _statsCache.TotalBytesSent += (ulong)data.Count;
            _statsCache.TotalMessagesSent += 1;

            var peer = new PeerID((UInt32)clientId);
            var bytes = new byte[data.Count];
            Array.Copy(data.Array, 0, bytes, 0, data.Count);
            if (bytes.Length > kMaxDataSize)
            {
                Log.Error("Trying to send some massive data");
            }

            _networking.SendData(new List<PeerID>() { peer }, kNetcodeDataTag, bytes);
        }

        /// <summary>
        /// Polls for incoming events, with an extra output parameter to report the precise time the event was received.
        /// </summary>
        /// <param name="clientId">The clientId this event is for</param>
        /// <param name="payload">The incoming data payload</param>
        /// <param name="receiveTime">The time the event was received, as reported by Time.realtimeSinceStartup.</param>
        /// <returns>Returns the event type</returns>
        [PublicAPI]
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload,
            out float receiveTime)
        {
            CachedEvent ev;
            bool gotEvent;
            lock (_eventCache)
            {
                gotEvent = _eventCache.TryDequeue(out ev);
            }

            if (gotEvent)
            {
                clientId = ev.clientId;
                payload = ev.payload;
                receiveTime = ev.receiveTime;
                return ev.eventType;
            }
            else
            {
                payload = default;
                receiveTime = 0;
                clientId = 0;
                return NetworkEvent.Nothing;
            }
        }

        private void QueueNetworkEvent(NetworkEvent type, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (clientId == _serverClient)
            {
                clientId = ServerClientId;
            }

            var cache = new CachedEvent();
            cache.eventType = type;
            cache.clientId = clientId;
            cache.payload = payload;
            cache.receiveTime = receiveTime;

            lock (_eventCache)
            {
                _eventCache.Enqueue(cache);
            }
        }

        /// <summary>
        /// Set the Lightship Room that we want to use Netcode for Gameobjects in. Set this
        /// before calling "StartClient" or "StartServer".
        /// </summary>
        [PublicAPI]
        public void SetRoom(IRoom room)
        {
            _room = room;
            if (room.Networking != null)
            {
                _networking = room.Networking;
            }
        }

        private void PrepareNetworkSession()
        {
            if (_room == null)
            {
                Log.Warning("Cannot start network session, as room is not set yet");
                return;
            }
            _networkWatch.Start();
            _statsCache = new();

            _room.Initialize(); // This will init native and create Networking object
            _networking = _room.Networking;

            _networking.DataReceived += OnPeerDataReceived;
            _networking.PeerAdded += OnPeerAdded;
            _networking.PeerRemoved += OnPeerRemoved;
            _networking.NetworkEvent += OnNetworkEvent;
        }

        /// <summary>
        /// Connects client to the server
        /// </summary>
        /// <returns>Returns success or failure</returns>
        [PublicAPI]
        public override bool StartClient()
        {
            PrepareNetworkSession();
            if (_networking == null) {
                Log.Warning("Preparing network session failed.");
                return false;
            }

            if (_networking.NetworkState == NetworkState.NotInRoom)
            {
                _room.Join();
            }
            else if (_networking.NetworkState == NetworkState.InRoom)
            {
                SetupClient();
            }
            return true;
        }

        /// <summary>
        /// Starts to listening for incoming clients
        /// </summary>
        /// <returns>Returns success or failure</returns>
        [PublicAPI]
        public override bool StartServer()
        {
            _isHost = true;

            _clientPeers = new Dictionary<uint, bool>();

            PrepareNetworkSession();
            if (_networking == null) {
                Log.Warning("Preparing network session failed.");
                return false;
            }

            if (_networking.NetworkState == NetworkState.NotInRoom)
            {
                _room.Join();
            }
            else if (_networking.NetworkState == NetworkState.InRoom)
            {
                SetupServer();
            }

            return true;
        }

        private float GetTimeMS()
        {
            return (float)_networkWatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Disconnects a client from the server
        /// </summary>
        /// <param name="clientId">The clientId to disconnect</param>
        [PublicAPI]
        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (_networking == null)
            {
                return;
            }
            _networking.SendData(
                new List<PeerID>() { new PeerID((UInt32)clientId) },
                kServerForceDisconnectTag,
                _emptyMessage);
        }

        /// <summary>
        /// Disconnects the local client from the server
        /// </summary>
        [PublicAPI]
        public override void DisconnectLocalClient()
        {
            if (_room != null)
            {
                _room.Leave();
            }
        }

        /// <summary>
        /// Gets the round trip time for a specific client. This method is not implemented for Lightship
        /// </summary>
        /// <param name="clientId">The clientId to get the RTT from</param>
        /// <returns>Returns 0 always</returns>
        [PublicAPI]
        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        /// <summary>
        /// Shuts down the transport
        /// </summary>
        [PublicAPI]
        public override void Shutdown()
        {
            if (_room != null)
            {
                if (_networking != null)
                {
                    _networking.DataReceived -= OnPeerDataReceived;
                    _networking.PeerAdded -= OnPeerAdded;
                    _networking.PeerRemoved -= OnPeerRemoved;
                    _networking.NetworkEvent -= OnNetworkEvent;
                    _networking = null;
                }
                DisconnectLocalClient();
                _networkWatch.Stop();
                lock (_eventCache)
                {
                    _eventCache.Clear();
                }
                _room.Dispose(); // clean up native resources
                _room = null;
            }
        }

        /// <summary>
        /// Initializes the transport. Automatically called by Netcode.
        /// </summary>
        /// <param name="networkManager">NetworkManager managing the netcode session</param>
        [PublicAPI]
        public override void Initialize(NetworkManager manager)
        {
            _serverClientHasValidId = false;
            _lastNetworkError = 0;
        }

        private void SetupServer()
        {
            if (_networking == null)
            {
                Log.Error("Networking was null when setting up server");
                return;
            }

            _serverClient = _networking.SelfPeerID.ToUint32();
            _serverClientHasValidId = true;
        }

        private void SetupClient()
        {
            if (_networking == null)
            {
                Log.Error("Networking was null when setting up client");
                return;
            }

            _networking.SendData(_networking.PeerIDs, kClientAnnouncementTag, _emptyMessage);
        }

        private void OnNetworkEvent(NetworkEventArgs args)
        {
            if (args.networkEvent == NetworkEvents.Connected)
            {
                if (_isHost)
                {
                    SetupServer();
                }
                else
                {
                    SetupClient();
                }
            }

            if (args.networkEvent == NetworkEvents.Disconnected ||
                args.networkEvent == NetworkEvents.ArdkShutdown)
            {
                QueueNetworkEvent
                (
                    NetworkEvent.Disconnect,
                    _networking.SelfPeerID.ToUint32(),
                    new ArraySegment<byte>(),
                    GetTimeMS()
                );

                // Shutdown network resources if ARDK is shutting down
                if (args.networkEvent == NetworkEvents.ArdkShutdown)
                {
                    Shutdown();
                }
            }

            if (args.networkEvent == NetworkEvents.ConnectionError ||
                args.networkEvent == NetworkEvents.RoomFull)
            {
                QueueNetworkEvent
                (
                    NetworkEvent.TransportFailure,
                    _networking.SelfPeerID.ToUint32(),
                    new ArraySegment<byte>(),
                    GetTimeMS()
                );
                _lastNetworkError = args.errorCode;
            }
        }

        private void OnPeerRemoved(PeerIDArgs args)
        {
            var leftPeerId = args.PeerID.ToUint32();
            // Host receives all peer removed event
            // Client receives host peer removed event only
            if (_isHost || leftPeerId == _serverClient)
            {
                QueueNetworkEvent
                (
                    NetworkEvent.Disconnect,
                    leftPeerId,
                    new ArraySegment<byte>(),
                    GetTimeMS()
                );
            }

            // host maintains the client map
            if (_isHost)
            {
                if (_clientPeers != null)
                {
                    _clientPeers.Remove(leftPeerId);
                }
            }
        }

        private void OnPeerAdded(PeerIDArgs args)
        {
            // Send client announcement if no server peerID set yet
            if (!_isHost && !_serverClientHasValidId)
            {
                _networking.SendData(_networking.PeerIDs, kClientAnnouncementTag, _emptyMessage);
            }
        }

        private void OnPeerDataReceived(DataReceivedArgs args)
        {
            if (args.PeerID.Equals(_networking.SelfPeerID))
                return;

            if (args.Tag == kNetcodeDataTag)
            {
                var idAsUlong = args.PeerID.ToUint32();
                var data = args.CopyData();
                _statsCache.TotalBytesReceived += (ulong)data.Length;
                _statsCache.TotalMessagesReceived += 1;

                QueueNetworkEvent(
                    NetworkEvent.Data,
                    idAsUlong,
                    new ArraySegment<byte>(data),
                    GetTimeMS());
            }
            else if (args.Tag == kClientAnnouncementTag)
            {
                if (_isHost)
                {
                    // Check if already processed for this client
                    if (_clientPeers == null)
                    {
                        _clientPeers = new Dictionary<uint, bool>();
                    }
                    var sender = args.PeerID.ToUint32();
                    if (_clientPeers.ContainsKey(sender))
                    {
                        if (_clientPeers[sender])
                        {
                            // Already processed this client's announcement msg. Ignore.
                            return;
                        }
                    }
                    // process adding a client
                    QueueNetworkEvent(NetworkEvent.Connect,
                        args.PeerID.ToUint32(),
                        new ArraySegment<byte>(),
                        GetTimeMS());
                    _networking.SendData(
                        new List<PeerID>() { args.PeerID },
                        kServerAckTag,
                        _emptyMessage);
                    _clientPeers.Add(sender, true);
                }
            }
            else if (args.Tag == kServerAckTag)
            {
                var idAsUlong = args.PeerID.ToUint32();
                _serverClient = idAsUlong;
                _serverClientHasValidId = true;
                QueueNetworkEvent(
                    NetworkEvent.Connect,
                    _networking.SelfPeerID.ToUint32(),
                    new ArraySegment<byte>(),
                    GetTimeMS());
            }
            else if (args.Tag == kServerForceDisconnectTag && args.PeerID.ToUint32() == _serverClient)
            {
                DisconnectLocalClient();
            }
        }

        /// <summary>
        /// A constant netcode clientId that represents the server
        /// When this value is found in methods such as Send, it should be treated as a placeholder that means the server
        /// </summary>
        /// <param name="networkManager">NetworkManager managing the netcode session</param>
        [PublicAPI]
        public override ulong ServerClientId
        {
            get { return NetworkManager.ServerClientId; }
        }

        /// <summary>
        /// Get error code from the last network error. If no error, returns 0. Error codes are defined as const in
        /// Niantic.Lightship.SharedAR.Networking.NetworkEventErrorCode
        /// </summary>
        /// <returns>Error code</returns>
        [PublicAPI]
        public uint GetLastNetworkError()
        {
            return _lastNetworkError;
        }
    }
}
