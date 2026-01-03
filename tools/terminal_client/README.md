# 🖥️ Terminal Test Client - Antigravity Network Debug

Cliente de console para testar a comunicação de rede do multiplayer **sem precisar de duas instâncias do ONI**.

## 🎯 O que faz?

- Conecta ao host do jogo via LiteNetLib (porta 7777)
- Exibe todos os pacotes recebidos em tempo real
- Permite enviar comandos de teste
- Ajuda a debugar problemas de sincronização

## 🚀 Como usar

### 1. Compile o cliente de terminal
```batch
cd tools\terminal_client
dotnet build
```

### 2. Inicie o ONI como Host
```
1. Abra o ONI
2. F11 → LOCAL TEST → HOST
```

### 3. Execute o cliente de terminal
```batch
dotnet run
```

### 4. Comandos disponíveis
```
help          - Mostra comandos disponíveis
connect       - Conecta ao host (127.0.0.1:7777)
disconnect    - Desconecta
send <tipo>   - Envia mensagem de teste
status        - Mostra status da conexão
exit          - Sai do programa
```

## 📊 Exemplo de Output

```
[RECV] MessageType=WorldData Size=1024 bytes
  → Header: 01 00 00 00
  → Payload: [compressed save data]

[RECV] MessageType=GameStarting
  → ColonyName: "Test Colony"
  → IsNewGame: true
```

## 🔧 Arquitetura

```
terminal_client/
├── TerminalClient.csproj    # Projeto .NET Console
├── Program.cs               # Entry point
├── NetworkClient.cs         # Conexão LiteNetLib
├── MessageParser.cs         # Decodifica mensagens
└── README.md                # Esta documentação
```
