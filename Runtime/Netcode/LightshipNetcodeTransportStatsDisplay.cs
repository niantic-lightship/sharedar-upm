// Copyright 2022-2024 Niantic.
using System;
using System.IO;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Niantic.Lightship.SharedAR.Netcode
{
    public class LightshipNetcodeTransportStatsDisplay : NetworkBehaviour
    {
        [SerializeField]
        private LightshipNetcodeTransport _lightshipNetcodeTransport;

        [SerializeField]
        private UnityEngine.UI.Text _text;

        [SerializeField]
        private UnityEngine.UI.Image _bgImage;

        [SerializeField]
        private UnityEngine.UI.Button _button;

        [SerializeField]
        private float SampleRateInSeconds = 1.0f;

        [SerializeField]
        private bool VerboseText = false;

        private float _sampleTimer = 0.0f;
        private long _rttMeasurement = 0;
        private string _filePostfix;
        private LightshipNetcodeTransport.NetcodeSessionStats _lastStats;
        private System.Diagnostics.Stopwatch _frameIndependentWatch = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        protected void Start()
        {
            DateTime now = DateTime.Now;
            _filePostfix = now.ToString("ddMMyy_HHmmss");
            _button.onClick.AddListener(Hide);
        }

        protected void Update()
        {
            if (!IsSpawned)
                return;

            _sampleTimer += Time.deltaTime;
            if (_sampleTimer >= SampleRateInSeconds)
            {
                var stats = _lightshipNetcodeTransport.GetNetcodeSessionStats();
                LightshipNetcodeTransport.NetcodeSessionStats.GetPerSecondStats(stats,
                    _lastStats,
                    out var bytesSentPerSec,
                    out var messagesSentPerSec,
                    out var bytesReceivedPerSec,
                    out var messagesReceivedPerSec
                );
                _lastStats = stats;

                if (VerboseText)
                {
                    _text.text = "TotalBytesSent: " + stats.TotalBytesSent
                        + "\nTotalBytesReceived: " + stats.TotalBytesReceived
                        + "\nTotalMessagesSent: " + stats.TotalMessagesSent
                        + "\nTotalMessagesReceived: " + stats.TotalMessagesReceived
                        + "\nPeerCount: " + stats.PeerCount
                        + "\nTimestamp: " + stats.Timestamp
                        + "\nbytesSentPerSec: " + bytesSentPerSec
                        + "\nmessagesSentPerSec: " + messagesSentPerSec
                        + "\nbytesReceivedPerSec: " + bytesReceivedPerSec
                        + "\nmessagesReceivedPerSec: " + messagesReceivedPerSec
                        + $"\nPing to host (ms): {_rttMeasurement}ms";
                }
                else
                {
                    _text.text = $"PeerCount: {stats.PeerCount}\n" +
                                $"Kb sent/recv:\n{((stats.TotalBytesReceived + stats.TotalBytesSent) / 1024)}kb\n" +
                                $"Kb sent/recv per sec:\n{((bytesReceivedPerSec + bytesSentPerSec) / 1024)}kb/s\n" +
                                $"Ping to host (ms): {_rttMeasurement}ms";
                }

                using (var streamWriter = File.AppendText(GetFilePath()))
                {
                    streamWriter.WriteLine(
                        stats.TotalBytesSent
                        + "," + stats.TotalBytesReceived
                        + "," + stats.TotalMessagesSent
                        + "," + stats.TotalMessagesReceived
                        + "," + stats.PeerCount
                        + "," + stats.Timestamp
                        + "," + bytesSentPerSec
                        + "," + messagesSentPerSec
                        + "," + bytesReceivedPerSec
                        + "," + messagesReceivedPerSec
                        + "," + _rttMeasurement
                    );
                }

                _sampleTimer = 0.0f;
                if (!IsServer)
                {
                    _frameIndependentWatch.Restart();
                    RttPingPongServerRpc(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        public void Hide()
        {
            _button.enabled = false;
            _bgImage.enabled = false;
            _text.enabled = false;
        }

        public void Show()
        {
            _button.enabled = true;
            _bgImage.enabled = true;
            _text.enabled = true;
        }

        private string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, $"netdata_{_filePostfix}.csv");
        }

        [ClientRpc]
        private void RttPingPongClientRpc(ClientRpcParams clientParams){
            _rttMeasurement = _frameIndependentWatch.ElapsedMilliseconds;
            _frameIndependentWatch.Stop();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RttPingPongServerRpc(ulong senderId)
        {
            RttPingPongClientRpc(
                new ClientRpcParams {
                    Send = new ClientRpcSendParams{TargetClientIds = new List<ulong>{senderId}}
                });
        }
    }
}
