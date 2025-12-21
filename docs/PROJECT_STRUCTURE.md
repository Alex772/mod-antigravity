# 📁 Estrutura do Projeto - Antigravity Multiplayer Mod

## Visão Geral da Estrutura

```
📦 mod antigravity/
├── 📁 docs/                          # Documentação do projeto
│   ├── ARCHITECTURE.md               # Arquitetura técnica
│   ├── PROJECT_STRUCTURE.md          # Este arquivo
│   ├── CONTRIBUTING.md               # Guia de contribuição
│   ├── SETUP.md                      # Guia de configuração do ambiente
│   └── API.md                        # Documentação da API interna
│
├── 📁 src/                           # Código fonte principal
│   ├── 📁 Antigravity.Core/          # Biblioteca central (compartilhada)
│   │   ├── 📁 Commands/              # Sistema de comandos
│   │   │   ├── ICommand.cs           # Interface base de comando
│   │   │   ├── CommandDispatcher.cs  # Despachante de comandos
│   │   │   ├── CommandQueue.cs       # Fila de comandos
│   │   │   └── 📁 Impl/              # Implementações de comandos
│   │   │       ├── BuildCommand.cs
│   │   │       ├── DigCommand.cs
│   │   │       ├── PriorityCommand.cs
│   │   │       └── ...
│   │   │
│   │   ├── 📁 Network/               # Camada de rede
│   │   │   ├── 📁 Protocol/          # Protocolos de comunicação
│   │   │   │   ├── IPacket.cs        # Interface de pacote
│   │   │   │   ├── PacketRegistry.cs # Registro de tipos de pacotes
│   │   │   │   └── 📁 Packets/       # Definições de pacotes
│   │   │   │       ├── HandshakePacket.cs
│   │   │   │       ├── CommandPacket.cs
│   │   │   │       ├── SyncPacket.cs
│   │   │   │       └── ChatPacket.cs
│   │   │   │
│   │   │   ├── 📁 Transport/         # Camada de transporte
│   │   │   │   ├── ITransport.cs     # Interface de transporte
│   │   │   │   ├── LiteNetTransport.cs
│   │   │   │   └── SteamTransport.cs # (Futuro - integração Steam)
│   │   │   │
│   │   │   ├── NetworkManager.cs     # Gerenciador principal de rede
│   │   │   ├── ConnectionHandler.cs  # Handler de conexões
│   │   │   └── SessionManager.cs     # Gerenciador de sessões
│   │   │
│   │   ├── 📁 Sync/                  # Sistema de sincronização
│   │   │   ├── ISyncable.cs          # Interface para objetos sincronizáveis
│   │   │   ├── SyncEngine.cs         # Motor de sincronização
│   │   │   ├── StateDelta.cs         # Representação de delta de estado
│   │   │   ├── HardSyncManager.cs    # Sincronização completa
│   │   │   └── SoftSyncManager.cs    # Sincronização incremental
│   │   │
│   │   ├── 📁 Serialization/         # Serialização de dados
│   │   │   ├── ISerializer.cs        # Interface de serialização
│   │   │   ├── MessagePackSerializer.cs
│   │   │   └── GameStateSerializer.cs
│   │   │
│   │   ├── 📁 Logging/               # Sistema de logs
│   │   │   ├── Logger.cs             # Logger principal
│   │   │   └── LogLevel.cs           # Níveis de log
│   │   │
│   │   └── 📁 Utils/                 # Utilitários
│   │       ├── Extensions.cs         # Extension methods
│   │       ├── Constants.cs          # Constantes do mod
│   │       └── Helpers.cs            # Funções helper
│   │
│   ├── 📁 Antigravity.Patches/       # Patches do Harmony
│   │   ├── 📁 UI/                    # Patches de UI
│   │   │   ├── MainMenuPatch.cs      # Patch do menu principal
│   │   │   ├── PauseMenuPatch.cs     # Patch do menu de pausa
│   │   │   └── ToolbarPatch.cs       # Patch da barra de ferramentas
│   │   │
│   │   ├── 📁 Game/                  # Patches de gameplay
│   │   │   ├── BuildToolPatch.cs     # Patch de construção
│   │   │   ├── DigToolPatch.cs       # Patch de escavação
│   │   │   ├── PriorityPatch.cs      # Patch de prioridades
│   │   │   └── SaveLoadPatch.cs      # Patch de save/load
│   │   │
│   │   ├── 📁 Simulation/            # Patches de simulação
│   │   │   ├── SimTickPatch.cs       # Intercepta ticks da simulação
│   │   │   └── WorldGenPatch.cs      # Patch de geração de mundo
│   │   │
│   │   └── PatchManager.cs           # Gerenciador de todos os patches
│   │
│   ├── 📁 Antigravity.Client/        # Código específico do cliente
│   │   ├── ClientManager.cs          # Gerenciador do cliente
│   │   ├── InputHandler.cs           # Captura inputs do jogador
│   │   └── 📁 UI/                    # UI específica do cliente
│   │       ├── LobbyScreen.cs        # Tela de lobby
│   │       ├── PlayerList.cs         # Lista de jogadores
│   │       ├── ChatWindow.cs         # Janela de chat
│   │       └── ConnectionStatus.cs   # Status de conexão
│   │
│   ├── 📁 Antigravity.Server/        # Código específico do servidor
│   │   ├── ServerManager.cs          # Gerenciador do servidor
│   │   ├── PlayerManager.cs          # Gerenciador de jogadores
│   │   ├── GameStateManager.cs       # Gerenciador de estado do jogo
│   │   └── AuthManager.cs            # Autenticação (futuro)
│   │
│   └── 📁 Antigravity.Mod/           # Ponto de entrada do mod
│       ├── AntigravityMod.cs         # Classe principal do mod
│       ├── ModConfig.cs              # Configurações do mod
│       └── Loader.cs                 # Loader do Harmony
│
├── 📁 tests/                         # Testes automatizados
│   ├── 📁 Antigravity.Tests.Unit/    # Testes unitários
│   │   ├── 📁 Commands/
│   │   │   └── CommandDispatcherTests.cs
│   │   ├── 📁 Network/
│   │   │   ├── PacketSerializationTests.cs
│   │   │   └── ConnectionHandlerTests.cs
│   │   ├── 📁 Sync/
│   │   │   └── SyncEngineTests.cs
│   │   └── TestHelpers.cs
│   │
│   ├── 📁 Antigravity.Tests.Integration/ # Testes de integração
│   │   ├── NetworkIntegrationTests.cs
│   │   └── SyncIntegrationTests.cs
│   │
│   └── 📁 Antigravity.Tests.Mocks/   # Mocks para testes
│       ├── MockNetworkManager.cs
│       ├── MockGameState.cs
│       └── MockTransport.cs
│
├── 📁 tools/                         # Ferramentas de desenvolvimento
│   ├── 📁 scripts/                   # Scripts de automação
│   │   ├── build.ps1                 # Script de build (Windows)
│   │   ├── build.sh                  # Script de build (Linux/Mac)
│   │   ├── deploy-local.ps1          # Deploy para pasta de mods local
│   │   ├── run-tests.ps1             # Executa todos os testes
│   │   └── package.ps1               # Empacota para distribuição
│   │
│   ├── 📁 devserver/                 # Servidor de desenvolvimento
│   │   └── standalone-server.cs      # Servidor standalone para testes
│   │
│   └── 📁 analyzers/                 # Analisadores estáticos
│       └── SyncAnalyzer.cs           # Verifica problemas de sincronia
│
├── 📁 assets/                        # Assets do mod
│   ├── 📁 sprites/                   # Sprites/Ícones
│   │   ├── multiplayer_icon.png
│   │   └── player_cursor.png
│   │
│   ├── 📁 translations/              # Traduções (i18n)
│   │   ├── en.po                     # Inglês
│   │   └── pt-BR.po                  # Português Brasil
│   │
│   └── 📁 configs/                   # Configurações padrão
│       └── default_config.json
│
├── 📁 lib/                           # Bibliotecas externas
│   └── .gitkeep                      # (DLLs via NuGet, não commitadas)
│
├── 📁 dist/                          # Output de distribuição
│   └── .gitkeep                      # (Gerado pelo build)
│
├── 📁 .github/                       # Configurações GitHub
│   ├── 📁 workflows/                 # GitHub Actions
│   │   ├── build.yml                 # CI Build
│   │   └── release.yml               # Release automation
│   │
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   │
│   └── PULL_REQUEST_TEMPLATE.md
│
├── 📄 Antigravity.sln                # Solution do Visual Studio
├── 📄 mod.yaml                       # Metadados do mod para ONI
├── 📄 mod_info.yaml                  # Info de versão do mod
├── 📄 .gitignore                     # Git ignore
├── 📄 .editorconfig                  # Config do editor
├── 📄 README.md                      # README principal
├── 📄 LICENSE                        # Licença (MIT recomendado)
└── 📄 CHANGELOG.md                   # Histórico de mudanças
```

