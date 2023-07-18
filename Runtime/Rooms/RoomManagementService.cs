// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using Niantic.Lightship.SharedAR.Rooms.MarshMessages;
using Niantic.Lightship.SharedAR.Rooms.Implementation;
using UnityEngine;
using Niantic.Lightship.AR;
using Niantic.Lightship.AR.Loader;
using Niantic.Lightship.AR.Utilities;

namespace Niantic.Lightship.SharedAR.Rooms
{
    /// <summary>
    /// Status of Room Management Service requests. Value corresponds to HTTP response code.
    /// </summary>
    [PublicAPI]
    public enum RoomManagementServiceStatus :
        Int32
    {
        Ok = 200,
        BadRequest = 400,
        Unauthorized = 401,
        NotFound = 404,
    }

    /// <summary>
    /// The RoomManagementService provides interface to access Room Management Service backend to create, remove,
    /// find Rooms. A room is an entity to connect multiple peers through server relayed network.
    /// </summary>
    [PublicAPI]
    public static class RoomManagementService
    {
        private static _IRoomManagementServiceImpl _serviceImpl;

        // Prod Marsh REST endpoint
        // TODO: Set this from config later, instead of hard coded here
        private const string _marshEndPoint = "marsh-prod.nianticlabs.com:443";

        // Default ExperienceID
        internal static string DefaultExperienceId { get; private set; }

        static RoomManagementService()
        {
            _serviceImpl = _HttpRoomManagementServiceImpl._Instance;
            var appId = Application.identifier;
            var apiKey = LightshipSettings.Instance.ApiKey;
            _serviceImpl.InitializeService(_marshEndPoint, appId, apiKey);
        }

        internal static void _InitializeServiceForIntegrationTesting(string apiKey)
        {
            _serviceImpl = _HttpRoomManagementServiceImpl._Instance;
            var appId = Application.identifier;
            _serviceImpl.InitializeService(_marshEndPoint, appId, apiKey);
        }

        internal static void _InitializeServiceForTesting()
        {
            _serviceImpl = _FakeRoomManagementServiceImpl._Instance;
            var appId = Application.identifier;
            _serviceImpl.InitializeService("", appId, "");
            DefaultExperienceId = "experienceId";
        }

        /// <summary>
        /// Create a new room on the server.
        /// </summary>
        /// <param name="roomParams">Parameters of the room</param>
        /// <param name="outRoom">Created room as IRoom object. null if failed to create.</param>
        /// <returns>Status of the operation</returns>
        [PublicAPI]
        public static RoomManagementServiceStatus CreateRoom
        (
            RoomParams roomParams,
            out IRoom outRoom
        )
        {
            outRoom = null;
            if (_serviceImpl == null)
            {
                Debug.LogError("Must initialize RoomManagementService before using");
                return RoomManagementServiceStatus.BadRequest;
            }

            var request = new _CreateRoomRequest()
            {
                experienceId = roomParams.ExperienceId,
                name = roomParams.Name,
                description = roomParams.Description,
                capacity = roomParams.Capacity,
                passcode = roomParams.Visibility == RoomVisibility.Private ? roomParams.Passcode : ""
            };

            var response = _serviceImpl.CreateRoom(request, out var status);
            if (status != RoomManagementServiceStatus.Ok)
            {
                Debug.LogWarning($"Room Management Create request failed with status {status}");
                return status;
            }

            outRoom = new Room(response.room);

            return RoomManagementServiceStatus.Ok;
        }

        private static RoomManagementServiceStatus GetRoomsForExperience(string experienceId, out List<IRoom> rooms)
        {
            rooms = new List<IRoom>();
            if (_serviceImpl == null)
            {
                Debug.LogError("Must initialize RoomManagementService before using");
                return RoomManagementServiceStatus.BadRequest;
            }

            _GetRoomForExperienceRequest request;

            if (string.IsNullOrEmpty(experienceId))
            {
                request = new _GetRoomForExperienceRequest() { };
            }
            else
            {
                request = new _GetRoomForExperienceRequest() { experienceIds = new List<string>() { experienceId } };
            }


            var response = _serviceImpl.GetRoomsForExperience(request, out var status);
            if (status != RoomManagementServiceStatus.Ok)
            {
                Debug.LogWarning($"Room Management Get request failed with status {status}");
                return status;
            }

            // return not found if count is zero
            if (response.rooms.Count == 0)
            {
                return RoomManagementServiceStatus.NotFound;
            }

            foreach (var room in response.rooms)
            {
                rooms.Add(new Room(room));
            }

            return RoomManagementServiceStatus.Ok;
        }

