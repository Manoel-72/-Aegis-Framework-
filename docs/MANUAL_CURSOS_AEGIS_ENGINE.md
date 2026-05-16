# Manual da Aegis Engine — documentação para cursos e referência

**Versão do documento:** alinhada ao código da engine (API Lua registada em `LuaRuntime.RegisterAll`, física em `PhysicsWorld` / `CollisionSystem`, runtime em `AegisGame`).

Este manual serve para:

- **Montar currículos** (módulos por tema e por género de jogo).
- **Consulta rápida** das funções `aegis.*` disponíveis em Lua.
- **Definir pré-requisitos** (.NET, estrutura de pastas, CLI).

---

## 1. Visão geral da stack

| Camada | Tecnologia | Papel |
|--------|-------------|--------|
| Runtime | C# + MonoGame (DesktopGL) | Janela do jogo, gráficos, áudio, física, ciclo de frame |
| Scripting | Lua (NLua) | Lógica do jogo via API global `aegis.*` e callbacks `aegis_*` |
| Ferramentas | CLI `aegis` (`Aegis.CLI`) | `run`, `new`, `build`, `publish`, `doctor` |
| Editor (opcional) | Avalonia — `AegisEditor` | Pré-visualização, pipe nomeado com o runtime (`--editor-pipe`) |

**Filosofia para cursos:** a engine privilegia **2D**, **Lua simples** e **um ficheiro de entrada** `main.lua`; não há ECS nem editor obrigatório para o primeiro jogo.

---

## 2. Requisitos e primeiro projeto

### 2.1 Requisitos

- **.NET SDK 8** (recomendado) instalado no PATH.
- Pasta do jogo com pelo menos:
  - `main.lua` — ponto de entrada (obrigatório).
  - `aegis.toml` — opcional mas recomendado (título e resolução).
  - `res/` — texturas, sons, fontes (caminhos relativos a partir da pasta do jogo).

### 2.2 `aegis.toml` (mínimo)

```toml
title = "Meu Jogo"
width = 1280
height = 720
```

A CLI lê `title`, `width` e `height` (valores numéricos limitados internamente).

### 2.3 Executar um jogo (a partir da pasta do jogo ou com caminho)

```powershell
dotnet run --project caminho/para/src/Aegis.CLI/Aegis.CLI.csproj -- run .
```

Ou, com a CLI global / `aegis-cli` no PATH:

```text
aegis run pasta-do-jogo
```

**Flags úteis:**

- `--editor-pipe` — ativa o servidor de pipe para o **Aegis Editor** (variável de ambiente `AEGIS_EDITOR_PIPE`).
- `--audio-root=...` — raiz relativa para ficheiros de áudio (ver ajuda da CLI).

### 2.4 Criar projeto a partir de template

```text
aegis new nome-do-jogo
aegis new platformer|topdown|puzzle nome-do-jogo
```

Templates vivem em `templates/` no repositório do framework.

### 2.5 Build e publicação (resumo)

```text
aegis build [pasta] --target win-x64|linux-x64|osx|osx-arm64|web
aegis publish --itch utilizador/jogo [--target win-x64]
aegis doctor
```

---

## 3. Ciclo de vida obrigatório em Lua

O motor **exige** que `main.lua` defina:

| Função | Quando corre |
|--------|----------------|
| `aegis_init()` | Uma vez após carregar o script |
| `aegis_update(dt)` | Cada frame; `dt` em segundos |
| `aegis_draw()` | Desenho **em espaço de mundo** (afetado pela `Camera2D` se ativa) |
| `aegis_draw_ui()` | Opcional mas **recomendado** para HUD/menus — desenhado **sem** transformação da câmera |

**Regra de ouro para cursos:** tudo que deve “colar” ao ecrã (vida, pontuação, menus) vai em **`aegis_draw_ui`**; sprites do mundo e tilemaps em **`aegis_draw`**.

---

## 4. Modelo de física e colisão (para explicar em sala)

### 4.1 Conceitos

