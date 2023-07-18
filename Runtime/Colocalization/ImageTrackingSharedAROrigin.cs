// Copyright 2023 Niantic, Inc. All Rights Reserved.

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Niantic.Lightship.AR;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    public class ImageTrackingSharedAROrigin : SharedAROrigin
    {
        [SerializeField]
        private ARTrackedImageManager _imageTracker;

        [SerializeField]
        private RuntimeImageLibrary _runtimeImageLibrary;

        internal ImageTargetColocalization _colocalizer;

        protected void Start()
        {
            if (_colocalizer == null)
            {
                _colocalizer = new ImageTargetColocalization(_imageTracker, _runtimeImageLibrary);
            }
            _colocalizer.Start();
        }

        private void OnDisable()
        {
            _colocalizer.Stop();
            _colocalizer = null;
        }

        protected void Update()
        {
            Matrix4x4 sharedOrigin;
            if (_colocalizer.AlignedPoseToLocal(Matrix4x4.identity, out sharedOrigin) ==
                ImageTargetColocalization.ColocalizationAlignmentResult.Success)
            {
                transform.position = sharedOrigin.ToPosition();
                transform.rotation = sharedOrigin.ToRotation();
            }
        }
    }
}
