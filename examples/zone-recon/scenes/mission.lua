-- CENA: MISSION DEBRIEFING
local wait_t = 0

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
    aegis.log("[scene] mission init")
    if aegis.clearAll then aegis.clearAll() end

    local bg = aegis.newRect(1280, 720, 0.06, 0.08, 0.12)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    local m = GAME.mission
    local titulo = aegis.newLabel(m and m.title or "DEBRIEFING")
    aegis.setPosition(titulo, 420, 50)
    aegis.setColor(titulo, 0.3, 1.0, 0.5)

    local sub = aegis.newLabel("MISSAO CONCLUIDA")
    aegis.setPosition(sub, 480, 100)
    aegis.setColor(sub, 0.6, 1.0, 0.7)

    local stats = {
        "Inimigos eliminados:  " .. GAME.kills,
        "Tiros disparados:     " .. GAME.shots,
        "Precisao:             " .. (GAME.shots > 0 and math.floor(GAME.kills / GAME.shots * 100) or 0) .. " %",
    }
    for i, txt in ipairs(stats) do
        local l = aegis.newLabel(txt)
        aegis.setPosition(l, 200, 160 + (i-1) * 32)
        aegis.setColor(l, 0.7, 0.85, 0.7)
    end

    local inv_title = aegis.newLabel("INVENTARIO COLETADO")
    aegis.setPosition(inv_title, 700, 160)
    aegis.setColor(inv_title, 0.5, 0.7, 1.0)

    local kinds  = { "medkit", "ammobox", "keycard", "intel" }
    local labels = { "Medkit", "Municao", "Keycard", "Intel" }
    for i, k in ipairs(kinds) do
        local qty = GAME.inventory[k] or 0
        local l = aegis.newLabel(labels[i] .. ":  " .. qty)
        local co = color_for(k)
        aegis.setPosition(l, 700, 200 + (i-1) * 32)
        aegis.setColor(l, co[1], co[2], co[3])
    end

    local reward = m and m.reward or 0
    GAME.score = GAME.score + reward

    local rwd_lbl = aegis.newLabel("Recompensa:  +" .. reward .. " pts")
    aegis.setPosition(rwd_lbl, 460, 430)
    aegis.setColor(rwd_lbl, 1.0, 0.9, 0.3)

    local score_lbl = aegis.newLabel("Score total:  " .. GAME.score)
    aegis.setPosition(score_lbl, 460, 470)
    aegis.setColor(score_lbl, 0.8, 1.0, 0.8)

    if GAME.inventory.medkit > 0 then
        local heal_hint = aegis.newLabel("R: Usar Medkit  (HP " .. GAME.hp .. " -> " .. math.min(GAME.maxHp, GAME.hp + 40) .. ")")
        aegis.setPosition(heal_hint, 400, 530)
        aegis.setColor(heal_hint, 0.3, 1.0, 0.5)
    end

    local next_txt = GAME.zone >= GAME.maxZone
        and "Enter: Ver resultado final"
        or  "Enter: Avancar para Zona " .. (GAME.zone + 1)
    local next_lbl = aegis.newLabel(next_txt)
    aegis.setPosition(next_lbl, 420, 600)
    aegis.setColor(next_lbl, 0.6, 0.8, 1.0)

    wait_t = 0.5
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end

    if aegis.keyPressed("R") and GAME.inventory.medkit > 0 then
        GAME.inventory.medkit = GAME.inventory.medkit - 1
        GAME.hp = math.min(GAME.maxHp, GAME.hp + 40)
    end

    if aegis.keyPressed("Enter") or aegis.keyPressed("Space")
    or aegis.padPressed(0, "Start") or aegis.padPressed(0, "A") then
        if GAME.zone >= GAME.maxZone then
            aegis.transitionTo("win", "fade", 0.4)
        else
            GAME.zone      = GAME.zone + 1
            GAME.missionOk = false
            GAME.mission   = nil
            aegis.transitionTo("game", "fade", 0.4)
        end
    end
end

function aegis_draw() end