- **Collider** — caixa ou círculo associado a um `Object2D`; pode ser **sólido** ou **trigger**.
- **Rigidbody2D** — corpo com velocidade, gravidade, `isGrounded`, coyote time, atrito no chão.
- **Estático** — collider **sem** rigidbody: chão/parede.
- **Kinematic** — rigidbody sem gravidade/resolução como dinâmico; útil para plataformas movidas por script.

### 4.2 Layers e máscaras (strings pré-definidas)

| Nome | Bit (uso típico) |
|------|-------------------|
| `WORLD` | 1 — cenário |
| `PLAYER` | 2 |
| `ENEMY` | 4 |
| `BULLET` | 8 |
| `PICKUP` | 16 |
| `UI` | 32 |

Em Lua: `aegis.setColliderLayer(c, "PLAYER")` e `aegis.setColliderMask(c, "WORLD|ENEMY")` ou valores inteiros combinados por OR.

### 4.3 Comportamentos importantes (atual)

- **Resolução de eixo (sólido)** é **AABB-only** no solver. **CircleCollider + Rigidbody** para corpo sólido contra paredes **não é suportado** — a engine emite **aviso** no log; use **AABB** para personagens sólidos e círculo para **detecção/trigger**.
- **Plataforma one-way:** `aegis.setOneWay(collider, true)` — atravessar de baixo para cima ao saltar; apoio ao cair de cima.
- **Plataforma móvel (kinematic):** o motor aplica **carry horizontal** ao dinâmico apoiado no topo (`PhysicsWorld` — ajuste fino em Y pode exigir Lua em casos extremos).
- **Dois dinâmicos:** o solver de eixos **não** empurra dinâmico contra dinâmico; existe **separação suave** pós-passos para reduzir sobreposição visual (não substitui física completa corpo-a-corpo).
- **Contactos / triggers:** broadphase com **spatial hash** (células 128×128) para pares de colliders — melhor escala com muitas entidades.

### 4.4 Raycast e linha de visão

- `aegis.raycast(ox, oy, dx, dy, length)` → tabela com `hit`, `collider`, `x`, `y`, `nx`, `ny`, `dist` ou `nil`.
- `aegis.raycastMask(..., mask)` — `mask` string ou int.
- `aegis.lineOfSight(ax, ay, bx, by [, mask])` → `true` se não houver obstáculo.

---

## 5. Referência da API Lua (`aegis.*`)

Convenções:

- Cores em floats **0..1** salvo indicação contrária.
- **Object2D** — base de `SpriteNode`, `Bitmap`, `Label`, etc.
- Muitas funções aceitam **tipos concretos** devolvidos por outras APIs (guardar referências em variáveis Lua).

### 5.1 Núcleo e transformação

| API | Descrição |
|-----|-----------|
| `aegis.newSprite(path)` | Sprite a partir de ficheiro em `res/` |
| `aegis.newRect(w, h, r, g, b)` | Retângulo sólido (textura gerada) |
| `aegis.removeObject(obj)` | Remove objeto, colliders e rigidbody associados |
| `aegis.setPosition(obj, x, y)` | Posição em pixels |
| `aegis.setPositionNorm(obj, nx, ny)` | Posição normalizada 0..1 do ecrã |
| `aegis.centerX(obj)` | Centro X (útil com pivot) |
| `aegis.setZ` / `aegis.getZ` / `aegis.setZOrder` / `aegis.getZOrder` | Ordem de desenho |
| `aegis.move(obj, dx, dy)` | Transladação incremental |
| `aegis.setScale(obj, sx, sy)` | Escala |
| `aegis.setRotation(obj, rad)` | Rotação em radianos |
| `aegis.setAlpha(obj, a)` | Transparência 0..1 |
| `aegis.setVisible(obj, bool)` | Visibilidade |
| `aegis.setPivot(bitmap, px, py)` | Pivot 0..1 em sprites retangulares |
| `aegis.getX`, `aegis.getY`, `aegis.getWidth`, `aegis.getHeight` | Leituras de posição/tamanho |

### 5.2 Texto e UI

