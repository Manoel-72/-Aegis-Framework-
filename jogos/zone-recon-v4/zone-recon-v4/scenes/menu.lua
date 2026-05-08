-- CENA: MENU PRINCIPAL
local bg, titulo, sub, hint, ctrl, hs_label

function aegis_init()
    aegis.log("[scene] menu init")
    if aegis.clearAll then aegis.clearAll() end

    GAME.zone      = 1
    GAME.score     = 0
    GAME.kills     = 0
    GAME.shots     = 0
    GAME.hp        = GAME.maxHp
    GAME.missionOk = false
    GAME.mission   = nil
    GAME.inventory = { medkit=0, ammobox=0, keycard=0, intel=0 }

    bg = aegis.newRect(1280, 720, 0.05, 0.07, 0.10)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    titulo = aegis.newLabel("ZONE RECON")
    aegis.setPosition(titulo, 440, 160)
    aegis.setColor(titulo, 0.3, 1.0, 0.5)

    sub = aegis.newLabel("Top-Down Shooter | Coleta | Missoes")
    aegis.setPosition(sub, 390, 220)
    aegis.setColor(sub, 0.6, 0.8, 0.6)

    hint = aegis.newLabel("ENTER ou SPACE para iniciar")
    aegis.setPosition(hint, 430, 320)
    aegis.setColor(hint, 0.8, 0.8, 0.8)

    ctrl = aegis.newLabel("WASD mover  |  Mouse mirar  |  Clique atirar")
    aegis.setPosition(ctrl, 360, 400)
    aegis.setColor(ctrl, 0.45, 0.55, 0.45)

    local hs = aegis.load("highscore") or 0
    hs_label = aegis.newLabel("Recorde: " .. hs .. " pts")
    aegis.setPosition(hs_label, 460, 480)
    aegis.setColor(hs_label, 0.4, 0.7, 1.0)

    if aegis.tween then
        aegis.tween(titulo, { scaleX=1.06, scaleY=1.06 }, 0.9, "inout", nil, { loop=true, yoyo=true })
    end

    aegis.log("[scene] menu pronto")
end

function aegis_update(dt)
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space")
    or aegis.padPressed(0, "Start") or aegis.padPressed(0, "A") then
        aegis.transitionTo("game", "fade", 0.4)
    end
end

function aegis_draw() end
