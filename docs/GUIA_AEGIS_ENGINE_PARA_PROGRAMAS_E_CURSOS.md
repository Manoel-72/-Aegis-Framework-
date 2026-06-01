# Guia Aegis Engine Para Programas, Jogos E Cursos

Versao de referencia: Aegis Engine 0.9.9 preview.

Este documento explica a Aegis Engine de forma pratica para tres usos:

- entender a arquitetura da engine;
- construir jogos/programas 2D com Lua;
- criar cursos, aulas e materiais didaticos sobre a engine.

Ele deve ser usado junto com:

```text
docs/MVP_API.md
docs/STABLE_LUA_API.md
docs/ROADMAP_LANCAMENTO_AEGIS_ENGINE.md
docs/RELEASE_CHECKLIST_0.9.9.md
docs/GUIA_CURSO_AEGIS_ENGINE_VERSAO_ATUAL.md
```

## 1. O Que E A Aegis Engine

Aegis Engine e uma engine/framework 2D feita em C#/.NET 8, usando MonoGame
DesktopGL como backend grafico e Lua como linguagem de scripting.

O objetivo e permitir que jogos 2D sejam criados com uma API simples:

```lua
aegis.create(...)
aegis.destroy(...)
aegis.registerScene(...)
aegis.transitionTo(...)
aegis.playSound(...)
aegis.addCollider(...)
```

A engine tambem inclui:

- CLI para criar, rodar, validar e exportar jogos;
- editor visual em Avalonia;
- sistema de cenas;
- sprites;
- labels/fontes;
- audio;
- input;
- fisica simples;
- tilemaps;
- save/config;
- build Windows;
- testes automatizados;
- scripts de release.

## 2. Filosofia Da Engine

Aegis deve ser simples para iniciantes e organizada para crescer.

Principios:

- API pequena e estavel primeiro;
- recursos experimentais separados;
- compatibilidade com jogos existentes;
- exemplos funcionando sem erro;
- build/export confiavel;
- editor como camada amigavel, nao substituto da CLI;
- codigo C# limpo, modular e testavel;
- foco em 2D antes de qualquer 3D.

Regra importante:

```text
O framework continua sendo a base. O editor usa o framework, nao substitui ele.
```

## 3. Backend Oficial

Backend atual:

```text
MonoGame DesktopGL
```

Raylib:

```text
legacy/raylib-v0
```

Raylib esta guardado apenas como codigo historico. O runtime MVP atual usa
MonoGame.

## 4. Estrutura Do Projeto

Pastas principais:

```text
src/Aegis/                 runtime/framework principal
src/Aegis.CLI/             ferramenta de linha de comando
AegisEditor/               editor visual Avalonia
AegisEditor.Shared/        mensagens/modelos compartilhados do editor
templates/                 modelos para novos jogos
examples/                  jogos e demos oficiais
docs/                      documentacao
tests/Aegis.Tests/         testes automatizados
scripts/                   verify/package release
legacy/raylib-v0/          codigo antigo Raylib
dist/                      builds e pacotes gerados
archive/                   artefatos historicos locais
```

Exemplos oficiais:

```text
examples/demo-platformer
examples/card-battle
examples/physics-lab
examples/hyper-casual
examples/top-down-shooter
examples/zone-recon
```

## 5. Como Rodar

Na raiz do pacote:

```powershell
.\aegis.cmd run examples/demo-platformer
```

Rodar outro jogo:

```powershell
.\aegis.cmd run examples/card-battle
```

Criar projeto:

```powershell
.\aegis.cmd new meu-jogo
```

Build Windows:

```powershell
.\aegis.cmd build meu-jogo --target win-x64
```

Validar projeto:

```powershell
.\aegis.cmd doctor examples/demo-platformer
```

Abrir editor:

```powershell
dotnet run --project AegisEditor\src\AegisEditor\AegisEditor.csproj
```

## 6. Estrutura De Um Jogo

Projeto recomendado:

```text
meu-jogo/
  main.lua
  aegis.toml
  aegis.cfg
  scenes/
    menu.lua
    game.lua
    gameover.lua
  res/
    sprites/
    audio/
    fonts/
    tilemaps/
```

`main.lua` deve ser simples e registrar cenas:

