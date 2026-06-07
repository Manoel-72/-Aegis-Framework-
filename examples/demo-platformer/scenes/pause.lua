local panel, title, hint, menuHint, data

function aegis_init()
    data = aegis.sceneData and aegis.sceneData() or nil

    local sw = aegis.screenWidth()
    local sh = aegis.screenHeight()
    panel = aegis.newRect(sw, sh, 0.02, 0.04, 0.07, true)
    aegis.setAlpha(panel, 0.78)
    aegis.setPosition(panel, 0, 0)
    aegis.setZ(panel, 200)

    title = aegis.newLabelSize("PAUSADO", 36, true)
    hint = aegis.newLabelSize("Esc / P / Start: voltar ao jogo", 20, true)
    menuHint = aegis.newLabelSize("M: voltar ao menu", 18, true)

    aegis.setColor(title, 0.65, 1.0, 0.72, 1)
    aegis.setColor(hint, 0.88, 0.92, 0.95, 1)
    aegis.setColor(menuHint, 0.75, 0.82, 0.86, 1)

    aegis.setPosition(title, sw * 0.5 - 90, sh * 0.5 - 70)
    aegis.setPosition(hint, sw * 0.5 - 160, sh * 0.5 - 18)
    aegis.setPosition(menuHint, sw * 0.5 - 82, sh * 0.5 + 20)
end

function aegis_update(dt)
    if aegis.keyPressed("Escape") or aegis.keyPressed("P") or aegis.padPressed(0, "Start") then
        aegis.popScene()
        return
    end

    if aegis.keyPressed("M") or aegis.padPressed(0, "Back") then
        aegis.popScene()
        aegis.stopMusic()
        aegis.transitionTo("menu", "fade", 0.25, data)
    end
end

function aegis_draw() end
function aegis_draw_ui() end
