using System;
using System.Threading;
using Antigravity.TerminalClient;

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║   🖥️  Antigravity Terminal Client - Network Debug Tool v2.0      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var client = new NetworkClient();
var logger = new CommandLogger();
var running = true;

// Event handlers
client.OnConnected += () => 
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[✓] Conectado ao host!");
    Console.ResetColor();
    logger.LogEvent("CONNECTION", "Connected to host");
};

client.OnDisconnected += (reason) => 
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[!] Desconectado: {reason}");
    Console.ResetColor();
    logger.LogEvent("CONNECTION", $"Disconnected: {reason}");
};

client.OnDataReceived += (data) => 
{
    var parsed = MessageParser.ParseAndDisplay(data);
    logger.LogReceived(data, parsed);
};

Console.WriteLine("Comandos: help, connect, send, log, save, stats, clear, exit");
Console.WriteLine("Digite 'connect' para conectar ao host (127.0.0.1:7777).");
Console.WriteLine();

// Main loop
while (running)
{
    Console.Write("> ");
    var input = Console.ReadLine()?.Trim() ?? "";
    
    var parts = input.Split(' ', 2);
    var command = parts[0].ToLower();
    var cmdArgs = parts.Length > 1 ? parts[1] : "";

    switch (command)
    {
        case "help":
        case "h":
        case "?":
            ShowHelp();
            break;
            
        case "connect":
        case "c":
            var address = string.IsNullOrEmpty(cmdArgs) ? "127.0.0.1" : cmdArgs.Split(':')[0];
            var port = cmdArgs.Contains(':') ? int.Parse(cmdArgs.Split(':')[1]) : 7777;
            Console.WriteLine($"Conectando a {address}:{port}...");
            client.Connect(address, port);
            break;
            
        case "disconnect":
        case "dc":
            client.Disconnect();
            break;
            
        case "status":
        case "s":
            ShowStatus(client);
            break;
            
        case "send":
            if (string.IsNullOrEmpty(cmdArgs))
            {
                ShowSendHelp();
            }
            else
            {
                SendTestMessage(client, cmdArgs, logger);
            }
            break;
            
        case "hex":
            if (string.IsNullOrEmpty(cmdArgs))
            {
                Console.WriteLine("Uso: hex <dados em hexadecimal>");
            }
            else
            {
                SendRawHex(client, cmdArgs, logger);
            }
            break;
            
        case "log":
            if (cmdArgs.ToLower() == "on")
                logger.SetEnabled(true);
            else if (cmdArgs.ToLower() == "off")
                logger.SetEnabled(false);
            else
                logger.SetEnabled(!logger.IsEnabled);
            break;
            
        case "save":
            logger.SaveToFile();
            break;
            
        case "stats":
            logger.ShowStats();
            break;
            
        case "clear":
            logger.Clear();
            break;
            
        case "replay":
            // Replay a specific message type
            if (string.IsNullOrEmpty(cmdArgs))
            {
                Console.WriteLine("Uso: replay <tipo> - reenvia último pacote daquele tipo");
            }
            break;
            
        case "exit":
        case "quit":
        case "q":
            running = false;
            client.Disconnect();
            Console.WriteLine("Salvando log antes de sair...");
            if (logger.EntryCount > 0)
                logger.SaveToFile();
            break;
            
        case "":
            break;
            
        default:
            Console.WriteLine($"Comando desconhecido: {command}. Digite 'help' para ajuda.");
            break;
    }
    
    // Poll network
    client.Update();
    Thread.Sleep(10);
}

Console.WriteLine("Saindo...");

