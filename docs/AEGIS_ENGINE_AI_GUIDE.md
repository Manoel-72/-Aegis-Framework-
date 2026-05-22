# Aegis Engine AI Guide

Documento para orientar IAs e agentes de codigo a entender, modificar e criar jogos na Aegis Engine.

Versao observada do projeto: Aegis Engine v0.9.9 em desenvolvimento.

## 1. O que e a Aegis Engine

Aegis Engine e uma engine/framework 2D em C#/.NET 8 usando MonoGame DesktopGL como backend grafico e NLua para scripting Lua.

O objetivo pratico e permitir que jogos sejam escritos em Lua usando uma API global chamada `aegis.*`, enquanto o runtime, renderizacao, audio, input, fisica, cena, recursos e build ficam implementados em C#.

## 2. Estrutura do repositorio

Principais pastas:

- `src/Aegis/`: nucleo da engine.
- `src/Aegis.CLI/`: ferramenta de linha de comando `aegis`.
- `AegisEditor/`: editor visual desktop em Avalonia.
- `templates/`: modelos de jogos (`platformer`, `topdown`, `puzzle`).
- `demo-platformer/`: demo de plataforma.
- `zone-recon/`: jogo top-down shooter de exemplo.
- `physics-lab/`: laboratorio/demo de fisica.
- `card-battle/`: prototipo de jogo de cartas criado como projeto separado.
- `docs/`: documentacao.

Arquivos importantes:

- `Aegis.sln`: solucao principal.
- `src/Aegis/Core/AegisGame.cs`: loop principal MonoGame.
- `src/Aegis/Scripting/LuaRuntime.cs`: registra a API `aegis.*`.
- `src/Aegis.CLI/Program.cs`: CLI para run, new, build, publish, doctor, update.

## 3. Padrao de projeto de jogo

Cada jogo deve viver em sua propria pasta. Nao misture um jogo novo dentro de outro jogo existente.

Estrutura recomendada:

```txt
meu-jogo/
  main.lua
  aegis.toml
  aegis.cfg
  README.md
  scenes/
    menu.lua
    game.lua
    win.lua
    gameover.lua
  res/
    sprites/
    audio/
    fonts/
    tilemaps/
  saves/
    save.json
```

Exemplo de `aegis.toml`:

```toml
title  = "Meu Jogo"
width  = 1280
height = 720
```

## 4. Como rodar

Via wrapper local:

```powershell
.\aegis.cmd run card-battle
```

Via dotnet direto:

```powershell
dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- run card-battle
```

Comandos principais da CLI:

```bash
aegis run <pasta-do-jogo>
aegis new <nome>
aegis new platformer|topdown|puzzle <nome>
aegis build <pasta-do-jogo> --target win-x64|linux-x64|osx-x64|osx-arm64|web
aegis publish <pasta-do-jogo> --itch usuario/jogo --target win-x64
aegis doctor
aegis update
```

## 5. Ciclo de vida Lua

Todo jogo Lua usa funcoes globais especiais:

```lua
function aegis_init()
    -- chamado quando a cena/script e carregado
end

function aegis_update(dt)
    -- chamado a cada frame; dt em segundos
end

function aegis_draw()
    -- desenho manual opcional em coordenadas de mundo/camera
end

function aegis_draw_ui()
    -- desenho manual opcional sem transformacao de camera
end
```

Um `main.lua` tipico registra cenas e abre o menu:

```lua
GAME = { score = 0 }
local booted = false

function aegis_init()
    aegis.registerScene("menu", "scenes/menu.lua")
    aegis.registerScene("game", "scenes/game.lua")
    booted = false
end

function aegis_update(dt)
    if not booted then
        booted = true
        aegis.transitionTo("menu", "none", 0.01)
    end
end

function aegis_draw() end
```

## 6. Cenas

Use `aegis.registerScene(nome, caminho)` para registrar uma cena e `aegis.transitionTo(nome, efeito, duracao)` para trocar.

Exemplo:

