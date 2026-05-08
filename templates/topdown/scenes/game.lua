local player, speed

function aegis_init()
    if aegis.clearAll then aegis.clearAll() end
    player = aegis.newRect(40, 40, 0.4, 1.0, 0.6)
    aegis.setPosition(player, 120, 120)
    speed = 220
end

function aegis_update(dt)
    local x, y = aegis.getX(player), aegis.getY(player)
    if aegis.keyDown("A") or aegis.keyDown("Left") then x = x - speed * dt end
    if aegis.keyDown("D") or aegis.keyDown("Right") then x = x + speed * dt end
    if aegis.keyDown("W") or aegis.keyDown("Up") then y = y - speed * dt end
    if aegis.keyDown("S") or aegis.keyDown("Down") then y = y + speed * dt end
    aegis.setPosition(player, x, y)

    if aegis.keyPressed("Escape") or aegis.padPressed(0, "Back") then
        aegis.transitionTo("menu", "fade", 0.2)
    end
end

function aegis_draw()
    aegis.drawText("Aegis template: topdown", 20, 20, 0.7, 1.0, 0.7)
end
