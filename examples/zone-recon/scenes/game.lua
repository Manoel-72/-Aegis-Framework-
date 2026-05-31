-- ═══════════════════════════════════════════════════════════════════
-- CENA: GAME — Top-Down Shooter
-- Mecânicas: mover 8 direções, mirar com mouse, atirar, coletar itens,
--            inimigos com pathfinding, waves por zona, missão ao final
-- ═══════════════════════════════════════════════════════════════════

-- ── Constantes de gameplay ──────────────────────────────────────────
local P_SPEED    = 210        -- velocidade do player (px/s)
local P_SIZE     = 20         -- metade do hitbox do player
local BULLET_SPD = 480        -- velocidade da bala
local SHOOT_CD   = 0.18       -- cooldown entre tiros (segundos)
local ENEMY_SPD  = 72         -- velocidade base do inimigo
local ENEMY_HP   = 2          -- HP base do inimigo
local PATH_RATE  = 0.30       -- intervalo de recálculo de pathfinding
local WORLD_W    = 1920       -- largura do mundo
local WORLD_H    = 1440       -- altura do mundo

-- Forward declaration usada pelas funções de missão (closures).
local enemies = {}

-- Definição dos itens coletáveis por tipo
local ITEM_DEF = {
    medkit  = { r=0.2, g=1.0, b=0.4, w=18, h=18, label="MEDKIT",  value=1 },
    ammobox = { r=1.0, g=0.8, b=0.1, w=18, h=14, label="AMMO",    value=1 },
    keycard = { r=0.3, g=0.6, b=1.0, w=20, h=12, label="KEYCARD", value=1 },
    intel   = { r=1.0, g=0.4, b=0.8, w=14, h=18, label="INTEL",   value=1 },
}

-- Missões por zona
local MISSIONS = {
    [1] = {
        title = "ZONA 1 — Resgate de Intel",
        desc1 = "Colete 2 documentos de Intel",
        desc2 = "e elimine todos os inimigos.",
        check = function() return GAME.inventory.intel >= 2 and #enemies == 0 end,
        reward = 300,
    },
    [2] = {
        title = "ZONA 2 — Infiltração",
        desc1 = "Colete 1 Keycard e 2 Medkits",
        desc2 = "sem morrer.",
        check = function() return GAME.inventory.keycard >= 1 and GAME.inventory.medkit >= 2 end,
        reward = 500,
    },
    [3] = {
        title = "ZONA 3 — Extração",
        desc1 = "Elimine todos os inimigos",
        desc2 = "e colete 3 caixas de munição.",
        check = function() return #enemies == 0 and GAME.inventory.ammobox >= 3 end,
        reward = 800,
    },
}

-- ── Estado da cena ──────────────────────────────────────────────────
local player, playerCol, playerRb
local bullets       = {}   -- { obj, c, vx, vy, life }
enemies             = {}   -- { obj, c, hp, pathTimer, path, pathIndex, floatY }
local items         = {}   -- { obj, c, kind, collected }
local pendingRemove = {}   -- lista de funções de limpeza para executar no próximo frame
local walls         = {}   -- colliders de parede/borda

-- HUD
local hud, hud_hp, hud_score, hud_kills
local inv_labels    = {}
local mission_label, mission_desc2, zone_lbl, mission_obj

-- Timers e estado
local shootTimer    = 0
local hurtCooldown  = 0
local waveTimer     = 0
local waveCount     = 0
local missionActive = false
local missionDone   = false
local exitPortal    = nil
local exitCol       = nil
local nav           = nil

-- ── Helpers ─────────────────────────────────────────────────────────

local function zone_enemy_count()
    return 4 + GAME.zone * 2   -- zona 1: 6, zona 2: 8, zona 3: 10
end

local function zone_hp()
    return ENEMY_HP + (GAME.zone - 1)
end

local function update_hud()
    aegis.setText(hud_hp,    "HP  " .. GAME.hp .. " / " .. GAME.maxHp)
    aegis.setText(hud_score, "Score  " .. GAME.score)
    aegis.setText(hud_kills, "Kills  " .. GAME.kills)

    local inv = GAME.inventory
    aegis.setText(inv_labels.medkit,  "Medkit  " .. inv.medkit)
    aegis.setText(inv_labels.ammobox, "Ammo    " .. inv.ammobox)
    aegis.setText(inv_labels.keycard, "Keycard " .. inv.keycard)
    aegis.setText(inv_labels.intel,   "Intel   " .. inv.intel)
end

