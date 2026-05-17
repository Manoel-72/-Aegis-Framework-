# Guia de Curso - Aegis Engine versao atual

Este documento resume o estado atual da Aegis Engine para estudo, aulas,
mentoria e criacao de material didatico. Ele foca no que mudou recentemente,
quais recursos ja podem ser ensinados como MVP e quais partes ainda devem ser
tratadas como experimentais.

## Objetivo desta versao

A versao atual da engine esta caminhando para um MVP tecnico interno. O foco
principal deixou de ser adicionar muitos recursos soltos e passou a ser:

- estabilizar a API Lua;
- organizar criacao de componentes por fabrica;
- melhorar texto/fontes;
- tornar build/export Windows mais confiavel;
- separar recursos estaveis de recursos experimentais;
- preparar exemplos reais para curso.

## Principais novidades

### 1. API Lua menor e mais estavel

Foi criado um caminho recomendado para codigo novo:

```lua
local player = aegis.create("sprite", {
    path = "sprites/player.png",
    x = 100,
    y = 200,
    z = 10
})

aegis.destroy(player)
```

APIs antigas como `aegis.newSprite`, `aegis.newLabel` e `aegis.newRect`
continuam funcionando por compatibilidade, mas para cursos e templates novos o
ideal e ensinar primeiro `aegis.create(...)`.

### 2. ComponentFactory

A criacao de objetos foi isolada em uma fabrica:

```text
src/Aegis/Scripting/Components/ComponentFactory.cs
```

Isso ajuda a engine a crescer com mais organizacao. Em vez de colocar toda
criacao diretamente no `LuaRuntime`, novos componentes devem nascer na fabrica.

Componentes principais:

- `sprite`
- `rect`
- `label`
- `richLabel`
- `panel`
- `flow`
- `progressBar`
- `anim`
- `animatedSprite`

### 3. API classificada por estabilidade

A engine agora separa APIs em tres grupos:

- `Stable/MVP`: recomendadas para jogos, curso e templates oficiais.
- `Legacy`: antigas, ainda funcionam, mas nao devem ser prioridade em codigo novo.
- `Experimental`: podem mudar; a engine emite aviso no log quando usadas.

Documento principal:

```text
docs/MVP_API.md
```

Para curso, use o `MVP_API.md` como contrato principal da engine.

### 4. Fonte padrao automatica

Antes, texto podia nao aparecer se nenhuma fonte fosse carregada. Isso foi
melhorado.

Agora a engine tenta carregar uma fonte padrao automaticamente:

- primeiro em `res/fonts`;
- depois em fontes do sistema, como Segoe UI, Arial, DejaVu ou Liberation.

Novas APIs uteis:

```lua
local font = aegis.loadDefaultFont(28)
local title = aegis.newLabelSize("Meu Jogo", 32)
```

Recomendacao para aulas: ensine texto usando tamanho real de fonte, nao usando
`setScale` para aumentar label.

### 5. UI/HUD separada do mundo

A engine agora tem conceito de camada de UI/HUD.

Objetos do mundo sao afetados pela camera. Objetos de UI devem ficar fixos na
tela.

Exemplo:

```lua
local hud = aegis.create("flow", {
    direction = "horizontal",
    gap = 8,
    padding = 12,
    hud = true
})

local score = aegis.create("label", {
    text = "Score 0",
    size = 20,
    hud = true
})

aegis.flowAdd(hud, score)
aegis.setPosition(hud, 16, 16)
```

Tambem e possivel desenhar UI imediata:

```lua
function aegis_draw_ui()
    aegis.drawText("Vida 3", 20, 20, 1, 1, 1, 1)
end
```

### 6. Display mais seguro

Foi adicionado `displayMode` no `aegis.cfg`.

Modos recomendados:

```json
{
  "windowWidth": 1440,
  "windowHeight": 900,
  "displayMode": "windowed",
  "fullscreen": false
}
```

ou:

```json
{
  "windowWidth": 1440,
  "windowHeight": 900,
  "displayMode": "borderless",
  "fullscreen": true
}
```

Para MVP, o modo recomendado para tela cheia e `borderless`, porque evita
fullscreen exclusivo e reduz risco de problemas de driver/SDL/MonoGame.

Tambem foi adicionado `DisplayWakeLock` no Windows para evitar que o sistema
apague a tela durante o jogo.

### 7. Build/export Windows mais confiavel

O comando principal para curso:

```bash
aegis build meu-jogo --target win-x64
```

A build agora:

- publica o runtime;
- copia arquivos do jogo;
- ignora logs antigos;
- gera `JOGAR.bat`;
- gera `aegis-build.json`;
- valida se os arquivos essenciais existem.

Exemplo gerado:

```text
dist/aegis-demo-platformer-win-x64/
```

## Recursos bons para ensinar agora

### Aula 1 - Estrutura de projeto

