// Copyright 2022-2025 Niantic.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Niantic.Lightship.AR.Utilities.Logging;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    // Convenience class that allows for dynamic image tracking. This class takes in a disabled
    // ARTrackedImageManager and a Texture2D with its width in real life meters to use as image trackers.
    //
    // The main logic of the class waits for the ARSession to be started, loads the 
    // images into the session, waits for the images to be registered with the AR Image Tracking
    // system, then enables the ARTrackedImageManager which will look for the passed images.
    //
    // @note This is an experimental feature. Experimental features should not be used in
    // production products as they are subject to breaking changes, not officially supported, and
    // may be deprecated without notice
    internal class RuntimeImageLibrary
    {
        private Texture2D _image;
        private float _widthInMeters;
        private ARTrackedImageManager _imageTracker;
        internal string TrackingImageName { get; private set; }

        internal RuntimeImageLibrary(ARTrackedImageManager imageTracker, Texture2D image, float widthInMeters)
        {
            _imageTracker = imageTracker;
            _imageTracker.enabled = false;
            _imageTracker.requestedMaxNumberOfMovingImages = 1;

            _image = image;
            _widthInMeters = widthInMeters;
        }

        internal IEnumerator InitializeRuntimeImageLibrary()
        {
            // Wait for AR Session to start
            while (ARSession.state != ARSessionState.SessionTracking)
            {
                yield return null;
            }

            TrackingImageName = _image.name;

            // Check if reference image library already exists and image was registered
            var refImageLibrary = _imageTracker.referenceLibrary;
            if (refImageLibrary != null)
            {
                for (var i = 0; i < refImageLibrary.count; i++)
                {
                    var refimg = refImageLibrary[i];
                    if (refimg.name == _image.name &&
                        refimg.texture == _image)
                    {
                        // target image is found in the existing image library. Ready to start tracking
                        _imageTracker.enabled = true;
                        yield break;
                    }
                }
            }

            // No image library yet. Create a new one
            RuntimeReferenceImageLibrary runtimeLibrary = _imageTracker.CreateRuntimeLibrary();
            var mutableLibrary = runtimeLibrary as MutableRuntimeReferenceImageLibrary;
            _imageTracker.referenceLibrary = mutableLibrary;

            // Static dictionary of images taken by the user from the rest of the game.
            var job = mutableLibrary.ScheduleAddImageWithValidationJob(
                _image,
                _image.name,
                _widthInMeters
            );

            yield return new WaitUntil(() => job.jobHandle.IsCompleted);

            switch (job.status) {
                case AddReferenceImageJobStatus.ErrorInvalidImage:
                    Log.Error("RuntimeImageLibrary failed, ErrorInvalidImage");
                    break;
                case AddReferenceImageJobStatus.ErrorUnknown:
                    Log.Error("RuntimeImageLibrary failed, ErrorUnknown");
                    break;

                case AddReferenceImageJobStatus.Success:
                    _imageTracker.enabled = true;
                    break;

                default:
                    break;
            }
        }
    }
}
