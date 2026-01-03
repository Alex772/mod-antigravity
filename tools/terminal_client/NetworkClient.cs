using LiteNetLib;
using LiteNetLib.Utils;

namespace Antigravity.TerminalClient;

// Explicit delegates
public delegate void ConnectedHandler();
public delegate void DisconnectedHandler(string reason);
public delegate void DataReceivedHandler(byte[] data);

/// <summary>
/// Network client using LiteNetLib to connect to the game host.
/// </summary>
public class NetworkClient
{
    private NetManager _netManager;
    private EventBasedNetListener _listener;
    private NetPeer _peer;
    
    public bool IsConnected => _peer != null && _peer.ConnectionState == ConnectionState.Connected;
    public int PeerId => _peer?.Id ?? -1;
    public long PacketsReceived { get; private set; }
    public long PacketsSent { get; private set; }
    public long BytesReceived { get; private set; }
    
    public event ConnectedHandler OnConnected;
    public event DisconnectedHandler OnDisconnected;
    public event DataReceivedHandler OnDataReceived;
    
    public void Connect(string address, int port)
    {
        if (_netManager != null)
        {
            Disconnect();
        }
        
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
        
        _listener.PeerConnectedEvent += peer =>
        {
            _peer = peer;
            OnConnected?.Invoke();
        };
        
        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            _peer = null;
            OnDisconnected?.Invoke(info.Reason.ToString());
        };
        
        _listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            var data = new byte[reader.AvailableBytes];
            reader.GetBytes(data, data.Length);
            
            PacketsReceived++;
            BytesReceived += data.Length;
            
            OnDataReceived?.Invoke(data);
            
            reader.Recycle();
        };
        
        _listener.ConnectionRequestEvent += request =>
        {
            request.Accept();
        };
        
        _netManager.Start();
        _netManager.Connect(address, port, "Antigravity_v1");
    }
    
    public void Disconnect()
    {
        _peer?.Disconnect();
        _netManager?.Stop();
        _netManager = null;
        _listener = null;
        _peer = null;
    }
    
    public void Update()
    {
        _netManager?.PollEvents();
    }
    
    public void SendRaw(byte[] data)
    {
        if (_peer == null) return;
        
        var writer = new NetDataWriter();
        writer.Put(data);
        _peer.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
    }
    
    public byte[] SendClientReady()
    {
        // MessageType.ClientReady = 10 (based on NetworkMessage.cs)
        var writer = new NetDataWriter();
        writer.Put((byte)10); // MessageType
        writer.Put(0L);       // SenderSteamId (0 for terminal client)
        writer.Put(0L);       // Tick
        writer.Put(0);        // Payload length
        
        var data = writer.CopyData();
        _peer?.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
        return data;
    }
    
    public byte[] SendPing()
    {
        // MessageType.Ping = 12
        var writer = new NetDataWriter();
        writer.Put((byte)12); // MessageType
        writer.Put(0L);       // SenderSteamId
        writer.Put(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); // Tick (timestamp)
        writer.Put(0);        // Payload length
        
        var data = writer.CopyData();
        _peer?.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
        return data;
    }
    
    public byte[] SendPong()
    {
        // MessageType.Pong = 13
        var writer = new NetDataWriter();
        writer.Put((byte)13); // MessageType
        writer.Put(0L);       // SenderSteamId
        writer.Put(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); // Tick
        writer.Put(0);        // Payload length
        
        var data = writer.CopyData();
        _peer?.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
        return data;
    }
    
    public byte[] SendChat(string message)
    {
        // MessageType.ChatMessage = 7
        var writer = new NetDataWriter();
        writer.Put((byte)7);  // MessageType
        writer.Put(0L);       // SenderSteamId
        writer.Put(0L);       // Tick
        
        var payload = System.Text.Encoding.UTF8.GetBytes(message);
        writer.Put(payload.Length);
        writer.Put(payload);
        
        var data = writer.CopyData();
        _peer?.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
        return data;
    }
    
    public byte[] SendSyncRequest()
    {
        // MessageType.SyncRequest = 8
        var writer = new NetDataWriter();
        writer.Put((byte)8);  // MessageType
        writer.Put(0L);       // SenderSteamId
        writer.Put(0L);       // Tick
        writer.Put(0);        // Payload length
        
        var data = writer.CopyData();
        _peer?.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
        return data;
    }
    
    public byte[] SendTest()
    {
        // Send a simple test string
        var writer = new NetDataWriter();
        writer.Put((byte)255); // Custom test type
        writer.Put(0L);        // SenderSteamId
        writer.Put(0L);        // Tick
        
        var payload = System.Text.Encoding.UTF8.GetBytes("Terminal Client Test @ " + System.DateTime.Now.ToString("HH:mm:ss"));
        writer.Put(payload.Length);
        writer.Put(payload);
        
        var data = writer.CopyData();
        _peer?.Send(writer, DeliveryMethod.ReliableOrdered);
        PacketsSent++;
        return data;
    }
}
