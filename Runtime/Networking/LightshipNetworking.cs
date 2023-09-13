// Copyright 2022 Niantic, Inc. All Rights Reserved.

#pragma warning disable 0067

using System;
using AOT; // MonoPInvokeCallback attribute
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Niantic.Lightship.AR;
using Niantic.Lightship.SharedAR.Networking.API;

namespace Niantic.Lightship.SharedAR.Networking
{
    // @note This is an experimental feature. Experimental features should not be used in
    // production products as they are subject to breaking changes, not officially supported, and
    // may be deprecated without notice
    public class LightshipNetworking : INetworking
    {
        private bool _isDestroyed;
        private bool _didSubscribeToNativeEvents;
        private PeerID _selfPeerId = PeerID.InvalidID;
        private INetworkingApi _nativeApi;
        internal IntPtr _nativeHandle;

        #region Handles

        private IntPtr _cachedHandleIntPtr = IntPtr.Zero;
        private GCHandle _cachedHandle;

        // Approx memory size of native object
        // Magic number for 64
        private const long GCPressure = 64L * 1024L;

        // Used to round-trip a pointer through c++,
        // so that we can keep our this pointer even in c# functions
        // marshaled and called by native code
        private IntPtr _managedHandle
        {
            get
            {
                if (_cachedHandleIntPtr != IntPtr.Zero)
                    return _cachedHandleIntPtr;

                lock (this)
                {
                    if (_cachedHandleIntPtr != IntPtr.Zero)
                        return _cachedHandleIntPtr;

                    // https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.gchandle.tointptr.aspx
                    _cachedHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                    _cachedHandleIntPtr = GCHandle.ToIntPtr(_cachedHandle);
                }

                return _cachedHandleIntPtr;
            }
        }

        #endregion

        private const string DEFAULT_SESSION = "default_session_id";
        private const UInt64 MAX_PEER_COUNT = 32;

        public string SessionId { get; private set; } = DEFAULT_SESSION;
        public event Action<NetworkEventArgs> NetworkEvent;
        public event Action<PeerIDArgs> PeerAdded;
        public event Action<PeerIDArgs> PeerRemoved;
        public event Action<DataReceivedArgs> DataReceived;

        public LightshipNetworking
        (
            string serverAddr,
            string roomId
        ) : this(serverAddr, roomId,
            new LightshipNetworkingApi()
        )
        {
        }

        internal LightshipNetworking
        (
            string serverAddr,
            string roomId,
            INetworkingApi api
        )
        {
            LightshipUnityContext.OnDeinitialized += HandleArdkDeinitialized;
            _nativeApi = api;
            _nativeHandle = _nativeApi.Init(serverAddr, roomId);
            if (!IsNativeHandleValid()) {
                return;
            }
            GC.AddMemoryPressure(GCPressure);
            SessionId = roomId;
            SubscribeToNativeCallbacks();
        }

        private bool IsNativeHandleValid()
        {
            if (_nativeHandle == IntPtr.Zero)
            {
                Debug.LogWarning("Invalid native handle");
                return false;
            }
            return true;
        }

        public void Join()
        {
            if (!IsNativeHandleValid())
            {
                return;
            }
            _nativeApi.Join(_nativeHandle);
        }

        public void SendData
        (
            List<PeerID> targetPeers,
            uint tag,
            byte[] data
        )
        {
            if (!IsNativeHandleValid())
            {
                return;
            }
            var peerIdentifiers = new UInt32[targetPeers.Count];
            for (var i = 0; i < targetPeers.Count; i++)
            {
                peerIdentifiers[i] = targetPeers[i].ToUint32();
            }

            _nativeApi.SendData
            (
                _nativeHandle,
                tag,
                data,
                (ulong)data.Length,
                peerIdentifiers
            );
        }

        public NetworkState NetworkState
        {
            get
            {
                if (!IsNativeHandleValid())
                {
                    return NetworkState.NotInRoom;
                }
                return (NetworkState)_nativeApi.GetNetworkingState(_nativeHandle);
            }
        }

        public PeerID SelfPeerID
        {
            get
            {
                if (!IsNativeHandleValid())
                {
                    return PeerID.InvalidID;
                }

                if (_selfPeerId.Equals(PeerID.InvalidID))
                    _selfPeerId = new PeerID(_nativeApi.GetSelfPeerId(_nativeHandle));

                return _selfPeerId;
            }
        }

        public List<PeerID> PeerIDs
        {
            get
            {
                if (!IsNativeHandleValid())
                {
                    return null;
                }

                var outPeers = new UInt32[MAX_PEER_COUNT];
                var count = _nativeApi.GetPeerIds(_nativeHandle, outPeers, MAX_PEER_COUNT);

                var list = new List<PeerID>();
                for (UInt64 i = 0; i < count; ++i)
                    list.Add(new PeerID(outPeers[i]));

                return list;
            }
        }

