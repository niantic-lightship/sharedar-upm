// Copyright 2022-2024 Niantic.

using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    internal class SharedSpaceVpsTrackingOptions : ISharedSpaceTrackingOptions
    {
        private GameObject _arLocationObject;
        internal ARLocation _arLocation;
        internal bool _arLocationCreated { get; private set; }

        internal SharedSpaceVpsTrackingOptions(ARLocation location)
        {
            _arLocation = location;
            _arLocationCreated = false;
        }

        internal SharedSpaceVpsTrackingOptions(string payloadString)
        {
            _arLocationObject = new GameObject("ARLocation");
            _arLocation = _arLocationObject.AddComponent<ARLocation>();
            _arLocation.Payload = new ARPersistentAnchorPayload(payloadString);
            _arLocationCreated = true;
        }
    }
}
