# 🚀 Antigravity - ONI Multiplayer Mod

<div align="center">

![Version](https://img.shields.io/badge/version-0.1.0--alpha-blue)
![ONI Version](https://img.shields.io/badge/ONI-U52--600112-green)
![License](https://img.shields.io/badge/license-MIT-purple)
![Steam P2P](https://img.shields.io/badge/Steam-P2P%20Networking-blue)

**Jogue Oxygen Not Included com seus amigos!**

[Instalação](#-instalação) •
[Como Usar](#-como-usar) •
[Desenvolvimento](#-desenvolvimento) •
[Roadmap](#-roadmap)

</div>

---

## 📖 Sobre

**Antigravity** é um mod que adiciona suporte multiplayer ao Oxygen Not Included. Usa a rede P2P do Steam para conexão - não precisa de IP ou port forwarding!

### ✨ Funcionalidades Atuais (v0.1.0-alpha)

- 🎮 **Menu Multiplayer** - Botão no menu principal
- 🔗 **Steam P2P** - Conexão via Steam (sem IP necessário!)
- 📋 **Sistema de Lobby** - Crie/entre em lobbies com código
- 📋 **Copiar Código** - Um clique para copiar o código do lobby
- 🚀 **Fluxo de Início** - Host seleciona novo jogo ou carregar save

### 🗺️ Roadmap

| Fase | Status | Descrição |
|------|--------|-----------|
| Fase 1 | ✅ Concluída | Sistema de Lobby Steam |
| Fase 2 | ✅ Próxima | Sincronização inicial do mundo |
| Fase 3 | ✅ Planejado | Sincronização de comandos |
| Fase 4 | 🔄 Planejado | UI in-game (cursores, chat) |
| Fase 5 | 📋 Planejado | Polish e reconexão |

---

## 📥 Instalação

### Para Jogadores (Manual)

1. Baixe o `Antigravity_Mod.zip` em [Releases](../../releases)
2. Extraia para `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\`
3. Ative o mod no menu de mods do jogo

### Via Steam Workshop (Recomendado)

1. Acesse a [página do mod na Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3635632392)
2. Clique em **"+ Inscrever-se"**
3. O mod será instalado automaticamente!

---

## 🎮 Como Usar

### Criar uma Sessão (Host)

1. No menu principal, clique em **"MULTIPLAYER"**
2. Clique em **"🎮 HOST GAME (Steam)"**
3. Clique em **"📋 COPY CODE"** para copiar o código
4. Compartilhe o código com seu amigo
5. Clique em **"🚀 START GAME"** quando todos estiverem prontos
6. Escolha **"NEW COLONY"** ou **"LOAD SAVE"**

### Entrar em uma Sessão (Cliente)

1. No menu principal, clique em **"MULTIPLAYER"**
2. Cole o código do lobby no campo
3. Clique em **"🔗 JOIN GAME"**
4. Aguarde o host iniciar

---

## 🛠️ Desenvolvimento

### Pré-requisitos

- Visual Studio 2022 ou VS Code
- .NET SDK 6.0+
- Oxygen Not Included (Steam)

### Configuração

```bash
# Clone o repositório
git clone https://github.com/seu-usuario/antigravity.git

# Copie local.props.example para local.props
cp local.props.example local.props

# Compile
dotnet build Antigravity.sln

# Deploy para testar
.\deploy.bat
```

### Scripts Úteis

| Script | Descrição |
|--------|-----------|
| `deploy.bat` | Compila e copia para pasta de mods |
| `create_package.bat` | Cria ZIP para distribuição |

### Estrutura do Projeto

```
src/
├── Antigravity.Core/      # Networking, sync engine
├── Antigravity.Patches/   # Patches Harmony (UI, game)
├── Antigravity.Client/    # UI do multiplayer
├── Antigravity.Server/    # Lógica do servidor
└── Antigravity.Mod/       # Ponto de entrada

docs/
├── MULTIPLAYER_DESIGN.md  # Design do fluxo multiplayer
├── DEPLOY_MANUAL.md       # Instruções de deploy
├── TESTING_GUIDE.md       # Como testar
└── ...
```

Veja [docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) para detalhes.

---

## 🧪 Testando

### Teste Solo (básico)
1. Execute `deploy.bat`
2. Abra o ONI
3. MULTIPLAYER → HOST GAME
4. Verifique se o código aparece

### Teste com Amigo
1. Execute `create_package.bat` → gera `Antigravity_Mod.zip`
2. Envie o ZIP para o amigo
3. Amigo extrai na pasta de mods
4. Você faz HOST, compartilha o código
5. Amigo faz JOIN com o código

---

## 🤝 Contribuindo

Contribuições são bem-vindas!

1. Fork o projeto
2. Crie sua branch (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja [LICENSE](LICENSE) para detalhes.

---

## 🙏 Agradecimentos

- [ONI Multiplayer by onimp](https://github.com/onimp/oni_multiplayer) - Inspiração e referência
- [Cairath's Modding Guide](https://github.com/Cairath/Oxygen-Not-Included-Modding) - Recursos de modding
- Comunidade do ONI Modding Discord

---

<div align="center">

**Feito com ❤️ para a comunidade de ONI**

</div>
