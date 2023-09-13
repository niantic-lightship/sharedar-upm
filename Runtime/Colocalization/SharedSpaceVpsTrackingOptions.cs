// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;
using Niantic.Lightship.AR.Subsystems;

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
