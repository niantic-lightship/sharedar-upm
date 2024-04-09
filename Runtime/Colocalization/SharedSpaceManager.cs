// Copyright 2022-2024 Niantic.

using System;
using System.Collections;
using Niantic.Lightship.AR.Utilities.Logging;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
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

            MockColocalization = 999
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
        [Tooltip("Which tracking system to use for colocalization")]
        private ColocalizationType _colocalizationType;

        [SerializeField]
        private GameObject _sharedArRootPrefab;

        // needed for VPS colocalization
        [SerializeField]
        [Tooltip("Fill this field if there's an existing location manager in the scene. Otherwise, this component will make one itself.")]
        private ARLocationManager _arLocationManager;

        private GameObject _arLocationObject;
        private ARLocation _arLocation;
        private bool _usingCustomArLocationManager;

        private ImageTargetColocalization _imageTargetColocalization;
        private bool _imageTrackingColocalizedOnce = false;

        /// <summary>
        /// Reference to the GameObject representing shared origin/root
        /// </summary>
        [PublicAPI]
        public GameObject SharedArOriginObject { get; private set; }

        /// <summary>
        /// Get the ColocalizationType
        /// </summary>
        /// <returns>Colocalization type set on the SharedSpaceManager</returns>
        [PublicAPI]
        public ColocalizationType GetColocalizationType()
        {
            return _colocalizationType;
        }


        /// <summary>
        /// Getting currently active ISharedSpaceTrackingOptions set in the SharedSpaceManager
        /// </summary>
        [PublicAPI]
        public ISharedSpaceTrackingOptions SharedSpaceTrackingOptions { get; private set; }

        /// <summary>
        /// Getting currently active ISharedSpaceRoomOptions set in the SharedSpaceManager
        /// </summary>
        [PublicAPI]
        public ISharedSpaceRoomOptions SharedSpaceRoomOptions { get; private set; }

        // Do object creation in awake so that components are ready
        private void Awake()
        {
            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    // Add ARPersistentAnchorManager if not available
                    if (!_arLocationManager)
                    {
                        // No ARLocationManager set. Add ARLocationManager and ARLocation
                        _arLocationManager = gameObject.AddComponent<ARLocationManager>();
                        _arLocationObject = new GameObject("ARLocation");
                        _arLocationObject.transform.parent = gameObject.transform;
                        _arLocation = _arLocationObject.AddComponent<ARLocation>();
                        _usingCustomArLocationManager = false;
                    }
                    else
                    {
                        // Custom ARLocationManager is set
                        // Create the shared root under XR Origin but will reparent later
                        MakeOriginAndAdd<SharedAROrigin>(gameObject.transform);
                        _usingCustomArLocationManager = true;
                    }
                    break;
                }
                case ColocalizationType.ImageTrackingColocalization:
                {
                    break;
                }
                case ColocalizationType.MockColocalization:
                {
                    break;
                }
                default:
                {
                    Log.Info("Unknown colocalization type selected. unable to init ColocalizationManager");
                    break;
                }
            }
        }

        // Events and coroutines relying on other components in start
        private void Start()
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
                case ColocalizationType.MockColocalization:
                {
                    // nothing to do
                    break;
                }
                default:
                {
                    Log.Info("Unknown colocalization type selected. unable to init ColocalizationManager");
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (SharedArOriginObject)
            {
                Destroy(SharedArOriginObject);
            }

            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    _arLocationManager.locationTrackingStateChanged -= OnARLocationStateChanged;
                    // Only call StopTracking if we are the ones to call Start
                    if (!_usingCustomArLocationManager)
                    {
                        _arLocationManager.StopTracking();
                        var vpsTrackingOptions = SharedSpaceTrackingOptions as SharedSpaceVpsTrackingOptions;
                        if (vpsTrackingOptions != null)
                        {
                            if (vpsTrackingOptions._arLocationCreated && vpsTrackingOptions._arLocation)
                            {
                                Destroy(vpsTrackingOptions._arLocation);
                            }
                        }

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
                case ColocalizationType.MockColocalization:
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

        // Start tracking and prepare room to join

        /// <summary>
        /// Start tracking and prepare a Room.
        /// </summary>
        /// <param name="trackingOptions">Tracking settings</param>
        /// <param name="roomOptions">Room settings</param>
        [PublicAPI]
        public void StartSharedSpace(ISharedSpaceTrackingOptions trackingOptions, ISharedSpaceRoomOptions roomOptions)
        {
            SharedSpaceTrackingOptions = trackingOptions;
            SharedSpaceRoomOptions = roomOptions;

            // Prepare a Room to join
            var lightshipRoomOptions = SharedSpaceRoomOptions as SharedSpaceLightshipRoomOptions;
            if (lightshipRoomOptions != null)
            {
                lightshipRoomOptions.PrepareRoom();
            }

            // Start tracking
            switch (_colocalizationType)
            {
                case ColocalizationType.VpsColocalization:
                {
                    var vpsTrackingOptions = SharedSpaceTrackingOptions as SharedSpaceVpsTrackingOptions;
                    if (vpsTrackingOptions != null)
                    {
                        if (!_usingCustomArLocationManager)
                        {
                            // Create the shared root under XR Origin but will reparent later
                            MakeOriginAndAdd<SharedAROrigin>(gameObject.transform);
                            // start vps tracking
                            _arLocationManager.SetARLocations(vpsTrackingOptions._arLocation);
                            _arLocationManager.StartTracking();
                        }
                    }
                    else
                    {
                        Log.Error("Colocalization type selected and trackingOptions type does not match." +
                            "Both has to be vps tracking.");
                    }
                    break;
                }
                case ColocalizationType.ImageTrackingColocalization:
                {
#if UNITY_EDITOR
                    MakeOriginAndAdd<SharedAROrigin>(gameObject.transform);
                    // Immediately invoke tracking event
                    var args = new SharedSpaceManagerStateChangeEventArgs();
                    args.Tracking = true;
                    sharedSpaceManagerStateChanged?.Invoke(args);
#else
                    var imageTrackingOptions = SharedSpaceTrackingOptions as SharedSpaceImageTrackingOptions;
                    if (imageTrackingOptions != null)
                    {
                        // Add image tracking
                        var arImageTrackedManager = gameObject.AddComponent<ARTrackedImageManager>();
                        arImageTrackedManager.requestedMaxNumberOfMovingImages = 1;
                        arImageTrackedManager.enabled = false;
                        // TODO: Refactor RuntimeImageLibrary to simplify code here
                        var imageLib = gameObject.AddComponent<RuntimeImageLibrary>();
                        imageLib._imageTracker = arImageTrackedManager;
                        imageLib._images = new RuntimeImageLibrary.ImageAndWidth[1];
                        imageLib._images[0] = new RuntimeImageLibrary.ImageAndWidth();
                        imageLib._images[0].textureInRBG24 = imageTrackingOptions._targetImage;
                        imageLib._images[0].widthInMeters = imageTrackingOptions._widthInMeters;

                        // Add ImageTrackingSharedAROrigin to the sharedOrigin object
                        var sharedOrigin = MakeOriginAndAdd<ImageTrackingSharedAROrigin>(gameObject.transform);
                        _imageTargetColocalization = new ImageTargetColocalization(arImageTrackedManager, imageLib);
                        sharedOrigin._colocalizer = _imageTargetColocalization;
                        _imageTargetColocalization.ColocalizationStateUpdated += OnImageTrackingColocalizationStateUpdated;
                    }
                    else
                    {
                        Log.Error("Colocalization type selected and trackingOptions type does not match." +
                            "Both has to be image tracking.");
                    }
#endif
                    break;
                }
                case ColocalizationType.MockColocalization:
                {
                    var mockTrackingOptions = SharedSpaceTrackingOptions as SharedSpaceMockTrackingOptions;
                    if (mockTrackingOptions != null)
                    {
                        MakeOriginAndAdd<SharedAROrigin>(gameObject.transform);
                        // Immediately invoke tracking event
                        var args = new SharedSpaceManagerStateChangeEventArgs();
                        args.Tracking = true;
                        sharedSpaceManagerStateChanged?.Invoke(args);
                    }
                    else
                    {
                        Log.Error("Colocalization type selected and trackingOptions type does not match." +
                            "Both has to be mock tracking.");
                    }
                    break;
                }
                default:
                {
                    Log.Info("Unknown colocalization type selected. unable to localize");
                    break;
                }
            }
        }

        // handling ARLocation state change
        private void OnARLocationStateChanged(ARLocationTrackedEventArgs arLocationArgs)
        {
            if (SharedArOriginObject != null && arLocationArgs.Tracking)
            {
                // Re-parent the shared root object under localized AR Location object
                var location = arLocationArgs.ARLocation.gameObject;
                SharedArOriginObject.transform.SetParent(location.transform, false);
            }

            // invoke a state change event
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

        private T MakeOriginAndAdd<T>(Transform root) where T : SharedAROrigin
        {
            if (SharedArOriginObject != null)
            {
                Log.Error("Shared origin already exists");
                return null;
            }

            NetworkObject networkObject;
            if (_sharedArRootPrefab)
            {
                SharedArOriginObject = Instantiate(_sharedArRootPrefab, root, false);
                networkObject = SharedArOriginObject.GetComponent<NetworkObject>();
            }
            else
            {
                // Prefab is not set. Create shared AR root object by code
                // This should only be used by tests as network object generated this way will have sync issue in Netcode
                SharedArOriginObject = new GameObject("SharedArRoot");
                networkObject = SharedArOriginObject.AddComponent<NetworkObject>();
                networkObject.AlwaysReplicateAsRoot = true;
                networkObject.SynchronizeTransform = true;
                networkObject.ActiveSceneSynchronization = false;
                networkObject.SceneMigrationSynchronization = true;
                networkObject.SpawnWithObservers = true;
                networkObject.DontDestroyWithOwner = false;
                networkObject.AutoObjectParentSync = false;
            }

            // When Netcode connects the host's version of the sharedArOrigin will override
            // the transform of the origin unless we setup this callback to return false.
            networkObject.IncludeTransformWhenSpawning = (x) => false;

            return SharedArOriginObject.AddComponent<T>();
        }
    }
} // namespace
