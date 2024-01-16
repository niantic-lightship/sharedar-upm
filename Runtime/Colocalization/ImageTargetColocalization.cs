// Copyright 2022-2024 Niantic.
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    internal class ImageTargetColocalization
    {
        // @note This is an experimental feature. Experimental features should not be used in
        // production products as they are subject to breaking changes, not officially supported, and
        // may be deprecated without notice
        public enum ColocalizationState
        {
            Unknown = 0,
            Initialized,
            Colocalizing,
            Colocalized,
            LimitedTracking,
            Failed
        }

        public struct ColocalizationStateUpdatedArgs
        {
            public ColocalizationStateUpdatedArgs(ColocalizationState state) :
                this()
            {
                State = state;
            }

            public ColocalizationState State { get; private set; }
        }


        // @note This is an experimental feature. Experimental features should not be used in
        // production products as they are subject to breaking changes, not officially supported, and
        // may be deprecated without notice
        public enum ColocalizationAlignmentResult : byte
        {
            // <summary>
            // Returned if local user isn't colocalized or hasn't resolved the Pose Anchor for the
            // requested peer.
            // </summary>
            Failure = 0,

            // <summary>
            // Returned if local user is colocalized and has resolved the Pose Anchor for the
            // requested peer.
            // </summary>
            Success
        }

        private ARTrackedImageManager _imageTrackingManager;
        private ColocalizationState _selfColocalizationState = ColocalizationState.Unknown;
        private RuntimeImageLibrary _runtimeImageLibrary;

        public ImageTargetColocalization(ARTrackedImageManager manager, RuntimeImageLibrary runtimeImageLibrary)
        {
            InitializeWithTrackingManager(manager, runtimeImageLibrary);
        }

        // Use this with an empty constructor to configure the image tracking manager at runtime
        public void InitializeWithTrackingManager(ARTrackedImageManager manager, RuntimeImageLibrary runtimeImageLibrary)
        {
            _imageTrackingManager = manager;
            _runtimeImageLibrary = runtimeImageLibrary;
        }

        public void Start()
        {
            if (_imageTrackingManager == null)
            {
                return;
            }

            _selfColocalizationState = ColocalizationState.Colocalizing;
            InvokeStateUpdated(_selfColocalizationState);
            _imageTrackingManager.requestedMaxNumberOfMovingImages = 1;
            _imageTrackingManager.trackedImagesChanged += OnTrackingChanged;
        }

        public void Stop()
        {
            if (_imageTrackingManager == null)
            {
                return;
            }

            _selfColocalizationState = ColocalizationState.Unknown;
            InvokeStateUpdated(_selfColocalizationState);
            _imageTrackingManager.enabled = false;
            _imageTrackingManager.trackedImagesChanged -= OnTrackingChanged;
        }

        public Matrix4x4 AlignedSpaceOrigin { get; internal set; }

        private event Action<ColocalizationStateUpdatedArgs> _stateUpdated;

        public event Action<ColocalizationStateUpdatedArgs> ColocalizationStateUpdated
        {
            add
            {
                _stateUpdated += value;
                value(new ColocalizationStateUpdatedArgs(_selfColocalizationState));
            }
            remove { _stateUpdated -= value; }
        }

        public ColocalizationAlignmentResult AlignedPoseToLocal
        (
            Matrix4x4 poseInAlignedSpace,
            out Matrix4x4 poseInLocalSpace
        )
        {
            if (_selfColocalizationState != ColocalizationState.Colocalized &&
                _selfColocalizationState != ColocalizationState.LimitedTracking)
            {
                poseInLocalSpace = Matrix4x4.identity;
                return ColocalizationAlignmentResult.Failure;
            }

            poseInLocalSpace = ConvertToWorldSpace(poseInAlignedSpace);
            return ColocalizationAlignmentResult.Success;
        }

        private void OnTrackingChanged(ARTrackedImagesChangedEventArgs args)
        {
            if (args.added.Count > 0)
            {
                foreach (var image in args.added)
                {
                    if (!image.referenceImage.name.Equals(_runtimeImageLibrary.TrackingImageName) ||
                        image.trackingState != TrackingState.Tracking)
                    {
                        continue;
                    }

                    AlignedSpaceOrigin = image.transform.localToWorldMatrix;

                    if (_selfColocalizationState == ColocalizationState.Colocalizing)
                    {
                        _selfColocalizationState = ColocalizationState.Colocalized;
                        InvokeStateUpdated(_selfColocalizationState);
                    }
                }
            }

            foreach (var image in args.updated)
            {
                if (!image.referenceImage.name.Equals(_runtimeImageLibrary.TrackingImageName))
                {
                    continue;
                }

                // Don't update origin with limited information
                if (image.trackingState != TrackingState.Tracking)
                {
                    _selfColocalizationState = ColocalizationState.LimitedTracking;
                    InvokeStateUpdated(_selfColocalizationState);
                    continue;
                }

                AlignedSpaceOrigin = image.transform.localToWorldMatrix;

                if (_selfColocalizationState != ColocalizationState.Colocalized)
                {
                    _selfColocalizationState = ColocalizationState.Colocalized;
                    InvokeStateUpdated(_selfColocalizationState);
                }
            }

            foreach (var image in args.removed)
            {
                if (!image.referenceImage.name.Equals(_runtimeImageLibrary.TrackingImageName))
                {
                    continue;
                }

                if (_selfColocalizationState != ColocalizationState.LimitedTracking)
                {
                    _selfColocalizationState = ColocalizationState.LimitedTracking;
                    InvokeStateUpdated(_selfColocalizationState);
                }
            }
        }

        // Convert from node space to local space (assumes 0,0,0)
        private Matrix4x4 ConvertToWorldSpace(Matrix4x4 pose)
        {
            return AlignedSpaceOrigin * pose;
        }

        private void InvokeStateUpdated(ColocalizationState state)
        {
            var args = new ColocalizationStateUpdatedArgs(state);

            var handler = _stateUpdated;
            handler?.Invoke(args);
        }
    }
}
