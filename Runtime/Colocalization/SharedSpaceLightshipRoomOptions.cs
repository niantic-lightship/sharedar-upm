// Copyright 2022-2024 Niantic.

using Niantic.Lightship.AR.Utilities.Logging;
using Niantic.Lightship.SharedAR.Rooms;
using Niantic.Lightship.SharedAR.Netcode;
using UnityEngine;
using Unity.Netcode;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    internal class SharedSpaceLightshipRoomOptions : ISharedSpaceRoomOptions
    {
        internal string _name; // could be empty when passing
        internal int _capacity;
        internal string _description;
        private bool _useNetcode;
        public IRoom Room { get; private set; }

        internal SharedSpaceLightshipRoomOptions(string name, int capacity, string description, bool useNetcode)
        {
            _name = name;
            _capacity = capacity;
            _description = description;
            _useNetcode = useNetcode;
            Room = null;
        }

        internal SharedSpaceLightshipRoomOptions(
            SharedSpaceVpsTrackingOptions vpsTrackingArgsargs,
            string roomTag,
            int capacity,
            string description,
            bool useNetcode
        ) : this(roomTag + vpsTrackingArgsargs._arLocation.Payload.ToBase64(), capacity, description, useNetcode)
        {
        }

        internal void PrepareRoom()
        {
            var roomParams = new RoomParams(
                _capacity,
                _name,
                _description
            );
            var status = RoomManagementService.GetOrCreateRoomForName(roomParams, out var room);
            if (status == RoomManagementServiceStatus.Ok)
            {
                Room = room;
                // Set a Room to Netcode trasport if using NetcodeForGameObject
                if (_useNetcode)
                {
                    // get LightshipNetcodeTransport
                    if(NetworkManager.Singleton.NetworkConfig.NetworkTransport is LightshipNetcodeTransport lightshipTransport)
                    {
                        lightshipTransport.SetRoom(room);
                    }
                    else
                    {
                        Log.Error("Trying to set up Room but LightshipNetcodeTransport is not set to NetworkManager");
                    }
                }
            }
            else
            {
                Log.Warning($"Could not get or create a room for the wayspot {status}");
            }
        }
    }
}