| API | Descrição |
|-----|-----------|
| `aegis.newLabel(text)` | Label com fonte por defeito |
| `aegis.setText(label, text)` | |
| `aegis.setColor(label, r, g, b [, a])` | Cor do texto; **alpha opcional** |
| `aegis.newRichLabel(markup)` | Texto com markup |
| `aegis.setMarkup`, `aegis.setPivotRich` | |
| `aegis.loadFont(file, size)` | TTF |
| `aegis.setFont`, `aegis.setFontRich` | |
| `aegis.newPanel(path, border)` | Nine-slice |
| `aegis.setPanelSize(panel, w, h)` | |
| `aegis.newFlow(direction, opts?)` | `opts`: gap, padding, align |
| `aegis.flowAdd`, `aegis.flowLayout`, `aegis.flowSet` | Layout de filhos |

**Botões (hit-test automático por frame):**

| API | Descrição |
|-----|-----------|
| `aegis.newButton(obj [, onClick])` | Regista sprite/bitmap como botão |
| `aegis.onHover(obj, fn)` | Entrada em hover |
| `aegis.onPress(obj, fn)` | Pressionar com botão esquerdo |
| Callbacks recebem o **obj** em argumento | |

**Barras de progresso:**

| API | Descrição |
|-----|-----------|
| `aegis.newProgressBar(x, y, w, h)` | Devolve o objeto de **fundo** (usar nas outras funções) |
| `aegis.setBarValue(barBg, current, max)` | |
| `aegis.setBarColors(barBg, { bg = {r,g,b}, fill = {r,g,b} })` | Cores de fundo e preenchimento |

**Texto flutuante (dano, XP):**

| API | Descrição |
|-----|-----------|
| `aegis.floatText(x, y, text, opts?)` | `opts`: `r`, `g`, `b`, `speed`, `duration` |

### 5.3 Input

| API | Descrição |
|-----|-----------|
| `aegis.keyDown(k)`, `aegis.keyPressed(k)` | Teclado (nomes de tecla como string, ex. `"Space"`, `"F1"`) |
| `aegis.mouseX()`, `aegis.mouseY()` | |
| `aegis.mouseLeft()`, `aegis.mouseLeftJust()` | |
| `aegis.mouseRight()`, `aegis.mouseRightJust()` | |
| `aegis.mouseScroll()` | Delta da roda |
| `aegis.padConnected(i)`, `aegis.padDown`, `aegis.padPressed`, `aegis.padAxis` | Gamepad índice 0+ |
| `aegis.padVibrate(i, left, right [, seconds])` | |

### 5.4 Ecrã e utilitários

| API | Descrição |
|-----|-----------|
| `aegis.screenWidth()`, `aegis.screenHeight()` | |
| `aegis.log(msg)` | Log para consola / pipeline de debug |
| `aegis.randomInt(min, max)`, `aegis.randomFloat(min, max)` | |
| `aegis.clearAll()` / `aegis.worldClear()` | Limpa cena, física, tweens, partículas, botões, pools, barras, timer `getTime` |
| `aegis.drawText`, `aegis.drawRect`, `aegis.drawLine`, `aegis.drawCircle` | Primitivas no **mesmo** passo que `aegis_draw` ou `aegis_draw_ui` (atenção à câmera) |

### 5.5 Câmara e shake

| API | Descrição |
|-----|-----------|
| `aegis.setCameraTarget(obj [, speed])` | Seguimento |
| `aegis.setCameraOff()` | Desativa câmara 2D |
| `aegis.setCameraZoom(z)` | |
| `aegis.setCameraOffset(ox, oy)` | |
| `aegis.setCameraLimits(left, top, right, bottom)` | Limites do mundo |
| `aegis.setCameraDeadzone(w, h)` | |
| `aegis.setCameraLookahead(dist [, speed])` | |
| `aegis.cameraX()`, `aegis.cameraY()` | |
| `aegis.screenToWorldX(sx, sy)`, `aegis.screenToWorldY(...)` | |
| `aegis.screenShake(intensity, duration)` | |

### 5.6 Áudio

