# 📋 Plano de Implementação - Antigravity Multiplayer Mod

## Visão Geral

Este documento detalha o plano de implementação fase a fase para o mod multiplayer Antigravity.

---

## 🎯 Fase 1: Controle Compartilhado

**Objetivo**: Múltiplos jogadores controlam a mesma colônia simultaneamente.

### Sprint 1: Fundação (Semanas 1-2)

#### 1.1 Configuração do Ambiente
- [x] Estrutura de projeto criada
- [x] Solution do Visual Studio configurada
- [x] Scripts de build e deploy
- [ ] Configurar `local.props` com caminhos do ONI
- [ ] Testar compilação com referências do jogo
- [ ] Verificar mod carrega no ONI

#### 1.2 Networking Básico
- [ ] Testar LiteNetLib standalone
- [ ] Implementar conexão host/client básica
- [ ] Adicionar tratamento de erros de rede
- [ ] Testar em rede local (mesmo PC, 2 instâncias)

**Entregável**: Mod carrega, host pode iniciar servidor, client pode conectar

---

### Sprint 2: UI de Multiplayer (Semanas 3-4)

#### 2.1 Tela de Lobby
- [ ] Patch do menu principal para adicionar botão "Multiplayer"
- [ ] Criar tela de lobby (Criar/Entrar sessão)
- [ ] Campo para código de sessão
- [ ] Lista de jogadores conectados
- [ ] Botão de iniciar jogo (apenas host)

#### 2.2 Indicadores In-Game
- [ ] Status de conexão (HUD)
- [ ] Contador de jogadores online
- [ ] Indicador de sincronização

**Entregável**: UI funcional para criar/entrar em sessões

---

### Sprint 3: Sincronização de Comandos (Semanas 5-8)

#### 3.1 Sistema de Comandos Básicos
- [ ] Implementar `BuildCommand`
- [ ] Implementar `DigCommand`
- [ ] Implementar `MopCommand`
- [ ] Implementar `CancelCommand`
- [ ] Implementar `PriorityCommand`

#### 3.2 Patches de Ferramentas
- [ ] Patch `BuildTool` para interceptar builds
- [ ] Patch `DigTool` para interceptar dig
- [ ] Patch `MopTool` para interceptar mop
- [ ] Patch `PrioritizeTool` para interceptar prioridades

#### 3.3 Protocolo de Sync
- [ ] Definir formato de pacotes de comando
- [ ] Implementar serialização MessagePack
- [ ] Enviar comandos para todos os peers
- [ ] Executar comandos recebidos

**Entregável**: Comandos básicos sincronizam entre jogadores

---

### Sprint 4: Sincronização de UI (Semanas 9-10)

#### 4.1 Configurações da Colônia
- [ ] Sync tela de Prioridades
- [ ] Sync tela de Skills
- [ ] Sync tela de Schedules
- [ ] Sync tela de Consumables
- [ ] Sync árvore de Research

#### 4.2 Controles do Jogo
- [ ] Sync pause/resume
- [ ] Sync velocidade do jogo
- [ ] Sync configurações de warp

**Entregável**: Todas as configurações sincronizam

---

### Sprint 5: Recursos Sociais (Semanas 11-12)

#### 5.1 Cursores de Jogadores
- [ ] Capturar posição do cursor local
- [ ] Enviar posição periodicamente
- [ ] Renderizar cursores de outros jogadores
- [ ] Cores diferentes por jogador

#### 5.2 Sistema de Chat
- [ ] UI de chat (overlay)
- [ ] Envio/recebimento de mensagens
- [ ] Histórico de mensagens
- [ ] Notificações de chat

**Entregável**: Ver cursores dos outros, chat funcional

---

### Sprint 6: Hard Sync e Estabilidade (Semanas 13-14)

#### 6.1 Sistema de Hard Sync
- [ ] Trigger de sync a cada dia do jogo
- [ ] Salvar estado do host
- [ ] Transferir save para clients
- [ ] Clients carregam save
- [ ] Retomar jogo sincronizado

#### 6.2 Tratamento de Erros
- [ ] Reconexão automática
- [ ] Detecção de desync
- [ ] Sync forçado quando detecta erro
- [ ] Graceful degradation

**Entregável**: Jogo estável por longas sessões

---

### Sprint 7: Polish e Testes (Semanas 15-16)

#### 7.1 Otimizações
- [ ] Profiling de performance
- [ ] Reduzir overhead de rede
- [ ] Otimizar serialização
- [ ] Reduzir GC allocations

#### 7.2 Testes Extensivos
- [ ] Testes com 2 jogadores
- [ ] Testes com 4 jogadores
- [ ] Testes de longa duração (2h+)
- [ ] Testes de reconexão

**Entregável**: Versão Alpha pronta para release

---

## 📊 Métricas de Sucesso - Fase 1

| Métrica | Alvo |
|---------|------|
| Latência de comandos | < 100ms |
| Desyncs por sessão | < 5 |
| Sessões sem crash | > 95% |
| Reconexões bem-sucedidas | > 80% |

---

## 🔮 Fase 2: Colônias Separadas (Futuro)

### Pré-requisitos
- Fase 1 estável e testada
- Feedback da comunidade coletado
- Arquitetura preparada para extensão

### Funcionalidades Planejadas
- Cada jogador em asteroide diferente
- Sync independente por asteroide
- Sistema de troca de recursos
- Foguetes de transferência
- Duplicants visitantes

---

## 📝 Notas de Desenvolvimento

### Ferramentas Úteis
- **dnSpy**: Debuggar o jogo em tempo real
- **Wireshark**: Analisar tráfego de rede
- **Unity Explorer**: Inspecionar objetos do jogo

### Referências
- [ONI Multiplayer Mod](https://github.com/onimp/oni_multiplayer)
- [Cairath's Modding Guide](https://github.com/Cairath/Oxygen-Not-Included-Modding)
- [Harmony Documentation](https://harmony.pardeike.net/)
- [LiteNetLib Wiki](https://github.com/RevenantX/LiteNetLib/wiki)

### Discord da Comunidade
- [ONI Modding Discord](https://discord.gg/EBncbX2)
- [ONI Multiplayer Discord](https://discord.gg/3TQ97w8Qwq)
