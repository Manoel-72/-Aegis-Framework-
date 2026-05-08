-- ═══════════════════════════════════════════════════════════════
-- ZONE RECON — Top-Down Shooter com Coleta e Missões
-- Engine: Aegis v0.9
-- Estrutura de cenas:
--   menu  →  game (zona 1/2/3)  →  mission  →  gameover / win
-- ═══════════════════════════════════════════════════════════════

-- Estado global compartilhado entre todas as cenas
GAME = {
    -- Progresso
    zone       = 1,        -- zona atual (1, 2, 3)
    maxZone    = 3,

    -- Inventário de coleta
    inventory  = {
        medkit     = 0,    -- kit médico (restaura vida em missão)
        ammobox    = 0,    -- caixa de munição
        keycard    = 0,    -- cartão de acesso (abre porta final)
        intel      = 0,    -- documento de inteligência (pontos bônus)
    },

    -- Missão ativa
    mission    = nil,      -- tabela com dados da missão atual
    missionOk  = false,    -- missão concluída com sucesso?

    -- Stats
    score      = 0,
    kills      = 0,
    shots      = 0,

    -- Vida
    maxHp      = 100,
    hp         = 100,
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
        aegis.transitionTo("menu", "none", 0.01)
    end
end

function aegis_draw() end