| API | Descrição |
|-----|-----------|
| `aegis.playSound(file)` | |
| `aegis.playSoundEx(file, volume, pitch, pan)` | |
| `aegis.playMusic(file [, loop])` | |
| `aegis.stopMusic`, `aegis.pauseMusic`, `aegis.resumeMusic` | |
| `aegis.setSfxVolume`, `aegis.setMusicVolume` | 0..1 |
| `aegis.musicPlaying()` | |
| `aegis.playSoundAt(file, x, y, opts?)` | Atenuação por distância à câmara; `opts`: `maxDist`, `volume` |
| `aegis.setGroupVolume(group, volume)` | |
| `aegis.crossfadeTo(file, seconds)` | |
| `aegis.playMusicLooped(intro, loop)` | |

### 5.7 Sprites animados simples (grid)

| API | Descrição |
|-----|-----------|
| `aegis.newAnim(path, frameW, frameH)` | |
| `aegis.playAnim`, `stopAnim`, `resumeAnim`, `animFrame`, `animPlaying` | |

### 5.8 Spritesheet, atlas Aseprite, animator

| API | Descrição |
|-----|-----------|
| `aegis.setFrame(sprite, x, y, w, h)` | Recorte em pixels |
| `aegis.clearFrame(sprite)` | |
| `aegis.loadAtlas(jsonPath)` | JSON exportado Aseprite em `res/` |
| `aegis.setAtlasFrame(sprite, atlas, "nome_frame")` | |
| `aegis.newAnimator(sprite, fw, fh)` | Clips por índice de frame |
| `aegis.newAtlasAnimator(sprite, atlas)` | Clips por nome de frame |
| `aegis.addClip(anim, nome, {i,...}, fps [, loop])` | |
| `aegis.addAtlasClip(anim, nome, {"f1","f2"}, fps [, loop])` | |
| `aegis.play(anim, nome [, restart])` | |
| `aegis.stopAnimator`, `aegis.currentClip` | |

### 5.9 Colisores e rigidbody

| API | Descrição |
|-----|-----------|
| `aegis.addCollider(obj, w, h [, offX, offY])` | AABB |
| `aegis.addCircleCollider(obj, radius [, offX, offY])` | Círculo (ver limitações de sólido) |
| `aegis.removeCollider(c)` | |
| `aegis.setColliderLayer`, `aegis.setColliderMask` | String ou int |
| `aegis.setTrigger(c, bool)` | |
| `aegis.setOneWay(c, bool)` | Plataforma one-way |
| `aegis.setColliderOffset(c, ox, oy)` | |
| `aegis.onCollide`, `onCollideEnter`, `onCollideExit` | Callbacks `(a, b)` |
| `aegis.getColliderOf(obj)` | Primeiro collider encontrado (se existir) |
| `aegis.addRigidbody(obj)` | |
| `aegis.removeRigidbody(obj)` | |
| `aegis.setVelocity`, `setVelocityX`, `addImpulseY`, `setVel`, `setVelX`, `jumpY`, `addVelocity` | |
| `aegis.getVelocityX`, `getVelocityY` | |
| `aegis.setGravity(rb, scale)` | Escala da gravidade por corpo |
| `aegis.setGroundFriction(rb, k)` | |
| `aegis.setGlobalGravity(g)` | Pixels/s² (padrão ~800) |
| `aegis.setKinematic(rb, bool)` | |
| `aegis.isGrounded(rb)` | Inclui coyote time |
| `aegis.isTouchingWall(rb)`, `aegis.wallSide(rb)` | Wall jump / slide |

### 5.10 Tilemaps, navegação, cenas

