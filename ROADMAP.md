# Aegis Engine — Roadmap Atualizado

Estado real do código em abril/2026.

## v0.1 — Base & Fundação ✅
- Loop MonoGame 60fps com `dt` protegido
- `App` + `AegisGame`
- Hierarquia `Scene2D` / `Object2D`
- `Bitmap`, `Renderer`, `ResManager`
- `LuaRuntime` com API `aegis.*`
- CLI com `new` / `run` / `doctor` / `update`

## v0.2 — Animação & Câmera ✅
- `AnimatedSprite` (spritesheet)
- `Camera2D` follow, zoom, limites
- `aegis.screenShake(intensity, duration)`
- Input teclado/mouse
- **Gamepad/joystick básico**: `aegis.padConnected`, `aegis.padDown`, `aegis.padPressed`, `aegis.padAxis`
- Templates CLI: `basic`, `topdown`, `survivor`

## v0.3 — Audio & Texto ✅
- `AudioManager` (SFX/música)
- `Label` + `RichLabel`
- Fontes TTF (`FontManager`)
- `NineSlice` para UI

## v0.4 — Física & Colisão ✅
- AABB por eixos (`IntegrateX/ResolveX`, `IntegrateY/ResolveY`)
- Layer/Mask, Trigger e callbacks (`onCollide*`)
- `Rigidbody2D` com gravidade, grounded, coyote e atrito opcional
- Raycast/line-of-sight
- Compat de API (`setVel`, `setVelX`, `jumpY`)

## v0.5 — Mundo & Tilemap ✅
- `TilemapNode` (Tiled JSON)
- Culling por câmera
- Geração procedural (`PerlinNoise`)
- `SceneManager` + transição `fade`
- Triggers de área

## v0.6 — Save & Config ✅
- `save` / `load` (persistência local)
- `setFullscreen` / `setResolution`
- Configuração automática

## v0.7 — Partículas & FX ✅ (profissionalizado)
- `ParticleSystem2D` (`aegis.burst`)
- Opções avançadas em burst: `gravity`, `drag`, `spread`
- `TweenManager` (`aegis.tween`)
- `screenShake`, `fadeIn`, `fadeOut`, `flashScreen`

## v0.8 — Dev Experience ✅ (reforçado)
- Hot reload Lua (`setHotReload`) + `reloadNow`
- Debug overlay F1 com FPS/contagens
- Hitboxes visíveis no debug (toggle em API)
- `aegis.debugStats()` para telemetria Lua
- `aegis doctor` para diagnóstico rápido no CLI

## Próximos focos sugeridos (v0.98+)
- Audio spatial e mixer por bus
- Navegação/pathfinding
- Perfilador in-game com timelines
- Build pipeline para distribuição
