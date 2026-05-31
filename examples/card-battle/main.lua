GAME = {
    score = 0,
    kills = 0,
    cardContinue = false,
}

local booted = false

function aegis_init()
    aegis.log("[card-battle] boot")
    aegis.registerScene("menu", "scenes/menu.lua")
    aegis.registerScene("game", "scenes/game.lua")
    aegis.registerScene("win", "scenes/win.lua")
    aegis.registerScene("gameover", "scenes/gameover.lua")
    booted = false
end

function aegis_update(dt)
    if not booted then
        booted = true
        aegis.transitionTo("menu", "none", 0.01)
    end
end

function aegis_draw() end
