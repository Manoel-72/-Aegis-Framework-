GAME = {
    zone      = 1,   maxZone = 3,
    inventory = { medkit=0, ammobox=0, keycard=0, intel=0 },
    mission   = nil, missionOk = false,
    score     = 0,   kills = 0, shots = 0,
    maxHp     = 100, hp    = 100,
}

local _booted = false

function aegis_init()
    aegis.log("[main] boot Zone Recon")
    aegis.registerScene("menu",     "scenes/menu.lua")
    aegis.registerScene("game",     "scenes/game.lua")
    aegis.registerScene("mission",  "scenes/mission.lua")
    aegis.registerScene("gameover", "scenes/gameover.lua")
    aegis.registerScene("win",      "scenes/win.lua")
    _booted = false
end

function aegis_update(dt)
    if not _booted then
        _booted = true
        aegis.log("[main] -> transitionTo(menu)")
        aegis.transitionTo("menu", "none", 0.01)
    end
end

function aegis_draw() end
