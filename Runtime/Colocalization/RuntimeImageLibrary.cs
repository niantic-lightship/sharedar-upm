// Copyright 2022-2024 Niantic.

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Niantic.Lightship.AR.Utilities.Logging;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    // Convenience component that allows for cross platform static image tracking without version
    // control woes.
    // @note This is an experimental feature. Experimental features should not be used in
    // production products as they are subject to breaking changes, not officially supported, and
    // may be deprecated without notice
    internal class RuntimeImageLibrary : MonoBehaviour
    {
        internal ARTrackedImageManager _imageTracker;

        internal struct ImageAndWidth
        {
            public Texture2D textureInRBG24;
            public float widthInMeters;
        }

        internal ImageAndWidth[] _images;

        private AddReferenceImageJobState _imageJob;
        private MutableRuntimeReferenceImageLibrary _mutableLibrary;
        private bool _isRuntimeImageLibraryInitialized;
        public string TrackingImageName { get; private set; }

        private AddReferenceImageJobStatus _previousStatus;

        protected void Start()
        {
            _isRuntimeImageLibraryInitialized = false;
            ARSession.stateChanged += OnArSessionStateChange;
            if (ARSession.state == ARSessionState.SessionTracking)
            {
                InitializeRuntimeImageLibrary();
            }
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= OnArSessionStateChange;
        }

        private void OnArSessionStateChange(ARSessionStateChangedEventArgs args)
        {
            if (args.state == ARSessionState.SessionTracking)
            {
                InitializeRuntimeImageLibrary();
            }
        }
        private void InitializeRuntimeImageLibrary()
        {
            // Check if reference image library already exists and image was registered
            var refImageLibrary = _imageTracker.referenceLibrary;
            if (refImageLibrary != null && _images.Length > 0)
            {
                var image = _images[0];
                for (var i=0; i<refImageLibrary.count; i++)
                {
                    var refimg = refImageLibrary[i];
                    if (refimg.name == image.textureInRBG24.name &&
                        refimg.texture == image.textureInRBG24)
                    {
                        // target image is found in the existing image library. Ready to start tracking
                        _mutableLibrary = refImageLibrary as MutableRuntimeReferenceImageLibrary;
                        _imageTracker.enabled = true;
                        TrackingImageName = refimg.name;
                        enabled = false;
                        return;
                    }
                }
            }

            // No image library yet. Create a new one
            if (refImageLibrary == null)
            {
                RuntimeReferenceImageLibrary runtimeLibrary = _imageTracker.CreateRuntimeLibrary();
                _mutableLibrary = runtimeLibrary as MutableRuntimeReferenceImageLibrary;
                _imageTracker.referenceLibrary = _mutableLibrary;
            }

            // Static dictionary of images taken by the user from the rest of the game.
            foreach (var image in _images)
            {
                _imageJob =  _mutableLibrary.ScheduleAddImageWithValidationJob(
                    image.textureInRBG24,
                    image.textureInRBG24.name,
                    image.widthInMeters
                );
                TrackingImageName = image.textureInRBG24.name;
                break;
            }

            _isRuntimeImageLibraryInitialized = true;
        }

        protected void Update()
        {
            if (!_isRuntimeImageLibraryInitialized)
            {
                return;
            }

            if (_previousStatus != AddReferenceImageJobStatus.Success &&
                _imageJob.status == AddReferenceImageJobStatus.Success)
            {
                // Enable ARTrackedImageManager after image library is ready
                _imageTracker.enabled = true;
                enabled = false;
            }
            if (_imageJob.status  == AddReferenceImageJobStatus.Pending)
            {
                // do nothing
            }
            else if (_imageJob.status == AddReferenceImageJobStatus.ErrorInvalidImage)
            {
                Log.Error("RuntimeImageLibrary failed, ErrorInvalidImage");
                enabled = false;
            }
            else if (_imageJob.status == AddReferenceImageJobStatus.ErrorUnknown)
            {
                Log.Error("RuntimeImageLibrary failed, ErrorUnknown");
                enabled = false;
            }

            _previousStatus = _imageJob.status;
        }
}
}
