-- ═══════════════════════════════════════
-- CENA: MENU PRINCIPAL
-- ═══════════════════════════════════════
local bg, titulo, sub, hint, hs_label
local pulse_dir = 1
local pulse_t   = 0

function aegis_init()
    aegis.clearAll()
    local sw = aegis.screenWidth()
    local sh = aegis.screenHeight()

    -- Reseta estado do jogo ao voltar ao menu
    GAME.zone      = 1
    GAME.score     = 0
    GAME.kills     = 0
    GAME.shots     = 0
    GAME.hp        = GAME.maxHp
    GAME.missionOk = false
    GAME.mission   = nil
    GAME.inventory = { medkit=0, ammobox=0, keycard=0, intel=0 }

    -- Fundo escuro
    bg = aegis.newRect(sw, sh, 0.05, 0.07, 0.10)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)

    -- Título com tween de pulso
    titulo = aegis.newLabel("ZONE RECON")
    aegis.setPosition(titulo, sw / 2, sh * 0.28)
    aegis.setColor(titulo, 0.3, 1.0, 0.5)
    aegis.tween(titulo, { scaleX=1.06, scaleY=1.06 }, 0.9, "inout", nil,
        { loop=true, yoyo=true })

    sub = aegis.newLabel("Top-Down Shooter | Coleta | Missões")
    aegis.setPosition(sub, sw / 2, sh * 0.42)
    aegis.setColor(sub, 0.6, 0.8, 0.6)

    hint = aegis.newLabel("ENTER ou SPACE para iniciar")
    aegis.setPosition(hint, sw / 2, sh * 0.62)
    aegis.setColor(hint, 0.8, 0.8, 0.8)

    -- Controles
    local ctrl = aegis.newLabel("WASD mover  |  Mouse mirar  |  Clique atirar  |  E coletar")
    aegis.setPosition(ctrl, sw / 2, sh * 0.74)
    aegis.setColor(ctrl, 0.45, 0.55, 0.45)

    -- High score
    local hs = aegis.load("highscore") or 0
    hs_label = aegis.newLabel("Recorde: " .. hs .. " pts")
    aegis.setPosition(hs_label, sw / 2, sh * 0.85)
    aegis.setColor(hs_label, 0.4, 0.7, 1.0)

    aegis.fadeIn(0.5)
    aegis.playMusic("audio/ambient.ogg")
    aegis.setMusicVolume(0.4)
end

function aegis_update(dt)
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space")
    or aegis.padPressed(0, "Start") or aegis.padPressed(0, "A") then
        aegis.stopMusic()
        aegis.transitionTo("game", "fade", 0.4)
    end
end

function aegis_draw() end
