# 🧪 Checklist de Testes - Antigravity Multiplayer Mod

## Níveis de Teste

```
┌─────────────────────────────────────────────────────────────┐
│                    PIRÂMIDE DE TESTES                        │
│                                                              │
│                        /\                                    │
│                       /  \      🔺 Testes E2E                │
│                      /    \        (Manuais, 2 instâncias)   │
│                     /──────\                                 │
│                    /        \   🔺 Testes de Integração      │
│                   /          \     (Network, Sync)           │
│                  /────────────\                              │
│                 /              \ 🔺 Testes Unitários         │
│                /________________\   (Logic, Commands)        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## ✅ Testes Unitários (Automatizados)

### Commands
- [ ] `CommandDispatcher` encaminha comandos corretamente
- [ ] `CommandQueue` mantém ordem FIFO
- [ ] Cada tipo de comando serializa/deserializa corretamente
- [ ] Comandos inválidos são rejeitados

### Network
- [ ] `PacketRegistry` registra e recupera tipos de pacotes
- [ ] Serialização de pacotes é bidirecional (encode/decode)
- [ ] `ConnectionHandler` detecta timeouts
- [ ] `SessionManager` gerencia sessões corretamente

### Sync
- [ ] `StateDelta` calcula diferenças corretamente
- [ ] `SyncEngine` aplica deltas na ordem correta
- [ ] Hard sync substitui estado completamente
- [ ] Soft sync mescla apenas campos alterados

### Utils
- [ ] Extension methods funcionam como esperado
- [ ] Helpers são thread-safe (se aplicável)

---

## 🔗 Testes de Integração (Automatizados)

### Network Integration
- [ ] Cliente conecta ao servidor (localhost)
- [ ] Handshake completo com sucesso
- [ ] Múltiplos clientes simultâneos
- [ ] Reconexão após desconexão
- [ ] Timeout de conexão funciona
- [ ] Pacotes grandes são fragmentados e reagrupados

### Sync Integration
- [ ] Comando enviado pelo cliente chega ao servidor
- [ ] Servidor propaga comando para outros clientes
- [ ] Hard sync transfere save file completo
- [ ] Estado após sync é idêntico em todos os clientes

---

## 🎮 Testes Manuais (End-to-End)

### Setup de Teste
```
Requisitos:
- 2 instâncias do ONI (2 PCs ou VM)
- Mod instalado em ambas
- Conexão de rede entre as máquinas
```

### Cenário 1: Conexão Básica

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Host cria novo jogo multiplayer | Lobby criado, código exibido |
| 2 | Cliente insere código e conecta | Conexão estabelecida |
| 3 | Ambos veem status "Connected" | ✅ UI atualizada |
| 4 | Host inicia o jogo | Ambos carregam o mundo |

### Cenário 2: Sincronização de Comandos

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Host marca área para cavar | Área marcada em ambos |
| 2 | Cliente marca área para construir | Área marcada em ambos |
| 3 | Host pausa o jogo | Jogo pausa em ambos |
| 4 | Cliente altera velocidade | Velocidade muda em ambos |

### Cenário 3: UI Sync

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Host abre tela de Skills | Nenhum efeito no cliente |
| 2 | Host altera skill de duplicant | Skill muda em ambos |
| 3 | Cliente abre prioridades | Nenhum efeito no host |
| 4 | Cliente altera prioridade | Prioridade muda em ambos |

### Cenário 4: Sincronização de Construção

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Host constrói uma porta | Porta aparece em ambos |
| 2 | Cliente constrói um cano | Cano aparece em ambos |
| 3 | Host cancela construção | Cancelamento em ambos |
| 4 | Duplicant constrói item | Construção visível em ambos |

### Cenário 5: Hard Sync (Periódico)

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Joga até amanhecer (novo dia) | Hard sync executado |
| 2 | Verifica estados | Estados idênticos |
| 3 | Introduz desync artificial | Estados diferentes |
| 4 | Aguarda próximo hard sync | Estados reconciliados |

### Cenário 6: Reconexão

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Cliente desconecta (ALT+F4) | Host continua jogando |
| 2 | Cliente reconecta | Re-sync completo |
| 3 | Estado do cliente atualizado | Idêntico ao host |

### Cenário 7: Save/Load Multiplayer

| Passo | Ação | Resultado Esperado |
|-------|------|-------------------|
| 1 | Host salva o jogo | Save criado |
| 2 | Todos desconectam | Sessão encerrada |
| 3 | Host carrega save MP | Lobby recriado |
| 4 | Clientes reconectam | Estado restaurado |

---

## 🐛 Testes de Edge Cases

### Rede
- [ ] Latência alta (500ms+) - usar throttle de rede
- [ ] Perda de pacotes (10%) - simular com tools
- [ ] Conexão instável (on/off)
- [ ] Muitos jogadores simultâneos (4+)

### Desync
- [ ] Comandos conflitantes simultâneos
- [ ] Tick de simulação diferente
- [ ] Ações durante pause transition

### Performance
- [ ] Colônia grande (200+ cycles)
- [ ] Muitas construções simultâneas
- [ ] Muita atividade de duplicants

### Compatibilidade
- [ ] Mod sozinho (vanilla)
- [ ] Com outros mods populares
- [ ] Diferentes versões do ONI
- [ ] Com e sem DLC (Spaced Out!)

---

## 📊 Métricas a Monitorar

| Métrica | Alvo | Crítico |
|---------|------|---------|
| Latência de comando | < 100ms | > 500ms |
| Hard sync duration | < 5s | > 30s |
| Memory leak por hora | < 50MB | > 200MB |
| Desyncs por hora | < 5 | > 20 |
| CPU overhead | < 5% | > 15% |

---

## 📝 Template de Bug Report

```markdown
## Descrição
[Descreva o bug]

## Passos para Reproduzir
1. ...
2. ...
3. ...

## Comportamento Esperado
[O que deveria acontecer]

## Comportamento Atual
[O que está acontecendo]

## Ambiente
- Versão do Mod: 
- Versão do ONI: 
- OS: 
- Número de jogadores: 
- Tipo de conexão (LAN/Internet): 

## Logs
[Anexar output_log.txt]

## Screenshots/Vídeos
[Se aplicável]
```
