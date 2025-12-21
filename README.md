# 🚀 Antigravity - ONI Multiplayer Mod

<div align="center">

![Version](https://img.shields.io/badge/version-0.1.0--alpha-blue)
![ONI Version](https://img.shields.io/badge/ONI-U52--600112-green)
![License](https://img.shields.io/badge/license-MIT-purple)

**Jogue Oxygen Not Included com seus amigos!**

[Instalação](#-instalação) •
[Como Usar](#-como-usar) •
[Desenvolvimento](#-desenvolvimento) •
[Roadmap](#-roadmap)

</div>

---

## 📖 Sobre

**Antigravity** é um mod que adiciona suporte multiplayer ao Oxygen Not Included. Na versão atual, múltiplos jogadores podem controlar a mesma colônia simultaneamente, compartilhando decisões e construções.

### ✨ Funcionalidades

- 🎮 **Controle Compartilhado** - Todos os jogadores controlam a mesma colônia
- 🔧 **Sincronização de Comandos** - Construir, cavar, configurar prioridades
- 💬 **Chat In-Game** - Comunique-se com outros jogadores
- 👥 **Cursores de Jogadores** - Veja onde outros jogadores estão olhando
- 💾 **Save Multiplayer** - Salve e continue jogos multiplayer

### 🗺️ Roadmap

| Fase | Status | Descrição |
|------|--------|-----------|
| Fase 1 | 🔄 Em Desenvolvimento | Controle compartilhado |
| Fase 2 | 📋 Planejado | Colônias separadas |
| Fase 3 | 📋 Planejado | Sistema de troca |

---

## 📥 Instalação

### Via Steam Workshop (Recomendado)
*Em breve...*

### Manual
1. Baixe a última release em [Releases](../../releases)
2. Extraia para `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\Antigravity`
3. Ative o mod no menu de mods do jogo

---

## 🎮 Como Usar

### Criar uma Sessão (Host)

1. No menu principal, clique em **"Multiplayer"**
2. Clique em **"Criar Sessão"**
3. Compartilhe o **código de sessão** com seus amigos
4. Inicie o jogo quando todos estiverem conectados

### Entrar em uma Sessão (Cliente)

1. No menu principal, clique em **"Multiplayer"**
2. Clique em **"Entrar em Sessão"**
3. Insira o **código de sessão** fornecido pelo host
4. Aguarde o início do jogo

---

## 🛠️ Desenvolvimento

### Pré-requisitos

- Visual Studio 2022
- .NET Framework 4.7.2
- Oxygen Not Included (Steam)

### Configuração

```bash
# Clone o repositório
git clone https://github.com/seu-usuario/antigravity.git

# Configure o ambiente
# Crie local.props com o caminho do ONI

# Compile
dotnet build Antigravity.sln
```

Veja [docs/SETUP.md](docs/SETUP.md) para instruções detalhadas.

### Estrutura do Projeto

```
src/
├── Antigravity.Core/      # Lógica central
├── Antigravity.Patches/   # Patches do Harmony
├── Antigravity.Client/    # Código do cliente
├── Antigravity.Server/    # Código do servidor
└── Antigravity.Mod/       # Ponto de entrada
```

Veja [docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) para detalhes.

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Veja [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md).

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