Arquivos principais:

- `main.lua`
- `aegis.toml`
- `aegis.cfg`
- `res/`
- `scenes/`

Callbacks principais:

```lua
function aegis_init()
end

function aegis_update(dt)
end

function aegis_draw()
end

function aegis_draw_ui()
end
```

### Aula 2 - Sprites e componentes

Use:

```lua
local player = aegis.create("sprite", {
    path = "sprites/player.png",
    x = 120,
    y = 240
})
```

Ensine:

- posicao;
- escala;
- rotacao;
- z-order;
- destruir objeto.

### Aula 3 - Texto e HUD

Use:

```lua
local score = aegis.create("label", {
    text = "Score 0",
    size = 24,
    hud = true,
    x = 20,
    y = 20
})
```

Ensine:

- fonte padrao;
- tamanho real;
- camada HUD;
- `aegis_draw_ui`.

### Aula 4 - Input

Use:

```lua
function aegis_update(dt)
    if aegis.keyDown("Right") then
        aegis.move(player, 160 * dt, 0)
    end
end
```

Ensine:

- `keyDown`;
- `keyPressed`;
- mouse;
- gamepad.

### Aula 5 - Cena e transicao

Use:

```lua
aegis.registerScene("menu", "scenes/menu.lua")
aegis.registerScene("fase1", "scenes/level.lua")
aegis.transitionTo("menu", "none")
```

Ensine:

- registrar cenas;
- trocar cenas;
- limpar mundo com `aegis.clearAll`;
- evitar chamar transicao varias vezes seguidas.

### Aula 6 - Fisica basica

Use:

```lua
local col = aegis.addCollider(player, 24, 32)
local rb = aegis.addRigidbody(player)
aegis.setVelocityX(rb, 120)
```

Ensine:

- collider;
- rigidbody;
- gravidade;
- trigger;
- colisao enter/exit.

### Aula 7 - Audio

Use:

```lua
aegis.playSound("jump.wav")
aegis.playMusic("music.wav", true)
```

Ensine:

- som curto;
- musica;
- volume por grupo;
- audio posicional simples com `playSoundAt`.

### Aula 8 - Export

Use:

```bash
aegis build demo-platformer --target win-x64
```

Explique:

- pasta `dist`;
- `JOGAR.bat`;
- `aegis-build.json`;
- como testar em outra maquina Windows.

## Recursos que ainda devem ser tratados como experimentais

Evite ensinar como base do curso principal:

- drag/drop;
- hand layout;
- upgrade system;
- autozoom;
- audio 3D;
- editor pipe;
- slope/one-way platform, se ainda nao estiver bem testado.

Eles podem aparecer em aula bonus, mas devem ser explicados como APIs que ainda
podem mudar.

## Estado tecnico atual

### Ja esta bom para MVP interno

- Criar jogos 2D simples.
- Cenas e transicoes.
- Sprites, labels, HUD.
- Input teclado/mouse/gamepad.
- Audio basico.
- Fisica simples.
- Save/config.
- Export Windows.

### Ainda precisa melhorar para lançamento publico

- quebrar `LuaRuntime.cs` em modulos menores;
- criar testes automatizados;
- limpar arquivos antigos e duplicados;
- organizar exemplos em `samples/` ou `examples/`;
- validar `borderless` em teste longo;
- remover comentarios internos de sprint/bug antes de release;
- revisar codigo antigo em `src/Scripting` e `src/Core`.

## Sugestao de roteiro de curso

1. Visao geral da engine.
2. Criando o primeiro projeto.
3. Desenhando sprites e textos.
4. Movimento com input.
5. Colisao e fisica simples.
6. HUD e UI.
7. Cenas: menu, jogo, game over.
8. Audio e efeitos simples.
9. Salvando configuracoes.
10. Exportando para Windows.
11. Projeto final: mini platformer ou card battle.
12. Aula bonus: APIs experimentais.

## Regras para IA gerar jogos nesta versao

Quando uma IA for criar jogos para Aegis Engine:

- preferir `aegis.create`;
- usar `hud = true` para HUD;
- usar `aegis_draw_ui` para overlays;
- evitar APIs experimentais em jogos de exemplo;
- usar `displayMode = "windowed"` ou `"borderless"`;
- carregar assets dentro de `res/`;
- criar cenas dentro de `scenes/`;
- evitar depender de fullscreen exclusivo;
- usar fonte em tamanho real, nao label escalada;
- testar build com `aegis build <jogo> --target win-x64`.

## Resumo final

A engine ja esta em um ponto bom para criar curso pratico de jogos 2D simples.
O curso deve ensinar o caminho estavel do MVP e deixar claro que alguns sistemas
ainda sao experimentais. Para publicacao ampla, o foco agora deve ser limpeza de
arquitetura, testes automatizados e organizacao dos exemplos.