| API | Descrição |
|-----|-----------|
| `aegis.loadTilemap(tiledJsonPath)` | JSON Tiled em `res/` |
| `aegis.generateTilemap(tileset, w, h, tw, th [, seed, scale])` | Procedural Perlin |
| `aegis.setTile`, `aegis.getTile`, `aegis.setTileCulling` | |
| `aegis.buildTilemapColliders(map, opts)` | `solidGids`, `merge`, `layer`, `mask`, `useTiledProperty` |
| `aegis.clearTilemapColliders`, `aegis.tilemapColliderCount` | |
| `aegis.newNavGrid(w, h, cellSize [, diagonal])` | |
| `aegis.navFromTilemap(map, opts)` | |
| `aegis.navFindPath(nav, sx, sy, gx, gy)` | Tabela de pontos `{x,y}` ou `nil` |
| `aegis.navSetSolid`, `aegis.navIsSolid` | |
| `aegis.perlin(x, y [, seed, octaves, scale])` | Ruído |
| `aegis.registerScene(name, luaFile)` | |
| `aegis.transitionTo(scene, mode, seconds)` | Modos ex.: `fade` |
| `aegis.newAreaTrigger(name, x, y, w, h [, oneShot])` | |
| `aegis.onTriggerEnter/Stay/Exit`, `aegis.checkTrigger` | |
| `aegis.clearTriggers()` | |

### 5.11 Configuração, save, efeitos

| API | Descrição |
|-----|-----------|
| `aegis.save(key, value)`, `aegis.load(key)` | JSON em `saves/` |
| `aegis.loadConfig(key)` | `aegis.cfg` |
| `aegis.setFullscreen`, `aegis.setResolution` | |
| `aegis.burst(x, y, opts?)` | Rajada de partículas |
| `aegis.newEmitter`, `aegis.stopEmitter` | Emissor contínuo |
| `aegis.tween(obj, props, duration [, ease, onComplete, opts])` | |
| `aegis.newSequence`, `seqAdd`, `seqWait`, `seqPlay`, `seqStop` | Sequências de tweens |
| `aegis.fadeIn`, `aegis.fadeOut`, `aegis.flashScreen` | |
| `aegis.setHotReload(bool)` | Ativar/desativar vigilância de `main.lua` |
| `aegis.setShader`, `aegis.clearShader` | Shader por objeto |
| `aegis.setScreenShader`, `aegis.clearScreenShader` | Shader de ecrã |

### 5.12 Gameplay e performance (APIs “Sprint 2/3”)

| API | Descrição |
|-----|-----------|
| `aegis.getTime()` | Tempo acumulado em segundos desde o início da sessão (reseta com `clearAll`) |
| `aegis.lookAt(obj, tx, ty)` | Rotação em direção a um ponto; devolve ângulo |
| `aegis.overlapCircle(cx, cy, radius [, mask])` | Tabela 1..n de `Collider` |
| `aegis.overlapRect(x, y, w, h [, mask])` | Idem |
| `aegis.newPool(spritePath, initialSize)` | Devolve **int** ID do pool |
| `aegis.poolGet(poolId [, x, y])` | `SpriteNode` |
| `aegis.poolReturn(poolId, sprite)` | |
| `aegis.poolClear(poolId)` | |
| `aegis.poolCount(poolId)` | `{ available, total }` |

---

## 6. Debug e hot reload (runtime)

- **F1** — alterna overlay de debug (FPS, contagem de objetos, rigidbodies, colliders, estado de hot reload).
- Com overlay ativo — **hitboxes** desenhadas no passo de mundo.
- **F5** — força recarregar `main.lua` (quando hot reload ativo).
- Hot reload também pode reagir a **alterações no disco** do `main.lua` (debounce interno).

Em código: `aegis.setHotReload(true)`.

---

## 7. Sugestão de **módulos de curso** (por tema)

Cada módulo pode ser 1–3 aulas (teoria + exercício + mini-projeto).

| Módulo | Conteúdo central | APIs principais |
|--------|------------------|-------------------|
| M0 — Ambiente | Instalar .NET, `aegis run`, estrutura `res/`, `aegis.toml` | CLI |
| M1 — Ciclo de jogo | `aegis_init` / `update` / `draw` / `draw_ui`, `dt` | callbacks |
| M2 — Sprites e movimento | `newSprite`, `setPosition`, `move`, `setZ` | núcleo |
| M3 — Input | Teclado, rato, opcional gamepad | input |
| M4 — Colisão estática | Colliders, layers, triggers, `onCollideEnter` | colisão |
| M5 — Plataforma | Rigidbody, pulo, gravidade, atrito, **one-way**, **paredes** | física + `setOneWay` + wall |
| M6 — Câmara | Follow, limites, shake, conversão ecrã↔mundo | câmara |
| M7 — Tilemaps | Tiled JSON, colliders a partir do mapa, culling | tilemap |
| M8 — Áudio e save | Sons, música, volumes, persistência simples | áudio + save |
| M9 — UI e fluxo | Labels, painéis, flow, **botões**, **barras**, texto flutuante | UI + sprint APIs |
| M10 — Combate / área | **overlapCircle**, pools de projéteis, partículas | overlap + pool |
| M11 — IA básica | NavGrid, pathfinding, `lineOfSight` | nav |
| M12 — Cenas e transições | `registerScene`, `transitionTo`, `clearAll` | cenas |
| M13 — Polimento | Tweens, sequências, fades, shaders leves | efeitos |
| M14 — Empacotamento | `build`, `publish`, `doctor` | CLI |