---

## 📝 Descrição dos Módulos

### `Antigravity.Core`
Biblioteca central com toda a lógica reutilizável. Não depende diretamente do ONI.

### `Antigravity.Patches`
Todos os patches do Harmony que modificam o comportamento do jogo.

### `Antigravity.Client`
Lógica específica do lado do cliente (UI, input handling).

### `Antigravity.Server`
Lógica específica do servidor (pode rodar standalone ou embedded).

### `Antigravity.Mod`
Ponto de entrada do mod. Inicializa todos os componentes.

---

## 🧪 Estratégia de Testes

### Testes Unitários
- Testam componentes isolados
- Mockam dependências externas (ONI, rede)
- Rápidos de executar

### Testes de Integração
- Testam comunicação entre componentes
- Usam transporte de rede real (localhost)
- Mais lentos, executados no CI

### Testes Manuais
- Checklist documentado em `docs/TESTING_CHECKLIST.md`
- Cenários específicos de multiplayer
- Testes de stress (muitos jogadores)

---

## 🚀 Vantagens desta Estrutura

1. **Modularidade**: Cada componente pode ser desenvolvido/testado independentemente
2. **Escalabilidade**: Fácil adicionar novos comandos, patches, ou protocolos
3. **Testabilidade**: Lógica de negócio separada das dependências do jogo
4. **Evolução**: Estrutura preparada para Fase 2 (colônias separadas)
5. **Manutenibilidade**: Código organizado e documentado
