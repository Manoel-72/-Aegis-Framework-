local wait_t = 0
local fonts = {}

local function load_ui_font()
    fonts = {}
    if aegis.loadDefaultFont then
        for _, size in ipairs({20, 22, 24, 34}) do
            local ok, font = pcall(aegis.loadDefaultFont, size)
            if ok and font then fonts[size] = font end
        end
    elseif aegis.loadFont then
        for _, size in ipairs({20, 22, 24, 34}) do
            local ok, font = pcall(aegis.loadFont, "Inter-Regular.ttf", size)
            if ok and font then fonts[size] = font end
        end
    end
end

local function pick_font(size)
    return fonts[size] or fonts[24] or fonts[20]
end

local function make_label(text, size)
    if aegis.newLabelSize then
        local ok, l = pcall(aegis.newLabelSize, text, size or 24)
        if ok and l then return l end
    end
    return aegis.newLabel(text)
end

local function sfx(name, volume, pitch)
    if aegis.playSoundEx then
        pcall(aegis.playSoundEx, name, volume or 0.45, pitch or 0, 0)
    elseif aegis.playSound then
        pcall(aegis.playSound, name)
    end
end

local function label(text, x, y, r, g, b, size)
    local l = make_label(text, size or 24)
    local font = pick_font(size or 24)
    if font then pcall(aegis.setFont, l, font) end
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

    local title = label("DERROTA NO DUELO", 420, 120, 1.00, 0.32, 0.32, 34)
    if aegis.tween then
        aegis.tween(title, { alpha=0.82 }, 0.7, "inout", nil, { loop=true, yoyo=true })
    end
    if aegis.flashScreen then aegis.flashScreen({ r=0.9, g=0.1, b=0.1 }, 0.12) end
    label("Seu personagem chegou a zero de vida.", 390, 220, 0.90, 0.78, 0.76, 22)
    label("Score: " .. GAME.score, 390, 272, 0.95, 0.82, 0.40, 24)
    label("Dica: use cartas de defesa antes de ficar com pouca vida.", 390, 324, 0.90, 0.78, 0.76, 22)
    label("Enter: tentar de novo", 390, 410, 0.90, 0.78, 0.76, 22)
    label("Esc: menu", 390, 462, 0.90, 0.78, 0.76, 22)
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
