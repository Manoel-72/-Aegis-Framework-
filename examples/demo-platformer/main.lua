-- Aegis demo platformer: registra cenas e abre menu.
GAME = GAME or { score = 0, lives = 3, level = 1, maxLevel = 9, tutorialSeen = false }
GAME.maxLevel = 9
local _booted = false

function aegis_init()
    aegis.log("[scene] boot main.lua init")
    aegis.registerScene("menu", "scenes/menu.lua")
    for i = 1, GAME.maxLevel do
        aegis.registerScene("level" .. tostring(i), "scenes/level.lua")
    end
    aegis.registerScene("pause", "scenes/pause.lua")
    aegis.registerScene("gameover", "scenes/gameover.lua")
    _booted = false
end

function aegis_update(dt)
    if not _booted then
        _booted = true
        aegis.log("[scene] main -> transitionTo(menu)")
        aegis.transitionTo("menu", "none", 0.01)
    end
end
function aegis_draw() end