-- HUD em coordenadas de tela: a câmera aplica transform no desenho, então
-- reposicionamos os nós para o equivalente em mundo a cada frame.
local function sync_hud_to_screen()
    if not hud then return end
    local sw = aegis.screenWidth()
    local pad = 14
    aegis.setPosition(hud,
        aegis.screenToWorldX(pad, pad),
        aegis.screenToWorldY(pad, pad))

    local kinds = { "medkit", "ammobox", "keycard", "intel" }
    for i, k in ipairs(kinds) do
        local lbl = inv_labels[k]
        if lbl then
            local sx = sw - 150
            local sy = pad + (i - 1) * 22
            aegis.setPosition(lbl,
                aegis.screenToWorldX(sx, sy),
                aegis.screenToWorldY(sx, sy))
        end
    end

    if zone_lbl then
        aegis.setPosition(zone_lbl,
            aegis.screenToWorldX(sw * 0.5, pad),
            aegis.screenToWorldY(sw * 0.5, pad))
    end
    if mission_label then
        aegis.setPosition(mission_label,
            aegis.screenToWorldX(sw * 0.5, 36),
            aegis.screenToWorldY(sw * 0.5, 36))
    end
    if mission_desc2 then
        aegis.setPosition(mission_desc2,
            aegis.screenToWorldX(sw * 0.5, 56),
            aegis.screenToWorldY(sw * 0.5, 56))
    end
end

-- ── Construção do mapa (paredes + nav) ──────────────────────────────

