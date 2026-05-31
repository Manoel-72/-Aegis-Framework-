# Zone Recon — Top-Down Shooter
### Aegis Engine v0.98 | Lua

---

## Como rodar

```bash
cd zone-recon
aegis run .
```

---

## Estrutura do projeto

```
zone-recon/
├── main.lua                 ← boot e registro de cenas
├── aegis.toml               ← título e resolução
├── scenes/
│   ├── menu.lua             ← menu principal
│   ├── game.lua             ← gameplay top-down shooter
│   ├── mission.lua          ← debriefing pós-zona
│   ├── gameover.lua         ← tela de game over
│   └── win.lua              ← tela de vitória
└── res/
    ├── sprites/             ← coloque seus sprites aqui
    └── audio/               ← coloque seus sons aqui
```

---

## Mecânicas implementadas

- **Movimento 8 direções** — WASD ou seta + gamepad analógico
- **Mira com mouse** — player rotaciona para o cursor
- **Atirar** — clique esquerdo, RB no gamepad ou Espaço
- **Inimigos com pathfinding A*** — calculam caminho desviando de obstáculos
- **Waves** — inimigos aparecem em ondas, quantidade aumenta por zona
- **4 tipos de item coletável** — Medkit, Ammobox, Keycard, Intel
- **3 missões** — cada zona tem objetivo diferente de coleta + combate
- **Portal de saída** — aparece só quando missão for concluída
- **Tela de debriefing** — mostra inventário, recompensa, permite usar medkit
- **3 zonas** — dificuldade crescente (mais inimigos, mais rápidos)
- **HUD completo** — HP, Score, Kills, Inventário por item
- **Persistência** — high score salvo entre sessões

---

## Como adicionar sprites de animação

O jogo funciona 100% com retângulos coloridos.
Quando seus sprites estiverem prontos, substitua assim:

### Player

No `scenes/game.lua`, procure:
```lua
-- Player (retângulo verde por enquanto — troque por sprite depois)
player = aegis.newRect(P_SIZE*2, P_SIZE*2, 0.3, 0.9, 0.5)
```

Substitua por:
```lua
player = aegis.newSprite("sprites/player.png")
aegis.setPivot(player, 0.5, 0.5)

-- Se tiver spritesheet com animações:
local atlas = aegis.loadAtlas("sprites/player.json")  -- exportado do Aseprite
local anim  = aegis.newAtlasAnimator(player, atlas)
aegis.addAtlasClip(anim, "idle",   {"idle_00"},                              1)
aegis.addAtlasClip(anim, "walk",   {"walk_00","walk_01","walk_02","walk_03"}, 10)
aegis.addAtlasClip(anim, "shoot",  {"shoot_00","shoot_01"},                  12, false)
aegis.play(anim, "idle")

-- No aegis_update, adicione:
-- if vx ~= 0 or vy ~= 0 then aegis.play(anim, "walk")
-- else aegis.play(anim, "idle") end
```

### Inimigos

Na função `spawn_enemy()`, procure:
```lua
local obj = aegis.newRect(24, 24, 0.9, 0.2, 0.2)
```

Substitua por:
```lua
local obj = aegis.newSprite("sprites/enemy.png")
aegis.setPivot(obj, 0.5, 0.5)
```

### Itens coletáveis

Na função `spawn_item()`, procure:
```lua
local obj = aegis.newRect(def.w, def.h, def.r, def.g, def.b)
```

Substitua por (um sprite por tipo):
```lua
local sprite_map = {
    medkit  = "sprites/medkit.png",
    ammobox = "sprites/ammobox.png",
    keycard = "sprites/keycard.png",
    intel   = "sprites/intel.png",
}
local obj = aegis.newSprite(sprite_map[kind])
aegis.setPivot(obj, 0.5, 0.5)
```

---

## Áudio necessário

Coloque em `res/audio/`:

| Arquivo | Quando toca |
|---|---|
| `ambient.ogg` | Música do menu |
| `game.ogg` | Música durante o jogo |
| `shoot.wav` | Cada tiro disparado |
| `hit.wav` | Bala acerta inimigo |
| `enemy_die.wav` | Inimigo morre |
| `hurt.wav` | Player leva dano |
| `collect.wav` | Item coletado |
| `wave_start.wav` | Nova wave começa |
| `mission_complete.wav` | Missão concluída |
| `empty.wav` | Sem munição |

Assets gratuitos recomendados: **kenney.nl** → "Impact Sounds" e "Space Shooter Redux"

---

## Missões por zona

| Zona | Missão | Itens necessários |
|---|---|---|
| 1 | Resgate de Intel | 2 × Intel + eliminar todos |
| 2 | Infiltração | 1 × Keycard + 2 × Medkit |
| 3 | Extração | Eliminar todos + 3 × Ammobox |

---

## Ajustar dificuldade

No topo de `scenes/game.lua`:

```lua
local P_SPEED    = 210   -- velocidade do player (aumente para mais ágil)
local BULLET_SPD = 480   -- velocidade da bala
local SHOOT_CD   = 0.18  -- cooldown entre tiros (diminua para atirar mais rápido)
local ENEMY_SPD  = 72    -- velocidade base do inimigo
local ENEMY_HP   = 2     -- HP base do inimigo
local PATH_RATE  = 0.30  -- frequência de recálculo do pathfinding
```

---

## Build e publicação

```bash
# Windows
aegis build examples/zone-recon --target win-x64

# Linux
aegis build examples/zone-recon --target linux-x64

# Publicar no itch.io
aegis publish examples/zone-recon --itch seunome/zone-recon --target win-x64
```