```lua
aegis.registerScene("win", "scenes/win.lua")
aegis.transitionTo("win", "fade", 0.35)
```

Recomendacao para IA:

- Use cenas separadas para menu, gameplay, vitoria e derrota.
- Chame `aegis.clearAll()` no `aegis_init` de cada cena para limpar objetos anteriores.
- Chame `aegis.setCameraOff()` em menus/HUDs fixos quando existir.

## 7. Texto e fontes

Importante: `aegis.newLabel()` so desenha se houver uma fonte carregada. A fonte padrao pode estar nula ate `aegis.loadFont()` ser usado.

Padrao seguro:

```lua
local ui_font = nil

local function load_ui_font()
    if not (aegis.loadFont and aegis.setFont) then return end
    local candidates = {
        "Inter-Regular.ttf",
        "Roboto-Regular.ttf",
        "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arial.ttf"
    }
    for _, path in ipairs(candidates) do
        local ok, font = pcall(aegis.loadFont, path, 20)
        if ok and font then ui_font = font; return end
    end
end

local function label(text, x, y)
    local l = aegis.newLabel(text)
    if ui_font then pcall(aegis.setFont, l, ui_font) end
    aegis.setPosition(l, x, y)
    return l
end
```

Arquivos TTF devem ficar em `res/fonts/`. O `FontManager` usa raiz `res/fonts`.

## 8. Assets

Raizes comuns:

- Sprites e imagens: `res/`
- Audio: `res/audio/`
- Fontes: `res/fonts/`
- Tilemaps: normalmente em `res/tilemaps/`

`aegis.newSprite("sprites/player.png")` carrega `res/sprites/player.png`.

`aegis.playSound("click.wav")` carrega `res/audio/click.wav`.

`aegis.loadFont("Inter-Regular.ttf", 20)` carrega `res/fonts/Inter-Regular.ttf`.

## 9. API Lua principal

### Core e objetos

- `aegis.newSprite(path)`
- `aegis.newRect(w, h, r, g, b)`
- `aegis.removeObject(obj)`
- `aegis.clearAll()`
- `aegis.worldClear()`
- `aegis.log(text)`

### Transform

- `aegis.setPosition(obj, x, y)`
- `aegis.setPositionNorm(obj, nx, ny)`
- `aegis.move(obj, dx, dy)`
- `aegis.setScale(obj, sx, sy)`
- `aegis.setRotation(obj, radians)`
- `aegis.setAlpha(obj, alpha)`
- `aegis.setVisible(obj, bool)`
- `aegis.setPivot(bitmap, px, py)`
- `aegis.setZ(obj, z)`
- `aegis.getX(obj)`, `aegis.getY(obj)`
- `aegis.getWidth(bitmap)`, `aegis.getHeight(bitmap)`

### Labels e texto

- `aegis.newLabel(text)`
- `aegis.setText(label, text)`
- `aegis.setColor(label, r, g, b, a?)`
- `aegis.loadFont(file, size)`
- `aegis.setFont(label, font)`
- `aegis.newRichLabel(markup)`
- `aegis.setMarkup(richLabel, markup)`

Observacao: `setColor` e para `Label`. Nao use em retangulos criados com `newRect`.

### Input

- `aegis.keyDown(key)`
- `aegis.keyPressed(key)`
- `aegis.mouseX()`, `aegis.mouseY()`
- `aegis.mouseLeft()`, `aegis.mouseLeftJust()`
- `aegis.mouseRight()`, `aegis.mouseRightJust()`
- `aegis.mouseScroll()`
- `aegis.padConnected(index)`
- `aegis.padDown(index, button)`
- `aegis.padPressed(index, button)`
- `aegis.padAxis(index, axis)`
- `aegis.padVibrate(index, left, right, seconds)`

### Tela e desenho manual

- `aegis.screenWidth()`, `aegis.screenHeight()`
- `aegis.drawText(text, x, y, r, g, b)`
- `aegis.drawRect(x, y, w, h, r, g, b)`
- `aegis.drawLine(x1, y1, x2, y2, r, g, b)`
- `aegis.drawCircle(x, y, radius, r, g, b)`

