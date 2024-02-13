// Copyright 2022-2024 Niantic.
using Niantic.Lightship.AR.Utilities.Logging;
using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Niantic.Lightship.SharedAR.Colocalization;

namespace Niantic.Lightship.SharedAR.Netcode
{
    /// <summary>
    /// The LightshipNetworkObject is a component to add to the colocalized objects. Essentially,
    /// LightshipNetworkObject bring Netcode for Gameobjects NetworkObject and re-parent the object under the Shared AR Origin
    /// when spawned.
    /// </summary>
    [PublicAPI]
    [RequireComponent(typeof(NetworkObject))]
    public class LightshipNetworkObject : NetworkBehaviour
    {
        [Tooltip("Whether to attempt to re-parent the object under the SharedAROrigin when spawned.")]
        [SerializeField]
        private bool _putInSharedOrigin = true;

        private bool _hasRerooted = false;

        private void AttemptToReroot()
        {
            if (!_hasRerooted && IsServer)
            {
                var selfNO = GetComponent<NetworkObject>();
                selfNO.AutoObjectParentSync = true;

                if (_putInSharedOrigin)
                {
                    var origin = FindObjectOfType<SharedAROrigin>();
                    if (origin == null)
                        Log.Error("In order for the SharedARNetworkObject to be aligned, " +
                            "you need a SharedAROrigin in your scene under the XR Origin");
                    else
                    {
                        _hasRerooted = selfNO.TrySetParent(origin.GetComponentInChildren<NetworkObject>(), false);
                    }
                }
                else
                {
                    selfNO.TrySetParent((NetworkObject)null, false);
                }
            }
        }

        private void Start()
        {
            AttemptToReroot();
            // Make sure sync local transform
            var networkTransform = GetComponent<NetworkTransform>();
            if (networkTransform)
            {
                networkTransform.InLocalSpace = true;
            }
        }
    }
}
