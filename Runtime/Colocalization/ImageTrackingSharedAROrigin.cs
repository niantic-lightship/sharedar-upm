// Copyright 2022-2024 Niantic.

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Niantic.Lightship.AR;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    internal class ImageTrackingSharedAROrigin : SharedAROrigin
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
                // On Android and second time using image colocalization, somehow
                // AlignedPoseToLocal() returns success but the matrix is zero. Skip if that case,
                // or deep inside Matrix4x4.ToRotation() prints error on each frame
                if (!sharedOrigin.Equals(Matrix4x4.zero))
                {
                    transform.position = sharedOrigin.ToPosition();

                    // Change the rotation of the anchor in global space to make it so the
                    // anchor's up-axis is facing Unity's up-axis
                    // In matrix form: anchor_with_unity_up = anchor_up_to_unity_up * anchor
                    var rotation = sharedOrigin.ToRotation();
                    var anchorUpAxis = rotation * Vector3.up;
                    transform.rotation = Quaternion.FromToRotation(anchorUpAxis, Vector3.up) * rotation;
                }
            }
        }
    }
}
