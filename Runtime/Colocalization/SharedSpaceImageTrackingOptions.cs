// Copyright 2022-2024 Niantic.

using UnityEngine;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    internal class SharedSpaceImageTrackingOptions : ISharedSpaceTrackingOptions
    {
        internal Texture2D _targetImage;
        internal float _widthInMeters;

        internal SharedSpaceImageTrackingOptions(Texture2D targetImage, float widthInMeters)
        {
            _targetImage = targetImage;
            _widthInMeters = widthInMeters;
        }
    }
}
