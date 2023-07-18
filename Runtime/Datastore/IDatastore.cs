// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.Lightship.SharedAR.Datastore
{
    // Result of the Datastore operation
    // @note This is an experimental feature. Experimental features should not be used in
    // production products as they are subject to breaking changes, not officially supported, and
    // may be deprecated without notice
    public enum Result : Byte
    {
        Invalid = 0,
        Success,
        Error, // too generic
        NotAuthorized

        // TODO: add more detailed status/error as needed
    };

    /// @brief enum representing type of operation
    public enum DatastoreOperationType : Byte
    {
        Invalid = 0,
        Set,
        Get,
        Delete,
        ServerChangeUpdated,
        ServerChangeDeleted
    };

    /// @brief callback args
    public struct DatastoreCallbackArgs {
        public DatastoreOperationType OperationType { get; set; }
        public Result Result  { get; set; }
        public UInt32 RequestId  { get; set; }
        public string Key  { get; set; }
        public byte[] Value  { get; set; }
        public UInt32 Version  { get; set; }

        public DatastoreCallbackArgs(
            DatastoreOperationType operationType,
            Result result,
            UInt32 requestId,
            string key,
            byte[] value,
            UInt32 version
        )
        {
            OperationType = operationType;
            Result = result;
            RequestId = requestId;
            Key = key;
            Value = value;
            Version = version;
        }
    };

    // Server-backed data storage that is associated with sessions or rooms.
    // Peers can set, update, and delete Key/Value pairs, and have the server notify
    //   all other peers in the session when updates occur.
    // @note This is an experimental feature. Experimental features should not be used in
    // production products as they are subject to breaking changes, not officially supported, and
    // may be deprecated without notice
    public interface IDatastore :
        IDisposable
    {
        /// @brief Set/Add data into storage asynchronously
        /// @param req_id ID to distinguish to identify th originated request in callback
        /// @param key Key of the data
        /// @param value Value to set
        void SetData(UInt32 requestId, string key, byte[] value);

        /// @brief Set data into storage asynchronously
        /// @param req_id ID to distinguish to identify th originated request in callback
        /// @param key Key of the data
        void GetData(UInt32 requestId, string key);

        /// @brief Delete the key-value pair from the storage asynchronously
        /// @param req_id ID to distinguish to identify th originated request in callback
        /// @param key Key of the data to delete
        void DeleteData(UInt32 requestId, string key);

        /// @brief Callback to listen to server response or changes
        /// This is called either when receiving a response from the request, or data changed
        /// on server side
        event Action<DatastoreCallbackArgs> DatastoreCallback;

    }
}
