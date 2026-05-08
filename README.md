# Aegis Engine v0.6

Engine/framework 2D em C# + MonoGame com scripting Lua.

## Rodar um jogo

```powershell
dotnet build
dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- run physics-lab
```

## Regras de física simples

- Chão, parede e plataforma parada: `Collider` somente.
- Player, inimigo e caixa dinâmica: `Rigidbody + Collider`.
- Item coletável: `Collider + Trigger`.

## v0.5 — Sprites & Assets

```lua
local sprite = aegis.newSprite("player.png")
aegis.setPivot(sprite, 0.5, 1.0)
aegis.setFrame(sprite, 0, 0, 32, 32)
```

- `newSprite(path)` carrega PNG/JPG/JPEG da pasta `res/`.
- `ResManager` mantém cache de textura por caminho.
- `setFrame(node, x, y, w, h)` recorta spritesheet.
- `clearFrame(node)` volta para a textura inteira.

## v0.6 — Animator

```lua
local player = aegis.newSprite("player-sheet.png")
local anim = aegis.newAnimator(player, 32, 32)
aegis.addClip(anim, "idle", {0, 1, 2, 3}, 8)
aegis.addClip(anim, "run",  {4, 5, 6, 7}, 12)
aegis.play(anim, "idle")
```

- `newAnimator(node, frameW, frameH)` cria animator por grade.
- `addClip(anim, name, frames, fps)` registra clipe.
- `play(anim, name)` troca por nome de estado.

## Logo oficial

O logo oficial da Aegis Engine está em:

- `res/aegis-logo.svg`
- `res/aegis-logo.png`

Os exemplos já incluem o logo em `res/`.

## Ícone oficial da Aegis

A engine agora usa `aegis-logo.svg`/`aegis-logo.png` como logo oficial em dois níveis:

1. **Janela em runtime**: durante `LoadContent`, `WindowIcon.TrySet(...)` tenta aplicar `res/aegis-logo.png` no topo esquerdo da janela via SDL/MonoGame DesktopGL.
2. **Executável/pacote Windows**: `src/Aegis.CLI/Aegis.CLI.csproj` define `<ApplicationIcon>res\aegis-logo.ico</ApplicationIcon>`, usado quando o apphost/executável é gerado no Windows.

Para jogos criados com `aegis new`, o template copia automaticamente:

```txt
res/aegis-logo.png
res/aegis-logo.svg
res/aegis-logo.ico
```

Se um jogo quiser um ícone próprio depois, basta substituir esses arquivos mantendo os mesmos nomes.

# Aegis Engine v0.7 — Tilemaps, Tiled, Procedural e SceneManager

## APIs Lua novas

```lua
local map = aegis.loadTilemap("tiled-demo.json")
aegis.setTileCulling(map, true, 3)

aegis.setTile(map, 0, 10, 8, 3)
local gid = aegis.getTile(map, 0, 10, 8)

local proc = aegis.generateTilemap("tiles.png", 160, 100, 16, 16, 2026, 0.06)
local n = aegis.perlin(10, 20, 2026, 4, 0.08)

aegis.registerScene("fase2", "scenes/fase2.lua")
aegis.transitionTo("fase2", "fade", 0.5)

local portal = aegis.newAreaTrigger("portal", 100, 100, 64, 64)
aegis.onTriggerEnter(portal, function(trigger, obj)
    aegis.transitionTo("fase2", "fade")
end)
aegis.checkTrigger(portal, player)
```

## Regras importantes

- Mapas Tiled devem ficar dentro de `res/`.
- O JSON suportado é ortogonal com `tilelayer` e `data` em array/CSV.
- Tileset externo `.tsx` ainda não foi implementado; use tileset embutido no JSON do Tiled.
- Tilemap usa culling por câmera: bom para mapas grandes.
- Triggers de área são leves e manuais: chame `aegis.checkTrigger(trigger, player)` no update.

## Exemplo

```powershell
dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- run games/tile-world
```

# Aegis v0.8 — Save, Config, Effects, Debug

Novas APIs Lua:

```lua
aegis.save("coins", 10)
local coins = aegis.load("coins") or 0

aegis.setResolution(1280, 720)
aegis.setFullscreen(true)

aegis.burst(300, 220, { count = 32, speed = 220, life = 0.45, size = 5, r = 0.6, g = 1.0, b = 0.4 })
aegis.tween(obj, { x = 500, y = 200, alpha = 0.5 }, 0.5, "out")
aegis.screenShake(8, 0.25)
aegis.fadeIn(0.35)
aegis.fadeOut(0.35)
aegis.flashScreen({ r = 1, g = 1, b = 1 }, 0.12)
```

Debug:

- `F1`: liga/desliga overlay
- mostra FPS, contagem de objetos, rigidbodies e colliders
- mostra hitboxes quando overlay está ativo
- `F5`: força hot reload do `main.lua`
- hot reload automático ao salvar `main.lua`

CLI:

```bash
aegis doctor
aegis update
```

Arquivos automáticos:

- `aegis.cfg`: resolução/fullscreen/vsync
- `saves/save.json`: save local JSON

---

# Aegis v0.9 — Build, Publish e Bundling

## Build nativo

Dentro do repositório da engine, rode:

```powershell
dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- build physics-lab --target win-x64
```

Depois de instalar a CLI global:

```bash
aegis build physics-lab --target win-x64
aegis build physics-lab --target linux-x64
aegis build physics-lab --target osx
aegis build physics-lab --target osx-arm64
```

Saída:

```txt
dist/<nome-do-jogo>-win-x64/
dist/<nome-do-jogo>-win-x64.zip
```

O build copia automaticamente:

```txt
main.lua
aegis.toml
aegis.cfg quando existir
assets/
res/
maps/
qualquer arquivo útil do jogo
```

E ignora automaticamente:

```txt
bin/
obj/
dist/
.git/
.vs/
node_modules/
*.zip
*.nupkg
```

## Executável clicável

O executável exportado roda o jogo automaticamente quando encontra `main.lua` na mesma pasta.

## itch.io

Instale o butler oficial do itch.io e faça login:

```bash
butler login
```

Publicar:

```bash
aegis publish physics-lab --itch usuario/jogo --target win-x64
```

Canais usados:

```txt
win-x64   -> windows
linux-x64 -> linux
osx-x64   -> mac
osx-arm64 -> mac
web       -> html5
```

## Web / WASM

```bash
aegis build physics-lab --target web
```

A engine atual usa DesktopGL/MonoGame. Por isso, o comando `web` gera um pacote explicativo preparado para itch.io, mas ainda não executa o jogo em WASM real. Para HTML5 real, a Aegis precisa de um backend WebGL/WASM separado.

## Diagnóstico

```bash
aegis doctor
```

Verifica `.NET`, estrutura do jogo, `main.lua`, `res/`, `aegis.toml`, `aegis.cfg` e `butler`.