### Camera

- `aegis.setCameraTarget(obj, speed?)`
- `aegis.setCameraOff()`
- `aegis.setCameraZoom(zoom)`
- `aegis.setCameraOffset(x, y)`
- `aegis.setCameraLimits(left, top, right, bottom)`
- `aegis.setCameraDeadzone(w, h)`
- `aegis.setCameraLookahead(distance, speed?)`
- `aegis.cameraX()`, `aegis.cameraY()`
- `aegis.screenToWorldX(x, y)`
- `aegis.screenToWorldY(x, y)`
- `aegis.setCameraAutozoom(enabled, opts)`

### Audio

- `aegis.playSound(file)`
- `aegis.playSoundEx(file, volume, pitch, pan)`
- `aegis.playSoundAt(file, x, y, opts?)`
- `aegis.playMusic(file, loop?)`
- `aegis.stopMusic()`
- `aegis.pauseMusic()`
- `aegis.resumeMusic()`
- `aegis.setSfxVolume(value)`
- `aegis.setMusicVolume(value)`
- `aegis.musicPlaying()`
- `aegis.setGroupVolume(group, volume)`
- `aegis.crossfadeTo(file, seconds?)`

### Fisica e colisao

Regra simples:

- Chao, parede e plataforma parada: `Collider` apenas.
- Player, inimigo e caixa dinamica: `Rigidbody + Collider`.
- Item coletavel: `Collider + Trigger`.

APIs:

- `aegis.addCollider(obj, w, h, offX?, offY?)`
- `aegis.addCircleCollider(obj, radius, offX?, offY?)`
- `aegis.addSlopeCollider(obj, w, h, dir, offX?, offY?)`
- `aegis.setSlopeDir(collider, "left"|"right")`
- `aegis.removeCollider(collider)`
- `aegis.setColliderLayer(collider, layer)`
- `aegis.setColliderMask(collider, mask)`
- `aegis.setTrigger(collider, bool)`
- `aegis.setOneWay(collider, bool)`
- `aegis.setColliderOffset(collider, x, y)`
- `aegis.onCollide(collider, callback)`
- `aegis.onCollideEnter(collider, callback)`
- `aegis.onCollideExit(collider, callback)`
- `aegis.getColliderOf(obj)`

Rigidbody:

- `aegis.addRigidbody(obj)`
- `aegis.removeRigidbody(obj)`
- `aegis.setVelocity(rb, vx, vy)`
- `aegis.setVelocityX(rb, vx)`
- `aegis.addImpulseY(rb, vy)`
- `aegis.setVel(rb, vx, vy)`
- `aegis.setVelX(rb, vx)`
- `aegis.jumpY(rb, vy)`
- `aegis.addVelocity(rb, dvx, dvy)`
- `aegis.getVelocityX(rb)`, `aegis.getVelocityY(rb)`
- `aegis.setGravity(rb, scale)`
- `aegis.setGroundFriction(rb, value)`
- `aegis.setGlobalGravity(value)`
- `aegis.setKinematic(rb, bool)`
- `aegis.isGrounded(rb)`
- `aegis.isTouchingWall(rb)`
- `aegis.wallSide(rb)`

### Sprites, animacao e atlas

- `aegis.setFrame(sprite, x, y, w, h)`
- `aegis.clearFrame(sprite)`
- `aegis.loadAtlas(file)`
- `aegis.setAtlasFrame(sprite, atlas, frameName)`
- `aegis.newAnim(path, frameW, frameH)`
- `aegis.playAnim(anim, start, end, loop, fps)`
- `aegis.stopAnim(anim)`
- `aegis.resumeAnim(anim)`
- `aegis.animFrame(anim)`
- `aegis.animPlaying(anim)`
- `aegis.newAnimator(sprite, frameW, frameH)`
- `aegis.addClip(animator, name, frames, fps, loop?)`
- `aegis.play(animator, clipName, restart?)`
- `aegis.stopAnimator(animator)`
- `aegis.currentClip(animator)`
- `aegis.newAtlasAnimator(sprite, atlas)`
- `aegis.addAtlasClip(animator, name, frameNames, fps, loop?)`

