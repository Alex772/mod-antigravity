using System;
using System.Collections.Generic;
using UnityEngine;

namespace Antigravity.Core.Network
{
    /// <summary>
    /// Manages in-game chat and system messages.
    /// </summary>
    public static class ChatManager
    {
        private static List<ChatEntry> _messages = new List<ChatEntry>();
        private const int MAX_MESSAGES = 100;
        
        public static event Action<ChatEntry> OnMessageReceived;
        public static IReadOnlyList<ChatEntry> Messages => _messages;
        
        /// <summary>
        /// Send a chat message to all players.
        /// </summary>
        public static void SendMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            var msg = new ChatMessage
            {
                SenderName = GetLocalPlayerName(),
                Text = text,
                MessageType = ChatMessageType.Player,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            
            if (MultiplayerState.IsHost)
            {
                // Host broadcasts to all
                GameSession.SendToAllClients(MessageType.Chat, msg);
                AddLocalMessage(msg);
            }
            else
            {
                // Client sends to host, host will broadcast
                GameSession.SendToHost(MessageType.Chat, msg);
                AddLocalMessage(msg);
            }
        }
        
        /// <summary>
        /// Send a system message (desync, join/leave, etc).
        /// </summary>
        public static void SendSystemMessage(string text, SystemMessageLevel level = SystemMessageLevel.Info)
        {
            var msg = new ChatMessage
            {
                SenderName = "SYSTEM",
                Text = text,
                MessageType = ChatMessageType.System,
                SystemLevel = level,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            
            if (MultiplayerState.IsHost)
            {
                GameSession.SendToAllClients(MessageType.Chat, msg);
            }
            
            AddLocalMessage(msg);
        }
        
        /// <summary>
        /// Handle received chat message.
        /// </summary>
        public static void HandleChatMessage(ChatMessage msg, ulong senderId)
        {
            // If host receives from client, broadcast to all
            if (MultiplayerState.IsHost && msg.MessageType == ChatMessageType.Player)
            {
                // Broadcast to other clients
                GameSession.SendToAllClients(MessageType.Chat, msg);
            }
            
            AddLocalMessage(msg);
        }
        
        private static void AddLocalMessage(ChatMessage msg)
        {
            var entry = new ChatEntry
            {
                SenderName = msg.SenderName,
                Text = msg.Text,
                Type = msg.MessageType,
                Level = msg.SystemLevel,
                Timestamp = msg.Timestamp
            };
            
            _messages.Add(entry);
            if (_messages.Count > MAX_MESSAGES)
            {
                _messages.RemoveAt(0);
            }
            
            OnMessageReceived?.Invoke(entry);
            
            // Log to Unity console
            string prefix = msg.MessageType == ChatMessageType.System ? "[SYSTEM]" : $"[{msg.SenderName}]";
            Debug.Log($"[Antigravity Chat] {prefix} {msg.Text}");
        }
        
        public static void Clear()
        {
            _messages.Clear();
        }
        
        private static string GetLocalPlayerName()
        {
            if (SteamManager.Initialized)
            {
                return Steamworks.SteamFriends.GetPersonaName();
            }
            return "Player";
        }
        
        // Convenience methods for system messages
        public static void NotifyPlayerJoined(string playerName)
        {
            SendSystemMessage($"{playerName} joined the game", SystemMessageLevel.Info);
        }
        
        public static void NotifyPlayerLeft(string playerName)
        {
            SendSystemMessage($"{playerName} left the game", SystemMessageLevel.Warning);
        }
        
        public static void NotifyDesync(string details)
        {
            SendSystemMessage($"⚠️ Desync detected: {details}", SystemMessageLevel.Warning);
        }
        
        public static void NotifyHostDisconnected()
        {
            SendSystemMessage("❌ Host disconnected! Returning to menu...", SystemMessageLevel.Error);
        }
        
        public static void NotifySyncPause(string reason)
        {
            SendSystemMessage($"⏸️ Game paused: {reason}", SystemMessageLevel.Info);
        }
    }
    
    /// <summary>
    /// Chat entry for display.
    /// </summary>
    public class ChatEntry
    {
        public string SenderName { get; set; }
        public string Text { get; set; }
        public ChatMessageType Type { get; set; }
        public SystemMessageLevel Level { get; set; }
        public long Timestamp { get; set; }
        
        public string FormattedTime
        {
            get
            {
                // Convert timestamp to hours:minutes
                long totalSeconds = Timestamp / 1000;
                int hours = (int)((totalSeconds % 86400) / 3600);
                int minutes = (int)((totalSeconds % 3600) / 60);
                return hours.ToString("00") + ":" + minutes.ToString("00");
            }
        }
        
        public string DisplayText => Type == ChatMessageType.System 
            ? $"[{FormattedTime}] {Text}" 
            : $"[{FormattedTime}] {SenderName}: {Text}";
    }
    
    // Note: ChatMessage, ChatMessageType, and SystemMessageLevel are defined in NetworkMessage.cs
}
