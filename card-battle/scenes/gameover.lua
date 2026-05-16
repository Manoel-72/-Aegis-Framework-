local wait_t = 0
local ui_font = nil

local function load_ui_font()
    if not (aegis.loadFont and aegis.setFont) then return end
    local candidates = {
        "Inter-Regular.ttf",
        "Roboto-Regular.ttf",
        "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arial.ttf"
    }
    for _, path in ipairs(candidates) do
        local ok, font = pcall(aegis.loadFont, path, 22)
        if ok and font then
            ui_font = font
            return
        end
    end
end

local function sfx(name, volume, pitch)
    if aegis.playSoundEx then
        pcall(aegis.playSoundEx, name, volume or 0.45, pitch or 0, 0)
    elseif aegis.playSound then
        pcall(aegis.playSound, name)
    end
end

local function label(text, x, y, r, g, b)
    local l = aegis.newLabel(text)
    if ui_font then pcall(aegis.setFont, l, ui_font) end
    aegis.setPosition(l, x, y)
    aegis.setColor(l, r or 1, g or 1, b or 1)
    return l
end

function aegis_init()
    if aegis.clearAll then aegis.clearAll() end
    if aegis.setCameraOff then aegis.setCameraOff() end
    load_ui_font()
    sfx("lose.wav", 0.38, 0)

    local bg = aegis.newRect(1280, 720, 0.08, 0.035, 0.04)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)
    local table_top = aegis.newRect(900, 360, 0.13, 0.06, 0.075)
    aegis.setPosition(table_top, 190, 210)
    aegis.setZ(table_top, -5)

    local title = label("DERROTA NO DUELO", 420, 120, 1.00, 0.32, 0.32)
    aegis.setScale(title, 1.30, 1.30)
    if aegis.tween then
        aegis.tween(title, { scaleX=1.38, scaleY=1.38 }, 0.7, "inout", nil, { loop=true, yoyo=true })
    end
    if aegis.flashScreen then aegis.flashScreen({ r=0.9, g=0.1, b=0.1 }, 0.12) end
    label("Seu personagem chegou a zero de vida.", 390, 220, 0.90, 0.78, 0.76)
    label("Score: " .. GAME.score, 390, 272, 0.95, 0.82, 0.40)
    label("Dica: use cartas de defesa antes de ficar com pouca vida.", 390, 324, 0.90, 0.78, 0.76)
    label("Enter: tentar de novo", 390, 410, 0.90, 0.78, 0.76)
    label("Esc: menu", 390, 462, 0.90, 0.78, 0.76)
    wait_t = 0.45
end

function aegis_update(dt)
    wait_t = math.max(0, wait_t - dt)
    if wait_t > 0 then return end
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space") then
        GAME.cardContinue = false
        aegis.transitionTo("game", "fade", 0.35)
    end
    if aegis.keyPressed("Escape") then
        aegis.transitionTo("menu", "fade", 0.35)
    end
end

function aegis_draw() end
