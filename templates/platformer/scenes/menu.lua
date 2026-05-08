-- Menu principal: teclado + gamepad, layout com FlowContainer.
local title, subtitle, menu

function aegis_init()
    aegis.clearAll()
    GAME.score = GAME.score or 0
    GAME.lives = 3
    GAME.level = 1
    GAME.won = false
    aegis.setScreenShader("vignette", { intensity = 0.45 })

    title = aegis.newLabel("AEGIS DEMO PLATFORMER")
    subtitle = aegis.newLabel("Enter/Start: jogar  |  Esc: sair")
    aegis.setColor(title, 0.65, 1.0, 0.72)
    aegis.setColor(subtitle, 0.8, 0.9, 0.84)

    menu = aegis.newFlow("vertical", { gap = 20, padding = 16, align = "center" })
    aegis.flowAdd(menu, title)
    aegis.flowAdd(menu, subtitle)
    aegis.setPosition(menu, aegis.screenWidth()/2 - 210, aegis.screenHeight()/2 - 90)
    aegis.flowLayout(menu)

    aegis.tween(title, { scaleX = 1.08, scaleY = 1.08 }, 0.7, "inout", nil, { loop = true, yoyo = true })
end

function aegis_update(dt)
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space") or aegis.padPressed(0, "Start") or aegis.padPressed(0, "A") then
        GAME.score = 0
        GAME.lives = 3
        GAME.level = 1
        aegis.clearScreenShader()
        aegis.transitionTo("level1", "fade", 0.35)
    end
end
function aegis_draw() end