local function build_walls()
    -- Bordas do mundo como paredes invisíveis
    local borders = {
        { x=0,            y=0,             w=WORLD_W, h=16        },   -- topo
        { x=0,            y=WORLD_H-16,    w=WORLD_W, h=16        },   -- baixo
        { x=0,            y=0,             w=16,       h=WORLD_H  },   -- esquerda
        { x=WORLD_W-16,   y=0,             w=16,       h=WORLD_H  },   -- direita
    }
    -- Obstáculos internos (caixas, muros) — ajuste as posições para seu tilemap
    local obstacles = {
        { x=300,  y=250,  w=120, h=32  },
        { x=600,  y=400,  w=32,  h=160 },
        { x=900,  y=200,  w=200, h=32  },
        { x=1100, y=500,  w=32,  h=200 },
        { x=400,  y=700,  w=160, h=32  },
        { x=700,  y=900,  w=32,  h=120 },
        { x=1300, y=300,  w=180, h=32  },
        { x=200,  y=1100, w=32,  h=160 },
        { x=1500, y=800,  w=120, h=32  },
        { x=800,  y=1200, w=200, h=32  },
    }

    for _, b in ipairs(borders) do
        local obj = aegis.newRect(b.w, b.h, 0.15, 0.18, 0.15)
        aegis.setPosition(obj, b.x, b.y)
        aegis.setZ(obj, 1)
        local c = aegis.addCollider(obj, b.w, b.h, 0, 0)
        aegis.setColliderLayer(c, "WORLD")
        aegis.setColliderMask(c, "PLAYER|ENEMY|BULLET")
        walls[#walls+1] = obj
    end

    for _, o in ipairs(obstacles) do
        local obj = aegis.newRect(o.w, o.h, 0.20, 0.24, 0.20)
        aegis.setPosition(obj, o.x, o.y)
        aegis.setZ(obj, 2)
        local c = aegis.addCollider(obj, o.w, o.h, 0, 0)
        aegis.setColliderLayer(c, "WORLD")
        aegis.setColliderMask(c, "PLAYER|ENEMY|BULLET")
        walls[#walls+1] = obj
    end
end

local function build_nav()
    -- Grid de navegação: células de 32px no mundo 1920x1440
    nav = aegis.newNavGrid(60, 45, 32)

    -- Marca bordas como sólidas
    for x = 0, 59 do
        aegis.navSetSolid(nav, x, 0,  true)
        aegis.navSetSolid(nav, x, 44, true)
    end
    for y = 0, 44 do
        aegis.navSetSolid(nav, 0,  y, true)
        aegis.navSetSolid(nav, 59, y, true)
    end

    -- Marca obstáculos como sólidos (mesmo grid dos retângulos acima)
    local solid_cells = {
        { cx=9,  cy=7,  cw=4, ch=1 },
        { cx=18, cy=12, cw=1, ch=5 },
        { cx=28, cy=6,  cw=7, ch=1 },
        { cx=34, cy=15, cw=1, ch=7 },
        { cx=12, cy=21, cw=5, ch=1 },
        { cx=21, cy=28, cw=1, ch=4 },
        { cx=40, cy=9,  cw=6, ch=1 },
        { cx=6,  cy=34, cw=1, ch=5 },
        { cx=46, cy=25, cw=4, ch=1 },
        { cx=25, cy=37, cw=7, ch=1 },
    }
    for _, sc in ipairs(solid_cells) do
        for dx = 0, sc.cw - 1 do
            for dy = 0, sc.ch - 1 do
                aegis.navSetSolid(nav, sc.cx + dx, sc.cy + dy, true)
            end
        end
    end
end

-- ── Spawnar inimigos ─────────────────────────────────────────────────

local function spawn_enemy(x, y)
    -- Por enquanto retângulo vermelho — troque por sprite depois
    local obj = aegis.newRect(24, 24, 0.9, 0.2, 0.2)
    aegis.setPosition(obj, x, y)
    aegis.setZ(obj, 10)

    -- Outline de shader para destaque
    aegis.setShader(obj, "outline", { r=1, g=0, b=0, width=2 })

    local e = {
        obj       = obj,
        hp        = zone_hp(),
        pathTimer = math.random() * PATH_RATE,  -- stagger inicial
        path      = nil,
        pathIndex = 1,
        floatY    = 0,
        floatT    = math.random() * 6.28,
        lastX     = x,
        lastY     = y,
        stuckTime = 0,
    }

    local c = aegis.addCollider(obj, 20, 20, 2, 2)
    aegis.setColliderLayer(c, "ENEMY")
    aegis.setColliderMask(c, "PLAYER|BULLET|WORLD")
    e.c = c

    -- Colisão com bala — dano ao inimigo
    aegis.onCollideEnter(c, function(a, b)
        -- verifica se b é uma bala (busca na lista)
        for i = #bullets, 1, -1 do
            if bullets[i].c == b then
                -- remove a bala
                local bul = bullets[i]
                pendingRemove[#pendingRemove+1] = function()
                    aegis.removeObject(bul.obj)
                    table.remove(bullets, i)
                end

                -- dano ao inimigo
                e.hp = e.hp - 1

                if e.hp <= 0 then
                    -- morte do inimigo
                    aegis.burst(aegis.getX(obj), aegis.getY(obj), {
                        count=22, speed=130, life=0.55, size=4,
                        r=1.0, g=0.3, b=0.1, spread=360
                    })
                    aegis.playSound("enemy_die.wav")
                    GAME.kills  = GAME.kills + 1
                    GAME.score  = GAME.score + 100

                    pendingRemove[#pendingRemove+1] = function()
                        for j = #enemies, 1, -1 do
                            if enemies[j].obj == obj then
                                table.remove(enemies, j)
                                break
                            end
                        end
                        aegis.removeObject(obj)
                    end

                    update_hud()

                    -- Checa missão após matar inimigo
                    local m = MISSIONS[GAME.zone]
                    if m and m.check() and not missionDone then
                        missionDone = true
                        aegis.setText(mission_label, "✓ MISSÃO CONCLUÍDA!")
                        aegis.setColor(mission_label, 0.3, 1.0, 0.5)
                        aegis.setVisible(exitPortal, true)
                        aegis.setVisible(mission_obj, true)
                        aegis.playSound("mission_complete.wav")
                        aegis.screenShake(4, 0.3)
                    end
                else
                    aegis.playSound("hit.wav")
                    aegis.screenShake(2, 0.08)
                end
                break
            end
        end
    end)

    enemies[#enemies+1] = e
end

local function spawn_wave()
    waveCount = waveCount + 1
    local count = zone_enemy_count()
    local px = aegis.getX(player)
    local py = aegis.getY(player)

    for i = 1, count do
        -- Spawna longe do player
        local angle = (i / count) * 6.28
        local dist  = 500 + math.random(0, 300)
        local ex = math.max(50, math.min(WORLD_W - 50, px + math.cos(angle) * dist))
        local ey = math.max(50, math.min(WORLD_H - 50, py + math.sin(angle) * dist))

        -- Evita nascer preso em célula sem rota válida.
        for _ = 1, 8 do
            local p = aegis.navFindPath(nav, ex, ey, px + P_SIZE, py + P_SIZE)
            if p and #p > 1 then break end
            ex = math.random(64, WORLD_W - 64)
            ey = math.random(64, WORLD_H - 64)
        end
        spawn_enemy(ex, ey)
    end

    aegis.playSound("wave_start.wav")
end

-- ── Spawnar itens coletáveis ─────────────────────────────────────────

local function spawn_item(kind, x, y)
    local def = ITEM_DEF[kind]
    if not def then return end

    local obj = aegis.newRect(def.w, def.h, def.r, def.g, def.b)
    aegis.setPosition(obj, x, y)
    aegis.setZ(obj, 8)

    -- Animação de flutuação
    aegis.tween(obj, { scaleX=1.15, scaleY=1.15 }, 0.7, "inout", nil,
        { loop=true, yoyo=true })

    local c = aegis.addCircleCollider(obj, 14)
    aegis.setTrigger(c, true)
    aegis.setColliderLayer(c, "PICKUP")
    aegis.setColliderMask(c, "PLAYER")

    local item = { obj=obj, c=c, kind=kind, collected=false }

    -- Label flutuante de identificação
    local lbl = aegis.newLabel("[" .. def.label .. "]")
    aegis.setPosition(lbl, x, y - 22)
    aegis.setColor(lbl, def.r, def.g, def.b)
    aegis.setZ(lbl, 9)
    item.lbl = lbl

    aegis.onCollideEnter(c, function(a, b)
        if item.collected then return end
        item.collected = true

        -- Adiciona ao inventário
        GAME.inventory[kind] = (GAME.inventory[kind] or 0) + 1
        GAME.score = GAME.score + 50

        aegis.playSound("collect.wav")
        aegis.burst(aegis.getX(obj), aegis.getY(obj), {
            count=16, speed=100, life=0.5, size=4,
            r=def.r, g=def.g, b=def.b, spread=360
        })

        -- Popup de texto
        local popup = aegis.newLabel("+" .. def.label)
        aegis.setPosition(popup, aegis.getX(obj), aegis.getY(obj) - 10)
        aegis.setColor(popup, def.r, def.g, def.b)
        aegis.setZ(popup, 100)
        aegis.tween(popup, { y = aegis.getY(obj) - 50, alpha = 0 }, 0.9, "out",
            function() aegis.removeObject(popup) end)

        pendingRemove[#pendingRemove+1] = function()
            aegis.removeObject(obj)
            aegis.removeObject(lbl)
            for i = #items, 1, -1 do
                if items[i].obj == obj then
                    table.remove(items, i)
                    break
                end
            end
        end

        update_hud()

        -- Checa se missão foi concluída com essa coleta
        local m = MISSIONS[GAME.zone]
        if m and m.check() and not missionDone then
            missionDone = true
            aegis.setText(mission_label, "✓ MISSÃO CONCLUÍDA!")
            aegis.setColor(mission_label, 0.3, 1.0, 0.5)
            aegis.setVisible(exitPortal, true)
            aegis.setVisible(mission_obj, true)
            aegis.playSound("mission_complete.wav")
            aegis.screenShake(4, 0.3)
        end
    end)

    items[#items+1] = item
end

local function spawn_zone_items()
    local z = GAME.zone
    -- Posições fixas por zona — depois substitua por leitura de tilemap
    local layouts = {
        [1] = {
            { kind="intel",   x=420,  y=380 },
            { kind="intel",   x=1100, y=700 },
            { kind="medkit",  x=650,  y=250 },
            { kind="ammobox", x=300,  y=900 },
        },
        [2] = {
            { kind="keycard", x=800,  y=600 },
            { kind="medkit",  x=350,  y=450 },
            { kind="medkit",  x=1400, y=850 },
            { kind="intel",   x=500,  y=1100 },
            { kind="ammobox", x=1200, y=300 },
        },
        [3] = {
            { kind="ammobox", x=400,  y=350 },
            { kind="ammobox", x=900,  y=600 },
            { kind="ammobox", x=1500, y=900 },
            { kind="keycard", x=700,  y=1100 },
            { kind="intel",   x=1300, y=450 },
            { kind="medkit",  x=200,  y=800 },
        },
    }
    for _, item in ipairs(layouts[z] or {}) do
        spawn_item(item.kind, item.x, item.y)
    end
end

-- ── Atirar ──────────────────────────────────────────────────────────

local function shoot()
    if GAME.inventory.ammobox <= 0 and GAME.zone > 1 then
        -- Sem munição nas zonas avançadas: mostra aviso
        aegis.playSound("empty.wav")
        return
    end

    local px = aegis.getX(player) + P_SIZE
    local py = aegis.getY(player) + P_SIZE

    -- Direção: do player até o cursor do mouse (em coordenadas de mundo)
    local mx = aegis.screenToWorldX(aegis.mouseX(), aegis.mouseY())
    local my = aegis.screenToWorldY(aegis.mouseX(), aegis.mouseY())
    local dx = mx - px
    local dy = my - py
    local len = math.sqrt(dx*dx + dy*dy)
    if len < 1 then return end
    dx = dx / len
    dy = dy / len

    -- Objeto bala
    local bobj = aegis.newRect(6, 3, 1.0, 0.95, 0.4)
    aegis.setPosition(bobj, px, py)
    aegis.setZ(bobj, 20)
    aegis.setRotation(bobj, math.atan(dy, dx))

    local bc = aegis.addCollider(bobj, 6, 3, 0, 0)
    aegis.setColliderLayer(bc, "BULLET")
    aegis.setColliderMask(bc, "ENEMY|WORLD")

    -- Bala para ao bater em parede
    aegis.onCollideEnter(bc, function(a, b)
        -- ignora colisão com inimigos aqui (tratado no inimigo)
        local isEnemy = false
        for _, e in ipairs(enemies) do
            if e.c == b then isEnemy = true; break end
        end
        if not isEnemy then
            -- Bateu em parede: remove
            for i = #bullets, 1, -1 do
                if bullets[i].c == bc then
                    local bul = bullets[i]
                    pendingRemove[#pendingRemove+1] = function()
                        aegis.removeObject(bul.obj)
                        table.remove(bullets, i)
                    end
                    break
                end
            end
        end
    end)

    bullets[#bullets+1] = {
        obj  = bobj,
        c    = bc,
        vx   = dx * BULLET_SPD,
        vy   = dy * BULLET_SPD,
        life = 1.2,   -- segundos até sumir automaticamente
    }

    aegis.playSound("shoot.wav")
    GAME.shots = GAME.shots + 1

    -- Consome munição nas zonas 2 e 3
    if GAME.zone > 1 and GAME.inventory.ammobox > 0 then
        -- Só consome a cada 8 tiros (ammobox = recarga)
        if GAME.shots % 8 == 0 then
            GAME.inventory.ammobox = math.max(0, GAME.inventory.ammobox - 1)
            update_hud()
        end
    end
end

-- ── HUD ─────────────────────────────────────────────────────────────

local function build_hud()
    -- Barra de HP
    local hp_icon = aegis.newRect(14, 14, 1.0, 0.3, 0.3)
    hud_hp = aegis.newLabel("HP  " .. GAME.hp .. " / " .. GAME.maxHp)
    aegis.setColor(hud_hp, 1.0, 0.5, 0.5)

    hud_score = aegis.newLabel("Score  " .. GAME.score)
    aegis.setColor(hud_score, 0.8, 1.0, 0.8)

    hud_kills = aegis.newLabel("Kills  " .. GAME.kills)
    aegis.setColor(hud_kills, 1.0, 0.8, 0.4)

    hud = aegis.newFlow("horizontal", { gap=18, padding=12, align="center" })
    aegis.flowAdd(hud, hp_icon)
    aegis.flowAdd(hud, hud_hp)
    aegis.flowAdd(hud, hud_score)
    aegis.flowAdd(hud, hud_kills)
    aegis.setPosition(hud, 14, 14)
    aegis.setZ(hud, 500)
    aegis.flowLayout(hud)

    -- Inventário (canto direito)
    local sw = aegis.screenWidth()
    local inv_labels_list = {}

    local kinds = { "medkit", "ammobox", "keycard", "intel" }
    local colors = {
        medkit  = { 0.2, 1.0, 0.4 },
        ammobox = { 1.0, 0.8, 0.1 },
        keycard = { 0.3, 0.6, 1.0 },
        intel   = { 1.0, 0.4, 0.8 },
    }

    for i, k in ipairs(kinds) do
        local lbl = aegis.newLabel(k .. "  0")
        local co = colors[k]
        aegis.setColor(lbl, co[1], co[2], co[3])
        aegis.setPosition(lbl, sw - 150, 14 + (i-1) * 22)
        aegis.setZ(lbl, 500)
        inv_labels[k] = lbl
    end

    -- Zona atual
    zone_lbl = aegis.newLabel("ZONA " .. GAME.zone .. " / " .. GAME.maxZone)
    aegis.setColor(zone_lbl, 0.5, 0.8, 1.0)
    aegis.setZ(zone_lbl, 500)

    -- Missão
    local m = MISSIONS[GAME.zone]
    mission_label = aegis.newLabel(m and m.desc1 or "")
    aegis.setColor(mission_label, 0.8, 0.9, 0.6)
    aegis.setZ(mission_label, 500)

    mission_desc2 = aegis.newLabel(m and m.desc2 or "")
    aegis.setColor(mission_desc2, 0.65, 0.75, 0.5)
    aegis.setZ(mission_desc2, 500)

    sync_hud_to_screen()
end

-- ── Portal de saída ──────────────────────────────────────────────────

local function build_exit_portal()
    local sw = aegis.screenWidth()

    -- Portal fica no canto inferior direito do mundo
    exitPortal = aegis.newRect(48, 48, 0.3, 1.0, 0.6)
    aegis.setPosition(exitPortal, WORLD_W - 100, WORLD_H - 100)
    aegis.setZ(exitPortal, 5)
    aegis.setVisible(exitPortal, false)   -- escondido até missão concluída
    aegis.setShader(exitPortal, "flash", { r=0.3, g=1.0, b=0.6 })

    -- Label do portal
    mission_obj = aegis.newLabel("[ SAÍDA ]")
    aegis.setPosition(mission_obj, WORLD_W - 100, WORLD_H - 130)
    aegis.setColor(mission_obj, 0.3, 1.0, 0.6)
    aegis.setZ(mission_obj, 6)
    aegis.setVisible(mission_obj, false)

    exitCol = aegis.addCollider(exitPortal, 48, 48, 0, 0)
    aegis.setTrigger(exitCol, true)
    aegis.setColliderLayer(exitCol, "PICKUP")
    aegis.setColliderMask(exitCol, "PLAYER")

    aegis.onCollideEnter(exitCol, function(a, b)
        if not missionDone then return end

        -- Salva dados da missão e vai para tela de missão
        GAME.mission   = MISSIONS[GAME.zone]
        GAME.missionOk = true
        local hs = aegis.load("highscore") or 0
        if GAME.score > hs then aegis.save("highscore", GAME.score) end

        aegis.stopMusic()
        aegis.transitionTo("mission", "fade", 0.4)
    end)
end

-- ── aegis_init ───────────────────────────────────────────────────────

function aegis_init()
    aegis.clearAll()

    bullets       = {}
    enemies       = {}
    items         = {}
    pendingRemove = {}
    walls         = {}
    playerRb      = nil
    missionDone   = false
    shootTimer    = 0
    hurtCooldown  = 0
    waveTimer     = 3.0   -- primeira wave em 3 segundos
    waveCount     = 0

    -- Limpa inventário ao entrar em nova zona
    if GAME.zone == 1 then
        GAME.inventory = { medkit=0, ammobox=0, keycard=0, intel=0 }
        GAME.hp        = GAME.maxHp
        GAME.kills     = 0
        GAME.shots     = 0
    end

    -- Chão base (plano de fundo)
    local floor = aegis.newRect(WORLD_W, WORLD_H, 0.08, 0.10, 0.09)
    aegis.setPosition(floor, 0, 0)
    aegis.setZ(floor, -5)

    -- Grade visual de fundo
    for gx = 0, 59 do
        for gy = 0, 44 do
            if (gx + gy) % 2 == 0 then
                local cell = aegis.newRect(32, 32, 0.09, 0.11, 0.10)
                aegis.setPosition(cell, gx*32, gy*32)
                aegis.setZ(cell, -4)
            end
        end
    end

    -- Paredes e nav
    build_walls()
    build_nav()

    -- Player (retângulo verde por enquanto — troque por sprite depois)
    player = aegis.newRect(P_SIZE*2, P_SIZE*2, 0.3, 0.9, 0.5)
    aegis.setPosition(player, 100, 100)
    aegis.setZ(player, 15)
    aegis.setShader(player, "outline", { r=0, g=0.5, b=0.2, width=2 })

    playerCol = aegis.addCollider(player, P_SIZE*2, P_SIZE*2, 0, 0)
    aegis.setColliderLayer(playerCol, "PLAYER")
    aegis.setColliderMask(playerCol, "WORLD|ENEMY|PICKUP")
    playerRb = aegis.addRigidbody(player)
    aegis.setGravity(playerRb, 0)
    aegis.setGroundFriction(playerRb, 0)

    -- Câmera
    aegis.setCameraTarget(player, 8)
    aegis.setCameraDeadzone(160, 120)
    aegis.setCameraLookahead(60, 3.0)
    aegis.setCameraLimits(0, 0, WORLD_W, WORLD_H)
    aegis.setCameraZoom(1.1)

    -- HUD
    build_hud()
    update_hud()

    -- Portal de saída
    build_exit_portal()

    -- Itens da zona
    spawn_zone_items()

    -- Emitter de neblina ambiente
    local fog = aegis.newEmitter(WORLD_W/2, WORLD_H/2, {
        rate=3, duration=-1, speed=12, life=8.0, size=60,
        r=0.12, g=0.16, b=0.14, gravity=0, spread=360
    })

    -- Áudio
    aegis.setGroupVolume("sfx",   0.7)
    aegis.setGroupVolume("music", 0.3)
    -- Música opcional (desativada até existir arquivo válido em res/audio).
    -- aegis.playMusic("game.ogg")
    aegis.setScreenShader("vignette", { intensity=0.45 })

    aegis.fadeIn(0.5)
    aegis.log("[game] zona " .. GAME.zone .. " iniciada")
end

-- ── aegis_update ─────────────────────────────────────────────────────

function aegis_update(dt)
    sync_hud_to_screen()

    -- ── Escape para menu ──────────────────────────────────────────
    if aegis.keyPressed("Escape") or aegis.padPressed(0, "Back") then
        aegis.stopMusic()
        aegis.clearScreenShader()
        aegis.transitionTo("menu", "fade", 0.35)
        return
    end

    -- ── Processa remoções pendentes ───────────────────────────────
    if #pendingRemove > 0 then
        for _, fn in ipairs(pendingRemove) do fn() end
        pendingRemove = {}
    end

    -- ── Timers ────────────────────────────────────────────────────
    shootTimer    = math.max(0, shootTimer - dt)
    hurtCooldown  = math.max(0, hurtCooldown - dt)

    -- ── Wave de inimigos ──────────────────────────────────────────
    if #enemies == 0 and not missionDone then
        waveTimer = waveTimer - dt
        if waveTimer <= 0 then
            waveTimer = 20 + GAME.zone * 5   -- próxima wave
            spawn_wave()
        end
    end

    -- ── INPUT: mover player 8 direções ───────────────────────────
    local vx, vy = 0, 0
    if aegis.keyDown("A") or aegis.keyDown("Left")  then vx = vx - P_SPEED end
    if aegis.keyDown("D") or aegis.keyDown("Right") then vx = vx + P_SPEED end
    if aegis.keyDown("W") or aegis.keyDown("Up")    then vy = vy - P_SPEED end
    if aegis.keyDown("S") or aegis.keyDown("Down")  then vy = vy + P_SPEED end

    -- Gamepad analógico
    local ax = aegis.padAxis(0, "LeftX")
    local ay = aegis.padAxis(0, "LeftY")
    if math.abs(ax) > 0.2 then vx = ax * P_SPEED end
    if math.abs(ay) > 0.2 then vy = ay * P_SPEED end

    -- Normaliza diagonal
    if vx ~= 0 and vy ~= 0 then
        vx = vx * 0.707
        vy = vy * 0.707
    end

    -- Move player pela física para colidir com paredes WORLD.
    aegis.setVelocity(playerRb, vx, vy)

    -- Rotaciona player para o cursor do mouse
    local mx  = aegis.screenToWorldX(aegis.mouseX(), aegis.mouseY())
    local my  = aegis.screenToWorldY(aegis.mouseX(), aegis.mouseY())
    local px  = aegis.getX(player) + P_SIZE
    local py  = aegis.getY(player) + P_SIZE
    local ang = math.atan(my - py, mx - px)
    aegis.setRotation(player, ang)

    -- ── INPUT: atirar ─────────────────────────────────────────────
    local firing = aegis.mouseLeft() or aegis.padDown(0, "RB")
                   or aegis.keyDown("Space")
    if firing and shootTimer <= 0 then
        shoot()
        shootTimer = SHOOT_CD
    end

    -- ── Atualiza balas ────────────────────────────────────────────
    for i = #bullets, 1, -1 do
        local b = bullets[i]
        b.life = b.life - dt
        if b.life <= 0 then
            pendingRemove[#pendingRemove+1] = function()
                aegis.removeObject(b.obj)
                table.remove(bullets, i)
            end
        else
            aegis.move(b.obj, b.vx * dt, b.vy * dt)
        end
    end

    -- ── Atualiza inimigos ─────────────────────────────────────────
    for _, e in ipairs(enemies) do
        -- Pathfinding
        e.pathTimer = e.pathTimer - dt
        if e.pathTimer <= 0 then
            e.pathTimer = PATH_RATE
            e.path = aegis.navFindPath(nav,
                aegis.getX(e.obj), aegis.getY(e.obj),
                aegis.getX(player) + P_SIZE, aegis.getY(player) + P_SIZE)
            e.pathIndex = 2
        end

        -- Segue o caminho
        local moved = false
        if e.path and e.path[e.pathIndex] then
            local p  = e.path[e.pathIndex]
            local ex = aegis.getX(e.obj)
            local ey = aegis.getY(e.obj)
            local dx = p.x - ex
            local dy = p.y - ey
            local d  = math.sqrt(dx*dx + dy*dy)
            if d < 6 then
                e.pathIndex = e.pathIndex + 1
            else
                local spd = ENEMY_SPD * (1 + (GAME.zone - 1) * 0.25)
                aegis.move(e.obj, dx/d * spd * dt, dy/d * spd * dt)
                -- Rotaciona inimigo na direção do movimento
                aegis.setRotation(e.obj, math.atan(dy, dx))
                moved = true
            end
        end

        -- Fallback: se não houver path válido, tenta perseguição direta com LOS.
        if not moved then
            local ex = aegis.getX(e.obj)
            local ey = aegis.getY(e.obj)
            local tx = aegis.getX(player) + P_SIZE
            local ty = aegis.getY(player) + P_SIZE
            local dx = tx - ex
            local dy = ty - ey
            local d  = math.sqrt(dx*dx + dy*dy)
            if d > 1 and aegis.lineOfSight(ex, ey, tx, ty, "WORLD") then
                local spd = ENEMY_SPD * 0.8
                aegis.move(e.obj, dx/d * spd * dt, dy/d * spd * dt)
                aegis.setRotation(e.obj, math.atan(dy, dx))
            end
        end

        local cex = aegis.getX(e.obj)
        local cey = aegis.getY(e.obj)
        local movedDist = math.abs(cex - e.lastX) + math.abs(cey - e.lastY)
        if movedDist < 0.35 then
            e.stuckTime = e.stuckTime + dt
        else
            e.stuckTime = 0
            e.lastX = cex
            e.lastY = cey
        end
        if e.stuckTime > 1.0 then
            -- Destrava inimigo preso em canto: reposiciona levemente e recalcula path.
            aegis.move(e.obj, math.random(-18, 18), math.random(-18, 18))
            e.pathTimer = 0
            e.stuckTime = 0
            e.lastX = aegis.getX(e.obj)
            e.lastY = aegis.getY(e.obj)
        end

        -- Flutuação leve (cosmético)
        e.floatT = e.floatT + dt * 2
        local fy = math.sin(e.floatT) * 2
        -- (aplicado visualmente apenas se quiser offset)

        -- Colisão inimigo + player (dano ao player)
        local epx = aegis.getX(e.obj) - aegis.getX(player)
        local epy = aegis.getY(e.obj) - aegis.getY(player)
        if math.sqrt(epx*epx + epy*epy) < 32 and hurtCooldown <= 0 then
            hurtCooldown = 0.8
            GAME.hp = math.max(0, GAME.hp - 10)
            aegis.flashScreen({ r=1, g=0.1, b=0.1 }, 0.12)
            aegis.screenShake(5, 0.2)
            aegis.tween(player, { alpha=0.3 }, 0.08, "out", function()
                aegis.tween(player, { alpha=1.0 }, 0.18, "out")
            end)
            aegis.playSoundAt("hurt.wav",
                aegis.getX(e.obj), aegis.getY(e.obj), { maxDist=600 })
            update_hud()

            if GAME.hp <= 0 then
                aegis.stopMusic()
                aegis.clearScreenShader()
                aegis.transitionTo("gameover", "fade", 0.4)
                return
            end
        end
    end
end

-- ── aegis_draw ───────────────────────────────────────────────────────

function aegis_draw()
    -- Mini instrução na tela
    aegis.drawText(
        "WASD mover  |  Mouse mirar  |  Clique atirar  |  Esc menu",
        aegis.screenWidth() / 2 - 240,
        aegis.screenHeight() - 28,
        0.45, 0.55, 0.45
    )

    -- Debug de inimigos restantes
    aegis.drawText(
        "Inimigos: " .. #enemies .. "   Balas: " .. #bullets,
        14,
        aegis.screenHeight() - 28,
        0.4, 0.5, 0.4
    )
end