        /// <summary>
        /// Delete a room on the server.
        /// </summary>
        /// <param name="roomId">Room ID of the room to delete</param>
        /// <returns>Status of the operation</returns>
        [PublicAPI]
        public static RoomManagementServiceStatus DeleteRoom(string roomId)
        {
            if (_serviceImpl == null)
            {
                Debug.LogError("Must initialize RoomManagementService before using");
                return RoomManagementServiceStatus.BadRequest;
            }

            var request = new _DestroyRoomRequest() { roomId = roomId };

            _serviceImpl.DestroyRoom(request, out var status);
            if (status != RoomManagementServiceStatus.Ok)
            {
                Debug.LogError($"Room Management Destroy request failed with status {status}");
                Debug.Log($"|{roomId}|");
                return status;
            }

            return RoomManagementServiceStatus.Ok;
        }

        /// <summary>
        /// Get a room by Room ID on the server
        /// </summary>
        /// <param name="roomId">Room ID as a string</param>
        /// <param name="outRoom">Found Room object. Null if operation failed or room ID not found<//param>
        /// <returns>Status of the operation</returns>
        [PublicAPI]
        public static RoomManagementServiceStatus GetRoom(string roomId, out IRoom outRoom)
        {
            outRoom = null;
            if (_serviceImpl == null)
            {
                Debug.LogError("Must initialize RoomManagementService before using");
                return RoomManagementServiceStatus.BadRequest;
            }

            var request = new _GetRoomRequest() { roomId = roomId };

            var response = _serviceImpl.GetRoom(request, out var status);
            if (status != RoomManagementServiceStatus.Ok)
            {
                Debug.LogWarning($"Room Management Get request failed with status {status}");
                return status;
            }

            outRoom = new Room(response.room);

            return RoomManagementServiceStatus.Ok;
        }

        /// <summary>
        /// Query room(s) by name on the server
        /// </summary>
        /// <param name="name">Name of the room to find</param>
        /// <param name="rooms">A List of rooms which has matching name </param>
        /// <returns>Status of the operation</returns>
        [PublicAPI]
        public static RoomManagementServiceStatus QueryRoomsByName(string name, out List<IRoom> rooms)
        {
            var status = GetRoomsForExperience(
                DefaultExperienceId, out rooms);
            if (status == RoomManagementServiceStatus.Ok)
            {
                for (int i = rooms.Count - 1; i >= 0; i--)
                {
                    if (rooms[i].RoomParams.Name != name)
                    {
                        rooms.RemoveAt(i);
                    }
                }
            }

            // return not found if count is zero
            if (rooms.Count == 0)
            {
                return RoomManagementServiceStatus.NotFound;
            }

            return status;
        }

        /// <summary>
        /// Get all rooms on the server, which was created by this app
        /// </summary>
        /// <param name="rooms">List of rooms available for this app</param>
        /// <returns>Status of the operation</returns>
        [PublicAPI]
        public static RoomManagementServiceStatus GetAllRooms(out List<IRoom> rooms)
        {
            var status = GetRoomsForExperience(
                DefaultExperienceId, out rooms);

            return status;
        }

        /// <summary>
        /// Get a IRoom object that has a given name on the server.
        /// If no room found with the name, create a new room using given room parameters
        /// </summary>
        /// <param name="roomParams">Room parameters of the room to get or create</param>
        /// <param name="outRoom">IRoom object. null if server operarion failed</param>
        /// <returns>Status of the operation</returns>
        [PublicAPI]
        public static RoomManagementServiceStatus GetOrCreateRoomForName
        (
            RoomParams roomParams,
            out IRoom outRoom
        )
        {
            var status = QueryRoomsByName(roomParams.Name, out var rooms);
            if (status == RoomManagementServiceStatus.NotFound)
            {
                // Create a new one this case
                status = CreateRoom(roomParams, out outRoom);
                return status;
            }
            if (status != RoomManagementServiceStatus.Ok)
            {
                outRoom = null;
                return status;
            }

            if (rooms.Count > 1)
            {
                Debug.Log($"num of rooms = {rooms.Count}, but use first room" );
            }
            // Return the first room for now
            outRoom =rooms[0];
            return status;
        }
    }
}