```lua
function aegis_init()
    aegis.registerScene("menu", "scenes/menu.lua")
    aegis.registerScene("game", "scenes/game.lua")
    aegis.transitionTo("menu", "none")
end
```

## 7. Ciclo De Vida Lua

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

Uso:

- `aegis_init`: criar objetos, carregar cena, iniciar estado.
- `aegis_update(dt)`: atualizar logica do jogo.
- `aegis_draw`: desenho de mundo, quando necessario.
- `aegis_draw_ui`: UI/HUD fixa na tela.

## 8. API Recomendada Para Objetos

Use `aegis.create` para codigo novo:

```lua
local player = aegis.create("sprite", {
    path = "sprites/player.png",
    x = 100,
    y = 200,
    z = 10
})
```

Criar texto:

```lua
local label = aegis.create("label", {
    text = "Vida 3",
    size = 24,
    x = 20,
    y = 20,
    hud = true
})
```

Destruir:

```lua
aegis.destroy(player)
```

APIs antigas como `newSprite` e `newLabel` continuam por compatibilidade, mas
em cursos novos ensine `aegis.create`.

## 9. ComponentFactory

Criacao de componentes fica isolada em:

```text
src/Aegis/Scripting/Components/ComponentFactory.cs
```

Ela evita que o `LuaRuntime` vire uma classe gigante com toda regra de criacao.

Componentes principais:

```text
sprite
rect
label
richLabel
panel
flow
progressBar
anim
animatedSprite
group
```

## 10. LuaRuntime Modular

O runtime Lua foi dividido em arquivos por area:

```text
LuaRuntime.CoreApi.cs
LuaRuntime.DisplayApi.cs
LuaRuntime.InputApi.cs
LuaRuntime.AudioApi.cs
LuaRuntime.PhysicsApi.cs
LuaRuntime.SceneApi.cs
LuaRuntime.ConfigApi.cs
LuaRuntime.EffectsApi.cs
LuaRuntime.GameplayApi.cs
LuaRuntime.ExperimentalApi.cs
LuaRuntime.ApiRegistration.cs
```

Isso deixa a engine mais facil de manter e evoluir.

## 11. Cenas

Registrar:

```lua
aegis.registerScene("menu", "scenes/menu.lua")
aegis.registerScene("level1", "scenes/level.lua")
```

Trocar:

```lua
aegis.transitionTo("level1", "fade", 0.35)
```

Boa pratica em cada cena:

```lua
function aegis_init()
    aegis.clearAll()
end
```

## 12. Sprites E Animacao

Sprite simples:

```lua
local coin = aegis.create("sprite", {
    path = "sprites/coin.png",
    x = 300,
    y = 160
})
```

Animação pode usar spritesheet/atlas, especialmente JSON exportado do Aseprite.

Para curso iniciante, comece com sprite simples antes de atlas.

## 13. Texto, Fontes E HUD

A engine tem fallback automatico de fonte.

Recomendado:

```lua
local hud = aegis.create("label", {
    text = "Score 0",
    size = 24,
    hud = true,
    x = 20,
    y = 20
})
```

Evite aumentar texto com escala gigante. Use `size`.

## 14. Input

Exemplo:

```lua
function aegis_update(dt)
    if aegis.keyDown("Right") then
        aegis.move(player, 160 * dt, 0)
    end
end
```

Ensinar:

- `keyDown`;
- `keyPressed`;
- mouse;
- gamepad quando necessario.

## 15. Audio

Som:

```lua
aegis.playSound("jump.wav")
```

Musica:

```lua
aegis.playMusic("music.wav", true)
```

Observacao:

```text
Arquivos de audio dos exemplos devem ser validos para evitar warnings no log.
```

## 16. Fisica E Colisao

Exemplo:

```lua
local col = aegis.addCollider(player, 24, 32)
local rb = aegis.addRigidbody(player)
aegis.setVelocityX(rb, 120)
```

Conceitos:

- collider;
- rigidbody;
- trigger;
- layer/mask;
- colisao enter/exit;
- gravidade.

Para curso base, mantenha AABB simples.

## 17. Tilemaps E Mapas

A engine ja suporta tilemaps Tiled JSON no fluxo atual.

Exemplo:

```lua
local map = aegis.loadTilemap("tilemaps/level1.json")
aegis.buildTilemapColliders(map, {
    solidGids = {1, 2, 3},
    merge = true,
    layer = "WORLD"
})
```

