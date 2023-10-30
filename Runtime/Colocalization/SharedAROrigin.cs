// Copyright 2023 Niantic, Inc. All Rights Reserved.

using Niantic.Lightship.AR.Utilities;
using UnityEngine;

namespace Niantic.Lightship.SharedAR.Colocalization
{
    /// <summary>
    /// The SharedAROrigin is used to find a root GameObject for all colocalized objects. GameObjects with
    /// LightshipNetworkObject will be a child of the GameObject with this component.
    /// </summary>
    [PublicAPI]
    public class SharedAROrigin : MonoBehaviour
    {
    }
}