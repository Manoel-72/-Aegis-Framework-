-- CENA: GAME OVER
local wait_t = 0

function aegis_init()
    aegis.log("[scene] gameover init")
    if aegis.clearAll then aegis.clearAll() end

    local hs = aegis.load("highscore") or 0
    if GAME.score > hs then
        aegis.save("highscore", GAME.score)
        hs = GAME.score
    end

    local bg = aegis.newRect(1280, 720, 0.10, 0.04, 0.04)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    local titulo = aegis.newLabel("GAME OVER")
    aegis.setPosition(titulo, 530, 160)
    aegis.setColor(titulo, 1.0, 0.2, 0.2)
    if aegis.tween then
        aegis.tween(titulo, { scaleX=1.08, scaleY=1.08 }, 0.6, "inout", nil, { loop=true, yoyo=true })
    end

    local zona_lbl = aegis.newLabel("Eliminado na Zona " .. GAME.zone)
    aegis.setPosition(zona_lbl, 490, 260)
    aegis.setColor(zona_lbl, 0.9, 0.5, 0.5)

    local score_lbl = aegis.newLabel("Score: " .. GAME.score)
    aegis.setPosition(score_lbl, 530, 320)
    aegis.setColor(score_lbl, 1.0, 0.9, 0.6)

    local kills_lbl = aegis.newLabel("Eliminacoes: " .. GAME.kills)
    aegis.setPosition(kills_lbl, 510, 370)
    aegis.setColor(kills_lbl, 0.9, 0.7, 0.5)

    local hs_lbl = aegis.newLabel("Recorde: " .. hs)
    aegis.setPosition(hs_lbl, 530, 420)
    aegis.setColor(hs_lbl, 0.4, 0.7, 1.0)

    local hint = aegis.newLabel("Enter: Tentar de novo  |  Esc: Menu")
    aegis.setPosition(hint, 400, 540)
    aegis.setColor(hint, 0.6, 0.6, 0.6)

    wait_t = 0.6
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space") or aegis.padPressed(0, "Start") then
        aegis.transitionTo("game", "fade", 0.35)
    end
    if aegis.keyPressed("Escape") or aegis.padPressed(0, "Back") then
        aegis.transitionTo("menu", "fade", 0.35)
    end
end

function aegis_draw() end
