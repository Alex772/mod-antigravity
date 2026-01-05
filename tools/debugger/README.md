# Antigravity Debugger

Um aplicativo desktop para debug e testes do multiplayer Antigravity no Oxygen Not Included.

## 🚀 Funcionalidades

| Funcionalidade | Descrição |
|----------------|-----------|
| **Conexão** | Conecta ao host do jogo via LiteNetLib (porta 7777) |
| **Packet Viewer** | Lista todos os pacotes recebidos e enviados em tempo real |
| **Inspector** | Visualiza detalhes do pacote: tipo, sender, tick, payload JSON |
| **HEX View** | Visualização hexadecimal do pacote raw |
| **Replay** | Reenvia pacotes capturados para o host |
| **Save Session** | Salva todos os pacotes da sessão em arquivo JSON |

## 📋 Requisitos

- .NET 8 ou superior
- ONI com o mod Antigravity instalado

## 🛠️ Build

```powershell
cd tools/debugger
dotnet build
```

## 🎮 Como Usar

### 1. Preparar o Host (no jogo)

1. Abra Oxygen Not Included
2. Pressione **F11** para abrir o menu Antigravity
3. Clique em **HOST**
4. Selecione **New World** ou carregue um save existente
5. O host local iniciará na porta **7777**

### 2. Conectar o Debugger

1. Execute o debugger:
   ```powershell
   cd tools/debugger
   dotnet run
   ```

2. Na janela do debugger:
   - **Host**: `127.0.0.1` (localhost)
   - **Port**: `7777`
   - Clique em **Connect**

3. O status deve mudar para "Connected"

### 3. Capturar Pacotes

1. No jogo, faça qualquer ação:
   - Escavar (dig)
   - Construir
   - Priorizar
   - Cancelar tarefas

2. Os pacotes aparecerão na lista do debugger

### 4. Inspecionar Pacotes

1. Clique em um pacote na lista
2. O painel **INSPECTOR** mostra:
   - **Type**: Tipo da mensagem (Command, Ping, etc.)
   - **Sender**: ID do jogador que enviou
   - **Tick**: Tick do jogo
   - **Payload (JSON)**: Conteúdo deserializado

3. O painel **HEX VIEW** mostra os bytes raw

### 5. Replay de Pacotes

1. Selecione um pacote na lista
2. Clique em **▶ Replay Selected**
3. O pacote será reenviado para o host
4. No jogo, a ação deve ser executada

### 6. Salvar Sessão

1. Clique em **💾 Save Session**
2. O arquivo JSON será salvo em:
   ```
   tools/debugger/data/sessions/session_YYYY-MM-DD_HH-mm-ss.json
   ```

## 📁 Estrutura de Arquivos

```
tools/debugger/
├── Antigravity.Debugger.csproj    # Projeto principal
├── Program.cs                      # Entry point
├── App.axaml                       # Configuração Avalonia
├── GlobalUsings.cs                 # Global usings
│
├── Models/
│   ├── CapturedPacket.cs          # Modelo de pacote capturado
│   └── Session.cs                  # Modelo de sessão
│
├── Services/
│   ├── NetworkService.cs          # Serviço de rede (LiteNetLib)
│   └── SessionStorageService.cs   # Serviço de persistência JSON
│
├── ViewModels/
│   ├── ViewModelBase.cs           # Base para ViewModels
│   └── MainWindowViewModel.cs     # ViewModel principal
│
├── Views/
│   ├── MainWindow.axaml           # Layout principal
│   └── MainWindow.axaml.cs        # Code-behind
│
└── data/
    └── sessions/                   # Sessões salvas (JSON)
```

## 📦 Formato do Pacote

O protocolo Antigravity usa o seguinte formato:

| Campo | Tamanho | Descrição |
|-------|---------|-----------|
| Type | 1 byte | Tipo da mensagem (enum MessageType) |
| SenderSteamId | 8 bytes | ID Steam do remetente |
| Tick | 8 bytes | Tick do jogo |
| PayloadLength | 4 bytes | Tamanho do payload em bytes |
| Payload | variável | Dados JSON comprimidos |

### Tipos de Mensagem

| Valor | Nome | Descrição |
|-------|------|-----------|
| 1 | LobbyUpdate | Atualização do lobby |
| 10 | GameStarting | Jogo iniciando |
| 11 | WorldData | Dados do mundo |
| 12 | WorldDataChunk | Chunk de dados do mundo |
| 30 | **Command** | Comando do jogador (dig, build, etc.) |
| 31 | CommandBroadcast | Comando validado para todos |
| 50 | Chat | Mensagem de chat |
| 60 | Ping | Ping |
| 61 | Pong | Resposta de ping |

## 🐛 Troubleshooting

### "Connection refused" ou não conecta

1. Verifique se o jogo está aberto
2. Verifique se foi para F11 -> HOST
3. Verifique se a porta é 7777

### Replay não funciona

1. Verifique se está conectado (status verde)
2. Verifique os logs do jogo (Player.log):
   ```
   [Antigravity] LiteNetLib received X bytes from peer 1
   [Antigravity] CommandManager.OnLocalNetworkDataReceived: X bytes
   [Antigravity] Command queued for execution: Dig
   ```

3. Se aparecer "Failed to deserialize", o formato do pacote pode estar incorreto

### Onde encontrar logs do jogo

```
%LOCALAPPDATA%Low\Klei\Oxygen Not Included\Player.log
```

Ou use o script `clear_logs.bat` na raiz do projeto para limpar e depois monitorar:

```powershell
Get-Content "$env:APPDATA\..\LocalLow\Klei\Oxygen Not Included\Player.log" -Wait -Tail 100
```

## 📜 Formato de Sessão Salva

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "startTime": "2026-01-04T20:00:00",
  "endTime": "2026-01-04T21:30:00",
  "hostAddress": "127.0.0.1:7777",
  "packets": [
    {
      "id": 1,
      "timestamp": "2026-01-04T20:05:12.345",
      "direction": "Received",
      "rawData": "Hg...",
      "messageType": 30,
      "messageTypeName": "Command",
      "senderId": 503316480,
      "tick": 12906376724480,
      "payloadJson": "{\"Cells\":[50574],\"Type\":10}"
    }
  ],
  "stats": {
    "packetsReceived": 156,
    "packetsSent": 23,
    "bytesReceived": 12456,
    "bytesSent": 1890
  }
}
```

## 🎨 Interface

A interface usa o tema **Catppuccin Mocha** com as seguintes cores:

- **Background**: `#1e1e2e` (escuro)
- **Cards**: `#181825`
- **Primary**: `#89b4fa` (azul)
- **Success**: `#a6e3a1` (verde)
- **Error**: `#f38ba8` (vermelho)
- **Text**: `#cdd6f4` (claro)

## 📝 Licença

Este debugger faz parte do mod Antigravity para Oxygen Not Included.