---

## 8. Sugestão de **trilhos por estilo de jogo** (combinar módulos)

| Estilo | Módulos mínimos | Notas pedagógicas |
|--------|-----------------|-------------------|
| **Hyper-casual / puzzle 2D** | M0–M4, M8, M9 | Pouca física; foco em estados e UI |
| **Plataformer** | M0–M7, M5, M6 | Coyote, one-way, wall; tilemap com colisão |
| **Top-down / twin-stick** | M0–M4, M3, M6, M10 | `lookAt`, overlap, pools para balas |
| **Sobrevivência / hordas** | M0–M5, M6, M10, M13 | Pools obrigatórios; overlap para AoE; separação suave entre inimigos |
| **Card / UI-heavy** | M1, M3, M9, M8 | Botões, tweens, save; hit-test já no `newButton` |
| **Narrativo leve / níveis** | M7, M12, M8 | Cenas com `transitionTo` |

---

## 9. Limitações úteis para mencionar no final do curso

- **Rampas / slopes** — colisão inclinada por tile não é um sistema dedicado; trabalhar com degraus ou AABBs em Lua.
- **Colliders rotacionados** — o eixo solver usa AABB alinhado; rotação visual do sprite não roda a caixa de colisão.
- **Círculo como corpo sólido** — usar AABB para o corpo físico e círculo só como zona.
- **Empurrão rígido entre dois dinâmicos** — não é Box2D; para “empilhar caixas” realistas, esperar limitações ou simular em Lua.
- **Áudio 3D espacial completo** — `playSoundAt` aproxima por distância à câmara; não é motor de áudio tipo FMOD 3D.
- **Web/WASM** — o alvo `web` na CLI prepara pacote; runtime principal é DesktopGL (ver README do repo).

---

## 10. Onde está o código-fonte (para formadores)

| Área | Ficheiros / pastas |
|------|---------------------|
| Registo e implementação Lua | `src/Aegis/Scripting/LuaRuntime.cs` |
| Ciclo de frame e draw | `src/Aegis/Core/AegisGame.cs` |
| Física | `src/Aegis/Physics/PhysicsWorld.cs`, `CollisionSystem.cs`, `Rigidbody2D.cs`, `Collider.cs` |
| Câmara | `src/Aegis/Display/Camera2D.cs` (nome aproximado — procurar em `Display/`) |
| CLI | `src/Aegis.CLI/Program.cs` |
| Editor | `AegisEditor/src/AegisEditor/` |
| Mensagens editor ↔ runtime | `AegisEditor.Shared/Messages/`, `src/Aegis/Editor/EditorPipeHost.cs` |

---

## 11. Checklist “jogo pronto para demonstração”

- [ ] `main.lua` com `aegis_init`, `aegis_update`, `aegis_draw`
- [ ] HUD em `aegis_draw_ui` se usar câmara em movimento
- [ ] Assets referenciados com caminhos válidos em `res/`
- [ ] `aegis.toml` com título e resolução
- [ ] Testar `aegis run` e um `aegis build` para o alvo pretendido
- [ ] (Opcional) README do projeto com instruções para o professor

---

*Fim do manual. Para atualizar este documento quando a API mudar, compare com `LuaRuntime.RegisterAll()` e com a ajuda impressa por `aegis` sem argumentos ou comando inválido na CLI.*
