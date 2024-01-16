// Copyright 2022-2024 Niantic.

using Niantic.Lightship.AR.Utilities;
using Niantic.Lightship.SharedAR.Rooms;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    /// <summary>
    /// Room settings to use in Shared Space
    /// </summary>
    [PublicAPI]
    public interface ISharedSpaceRoomOptions
    {

        [PublicAPI]
        public IRoom Room { get; }

        /// <summary>
        /// Use to create ISharedSpaceRoomOptions when a Lightship Room is associated to a Wayspot
        /// </summary>
        /// <param name="trackingVpsLocation">VPS tracking options</param>
        /// <param name="roomTag">A prefix to the room name</param>
        /// <param name="capacity">Capacity of the room</param>
        /// <param name="description">Description of the room</param>
        /// <param name="useNetcode">If true, a Room is assigned to LightshipNetcodeTransport</param>
        /// <returns>Returns ISharedSpaceRoomOptions object</returns>
        [PublicAPI]
        public static ISharedSpaceRoomOptions CreateVpsRoomOptions(
            ISharedSpaceTrackingOptions trackingVpsLocation,
            string roomTag = "",
            int capacity = 10,
            string description = "",
            bool useNetcode = true
        )
        {
            var vpsTrackingOptions = trackingVpsLocation as SharedSpaceVpsTrackingOptions;
            if (vpsTrackingOptions != null)
            {
                return new SharedSpaceLightshipRoomOptions(
                    vpsTrackingOptions,
                    roomTag,
                    capacity,
                    description,
                    useNetcode
                );
            }
            // return null as passed tracking options is not vps tracking options
            return null;
        }

        /// <summary>
        /// Use to create ISharedSpaceRoomOptions for mock tracking or image target tracking, which requires to give
        /// a custom Room name
        /// </summary>
        /// <param name="name">Name of the room</param>
        /// <param name="capacity">Capacity of the room</param>
        /// <param name="description">Description of the room</param>
        /// <param name="useNetcode">If true, a Room is assigned to LightshipNetcodeTransport</param>
        /// <returns>Returns ISharedSpaceRoomOptions object</returns>
        [PublicAPI]
        public static ISharedSpaceRoomOptions CreateLightshipRoomOptions(string name,
            int capacity = 10,
            string description = "",
            bool useNetcode = true)
        {
            return new SharedSpaceLightshipRoomOptions(name, capacity, description, useNetcode);
        }

        /// <summary>
        /// Use when managing Rooms by application or using custom networking
        /// </summary>
        /// <returns>Returns ISharedSpaceRoomOptions object</returns>
        [PublicAPI]
        public static ISharedSpaceRoomOptions CreateCustomRoomOptions()
        {
            return new SharedSpaceCustomRoomOptions();
        }
    }
}
