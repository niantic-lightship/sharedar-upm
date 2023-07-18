// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.Lightship.SharedAR.Rooms.MarshMessages;

namespace Niantic.Lightship.SharedAR.Rooms
{
    // Provide an interface to call Marsh services
    /// @note This is an experimental feature. Experimental features should not be used in
    /// production products as they are subject to breaking changes, not officially supported, and
    /// may be deprecated without notice
    internal interface _IRoomManagementServiceImpl
    {
        // Initialize the underlying service
        public void InitializeService(string endpoint, string appId, string apiKey);

        public _CreateRoomResponse CreateRoom(_CreateRoomRequest request, out RoomManagementServiceStatus status);

        public _GetRoomResponse GetRoom(_GetRoomRequest request, out RoomManagementServiceStatus status);

        public _GetRoomForExperienceResponse GetRoomsForExperience
        (
            _GetRoomForExperienceRequest request,
            out RoomManagementServiceStatus status
        );

        // No return type now, rely on status code
        public void DestroyRoom(_DestroyRoomRequest request, out RoomManagementServiceStatus status);

        // Release a held service
        public void ReleaseService();
    }
}
