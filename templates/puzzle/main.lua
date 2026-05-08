-- Template puzzle da Aegis Engine.
-- Main mínimo: registra cenas e entra no menu com boot seguro.
local _booted = false

function aegis_init()
    aegis.log("[scene] boot puzzle template")
    aegis.registerScene("menu", "scenes/menu.lua")
    aegis.registerScene("game", "scenes/game.lua")
    _booted = false
end

function aegis_update(dt)
    if not _booted then
        _booted = true
        aegis.log("[scene] puzzle main -> menu")
        aegis.transitionTo("menu", "none", 0.01)
    end
end

function aegis_draw() end
