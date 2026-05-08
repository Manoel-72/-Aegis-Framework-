-- ═══════════════════════════════════════════════════════════════
-- CENA: MISSION — Debriefing pós-zona
-- Mostra itens coletados, recompensa, e permite usar medkits
-- ═══════════════════════════════════════════════════════════════
local bg, titulo, sub
local item_rows   = {}
local reward_done = false
local wait_t      = 0

local function color_for(kind)
    local c = {
        medkit  = { 0.2, 1.0, 0.4 },
        ammobox = { 1.0, 0.8, 0.1 },
        keycard = { 0.3, 0.6, 1.0 },
        intel   = { 1.0, 0.4, 0.8 },
    }
    return c[kind] or { 1, 1, 1 }
end

function aegis_init()
    aegis.clearAll()
    local sw = aegis.screenWidth()
    local sh = aegis.screenHeight()

    bg = aegis.newRect(sw, sh, 0.06, 0.08, 0.12)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    local m = GAME.mission

    -- Título
    titulo = aegis.newLabel(m and m.title or "DEBRIEFING")
    aegis.setPosition(titulo, sw / 2, sh * 0.10)
    aegis.setColor(titulo, 0.3, 1.0, 0.5)

    sub = aegis.newLabel("✓ MISSÃO CONCLUÍDA")
    aegis.setPosition(sub, sw / 2, sh * 0.19)
    aegis.setColor(sub, 0.6, 1.0, 0.7)

    -- Linha separadora (visual)
    local sep = aegis.newRect(sw - 200, 2, 0.2, 0.35, 0.25)
    aegis.setPosition(sep, 100, sh * 0.26)

    -- Stats da zona
    local stats = {
        "Inimigos eliminados:  " .. GAME.kills,
        "Tiros disparados:     " .. GAME.shots,
        "Precisão:             " .. (GAME.shots > 0
            and math.floor(GAME.kills / GAME.shots * 100) or 0) .. " %",
    }
    for i, txt in ipairs(stats) do
        local l = aegis.newLabel(txt)
        aegis.setPosition(l, sw * 0.25, sh * 0.30 + (i-1) * 28)
        aegis.setColor(l, 0.7, 0.85, 0.7)
    end

    -- Inventário coletado
    local inv_title = aegis.newLabel("INVENTÁRIO COLETADO")
    aegis.setPosition(inv_title, sw * 0.65, sh * 0.30)
    aegis.setColor(inv_title, 0.5, 0.7, 1.0)

    local kinds  = { "medkit", "ammobox", "keycard", "intel" }
    local labels = { "Medkit", "Munição", "Keycard", "Intel" }
    for i, k in ipairs(kinds) do
        local qty = GAME.inventory[k] or 0
        local txt = labels[i] .. ":  " .. qty
        local l   = aegis.newLabel(txt)
        local co  = color_for(k)
        aegis.setPosition(l, sw * 0.65, sh * 0.38 + (i-1) * 28)
        aegis.setColor(l, co[1], co[2], co[3])
        item_rows[#item_rows+1] = l
    end

    -- Recompensa
    local reward = m and m.reward or 0
    GAME.score   = GAME.score + reward

    local rwd_lbl = aegis.newLabel("Recompensa de missão:  +" .. reward .. " pts")
    aegis.setPosition(rwd_lbl, sw / 2, sh * 0.60)
    aegis.setColor(rwd_lbl, 1.0, 0.9, 0.3)
    aegis.tween(rwd_lbl, { scaleX=1.12, scaleY=1.12 }, 0.5, "out", nil,
        { loop=true, yoyo=true })

    -- Score total
    local score_lbl = aegis.newLabel("Score total:  " .. GAME.score)
    aegis.setPosition(score_lbl, sw / 2, sh * 0.68)
    aegis.setColor(score_lbl, 0.8, 1.0, 0.8)

    -- Usar medkit (R)
    if GAME.inventory.medkit > 0 then
        local heal_hint = aegis.newLabel("R — Usar Medkit  (HP " ..
            GAME.hp .. " → " ..
            math.min(GAME.maxHp, GAME.hp + 40) .. ")")
        aegis.setPosition(heal_hint, sw / 2, sh * 0.78)
        aegis.setColor(heal_hint, 0.3, 1.0, 0.5)
    end

    -- Continuar
    local next_txt = GAME.zone >= GAME.maxZone
        and "Enter — Ver resultado final"
        or  "Enter — Avançar para Zona " .. (GAME.zone + 1)
    local next_lbl = aegis.newLabel(next_txt)
    aegis.setPosition(next_lbl, sw / 2, sh * 0.88)
    aegis.setColor(next_lbl, 0.6, 0.8, 1.0)

    aegis.fadeIn(0.4)
    aegis.playSound("mission_complete.wav")

    -- Partículas de celebração
    aegis.burst(sw / 2, sh * 0.5, {
        count=35, speed=160, life=1.0, size=5,
        r=0.3, g=1.0, b=0.5, spread=360
    })

    wait_t = 0.5   -- pequeno delay antes de aceitar input
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end

    -- Usar medkit
    if aegis.keyPressed("R") and GAME.inventory.medkit > 0 then
        GAME.inventory.medkit = GAME.inventory.medkit - 1
        GAME.hp = math.min(GAME.maxHp, GAME.hp + 40)
        aegis.playSound("collect.wav")
        aegis.log("[mission] medkit usado, HP agora " .. GAME.hp)
    end

    -- Avançar
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space")
    or aegis.padPressed(0, "Start") or aegis.padPressed(0, "A") then
        if GAME.zone >= GAME.maxZone then
            -- Fim do jogo — vitória
            aegis.transitionTo("win", "fade", 0.4)
        else
            -- Próxima zona
            GAME.zone      = GAME.zone + 1
            GAME.missionOk = false
            GAME.mission   = nil
            aegis.transitionTo("game", "fade", 0.4)
        end
    end
end

function aegis_draw() end
