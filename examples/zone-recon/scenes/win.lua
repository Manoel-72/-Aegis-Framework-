-- CENA: WIN
local wait_t = 0

function aegis_init()
    aegis.log("[scene] win init")
    if aegis.clearAll then aegis.clearAll() end

    local hs = aegis.load("highscore") or 0
    if GAME.score > hs then
        aegis.save("highscore", GAME.score)
        hs = GAME.score
    end

    local bg = aegis.newRect(1280, 720, 0.04, 0.10, 0.06)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    local titulo = aegis.newLabel("MISSAO COMPLETA")
    aegis.setPosition(titulo, 460, 100)
    aegis.setColor(titulo, 0.3, 1.0, 0.5)
    if aegis.tween then
        aegis.tween(titulo, { scaleX=1.07, scaleY=1.07 }, 0.8, "inout", nil, { loop=true, yoyo=true })
    end

    local sub = aegis.newLabel("Todas as zonas liberadas!")
    aegis.setPosition(sub, 460, 160)
    aegis.setColor(sub, 0.6, 0.9, 0.7)

    local stats = {
        { txt="Score Final:      " .. GAME.score,  r=1.0, g=0.95, b=0.4 },
        { txt="Recorde:          " .. hs,           r=0.4, g=0.7,  b=1.0 },
        { txt="Eliminacoes:      " .. GAME.kills,   r=1.0, g=0.7,  b=0.4 },
        { txt="Tiros disparados: " .. GAME.shots,   r=0.7, g=0.8,  b=0.7 },
        { txt="HP restante:      " .. GAME.hp,      r=0.4, g=1.0,  b=0.5 },
    }
    for i, s in ipairs(stats) do
        local l = aegis.newLabel(s.txt)
        aegis.setPosition(l, 420, 220 + (i-1) * 38)
        aegis.setColor(l, s.r, s.g, s.b)
    end

    local hint = aegis.newLabel("Enter: Jogar de novo  |  Esc: Menu")
    aegis.setPosition(hint, 400, 580)
    aegis.setColor(hint, 0.6, 0.8, 0.6)

    wait_t = 0.8
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space") or aegis.padPressed(0, "Start") then
        aegis.transitionTo("game", "fade", 0.4)
    end
    if aegis.keyPressed("Escape") or aegis.padPressed(0, "Back") then
        aegis.transitionTo("menu", "fade", 0.4)
    end
end

function aegis_draw() end
