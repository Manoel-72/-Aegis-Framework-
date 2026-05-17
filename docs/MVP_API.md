# Aegis Engine MVP API

Este documento congela a superficie recomendada para o MVP da Aegis Engine.
O objetivo e reduzir instabilidade, proteger jogos existentes e dar um contrato claro
para templates, exemplos e IA.

## Status de API

- `Stable/MVP`: pode ser usado em jogos e templates oficiais.
- `Legacy`: mantido para compatibilidade, mas nao deve aparecer em codigo novo.
- `Experimental`: pode mudar ou ser removido antes do MVP. A engine emite aviso no log quando usado.

## Stable/MVP

### Ciclo de vida

- `aegis_init`
- `aegis_update(dt)`
- `aegis_draw`
- `aegis_draw_ui`

### Cena

- `aegis.registerScene(name, file)`
- `aegis.transitionTo(name)`
- `aegis.clearAll`
- `aegis.worldClear`
- `aegis.destroy(obj)`

### Criacao de componentes

- `aegis.create(kind, opts)`

Tipos estaveis para `aegis.create`:

- `group`
- `sprite`
- `rect`
- `label`
- `richLabel`
- `panel`
- `flow`
- `progressBar`
- `anim`
- `animatedSprite`

### Transform

- `aegis.setPosition(obj, x, y)`
- `aegis.setPositionNorm(obj, nx, ny)`
- `aegis.move(obj, dx, dy)`
- `aegis.setScale(obj, sx, sy)`
- `aegis.setRotation(obj, radians)`
- `aegis.setAlpha(obj, alpha)`
- `aegis.setVisible(obj, visible)`
- `aegis.setPivot(bitmap, px, py)`
- `aegis.setZ(obj, z)`
- `aegis.getZ(obj)`
- `aegis.getX(obj)`
- `aegis.getY(obj)`
- `aegis.getWidth(bitmap)`
- `aegis.getHeight(bitmap)`
- `aegis.centerX(obj)`

### Texto e fontes

- Fonte padrao automatica: `aegis.create("label", ...)`, `aegis.newLabel` e `aegis.drawText`
  devem funcionar sem o jogo carregar fonte manualmente.
- A engine procura primeiro em `res/fonts` e depois nas fontes do sistema.
- Fonte recomendada para jogos: `res/fonts/Inter-Regular.ttf` ou `NotoSans-Regular.ttf`.
- Para texto grande, use fonte no tamanho real (`size` ou `newLabelSize`) em vez de escalar label.

- `aegis.setText(label, text)`
- `aegis.setColor(label, r, g, b, a)`
- `aegis.loadFont(path, size)`
- `aegis.loadDefaultFont(size)`
- `aegis.newLabelSize(text, size)`
- `aegis.newRichLabelSize(markup, size)`
- `aegis.setFont(label, font)`
- `aegis.setMarkup(richLabel, markup)`
- `aegis.setFontRich(richLabel, font)`
- `aegis.setPivotRich(richLabel, px, py)`

### Input

- `aegis.keyDown(key)`
- `aegis.keyPressed(key)`
- `aegis.mouseX`
- `aegis.mouseY`
- `aegis.mouseLeft`
- `aegis.mouseLeftJust`
- `aegis.mouseRight`
- `aegis.mouseRightJust`
- `aegis.mouseScroll`
- `aegis.padConnected`
- `aegis.padDown`
- `aegis.padPressed`
- `aegis.padAxis`
- `aegis.padVibrate`

### Tela e camera basica

- `aegis.screenWidth`
- `aegis.screenHeight`
- `aegis.setCameraTarget`
- `aegis.setCameraOff`
- `aegis.setCameraZoom`
- `aegis.setCameraOffset`
- `aegis.setCameraLimits`
- `aegis.cameraX`
- `aegis.cameraY`
- `aegis.screenToWorldX`
- `aegis.screenToWorldY`
- `aegis.screenShake`

### Audio

- `aegis.playSound`
- `aegis.playSoundEx`
- `aegis.playSoundAt`
- `aegis.playMusic`
- `aegis.playMusicLooped`
- `aegis.stopMusic`
- `aegis.pauseMusic`
- `aegis.resumeMusic`
- `aegis.setSfxVolume`
- `aegis.setMusicVolume`
- `aegis.setGroupVolume`
- `aegis.crossfadeTo`
- `aegis.musicPlaying`

### Fisica basica

