using System.Collections.Generic;
using System.Linq;

namespace Antigravity.Debugger.Models;

public class PacketGroup
{
    public int GameTick { get; set; }
    public string TimeString { get; set; } = "";
    public string MainCommandName { get; set; } = "";
    public List<CapturedPacket> Packets { get; set; } = new();
    public bool IsExpanded { get; set; } = false;
    
    public int PacketCount => Packets.Count;
    public string DirectionSymbol => Packets.FirstOrDefault()?.DirectionSymbol ?? "→";
    public int TotalBytes => Packets.Sum(p => p.RawData.Length);
    public string TotalBytesString => TotalBytes < 1024 ? $"{TotalBytes} B" : $"{TotalBytes / 1024.0:F1} KB";
    
    public static PacketGroup FromPackets(IEnumerable<CapturedPacket> packets)
    {
        var list = packets.ToList();
        var first = list.FirstOrDefault();
        var mainPacket = list.OrderBy(p => GetPriority(p.CommandType)).ThenBy(p => p.Id).FirstOrDefault();
        
        return new PacketGroup
        {
            GameTick = first?.GameTick ?? 0,
            TimeString = first?.TimeString ?? "",
            MainCommandName = mainPacket?.CommandTypeName ?? "Unknown",
            Packets = list
        };
    }
    
    private static int GetPriority(int type) => type switch
    {
        10 or 11 => 0, 20 or 21 or 22 => 1, 30 or 31 => 2, 40 or 41 => 3, 50 or 51 => 4,
        60 or 61 => 5, 70 or 71 => 6, 80 or 81 => 7, 85 or 86 or 87 => 8, 88 => 50,
        89 => 51, 90 => 52, 91 or 92 or 93 or 94 => 100, _ => 99
    };
}
