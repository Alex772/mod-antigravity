# Como Testar o Mod Multiplayer

## 📦 Distribuição para Amigos

Execute o script `create_package.bat` para criar um ZIP:

```
d:\Desenvolvimento\ONI\mod antigravity\create_package.bat
```

Isso cria `Antigravity_Mod.zip` que você pode enviar para amigos.

### Instruções para o Amigo:

1. Baixar o ZIP que você enviou
2. Extrair o conteúdo
3. Copiar a pasta `Antigravity` para:
   ```
   %USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\
   ```
4. Iniciar o ONI e ativar o mod

---

## 🧪 Testar Sozinho (Sem Amigo)

### Opção 1: Duas Contas Steam (Recomendado)

Se você tiver **dois PCs** ou um **amigo online**:
1. Você usa sua conta Steam em um PC
2. Usa outra conta Steam em outro PC (ou o amigo usa a dele)
3. Ambos instalam o mod
4. Um faz Host, o outro entra com o código

### Opção 2: Usar o Modo de Debug

Vou adicionar um modo "Debug Solo" que simula a conexão:

1. Host Game → cria o lobby normalmente
2. A tela mostra o código e jogadores conectados
3. Você pode verificar nos logs se está funcionando

### Opção 3: Testar com Discord/Steam Remote Play

1. Crie o lobby no ONI
2. Use o **Steam Remote Play Together** para convidar alguém
3. A pessoa nem precisa ter o jogo!

### Opção 4: Virtual Machine (Avançado)

1. Instale o VirtualBox/VMware
2. Crie uma VM com Windows
3. Instale Steam com outra conta
4. Instale ONI e o mod
5. Teste a conexão entre host (sua máquina) e VM

---

## 📋 Verificar se Está Funcionando

### Logs do Steam (no jogo):

Quando você clica em **HOST GAME**, verifique o log:

```
%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log
```

Procure por:
```
[Antigravity] Steam user: SeuNome (...)
[Antigravity] Creating Steam lobby...
[Antigravity] Lobby created! Code: 123456789...
```

Se aparecer isso, o lobby Steam foi criado com sucesso!

---

## 🎮 Fluxo do Teste

1. **Você (Host)**:
   - Abre ONI
   - Clica em MULTIPLAYER → HOST GAME
   - Anota o código que aparece

2. **Amigo (Client)**:
   - Abre ONI  
   - Clica em MULTIPLAYER
   - Cola o código → JOIN GAME

3. **Verificação**:
   - Ambos devem ver a lista de jogadores
   - Logs mostram "[Antigravity] Player joined: NomeDoAmigo"

---

## 🔧 Modo Debug (Testar API Steam localmente)

Para testar se a API Steam está funcionando:

1. Faça o deploy do mod
2. Abra o ONI
3. Vá para MULTIPLAYER → HOST GAME
4. Verifique se:
   - Um código numérico aparece (lobby criado)
   - Seu nome aparece na lista de jogadores
   - Não há erros vermelhos no log

Se tudo isso funcionar, a integração Steam está OK!
