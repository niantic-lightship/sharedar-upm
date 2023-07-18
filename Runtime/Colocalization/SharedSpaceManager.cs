// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using Niantic.Lightship.AR.Subsystems;
using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using Niantic.Lightship.SharedAR.Rooms;
using Niantic.Lightship.SharedAR.Netcode;
using Unity.Netcode;
using UnityEngine.XR.ARFoundation;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    /// <summary>
    /// The SharedSpaceManager manages to set up necessary components for colocalization. The SharedSpaceManager hides
    /// complexity in setting up colocalization related object hierarchy and networking room associated to the
    /// tracking target.
    /// </summary>
    [PublicAPI]
    public class SharedSpaceManager : MonoBehaviour
    {
        /// <summary>
        /// Enumeration to specify the tracking system to use for colocalization
        /// </summary>
        [PublicAPI]
        public enum ColocalizationType
        {
            VpsColocalization = 0,
            ImageTrackingColocalization,

            SkipColocalization = 999
        }

        /// <summary>
        /// A struct to pass in sharedSpaceManagerStateChanged event
        /// </summary>
        [PublicAPI]
        public struct SharedSpaceManagerStateChangeEventArgs
        {
            public bool Tracking;
        }

        /// <summary>
        /// An event invoked when state colocalization related state changed. At the moment, only invoked when
        /// underlying tracking state changed
        /// </summary>
        [PublicAPI]
        public event Action<SharedSpaceManagerStateChangeEventArgs> sharedSpaceManagerStateChanged;

        [SerializeField]
        private ColocalizationType _colocalizationType;
        /// <summary>
        /// Get the ColocalizationType
        /// </summary>
        /// <returns>Colocalization type set on the SharedSpaceManager</returns>
        [PublicAPI]
        public ColocalizationType GetColocalizationType()
        {
            return _colocalizationType;
        }

        [SerializeField]
        private GameObject _sharedArRootPrefab;

        // needed for VPS colocalization
        [SerializeField]
        public ARLocationManager _arLocationManager;
        [SerializeField]
        private GameObject _anchorPrefab;
        private GameObject _arLocationObject;
        private ARLocation _arLocation;
        private bool _didStartTracking;

        // needed for Image tracking colocalization
        //[SerializeField] // TODO: re-enable later
        private Texture2D _targetImage;
        //[SerializeField] // TODO: re-enable later
        private float _targetImageSize;
        private ImageTargetColocalization _imageTargetColocalization;
        private bool _imageTrackingColocalizedOnce = false;

        /// <summary>
        /// Access IRoom assigned to colocalization session
        /// </summary>
        [PublicAPI]
        public IRoom _room { get; private set; }

        /// <summary>
        /// Reference to the GameObject representing shared origin/root
        /// </summary>
        [PublicAPI]
        public GameObject _sharedArOriginObject { get; private set; }

        // Do object creation in awake so that components are ready
        void Awake()
        {
            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    // Add ARPersistentAnchorManager if not avaialable
                    if (!_arLocationManager)
                    {
                        // No ARLocationManager set. Add ARLocationManager and ARLocation
                        _arLocationManager = gameObject.AddComponent<ARLocationManager>();
                        _arLocationObject = new GameObject("ARLocation");
                        _arLocationObject.transform.parent = gameObject.transform;
                        _arLocation = _arLocationObject.AddComponent<ARLocation>();
                        if (_anchorPrefab)
                        {
                            // add an anchor prefab under the ARLocation
                            Instantiate(
                                _anchorPrefab,
                                Vector3.zero,
                                Quaternion.identity,
                                _arLocationObject.transform);
                        }
                    }
                    break;
                }
                case ColocalizationType.ImageTrackingColocalization:
                {
                    break;
                }
                case ColocalizationType.SkipColocalization:
                {
                    MakeOriginAndAdd<SharedAROrigin>(gameObject.transform);
                    break;
                }
                default:
                {
                    Debug.Log("Unknown colocalization type selected. unable to init ColocalizationManager");
                    break;
                }
            }
        }

        // Events and coroutines relying on other components in start
        void Start()
        {
            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    _arLocationManager.locationTrackingStateChanged += OnARLocationStateChanged;
                    break;
                }
                case ColocalizationType.ImageTrackingColocalization:
                {
                    break;
                }
                case ColocalizationType.SkipColocalization:
                {
                    StartCoroutine(InvokeTrackingEventForSkipColocalization());
                    break;
                }
                default:
                {
                    Debug.Log("Unknown colocalization type selected. unable to init ColocalizationManager");
                    break;
                }
            }
        }

        void OnDestroy()
        {
            if (_sharedArOriginObject)
            {
                Destroy(_sharedArOriginObject);
            }

            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    _arLocationManager.locationTrackingStateChanged -= OnARLocationStateChanged;
                    // Only call StopTracking if we are the ones to call Start
                    if (_didStartTracking)
                    {
                        _arLocationManager.StopTracking();
                    }

                    // If we created this to wrap a payload, destroy it now
                    if (_arLocationObject)
                    {
                        Destroy(_arLocationObject);
                    }

                    break;
                }
                case ColocalizationType.ImageTrackingColocalization:
                {
                    if (_imageTargetColocalization != null)
                    {
                        _imageTargetColocalization.ColocalizationStateUpdated -= OnImageTrackingColocalizationStateUpdated;
                        _imageTargetColocalization = null;
                    }
                    break;
                }
                case ColocalizationType.SkipColocalization:
                {
                    // nothing to do
                    break;
                }
                default:
                {
                    // nothing to do
                    break;
                }
            }
        }

        /// <summary>
        /// Start tracking using selected colocalization method
        /// </summary>
        /// <param name="target">A text string to identify target. For VPS colocalization, this should be AR
        /// Location payload</param>
        [PublicAPI]
        public void StartTracking(string target)
        {
            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    if (_arLocationManager && _arLocation)
                    {
                        _arLocation.Payload = new ARPersistentAnchorPayload(target);
                        _arLocationManager.StartTracking(_arLocation);
                        _didStartTracking = true;
                    }
                    break;
                }
                case ColocalizationType.ImageTrackingColocalization:
                {
                    // Add image tracking
                    var arImageTrackedManager = gameObject.AddComponent<ARTrackedImageManager>();
                    arImageTrackedManager.requestedMaxNumberOfMovingImages = 1;
                    // TODO: Refactor RuntimeImageLibrary to simplify code here
                    var imageLib = gameObject.AddComponent<RuntimeImageLibrary>();
                    imageLib._imageTracker = arImageTrackedManager;
                    imageLib._images = new RuntimeImageLibrary.ImageAndWidth[1];
                    imageLib._images[0] = new RuntimeImageLibrary.ImageAndWidth();
                    imageLib._images[0].textureInRBG24 = _targetImage;
                    imageLib._images[0].widthInMeters = _targetImageSize;

                    // Add ImageTrackingSharedAROrigin to the sharedOrigin object
                    var sharedOrigin = MakeOriginAndAdd<ImageTrackingSharedAROrigin>(gameObject.transform);
                    _imageTargetColocalization = new ImageTargetColocalization(arImageTrackedManager, imageLib);
                    sharedOrigin._colocalizer = _imageTargetColocalization;
                    _imageTargetColocalization.ColocalizationStateUpdated += OnImageTrackingColocalizationStateUpdated;
                    break;
                }
                case ColocalizationType.SkipColocalization:
                {
                    // nothing to start when skipping colocalization
                    break;
                }
                default:
                {
                    Debug.Log("Unknown colocalization type selected. unable to localize");
                    break;
                }
            }
        }

        /// <summary>
        /// Prepare a network "Room" to join
        /// </summary>
        /// <param name="roomName">Room name. First try to find a room with this name. If not found,
        /// make a new room</param>
        /// <param name="capacity">Capacity of the room (between 2-32). Actual number of max peers can connect and sync
        /// reliably depends on amount of data to sync and network environment. Only used when creating a new room.
        /// </param>
        /// <param name="description">Description of the room. Only used when creating a new room.</param>
        [PublicAPI]
        public void PrepareRoom(string roomName, int capacity, string description)
        {
            var roomParams = new RoomParams(
                capacity,
                roomName,
                description
            );
            var status = RoomManagementService.GetOrCreateRoomForName(roomParams, out var room);
            if (status == RoomManagementServiceStatus.Ok)
            {
                // get LightshipNetcodeTransport
                if(NetworkManager.Singleton.NetworkConfig.NetworkTransport is LightshipNetcodeTransport lightshipTransport)
                {
                    lightshipTransport.SetRoom(room);
                }
                else
                {
                    Debug.LogError("Can only use ColocalizationManager with a LightshipNetcodeTransport");
                }

                // Set a Room to Netcode
                _room = room;
            }
            else
            {
                Debug.Log($"Could not get or create a room for the wayspot {status}");
            }
        }

        // handling ARLocation state change
        private void OnARLocationStateChanged(ARLocationTrackedEventArgs arLocationArgs)
        {
            if (_sharedArOriginObject == null && arLocationArgs.Tracking)
            {
                var location = arLocationArgs.ARLocation.gameObject;
                MakeOriginAndAdd<SharedAROrigin>(location.transform);
            }

            var args = new SharedSpaceManagerStateChangeEventArgs();
            args.Tracking = arLocationArgs.Tracking;
            sharedSpaceManagerStateChanged?.Invoke(args);
        }

        //
        private void OnImageTrackingColocalizationStateUpdated(
            ImageTargetColocalization.ColocalizationStateUpdatedArgs imgTackingArgs
        )
        {
            if (imgTackingArgs.State == ImageTargetColocalization.ColocalizationState.Colocalized &&
                !_imageTrackingColocalizedOnce)
            {
                _imageTrackingColocalizedOnce = true;
                var args = new SharedSpaceManagerStateChangeEventArgs();
                args.Tracking = true;
                sharedSpaceManagerStateChanged?.Invoke(args);
            }
            else if (imgTackingArgs.State == ImageTargetColocalization.ColocalizationState.Failed)
            {
                var args = new SharedSpaceManagerStateChangeEventArgs();
                args.Tracking = false;
                sharedSpaceManagerStateChanged?.Invoke(args);
            }
        }

        private IEnumerator InvokeTrackingEventForSkipColocalization()
        {
            yield return null;
            var args = new SharedSpaceManagerStateChangeEventArgs();
            args.Tracking = true;
            sharedSpaceManagerStateChanged?.Invoke(args);
        }

        private T MakeOriginAndAdd<T>(Transform root) where T : SharedAROrigin
        {
            if (_sharedArOriginObject != null)
            {
                Debug.LogError("Shared origin already exists");
                return null;
            }

            _sharedArOriginObject = Instantiate(_sharedArRootPrefab, root, false);
            var networkObject = _sharedArOriginObject.GetComponent<NetworkObject>();

            // When Netcode connects the host's version of the sharedArOrigin will override
            // the transform of the origin unless we setup this callback to return false.
            networkObject.IncludeTransformWhenSpawning = (x) => false;

            return _sharedArOriginObject.AddComponent<T>();
        }
    }
} // namespace