Estado atual:

- tilemap funciona;
- culling por camera existe;
- colliders por tilemap existem;
- nav grid/pathfinding existe em exemplos;
- object layers e map pipeline completo ainda estao no roadmap.

Meta futura:

```lua
local map = aegis.loadMap("tilemaps/fase1.json", {
    collision = true,
    nav = true
})
```

## 18. AssetValidator E Doctor

Existe base de validação de assets:

```text
src/Aegis/Resource/AssetValidator.cs
```

O objetivo e permitir:

```powershell
.\aegis.cmd doctor examples/demo-platformer
```

Valida:

- `main.lua`;
- `aegis.toml`;
- `aegis.cfg`;
- pasta `res`;
- `.png`;
- `.wav`;
- `.json`;
- `.ttf/.otf`;
- referencias ausentes em Lua.

Isso e a base para um asset pipeline mais profissional.

## 19. Editor

O editor existe em:

```text
AegisEditor/
```

Estado atual:

- editor Avalonia;
- Hub inicial para iniciantes;
- Abrir Projeto;
- Abrir Exemplo;
- Documentacao;
- Projetos recentes;
- workspace tecnico ainda preservado;
- Run + Connect via runtime/pipe.

Meta do editor:

```text
Ser uma camada amigavel em cima do framework.
```

O editor nao deve substituir a CLI. Ele deve facilitar:

- abrir projeto;
- criar projeto;
- rodar;
- validar;
- ver logs;
- fazer build;
- visualizar mapa.

## 20. Build E Release

Validar:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify.ps1 -Configuration Release -SkipZip
```

Gerar pacote:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1 -Version 0.9.9 -Configuration Release
```

Checklist:

```text
docs/RELEASE_CHECKLIST_0.9.9.md
```

CI:

```text
.github/workflows/ci-release.yml
```

## 21. Como Criar Curso Sobre Aegis

Roteiro recomendado:

1. O que e a Aegis Engine.
2. Instalacao e CLI.
3. Criar primeiro projeto.
4. Sprites e labels.
5. Input e movimento.
6. Cenas e transicoes.
7. Colisao e fisica.
8. Audio.
9. HUD e UI.
10. Tilemap basico.
11. Save/config.
12. Build Windows.
13. Projeto final.
14. Bonus: editor, assets e mapa.

Cada aula deve ter:

- objetivo;
- explicacao curta;
- codigo pequeno;
- exercicio;
- checklist de teste.

## 22. Regras Para IA Criar Conteudo

Uma IA deve:

- usar a versao 0.9.9 como referencia;
- usar `.\aegis.cmd`;
- preferir `aegis.create`;
- evitar APIs experimentais no curso principal;
- usar exemplos em `examples/`;
- consultar `docs/MVP_API.md`;
- nao inventar APIs;
- separar conteudo iniciante de conteudo avancado;
- sempre finalizar com um jogo rodando;
- incluir comandos sem copiar cercas Markdown.

Prompt base:

```text
Crie uma aula pratica da Aegis Engine 0.9.9 usando Lua.
Use apenas a API Stable/MVP, preferindo aegis.create.
Inclua objetivo, explicacao, codigo, exercicio e checklist de teste.
Nao invente APIs que nao existem.
```

## 23. O Que Ainda Falta Para A Engine Ficar Mais Forte

Prioridades atuais:

1. Confirmar exemplos sem warnings.
2. Melhorar `doctor <jogo>`.
3. Criar asset pipeline mais completo.
4. Criar map pipeline 2D.
5. Ler object layers do Tiled.
6. Criar `aegis.spawnMapObjects`.
7. Melhorar debug visual.
8. Evoluir editor para Run/Build/Doctor visual.
9. Criar preview de mapa.
10. Testar release em maquina limpa.

## 24. Resumo

A Aegis 0.9.9 ja tem uma base boa para jogos 2D simples, cursos e prototipos.
O caminho profissional agora e fortalecer:

```text
assets, mapas, doctor, editor, debug, docs e release.
```

Para alunos e iniciantes, use o editor e exemplos.

Para programadores e IAs, use a CLI, `aegis.create`, `docs/MVP_API.md` e os
templates.

Para lancamento publico, siga o roadmap e o checklist de release.