- `aegis.addCollider`
- `aegis.addCircleCollider`
- `aegis.removeCollider`
- `aegis.setColliderLayer`
- `aegis.setColliderMask`
- `aegis.setTrigger`
- `aegis.setColliderOffset`
- `aegis.getColliderOf`
- `aegis.onCollide`
- `aegis.onCollideEnter`
- `aegis.onCollideExit`
- `aegis.addRigidbody`
- `aegis.removeRigidbody`
- `aegis.setVelocity`
- `aegis.setVelocityX`
- `aegis.addImpulseY`
- `aegis.setVel`
- `aegis.setVelX`
- `aegis.jumpY`
- `aegis.addVelocity`
- `aegis.getVelocityX`
- `aegis.getVelocityY`
- `aegis.setGravity`
- `aegis.setGroundFriction`
- `aegis.setGlobalGravity`
- `aegis.setKinematic`
- `aegis.isGrounded`
- `aegis.raycast`
- `aegis.raycastMask`
- `aegis.lineOfSight`
- `aegis.overlapCircle`
- `aegis.overlapRect`

### Save/config

- `aegis.save`
- `aegis.load`
- `aegis.loadConfig`
- `aegis.setDisplayMode`
- `aegis.setFullscreen`
- `aegis.setResolution`

Modos de display estaveis no `aegis.cfg`:

- `"displayMode": "windowed"`: janela normal.
- `"displayMode": "borderless"`: tela cheia sem borda, recomendado para MVP.

`fullscreen` continua existindo por compatibilidade. Codigo novo deve preferir
`displayMode`, porque evita fullscreen exclusivo e reduz risco de tela preta em
drivers/SDL/MonoGame.

### Tween e particulas simples

- `aegis.tween`
- `aegis.newSequence`
- `aegis.seqAdd`
- `aegis.seqWait`
- `aegis.seqPlay`
- `aegis.seqStop`
- `aegis.burst`
- `aegis.newEmitter`
- `aegis.stopEmitter`

### Build Windows

Para o MVP, a validacao oficial e:

```powershell
dotnet build src\Aegis.CLI\Aegis.CLI.csproj --no-restore
```

O contrato de exportacao Windows do MVP e:

```powershell
aegis build meu-jogo --target win-x64
```

Saida esperada:

```text
dist/<nome-do-jogo>-win-x64/
dist/<nome-do-jogo>-win-x64.zip
```

O build deve conter:

- `aegis-cli.exe`
- `main.lua`
- `aegis.toml`
- `res/`
- `JOGAR.bat`
- `README-RUN.txt`
- `aegis-build.json`

Builds paralelos de `Aegis` e `Aegis.CLI` devem ser evitados porque os dois podem
tentar gravar/copiar o mesmo `Aegis.dll` ao mesmo tempo.

## Legacy

Estas APIs continuam funcionando, mas codigo novo deve preferir `aegis.create` e `aegis.destroy`:

- `aegis.newSprite`
- `aegis.newRect`
- `aegis.removeObject`
- `aegis.newLabel`
- `aegis.newAnim`
- `aegis.newRichLabel`
- `aegis.newPanel`
- `aegis.newFlow`
- `aegis.newProgressBar`
- `aegis.setZOrder`
- `aegis.getZOrder`

## Experimental

Estas APIs nao fazem parte do contrato MVP. Elas podem existir no runtime, mas nao devem ser usadas por templates oficiais:

- Drag/drop:
  `aegis.newDraggable`, `aegis.onDragStart`, `aegis.onDragMove`, `aegis.onDragEnd`, `aegis.getDragTarget`
- Hand/card layout:
  `aegis.newHand`, `aegis.handAdd`, `aegis.handRemove`, `aegis.handLayout`, `aegis.handSetHover`
- Upgrade system:
  `aegis.addUpgrade`, `aegis.onUpgradeChosen`, `aegis.getUpgradeLevel`, `aegis.showUpgrades`, `aegis.hideUpgrades`
- Camera autozoom:
  `aegis.setCameraAutozoom`
- Audio 3D:
  `aegis.playSoundAt3D`
- Fisica ainda nao congelada:
  `aegis.addSlopeCollider`, `aegis.setSlopeDir`, `aegis.setOneWay`
- Editor pipe:
  `Aegis.Editor.EditorPipeHost` e comunicacao por named pipe com o editor.

## Regra Para Templates

Templates novos devem usar apenas APIs `Stable/MVP`.
Se um template precisar de API experimental, isso deve estar declarado no README do template.

## Regra Para Evolucao

1. Recurso novo nasce como `Experimental`.
2. Precisa de exemplo, documentacao e build verde para virar `Stable/MVP`.
3. API antiga que foi substituida vira `Legacy`.
4. Nada experimental deve ser requisito para build, templates oficiais ou documentacao principal do MVP.
