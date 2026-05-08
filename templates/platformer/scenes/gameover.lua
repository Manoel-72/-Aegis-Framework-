local root, title, info
function aegis_init()
    aegis.clearAll()
    aegis.setScreenShader("crt", { intensity = 0.25 })
    local msg = GAME.won and "VOCÊ VENCEU" or "GAME OVER"
    title = aegis.newLabel(msg)
    info = aegis.newLabel("Score: " .. tostring(GAME.score or 0) .. "  |  Enter/Start para voltar")
    aegis.setColor(title, 1.0, 0.35, 0.35)
    root = aegis.newFlow("vertical", { gap = 18, padding = 12, align = "center" })
    aegis.flowAdd(root, title)
    aegis.flowAdd(root, info)
    aegis.setPosition(root, aegis.screenWidth()/2 - 180, aegis.screenHeight()/2 - 70)
    aegis.flowLayout(root)
    aegis.burst(aegis.screenWidth()/2, aegis.screenHeight()/2, { count=45, speed=120, life=0.8, size=4, r=1, g=0.2, b=0.2 })
end
function aegis_update(dt)
    if aegis.keyPressed("Enter") or aegis.padPressed(0, "Start") then
        aegis.clearScreenShader()
        aegis.transitionTo("menu", "fade", 0.35)
    end
end
function aegis_draw() end
