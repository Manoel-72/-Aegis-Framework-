local title, info, hint

function aegis_init()
    if aegis.clearAll then aegis.clearAll() end

    title = aegis.newLabel("TOPDOWN TEMPLATE")
    info = aegis.newLabel("Enter/Space/A: iniciar")
    hint = aegis.newLabel("WASD/Setas movem  |  Esc volta")
    aegis.setColor(title, 0.65, 1.0, 0.72)
    aegis.setColor(info, 0.85, 0.9, 0.85)
    aegis.setColor(hint, 0.8, 0.82, 0.9)

    aegis.setPosition(title, 420, 220)
    aegis.setPosition(info, 430, 270)
    aegis.setPosition(hint, 360, 310)
end

function aegis_update(dt)
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space") or aegis.padPressed(0, "A") then
        aegis.transitionTo("game", "fade", 0.2)
    end
end

function aegis_draw() end
