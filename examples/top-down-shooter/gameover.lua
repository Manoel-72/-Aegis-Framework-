-- ═══════════════════════════════════════
-- CENA: GAME OVER
-- ═══════════════════════════════════════
local wait_t = 0

function aegis_init()
    aegis.clearAll()
    local sw = aegis.screenWidth()
    local sh = aegis.screenHeight()

    local bg = aegis.newRect(sw, sh, 0.10, 0.04, 0.04)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    -- High score
    local hs = aegis.load("highscore") or 0
    if GAME.score > hs then
        aegis.save("highscore", GAME.score)
        hs = GAME.score
    end

    local titulo = aegis.newLabel("GAME OVER")
    aegis.setPosition(titulo, sw / 2, sh * 0.25)
    aegis.setColor(titulo, 1.0, 0.2, 0.2)
    aegis.tween(titulo, { scaleX=1.08, scaleY=1.08 }, 0.6, "inout", nil,
        { loop=true, yoyo=true })

    local zona_lbl = aegis.newLabel("Eliminado na Zona " .. GAME.zone)
    aegis.setPosition(zona_lbl, sw / 2, sh * 0.40)
    aegis.setColor(zona_lbl, 0.9, 0.5, 0.5)

    local score_lbl = aegis.newLabel("Score: " .. GAME.score)
    aegis.setPosition(score_lbl, sw / 2, sh * 0.52)
    aegis.setColor(score_lbl, 1.0, 0.9, 0.6)

    local kills_lbl = aegis.newLabel("Eliminações: " .. GAME.kills)
    aegis.setPosition(kills_lbl, sw / 2, sh * 0.61)
    aegis.setColor(kills_lbl, 0.9, 0.7, 0.5)

    local hs_lbl = aegis.newLabel("Recorde: " .. hs)
    aegis.setPosition(hs_lbl, sw / 2, sh * 0.70)
    aegis.setColor(hs_lbl, 0.4, 0.7, 1.0)

    local hint = aegis.newLabel("Enter — Tentar de novo  |  Esc — Menu")
    aegis.setPosition(hint, sw / 2, sh * 0.85)
    aegis.setColor(hint, 0.6, 0.6, 0.6)

    aegis.fadeIn(0.4)
    aegis.burst(sw / 2, sh / 2, {
        count=50, speed=180, life=1.2, size=5,
        r=1.0, g=0.2, b=0.1, spread=360
    })
    wait_t = 0.6
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end

    if aegis.keyPressed("Enter") or aegis.keyPressed("Space")
    or aegis.padPressed(0, "Start") then
        aegis.transitionTo("game", "fade", 0.35)
    end
    if aegis.keyPressed("Escape") or aegis.padPressed(0, "Back") then
        aegis.transitionTo("menu", "fade", 0.35)
    end
end

function aegis_draw() end
