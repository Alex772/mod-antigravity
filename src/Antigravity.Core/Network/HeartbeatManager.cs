using System;
using System.Collections.Generic;
using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Manages heartbeat/ping system for disconnect detection.
    /// Host sends periodic heartbeats, clients respond.
    /// If a client or host doesn't respond in time, disconnect is detected.
    /// </summary>
    public static class HeartbeatManager
    {
        private const float HEARTBEAT_INTERVAL = 3.0f;  // Send heartbeat every 3 seconds
        private const float TIMEOUT_SECONDS = 10.0f;    // Disconnect after 10 seconds without response
        
        private static float _lastHeartbeatSent = 0f;
        private static float _lastHeartbeatReceived = 0f;
        private static bool _isActive = false;
        private static Dictionary<ulong, float> _clientLastResponse = new Dictionary<ulong, float>();
        
        // Define delegates to avoid System.Action conflicts
        public delegate void HostDisconnectedHandler();
        public delegate void ClientDisconnectedHandler(ulong clientId);
        
        public static event HostDisconnectedHandler OnHostDisconnected;
        public static event ClientDisconnectedHandler OnClientDisconnected;
        
        public static bool IsActive => _isActive;
        
        /// <summary>
        /// Start the heartbeat system.
        /// </summary>
        public static void Start()
        {
            _isActive = true;
            _lastHeartbeatSent = Time.realtimeSinceStartup;
            _lastHeartbeatReceived = Time.realtimeSinceStartup;
            _clientLastResponse.Clear();
            Debug.Log("[Antigravity] HeartbeatManager started");
        }
        
        /// <summary>
        /// Stop the heartbeat system.
        /// </summary>
        public static void Stop()
        {
            _isActive = false;
            _clientLastResponse.Clear();
            Debug.Log("[Antigravity] HeartbeatManager stopped");
        }
        
        /// <summary>
        /// Update - call every frame.
        /// </summary>
        public static void Update()
        {
            if (!_isActive) return;
            if (!MultiplayerState.IsMultiplayerSession) return;
            
            float now = Time.realtimeSinceStartup;
            
            if (MultiplayerState.IsHost)
            {
                UpdateHost(now);
            }
            else
            {
                UpdateClient(now);
            }
        }
        
        private static void UpdateHost(float now)
        {
            // Send periodic heartbeat to all clients
            if (now - _lastHeartbeatSent >= HEARTBEAT_INTERVAL)
            {
                SendHeartbeat();
                _lastHeartbeatSent = now;
            }
            
            // Check for client timeouts
            var disconnected = new List<ulong>();
            foreach (var kvp in _clientLastResponse)
            {
                if (now - kvp.Value > TIMEOUT_SECONDS)
                {
                    Debug.LogWarning($"[Antigravity] Client {kvp.Key} timed out!");
                    disconnected.Add(kvp.Key);
                }
            }
            
            foreach (var clientId in disconnected)
            {
                _clientLastResponse.Remove(clientId);
                OnClientDisconnected?.Invoke(clientId);
            }
        }
        
        private static void UpdateClient(float now)
        {
            // Check if host stopped responding
            if (now - _lastHeartbeatReceived > TIMEOUT_SECONDS)
            {
                Debug.LogError("[Antigravity] Host connection lost! No heartbeat in " + TIMEOUT_SECONDS + " seconds");
                OnHostDisconnected?.Invoke();
                Stop();
            }
        }
        
        private static void SendHeartbeat()
        {
            var msg = new HeartbeatMessage
            {
                ServerTime = Time.realtimeSinceStartup,
                GameTick = GameSession.GetCurrentGameTick()
            };
            
            GameSession.SendToAllClients(MessageType.Heartbeat, msg);
        }
        
        /// <summary>
        /// [CLIENT] Handle heartbeat from host.
        /// </summary>
        public static void HandleHeartbeat(HeartbeatMessage msg)
        {
            if (MultiplayerState.IsHost) return;
            
            _lastHeartbeatReceived = Time.realtimeSinceStartup;
            
            // Send acknowledgment back
            var ack = new HeartbeatAckMessage
            {
                ClientTime = Time.realtimeSinceStartup,
                OriginalServerTime = msg.ServerTime
            };
            
            GameSession.SendToHost(MessageType.HeartbeatAck, ack);
        }
        
        /// <summary>
        /// [HOST] Handle heartbeat acknowledgment from client.
        /// </summary>
        public static void HandleHeartbeatAck(ulong senderId, HeartbeatAckMessage msg)
        {
            if (!MultiplayerState.IsHost) return;
            
            _clientLastResponse[senderId] = Time.realtimeSinceStartup;
            
            // Calculate latency
            float latency = Time.realtimeSinceStartup - msg.OriginalServerTime;
            // Could store this for ping display
        }
        
        /// <summary>
        /// Register a client for tracking.
        /// </summary>
        public static void RegisterClient(ulong clientId)
        {
            _clientLastResponse[clientId] = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// Remove a client from tracking.
        /// </summary>
        public static void UnregisterClient(ulong clientId)
        {
            _clientLastResponse.Remove(clientId);
        }
    }
    
    /// <summary>
    /// Heartbeat message sent from host to clients.
    /// </summary>
    [Serializable]
    public class HeartbeatMessage
    {
        public float ServerTime { get; set; }
        public long GameTick { get; set; }
    }
    
    /// <summary>
    /// Heartbeat acknowledgment sent from client to host.
    /// </summary>
    [Serializable]
    public class HeartbeatAckMessage
    {
        public float ClientTime { get; set; }
        public float OriginalServerTime { get; set; }
    }
}
