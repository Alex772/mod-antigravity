using System;
using System.IO;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using Antigravity.Debugger.Models;

namespace Antigravity.Debugger.Services;

public class NetworkService : IDisposable
{
    private const string CONNECTION_KEY = "Antigravity_v1";
    private EventBasedNetListener? _listener;
    private NetManager? _netManager;
    private NetPeer? _peer;
    private int _packetIdCounter = 0;
    
    public event EventHandler? Connected;
    public event EventHandler<string>? Disconnected;
    public event EventHandler<CapturedPacket>? PacketReceived;
    public event EventHandler<CapturedPacket>? PacketSent;
    public event EventHandler<string>? Error;
    
    public bool IsConnected => _peer?.ConnectionState == ConnectionState.Connected;
    public string? ConnectedAddress { get; private set; }
    
    public void Connect(string address, int port)
    {
        if (_netManager != null) Disconnect();
        
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        
        _listener.PeerConnectedEvent += peer => {
            _peer = peer;
            ConnectedAddress = $"{address}:{port}";
            Connected?.Invoke(this, EventArgs.Empty);
        };
        
        _listener.PeerDisconnectedEvent += (peer, info) => {
            _peer = null;
            ConnectedAddress = null;
            Disconnected?.Invoke(this, info.Reason.ToString());
        };
        
        _listener.NetworkReceiveEvent += HandleNetworkReceive;
        _listener.ConnectionRequestEvent += request => request.Accept();
        _listener.NetworkErrorEvent += (endpoint, error) => Error?.Invoke(this, $"Network error: {error}");
        
        _netManager.Start();
        _netManager.Connect(address, port, CONNECTION_KEY);
    }
    
    private void HandleNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            int length = reader.GetInt();
            if (length > 0 && length <= reader.AvailableBytes)
            {
                byte[] data = new byte[length];
                reader.GetBytes(data, length);
                var packet = ParsePacket(data, PacketDirection.Received);
                PacketReceived?.Invoke(this, packet);
            }
        }
        finally { reader.Recycle(); }
    }
    
    public CapturedPacket? SendRaw(byte[] data)
    {
        if (_peer == null) return null;
        var writer = new NetDataWriter();
        writer.Put(data.Length);
        writer.Put(data);
        _peer.Send(writer, DeliveryMethod.ReliableOrdered);
        var packet = ParsePacket(data, PacketDirection.Sent);
        PacketSent?.Invoke(this, packet);
        return packet;
    }
    
    private CapturedPacket ParsePacket(byte[] data, PacketDirection direction)
    {
        var packet = new CapturedPacket { Id = ++_packetIdCounter, Timestamp = SystemDateTime.Now, Direction = direction, RawData = data };
        
        if (data.Length >= 21)
        {
            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);
                packet.MessageType = reader.ReadByte();
                packet.MessageTypeName = MessageTypes.GetName(packet.MessageType);
                packet.SenderId = reader.ReadUInt64();
                packet.Tick = reader.ReadInt64();
                int payloadLength = reader.ReadInt32();
                if (payloadLength > 0 && payloadLength <= ms.Length - ms.Position)
                {
                    packet.Payload = reader.ReadBytes(payloadLength);
                    packet.PayloadJson = Encoding.UTF8.GetString(packet.Payload);
                    try
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(packet.PayloadJson);
                        var root = jsonDoc.RootElement;
                        if (root.TryGetProperty("GameTick", out var tickProp)) packet.GameTick = tickProp.GetInt32();
                        if (root.TryGetProperty("Type", out var typeProp)) packet.CommandType = typeProp.GetInt32();
                    }
                    catch { }
                }
            }
            catch { }
        }
        return packet;
    }
    
    public void Update() => _netManager?.PollEvents();
    
    public void Disconnect()
    {
        bool wasConnected = _peer != null || _netManager != null;
        try { _peer?.Disconnect(); } catch { }
        _netManager?.Stop();
        _netManager = null; _listener = null; _peer = null; ConnectedAddress = null;
        if (wasConnected) Disconnected?.Invoke(this, "User disconnected");
    }
    
    public void Dispose() => Disconnect();
}
