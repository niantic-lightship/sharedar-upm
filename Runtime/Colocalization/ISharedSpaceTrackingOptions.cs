// Copyright 2022-2024 Niantic.

using Niantic.Lightship.AR.LocationAR;
using UnityEngine;
using Niantic.Lightship.AR.Utilities;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    /// <summary>
    /// Tracking settings to use in Shared Space
    /// </summary>
    [PublicAPI]
    public interface ISharedSpaceTrackingOptions
    {
        /// <summary>
        /// Vps tracking settings with Wayspot anchor payload string
        /// </summary>
        /// <param name="payload">Wayspot anchor payload</param>
        /// <returns>ISharedSpaceTrackingOptions object using VPS</returns>
        [PublicAPI]
        public static ISharedSpaceTrackingOptions CreateVpsTrackingOptions(string payload)
        {
            return new SharedSpaceVpsTrackingOptions(payload);
        }

        /// <summary>
        /// Vps tracking settings with ARLocation
        /// </summary>
        /// <param name="location">The target ARLocation object to track</param>
        /// <returns>ISharedSpaceTrackingOptions object using VPS</returns>
        [PublicAPI]
        public static ISharedSpaceTrackingOptions CreateVpsTrackingOptions(ARLocation location)
        {
            return new SharedSpaceVpsTrackingOptions(location);
        }

        /// <summary>
        /// Image tracking settings
        /// </summary>
        /// <param name="targetImage">Target image to track as Texture2D</param>
        /// <param name="widthInMeters">Physical width of the target image in meters</param>
        /// <returns>ISharedSpaceTrackingOptions object using image tracking</returns>
        [PublicAPI]
        public static ISharedSpaceTrackingOptions CreateImageTrackingOptions(Texture2D targetImage, float widthInMeters)
        {
            return new SharedSpaceImageTrackingOptions(targetImage, widthInMeters);
        }

        /// <summary>
        /// Use mock tracking, which the tracking event happens immediately when calling StartSharedSpace()
        /// </summary>
        /// <returns>ISharedSpaceTrackingOptions object with mock tracking</returns>
        [PublicAPI]
        public static ISharedSpaceTrackingOptions CreateMockTrackingOptions()
        {
            return new SharedSpaceMockTrackingOptions();
        }
    }
}
