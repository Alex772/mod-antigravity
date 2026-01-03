using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Antigravity.TerminalClient;

/// <summary>
/// Parses and displays network messages in a readable format.
/// Based on the NetworkMessage format from the main mod.
/// </summary>
public static class MessageParser
{
    // Message types from NetworkMessage.cs
    private static readonly Dictionary<byte, string> MessageTypes = new()
    {
        { 0, "None" },
        { 1, "WorldData" },
        { 2, "GameStarting" },
        { 3, "GameStarted" },
        { 4, "Command" },
        { 5, "PlayerJoined" },
        { 6, "PlayerLeft" },
        { 7, "ChatMessage" },
        { 8, "SyncRequest" },
        { 9, "SyncResponse" },
        { 10, "ClientReady" },
        { 11, "HostReady" },
        { 12, "Ping" },
        { 13, "Pong" },
        { 14, "PositionSync" },
        { 15, "ChoreAssignment" },
        { 16, "MinionChecksum" },
        { 17, "CursorPosition" },
        { 255, "Test" }
    };
    
    /// <summary>
    /// Parse and display a network message, returning a summary string for logging.
    /// </summary>
    public static string ParseAndDisplay(byte[] data)
    {
        if (data.Length == 0)
        {
            Console.WriteLine("[←] Pacote vazio recebido");
            return "Empty packet";
        }
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  [←] PACOTE RECEBIDO - {data.Length,5} bytes @ {System.DateTime.Now:HH:mm:ss.fff}              ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        
        // Show raw hex (first 64 bytes)
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  HEX: ");
        var hexBytes = Math.Min(data.Length, 64);
        for (int i = 0; i < hexBytes; i++)
        {
            Console.Write($"{data[i]:X2} ");
            if ((i + 1) % 16 == 0 && i < hexBytes - 1)
                Console.Write("\n       ");
        }
        if (data.Length > 64)
            Console.Write("...");
        Console.WriteLine();
        Console.ResetColor();
        
        // Parse message
        var summary = new StringBuilder();
        
        if (data.Length >= 1)
        {
            var messageType = data[0];
            var typeName = MessageTypes.TryGetValue(messageType, out var name) ? name : $"Unknown({messageType})";
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  Tipo: {typeName}");
            Console.ResetColor();
            
            summary.Append($"Type={typeName}");
            
            // Parse based on type
            try
            {
                var details = ParseMessageDetails(messageType, data);
                if (!string.IsNullOrEmpty(details))
                    summary.Append($" | {details}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Erro ao parsear: {ex.Message}");
                Console.ResetColor();
                summary.Append($" | ParseError: {ex.Message}");
            }
        }
        
        Console.WriteLine();
        return summary.ToString();
    }
    
    private static string ParseMessageDetails(byte type, byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        
        reader.ReadByte(); // type (already read)
        
        var details = new StringBuilder();
        
        // Standard header: SenderSteamId (8 bytes), Tick (8 bytes)
        if (data.Length >= 17)
        {
            var senderId = reader.ReadInt64();
            var tick = reader.ReadInt64();
            
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  SenderId: {senderId}");
            Console.WriteLine($"  Tick: {tick}");
            Console.ResetColor();
            
            details.Append($"Sender={senderId}, Tick={tick}");
        }
        
        // Payload length
        if (data.Length >= 21)
        {
            var payloadLength = reader.ReadInt32();
            Console.WriteLine($"  Payload: {payloadLength} bytes");
            details.Append($", Payload={payloadLength}b");
            
            // Type-specific parsing
            switch (type)
            {
                case 1: // WorldData
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  → Dados do mundo (comprimido)");
                    Console.ResetColor();
                    details.Append(" [WorldData]");
                    break;
                    
                case 2: // GameStarting
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  → Host iniciando o jogo...");
                    Console.ResetColor();
                    details.Append(" [GameStarting]");
                    break;
                    
                case 3: // GameStarted
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  → Jogo iniciado!");
                    Console.ResetColor();
                    details.Append(" [GameStarted]");
                    break;
                    
                case 4: // Command
                    if (payloadLength > 0)
                    {
                        var commandData = reader.ReadBytes(Math.Min(payloadLength, 100));
                        var preview = BitConverter.ToString(commandData).Replace("-", " ");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"  → Comando: {preview}");
                        Console.ResetColor();
                        details.Append($" [Command: {preview.Substring(0, Math.Min(30, preview.Length))}...]");
                    }
                    break;
                    
                case 5: // PlayerJoined
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  → Jogador entrou!");
                    Console.ResetColor();
                    details.Append(" [PlayerJoined]");
                    break;
                    
                case 6: // PlayerLeft
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  → Jogador saiu");
                    Console.ResetColor();
                    details.Append(" [PlayerLeft]");
                    break;
                    
                case 7: // ChatMessage
                    if (payloadLength > 0)
                    {
                        var messageBytes = reader.ReadBytes(payloadLength);
                        var message = System.Text.Encoding.UTF8.GetString(messageBytes);
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"  → Chat: \"{message}\"");
                        Console.ResetColor();
                        details.Append($" [Chat: \"{message}\"]");
                    }
                    break;
                    
                case 8: // SyncRequest
                    Console.WriteLine($"  → Pedido de sincronização");
                    details.Append(" [SyncRequest]");
                    break;
                    
                case 9: // SyncResponse
                    Console.WriteLine($"  → Resposta de sincronização");
                    details.Append(" [SyncResponse]");
                    break;
                    
                case 10: // ClientReady
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  → Cliente pronto!");
                    Console.ResetColor();
                    details.Append(" [ClientReady]");
                    break;
                    
                case 11: // HostReady
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  → Host pronto!");
                    Console.ResetColor();
                    details.Append(" [HostReady]");
                    break;
                    
                case 12: // Ping
                    Console.WriteLine($"  → Ping");
                    details.Append(" [Ping]");
                    break;
                    
                case 13: // Pong
                    Console.WriteLine($"  → Pong");
                    details.Append(" [Pong]");
                    break;
                    
                case 14: // PositionSync
                    Console.WriteLine($"  → Sincronização de posições");
                    details.Append(" [PositionSync]");
                    break;
                    
                case 17: // CursorPosition
                    Console.WriteLine($"  → Posição do cursor");
                    details.Append(" [CursorPosition]");
                    break;
                    
                case 255: // Test
                    if (payloadLength > 0)
                    {
                        var testData = reader.ReadBytes(payloadLength);
                        var testMsg = System.Text.Encoding.UTF8.GetString(testData);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  → Test: \"{testMsg}\"");
                        Console.ResetColor();
                        details.Append($" [Test: \"{testMsg}\"]");
                    }
                    break;
            }
        }
        
        return details.ToString();
    }
}
