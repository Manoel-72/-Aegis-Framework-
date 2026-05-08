-- ═══════════════════════════════════════
-- CENA: WIN — Vitória final
-- ═══════════════════════════════════════
local wait_t = 0

function aegis_init()
    aegis.clearAll()
    local sw = aegis.screenWidth()
    local sh = aegis.screenHeight()

    local bg = aegis.newRect(sw, sh, 0.04, 0.10, 0.06)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    -- High score final
    local hs = aegis.load("highscore") or 0
    if GAME.score > hs then
        aegis.save("highscore", GAME.score)
        hs = GAME.score
    end

    local titulo = aegis.newLabel("MISSÃO COMPLETA")
    aegis.setPosition(titulo, sw / 2, sh * 0.18)
    aegis.setColor(titulo, 0.3, 1.0, 0.5)
    aegis.tween(titulo, { scaleX=1.07, scaleY=1.07 }, 0.8, "inout", nil,
        { loop=true, yoyo=true })

    local sub = aegis.newLabel("Todas as zonas liberadas. Operação concluída.")
    aegis.setPosition(sub, sw / 2, sh * 0.30)
    aegis.setColor(sub, 0.6, 0.9, 0.7)

    -- Stats finais
    local stats = {
        { txt = "Score Final:       " .. GAME.score,  r=1.0, g=0.95, b=0.4 },
        { txt = "Recorde:           " .. hs,           r=0.4, g=0.7,  b=1.0 },
        { txt = "Eliminações:       " .. GAME.kills,   r=1.0, g=0.7,  b=0.4 },
        { txt = "Tiros disparados:  " .. GAME.shots,   r=0.7, g=0.8,  b=0.7 },
        { txt = "HP restante:       " .. GAME.hp,      r=0.4, g=1.0,  b=0.5 },
    }
    for i, s in ipairs(stats) do
        local l = aegis.newLabel(s.txt)
        aegis.setPosition(l, sw / 2, sh * 0.40 + (i-1) * 32)
        aegis.setColor(l, s.r, s.g, s.b)
    end

    -- Inventário final
    local inv = GAME.inventory
    local inv_txt = "Medkits restantes: " .. inv.medkit ..
                    "  |  Keycards: " .. inv.keycard ..
                    "  |  Intel: " .. inv.intel
    local inv_l = aegis.newLabel(inv_txt)
    aegis.setPosition(inv_l, sw / 2, sh * 0.73)
    aegis.setColor(inv_l, 0.5, 0.75, 0.6)

    local hint = aegis.newLabel("Enter — Jogar de novo  |  Esc — Menu")
    aegis.setPosition(hint, sw / 2, sh * 0.88)
    aegis.setColor(hint, 0.6, 0.8, 0.6)

    aegis.fadeIn(0.5)
    -- Chuva de partículas de vitória
    for i = 1, 4 do
        local cx = (i / 5) * sw
        aegis.burst(cx, sh * 0.5, {
            count=25, speed=150, life=1.0, size=5,
            r = math.random(), g = 1.0, b = math.random() * 0.5,
            spread=360
        })
    end
    wait_t = 0.8
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end

    if aegis.keyPressed("Enter") or aegis.keyPressed("Space")
    or aegis.padPressed(0, "Start") then
        aegis.transitionTo("game", "fade", 0.4)
    end
    if aegis.keyPressed("Escape") or aegis.padPressed(0, "Back") then
        aegis.transitionTo("menu", "fade", 0.4)
    end
end

function aegis_draw() end