### Tilemap, mundo e pathfinding

- `aegis.loadTilemap(file)`
- `aegis.generateTilemap(tileset, cols, rows, tileW, tileH, seed, scale)`
- `aegis.setTile(tilemap, layer, x, y, gid)`
- `aegis.getTile(tilemap, layer, x, y)`
- `aegis.setTileCulling(tilemap, enabled, margin?)`
- `aegis.buildTilemapColliders(tilemap, layer?)`
- `aegis.clearTilemapColliders(tilemap)`
- `aegis.tilemapColliderCount(tilemap)`
- `aegis.newNavGrid(cols, rows, cellSize)`
- `aegis.navFromTilemap(tilemap, layer?)`
- `aegis.navFindPath(nav, sx, sy, tx, ty)`
- `aegis.navSetSolid(nav, x, y, bool)`
- `aegis.navIsSolid(nav, x, y)`
- `aegis.perlin(x, y, seed, octaves, scale)`

### Triggers e areas

- `aegis.newAreaTrigger(name, x, y, w, h)`
- `aegis.onTriggerEnter(trigger, callback)`
- `aegis.onTriggerStay(trigger, callback)`
- `aegis.onTriggerExit(trigger, callback)`
- `aegis.checkTrigger(trigger, obj)`
- `aegis.clearTriggers()`

### Save, config e debug

- `aegis.save(key, value)`
- `aegis.load(key)`
- `aegis.loadConfig()`
- `aegis.setFullscreen(bool)`
- `aegis.setResolution(width, height)`
- `aegis.setHotReload(bool)`

Arquivos:

- `saves/save.json`: persistencia local.
- `aegis.cfg`: janela/fullscreen/vsync.

### Efeitos, tween e particulas

- `aegis.burst(x, y, opts)`
- `aegis.newEmitter(x, y, opts)`
- `aegis.stopEmitter(emitter)`
- `aegis.tween(obj, props, seconds, easing, callback?, opts?)`
- `aegis.newSequence()`
- `aegis.seqAdd(sequence, obj, props, seconds, easing?)`
- `aegis.seqWait(sequence, seconds)`
- `aegis.seqPlay(sequence)`
- `aegis.seqStop(sequence)`
- `aegis.fadeIn(seconds)`
- `aegis.fadeOut(seconds)`
- `aegis.flashScreen(color, seconds)`
- `aegis.screenShake(intensity, duration)`
- `aegis.setShader(obj, name, opts?)`
- `aegis.clearShader(obj)`
- `aegis.setScreenShader(name, opts?)`
- `aegis.clearScreenShader()`

### UI e interacao

- `aegis.newPanel(...)`
- `aegis.setPanelSize(panel, w, h)`
- `aegis.newFlow(...)`
- `aegis.flowAdd(flow, obj)`
- `aegis.flowLayout(flow)`
- `aegis.flowSet(flow, opts)`
- `aegis.newButton(obj, onClick)`
- `aegis.onHover(obj, callback)`
- `aegis.onPress(obj, callback)`
- `aegis.floatText(x, y, text, opts)`
- `aegis.newProgressBar(x, y, w, h)`
- `aegis.setBarValue(bar, current, max)`
- `aegis.setBarColors(bar, opts)`
- `aegis.newDraggable(obj)`
- `aegis.onDragStart(obj, callback)`
- `aegis.onDragMove(obj, callback)`
- `aegis.onDragEnd(obj, callback)`
- `aegis.getDragTarget()`
- `aegis.bringToFront(obj)`
- `aegis.sendToBack(obj)`
- `aegis.setZRelative(obj, delta)`
- `aegis.newHand(cx, cy, opts)`
- `aegis.handAdd(hand, obj)`
- `aegis.handRemove(hand, obj)`
- `aegis.handLayout(hand)`
- `aegis.handSetHover(hand, index)`

