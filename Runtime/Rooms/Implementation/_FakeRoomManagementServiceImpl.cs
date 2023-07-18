// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.SharedAR.Rooms.MarshMessages;

namespace Niantic.Lightship.SharedAR.Rooms.Implementation
{
    /// @note This is an experimental feature. Experimental features should not be used in
    /// production products as they are subject to breaking changes, not officially supported, and
    /// may be deprecated without notice
    internal class _FakeRoomManagementServiceImpl :
        _IRoomManagementServiceImpl
    {
        internal static _FakeRoomManagementServiceImpl _Instance = new _FakeRoomManagementServiceImpl();
        private string _appId;
        private string _endpoint;
        private readonly Dictionary<string, _RoomInternal> _rooms = new Dictionary<string, _RoomInternal>();

        public void InitializeService(string endpoint, string appId, string apiKey)
        {
            _endpoint = endpoint;
            _appId = appId;
            _rooms.Clear();
            // ignore API key in the fake impl
        }

        public _CreateRoomResponse CreateRoom
        (
            _CreateRoomRequest request,
            out RoomManagementServiceStatus status
        )
        {
            var expId = request.experienceId;
            if (expId == "")
            {
                expId = RoomManagementService.DefaultExperienceId;
            }

            var room = new _RoomInternal()
            {
                roomId = Guid.NewGuid().ToString(),
                name = request.name,
                description = request.description,
                capacity = request.capacity,
                experienceId = expId,
                passcodeEnabled = !string.IsNullOrEmpty(request.passcode)
            };

            _rooms[room.roomId] = room;

            var res = new _CreateRoomResponse() { room = room };

            status = RoomManagementServiceStatus.Ok;
            return res;
        }

        public _GetRoomResponse GetRoom(_GetRoomRequest request, out RoomManagementServiceStatus status)
        {
            if (!_rooms.ContainsKey(request.roomId))
            {
                status = RoomManagementServiceStatus.NotFound;
                return new _GetRoomResponse();
            }

            status = RoomManagementServiceStatus.Ok;
            return new _GetRoomResponse() { room = _rooms[request.roomId] };
        }

        public _GetRoomForExperienceResponse GetRoomsForExperience
        (
            _GetRoomForExperienceRequest request,
            out RoomManagementServiceStatus status
        )
        {
            var roomList = new List<_RoomInternal>();
            foreach (var room in _rooms.Values)
            {
                if (room.experienceId.Equals(request.experienceIds.First()))
                {
                    roomList.Add(room);
                }
            }

            status = RoomManagementServiceStatus.Ok;
            return new _GetRoomForExperienceResponse() { rooms = roomList };
        }

        public void DestroyRoom(_DestroyRoomRequest request, out RoomManagementServiceStatus status)
        {
            _rooms.Remove(request.roomId);
            status = RoomManagementServiceStatus.Ok;
        }

        public void ReleaseService()
        {
            _endpoint = null;
            _appId = null;
            _rooms.Clear();
            ;
        }
    }
}
