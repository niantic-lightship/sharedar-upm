// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Niantic.Lightship.AR;
using UnityEngine;

#pragma warning disable 0067

namespace Niantic.Lightship.SharedAR.Networking.API
{
    internal class LightshipNetworkingApi : INetworkingApi
    {
        public IntPtr Init(string serverAddr, string roomId)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            Debug.Log("Attempting to initialize networking in CAPI");
            if (LightshipUnityContext.UnityContextHandle == IntPtr.Zero)
            {
                Debug.LogWarning("Could not initialize networking. Lightship context is not initialized.");
                return IntPtr.Zero;
            }
            return _InitRoom(LightshipUnityContext.UnityContextHandle, roomId);
#else
        throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void Join(IntPtr nativeHandle)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _Join(nativeHandle);
#else
        throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void Leave(IntPtr nativeHandle)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _Leave(nativeHandle);
#else
        throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void Release(IntPtr nativeHandle)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _Release(nativeHandle);
#else
        throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void SendData(
            IntPtr nativeHandle,
            UInt32 tag,
            byte[] data,
            UInt64 dataSize,
            UInt32[] peerIdentifiers
        )
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _SendData(nativeHandle, tag, data, dataSize, peerIdentifiers, (UInt64)peerIdentifiers.Length);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public byte GetNetworkingState(IntPtr nativeHandle)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            return _GetNetworkingState(nativeHandle);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public UInt32 GetSelfPeerId(IntPtr nativeHandle)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            return _GetSelfPeerId(nativeHandle);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public UInt64 GetPeerIds(IntPtr nativeHandle, UInt32[] outPeerIds, UInt64 maxPeers)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            return _GetPeerIds(nativeHandle, outPeerIds, maxPeers);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void SetNetworkEventCallback
            (IntPtr managedHandle, IntPtr nativeHandle, INetworkingApi.NetworkEventCallback cb)
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            UnityEngine.Debug.Log("Setting network event cb in CAPI");
            _SetConnectionEventCallback(managedHandle, nativeHandle, cb);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void SetPeerAddedCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.PeerAddedOrRemovedCallback cb
        )
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _SetPeerAddedCallback(managedHandle, nativeHandle, cb);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void SetPeerRemovedCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.PeerAddedOrRemovedCallback cb
        )
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _SetPeerRemovedCallback(managedHandle, nativeHandle, cb);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

        public void SetDataReceivedCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.DataReceivedCallback cb
        )
        {
#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED
            _SetDataReceivedCallback(managedHandle, nativeHandle, cb);
#else
      throw new PlatformNotSupportedException("Unsupported platform");
#endif
        }

#if NIANTIC_LIGHTSHIP_AR_LOADER_ENABLED

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Unity_Sharc_Room_Init")]
        private static extern IntPtr _InitRoom(IntPtr unityContextHandle, string roomId);

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Room_Join")]
        private static extern void _Join(IntPtr nativeHandle);

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Room_Leave")]
        private static extern void _Leave(IntPtr nativeHandle);

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Release")]
        private static extern void _Release(IntPtr nativeHandle);

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_SendData")]
        private static extern void _SendData
        (
            IntPtr nativeHandle,
            UInt32 tag,
            byte[] data,
            UInt64 dataSize,
            UInt32[] peerIdentifiers,
            UInt64 peerIdentifiersSize
        );

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_GetNetworkingState")]
        private static extern byte _GetNetworkingState(IntPtr nativeHandle);

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_GetSelfPeerId")]
        private static extern UInt32 _GetSelfPeerId
        (
            IntPtr nativeHandle
        );

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_GetPeerIds")]
        private static extern UInt64 _GetPeerIds
        (
            IntPtr nativeHandle,
            UInt32[] outPeerIds,
            UInt64 maxPeers
        );

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_SetNetworkingEventCallback")]
        private static extern void _SetConnectionEventCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.NetworkEventCallback cb
        );

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_SetPeerAddedCallback")]
        private static extern void _SetPeerAddedCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.PeerAddedOrRemovedCallback cb
        );

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_SetPeerRemovedCallback")]
        private static extern void _SetPeerRemovedCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.PeerAddedOrRemovedCallback cb
        );

        [DllImport(_LightshipPlugin.Name, EntryPoint = "Lightship_ARDK_Sharc_Networking_SetDataReceivedCallback")]
        private static extern void _SetDataReceivedCallback
        (
            IntPtr managedHandle,
            IntPtr nativeHandle,
            INetworkingApi.DataReceivedCallback cb
        );

#endif
    }
}

#pragma warning restore 0067
