local player = nil
local title = nil
local speed = 260

function aegis_init()
    aegis.clearAll()

    title = aegis.create("label", {
        text = "Meu Jogo Aegis",
        size = 30,
        x = 32,
        y = 28,
        hud = true
    })

    aegis.create("label", {
        text = "Use WASD ou setas para mover. ESC fecha a janela.",
        size = 18,
        x = 32,
        y = 68,
        hud = true
    })

    player = aegis.create("rect", {
        width = 48,
        height = 48,
        color = "#3b82f6",
        x = 616,
        y = 336,
        z = 10
    })
end

function aegis_update(dt)
    if player == nil then return end

    local dx = 0
    local dy = 0

    if aegis.keyDown("Right") or aegis.keyDown("D") then dx = dx + 1 end
    if aegis.keyDown("Left") or aegis.keyDown("A") then dx = dx - 1 end
    if aegis.keyDown("Down") or aegis.keyDown("S") then dy = dy + 1 end
    if aegis.keyDown("Up") or aegis.keyDown("W") then dy = dy - 1 end

    local x = aegis.getX(player) + dx * speed * dt
    local y = aegis.getY(player) + dy * speed * dt

    x = math.max(0, math.min(aegis.screenWidth() - aegis.getWidth(player), x))
    y = math.max(110, math.min(aegis.screenHeight() - aegis.getHeight(player), y))

    aegis.setPosition(player, x, y)
end
