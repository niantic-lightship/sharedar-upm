// Copyright 2023 Niantic Labs. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    // Convenience component that allows for cross platform static image tracking without version
    // control woes.
    // @note This is an experimental feature. Experimental features should not be used in
    // production products as they are subject to breaking changes, not officially supported, and
    // may be deprecated without notice
    public class RuntimeImageLibrary : MonoBehaviour
    {
        [SerializeField]
        internal ARTrackedImageManager _imageTracker;

        [Serializable]
        internal struct ImageAndWidth
        {
            public Texture2D textureInRBG24;
            public float widthInMeters;
        }

        [SerializeField]
        internal ImageAndWidth[] _images;

        private AddReferenceImageJobState _imageJob;
        private MutableRuntimeReferenceImageLibrary _mutableLibrary;
        private bool _isRuntimeImageLibraryInitialized;
        public string TrackingImageName { get; private set; }

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
            RuntimeReferenceImageLibrary runtimeLibrary = _imageTracker.CreateRuntimeLibrary();
            _mutableLibrary = runtimeLibrary as MutableRuntimeReferenceImageLibrary;

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
            _imageTracker.referenceLibrary = _mutableLibrary;
            _imageTracker.enabled = true;

            _isRuntimeImageLibraryInitialized = true;
        }

        protected void Update()
        {
            if (!_isRuntimeImageLibraryInitialized)
            {
                return;
            }
            if (_imageJob.status  == AddReferenceImageJobStatus.Pending)
            {
                Debug.Log("status pending");
                enabled = false;
            }
            else if (_imageJob.status == AddReferenceImageJobStatus.ErrorInvalidImage)
            {
                Debug.LogError("RuntimeImageLibrary failed, ErrorInvalidImage");
                enabled = false;
            }
            else if (_imageJob.status == AddReferenceImageJobStatus.ErrorUnknown)
            {
                Debug.LogError("RuntimeImageLibrary failed, ErrorUnknown");
                enabled = false;
            }
        }
}
}
