using System;
using System.Collections.Generic;

namespace Antigravity.Debugger.Models;

public class Session
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SystemDateTime StartTime { get; set; } = SystemDateTime.Now;
    public SystemDateTime? EndTime { get; set; }
    public string HostAddress { get; set; } = "";
    public List<CapturedPacket> Packets { get; set; } = new();
    public SessionStats Stats { get; set; } = new();
    public string DisplayName => $"{StartTime:yyyy-MM-dd HH:mm} - {HostAddress}";
}

public class SessionStats
{
    public int PacketsReceived { get; set; }
    public int PacketsSent { get; set; }
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
}