        public void Leave()
        {
            if (!IsNativeHandleValid())
            {
                return;
            }
            _nativeApi.Leave(_nativeHandle);
        }

        public void Dispose()
        {
            LightshipUnityContext.OnDeinitialized -= HandleArdkDeinitialized;
            if (!IsNativeHandleValid())
            {
                return;
            }
            _nativeApi.Release(_nativeHandle);
            _registeredNetworking = null;
            _nativeHandle = IntPtr.Zero;
        }

        // TODO: Temporary solution until AR-16347
        private static LightshipNetworking _registeredNetworking;

        private void SubscribeToNativeCallbacks()
        {
            if (_didSubscribeToNativeEvents)
                return;

            lock (this)
            {
                if (_didSubscribeToNativeEvents)
                    return;

                _registeredNetworking = this;

                _nativeApi.SetNetworkEventCallback
                    (_managedHandle, _nativeHandle, _networkEventReceivedNative);
                _nativeApi.SetPeerAddedCallback(_managedHandle, _nativeHandle, _didAddPeerNative);
                _nativeApi.SetPeerRemovedCallback(_managedHandle, _nativeHandle, _didRemovePeerNative);
                _nativeApi.SetDataReceivedCallback(_managedHandle, _nativeHandle, _dataReceivedNative);

                _didSubscribeToNativeEvents = true;
            }
        }

        private void HandleArdkDeinitialized()
        {
            // Invoke ArdkShutdown event when ARDK is deinitializing, so that user of Networking can dispose
            // Networking resources
            LightshipUnityContext.OnDeinitialized -= HandleArdkDeinitialized;
            var args = new NetworkEventArgs(NetworkEvents.ArdkShutdown);
            NetworkEvent?.Invoke(args);
        }

        [MonoPInvokeCallback(typeof(INetworkingApi.NetworkEventCallback))]
        private static void _networkEventReceivedNative(IntPtr managedHandle, byte networkEvent)
        {
            var instance = GCHandle.FromIntPtr(managedHandle).Target as LightshipNetworking;

            if (instance == null || instance._isDestroyed)
                return;

            var handler = instance.NetworkEvent;
            if (handler != null)
            {
                var args = new NetworkEventArgs((NetworkEvents)networkEvent);
                handler(args);
            }
        }

        [MonoPInvokeCallback(typeof(INetworkingApi.PeerAddedOrRemovedCallback))]
        private static void _didAddPeerNative(IntPtr managedHandle, UInt32 peerIdUint)
        {
            var instance = GCHandle.FromIntPtr(managedHandle).Target as LightshipNetworking;

            if (instance == null || instance._isDestroyed)
            {
                return;
            }

            var peerId = new PeerID(peerIdUint);

            var handler = instance.PeerAdded;
            if (handler != null)
            {
                var args = new PeerIDArgs(peerId);
                handler(args);
            }
        }

        [MonoPInvokeCallback(typeof(INetworkingApi.PeerAddedOrRemovedCallback))]
        private static void _didRemovePeerNative(IntPtr managedHandle, UInt32 peerIdUint)
        {
            var instance = GCHandle.FromIntPtr(managedHandle).Target as LightshipNetworking;

            if (instance == null || instance._isDestroyed)
            {
                Debug.LogWarning("_didRemovePeerNative invoked after C# instance was destroyed.");
                return;
            }

            var peerId = new PeerID(peerIdUint);

            var handler = instance.PeerRemoved;
            if (handler != null)
            {
                var args = new PeerIDArgs(peerId);
                handler(args);
            }
        }

        [MonoPInvokeCallback(typeof(INetworkingApi.DataReceivedCallback))]
        private static void _dataReceivedNative
        (
            IntPtr managedHandle,
            UInt32 fromPeerId,
            UInt32 tag,
            IntPtr rawData,
            UInt64 rawDataSize
        )
        {
            var instance = GCHandle.FromIntPtr(managedHandle).Target as LightshipNetworking;

            if (instance == null || instance._isDestroyed)
            {
                Debug.LogWarning("_dataReceivedNative called after C# instance was destroyed.");
                return;
            }

            var data = new byte[rawDataSize];
            Marshal.Copy(rawData, data, 0, (int)rawDataSize);

            var peerId = new PeerID(fromPeerId);

            var handler = instance.DataReceived;
            if (handler != null)
            {
                handler(new DataReceivedArgs(peerId, tag, data));
            }
        }
    }
}
#pragma warning restore 0067
