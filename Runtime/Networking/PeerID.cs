// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System;
using System.Runtime.InteropServices;
using Niantic.Lightship.AR.Utilities;

namespace Niantic.Lightship.SharedAR.Networking
{
    /// <summary>
    /// Struct that represents the identifiers of other peers in the room.
    /// Can be compared with other PeerIDs and used as Keys in Dictionaries.
    /// </summary>
    [PublicAPI]
    public struct PeerID : IEquatable<PeerID>
    {
        /// <summary>
        /// The Invalid peer ID returned by functions that have errored.
        /// This PeerID returns 0 from ToUint32.
        /// </summary>
        [PublicAPI]
        public static PeerID InvalidID = new PeerID(0);

        private UInt32 _uintId;

        /// <summary>
        /// Constructor for the PeerID. PeerIDs should be received from the INetworking API,
        /// not manually constructed.
        /// </summary>
        [PublicAPI]
        public PeerID(UInt32 id)
        {
            _uintId = id;
        }

        /// <summary>
        /// Guid representation of the PeerID.
        /// </summary>
        [PublicAPI]
        public Guid Identifier
        {
            get { return new Guid(_uintId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); }
        }

        /// <summary>
        /// UInt32 representation of the PeerID.
        /// </summary>
        [PublicAPI]
        public UInt32 ToUint32()
        {
            return _uintId;
        }

        /// <summary>
        /// Equality implementation for PeerID.
        /// </summary>
        [PublicAPI]
        public bool Equals(PeerID other)
        {
            return _uintId == other._uintId;
        }

        /// <summary>
        /// Get unique hash for this PeerID. Necessary for Dictionary compatibility.
        /// </summary>
        [PublicAPI]
        public override int GetHashCode()
        {
            return _uintId.GetHashCode();
        }

        /// <summary>
        /// String representation of the PeerID.
        /// </summary>
        [PublicAPI]
        public override string ToString()
        {
            return _uintId.ToString();
        }
    }
}