### Gameplay helpers

- `aegis.getTime()`
- `aegis.lookAt(obj, tx, ty)`
- `aegis.overlapCircle(x, y, radius, mask?)`
- `aegis.overlapRect(x, y, w, h, mask?)`
- `aegis.raycast(ox, oy, dx, dy, length)`
- `aegis.raycastMask(ox, oy, dx, dy, length, mask)`
- `aegis.lineOfSight(ax, ay, bx, by, mask?)`
- `aegis.newPool(spritePath, initialSize)`
- `aegis.poolGet(pool, x, y)`
- `aegis.poolReturn(pool, obj)`
- `aegis.poolClear(pool)`
- `aegis.poolCount(pool)`

### Upgrade system

- `aegis.addUpgrade(id, title, desc, opts)`
- `aegis.onUpgradeChosen(callback)`
- `aegis.getUpgradeLevel(id)`
- `aegis.showUpgrades(count)`
- `aegis.hideUpgrades()`

## 10. Boas praticas para IAs

1. Crie jogos novos em pastas novas. Nao altere `zone-recon`, `demo-platformer` ou `physics-lab` para prototipos novos, a menos que o usuario peca explicitamente.
2. Use `aegis.clearAll()` no inicio de cada cena.
3. Carregue fonte antes de criar labels.
4. Use `pcall` para APIs opcionais quando a compatibilidade importar.
5. Nao use `setColor` em retangulos; recrie o retangulo ou mude labels/overlays.
6. Use `aegis.playSoundEx` para volume/pitch/pan; `playSound` aceita apenas arquivo.
7. Salve progresso com `aegis.save` em valores simples.
8. Em UI, mantenha hitboxes manuais se `newButton` nao for necessario.
9. Em jogos com camera, use `screenToWorldX/Y` para converter mouse.
10. Para menus, desligue a camera com `aegis.setCameraOff()`.

## 11. Exemplo minimo de menu

```lua
local title

local function load_font()
    local ok, font = pcall(aegis.loadFont, "Inter-Regular.ttf", 24)
    return ok and font or nil
end

function aegis_init()
    aegis.clearAll()
    if aegis.setCameraOff then aegis.setCameraOff() end
    local font = load_font()
    local bg = aegis.newRect(1280, 720, 0.04, 0.05, 0.08)
    aegis.setPosition(bg, 0, 0)
    title = aegis.newLabel("MEU JOGO")
    if font then aegis.setFont(title, font) end
    aegis.setPosition(title, 100, 100)
    aegis.setColor(title, 1.0, 0.8, 0.3)
end

function aegis_update(dt)
    if aegis.keyPressed("Enter") then
        aegis.transitionTo("game", "fade", 0.35)
    end
end
```

## 12. Exemplo minimo de player com fisica

```lua
local player, rb

function aegis_init()
    aegis.clearAll()
    player = aegis.newRect(32, 48, 0.2, 0.8, 0.4)
    aegis.setPosition(player, 100, 100)
    rb = aegis.addRigidbody(player)
    local col = aegis.addCollider(player, 32, 48, 0, 0)
    aegis.setColliderLayer(col, "PLAYER")
    aegis.setColliderMask(col, "WORLD")
end

function aegis_update(dt)
    local vx = 0
    if aegis.keyDown("A") then vx = vx - 180 end
    if aegis.keyDown("D") then vx = vx + 180 end
    aegis.setVelX(rb, vx)
    if aegis.keyPressed("Space") and aegis.isGrounded(rb) then
        aegis.jumpY(rb, -520)
    end
end
```

## 13. Estado atual e alertas

O projeto esta em desenvolvimento. Algumas areas possuem APIs registradas e recursos experimentais. Se a build falhar em arquivos da engine, verifique primeiro `src/Aegis/Scripting/LuaRuntime.cs`, pois ele concentra muitas APIs e pode conter trechos em evolucao.

Quando gerar um jogo, prefira depender das APIs ja usadas pelas demos existentes.
