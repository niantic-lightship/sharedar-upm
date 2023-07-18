// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using Niantic.Lightship.SharedAR.Colocalization;
using Niantic.Lightship.SharedAR.Networking;
using Niantic.Lightship.SharedAR.Datastore;
using UnityEngine;

namespace Niantic.Lightship.SharedAR.Rooms
{
    /// @note This is an experimental feature. Experimental features should not be used in
    /// production products as they are subject to breaking changes, not officially supported, and
    /// may be deprecated without notice
    public class Room :
        IRoom
    {
        public Room(RoomParams roomParams)
        {
            RoomParams = roomParams;
        }

        public RoomParams RoomParams { get; internal set; }
        public INetworking Networking { get; internal set; }

        public void Initialize()
        {
            if (Networking == null)
            {
                Networking = new LightshipNetworking("", RoomParams.RoomID);
            }
        }

        public void Join()
        {
            if (Networking != null)
            {
                Networking.Join();
            }
            else
            {
                Debug.LogWarning("Attempting to join network but network is not initialized");
            }
        }

        public void Leave()
        {
            Networking?.Leave();
        }

        public void Dispose()
        {
            if (Networking != null)
            {
                Networking.Dispose();
                Networking = null;
            }
        }
    }
}