void ShowHelp()
{
    Console.WriteLine();
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                         COMANDOS                                  ║");
    Console.WriteLine("╠═══════════════════════════════════════════════════════════════════╣");
    Console.WriteLine("║  CONEXÃO:                                                         ║");
    Console.WriteLine("║    connect [ip:port]  - Conecta ao host (default: 127.0.0.1:7777) ║");
    Console.WriteLine("║    disconnect, dc     - Desconecta do host                        ║");
    Console.WriteLine("║    status, s          - Mostra status da conexão                  ║");
    Console.WriteLine("╠═══════════════════════════════════════════════════════════════════╣");
    Console.WriteLine("║  MENSAGENS:                                                       ║");
    Console.WriteLine("║    send <tipo>        - Envia mensagem de teste                   ║");
    Console.WriteLine("║    hex <dados>        - Envia dados raw em hexadecimal            ║");
    Console.WriteLine("╠═══════════════════════════════════════════════════════════════════╣");
    Console.WriteLine("║  LOG:                                                             ║");
    Console.WriteLine("║    log [on/off]       - Toggle logging de mensagens               ║");
    Console.WriteLine("║    save               - Salva log em arquivo                      ║");
    Console.WriteLine("║    stats              - Mostra estatísticas do log                ║");
    Console.WriteLine("║    clear              - Limpa o log                               ║");
    Console.WriteLine("╠═══════════════════════════════════════════════════════════════════╣");
    Console.WriteLine("║  OUTROS:                                                          ║");
    Console.WriteLine("║    help, h, ?         - Mostra esta ajuda                         ║");
    Console.WriteLine("║    exit, quit, q      - Sai do programa                           ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
}

void ShowSendHelp()
{
    Console.WriteLine();
    Console.WriteLine("Uso: send <tipo> [dados]");
    Console.WriteLine();
    Console.WriteLine("Tipos disponíveis:");
    Console.WriteLine("  ready       - ClientReady (indica que cliente está pronto)");
    Console.WriteLine("  ping        - Ping request");
    Console.WriteLine("  pong        - Pong response");
    Console.WriteLine("  chat <msg>  - Envia mensagem de chat");
    Console.WriteLine("  sync        - SyncRequest");
    Console.WriteLine("  test        - Mensagem de teste genérica");
    Console.WriteLine();
    Console.WriteLine("Exemplos:");
    Console.WriteLine("  send ready");
    Console.WriteLine("  send chat Hello World!");
    Console.WriteLine("  send ping");
    Console.WriteLine();
}

void ShowStatus(NetworkClient c)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔═══════════════════════════════════════════╗");
    Console.WriteLine("║         CONNECTION STATUS                 ║");
    Console.WriteLine("╚═══════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine($"  Conectado:          {(c.IsConnected ? "✓ Sim" : "✗ Não")}");
    Console.WriteLine($"  Peer ID:            {c.PeerId}");
    Console.WriteLine($"  Pacotes recebidos:  {c.PacketsReceived}");
    Console.WriteLine($"  Pacotes enviados:   {c.PacketsSent}");
    Console.WriteLine($"  Bytes recebidos:    {c.BytesReceived}");
    Console.WriteLine($"  Logging:            {(logger.IsEnabled ? "ON" : "OFF")}");
    Console.WriteLine($"  Entradas no log:    {logger.EntryCount}");
    Console.WriteLine();
}

void SendTestMessage(NetworkClient c, string cmdArgs, CommandLogger log)
{
    if (!c.IsConnected)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Erro: Não está conectado!");
        Console.ResetColor();
        return;
    }
    
    var parts = cmdArgs.Split(' ', 2);
    var msgType = parts[0].ToLower();
    var msgData = parts.Length > 1 ? parts[1] : "";
    
    byte[] sentData = null;
    string description = "";
    
    switch (msgType)
    {
        case "ready":
            sentData = c.SendClientReady();
            description = "ClientReady";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[→] ClientReady enviado");
            break;
            
        case "ping":
            sentData = c.SendPing();
            description = "Ping";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[→] Ping enviado");
            break;
            
        case "pong":
            sentData = c.SendPong();
            description = "Pong";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[→] Pong enviado");
            break;
            
        case "chat":
            if (string.IsNullOrEmpty(msgData))
            {
                Console.WriteLine("Uso: send chat <mensagem>");
                return;
            }
            sentData = c.SendChat(msgData);
            description = $"ChatMessage: {msgData}";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[→] Chat enviado: \"{msgData}\"");
            break;
            
        case "sync":
            sentData = c.SendSyncRequest();
            description = "SyncRequest";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[→] SyncRequest enviado");
            break;
            
        case "test":
            sentData = c.SendTest();
            description = "Test Message";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[→] Mensagem de teste enviada");
            break;
            
        default:
            Console.WriteLine($"Tipo desconhecido: {msgType}. Digite 'send' para ver opções.");
            return;
    }
    
    Console.ResetColor();
    
    if (sentData != null)
    {
        log.LogSent(sentData, description);
    }
}

void SendRawHex(NetworkClient c, string hexData, CommandLogger log)
{
    if (!c.IsConnected)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Erro: Não está conectado!");
        Console.ResetColor();
        return;
    }
    
    try
    {
        var bytes = Convert.FromHexString(hexData.Replace(" ", ""));
        c.SendRaw(bytes);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[→] {bytes.Length} bytes enviados");
        Console.ResetColor();
        log.LogSent(bytes, $"Raw hex data ({bytes.Length} bytes)");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Erro ao converter hex: {ex.Message}");
        Console.ResetColor();
    }
}
