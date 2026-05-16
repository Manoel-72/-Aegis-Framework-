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
    sfx("win.wav", 0.40, 0)

    local hs = aegis.load("highscore") or 0
    if GAME.score > hs then
        aegis.save("highscore", GAME.score)
        hs = GAME.score
    end

    local bg = aegis.newRect(1280, 720, 0.035, 0.075, 0.055)
    aegis.setPosition(bg, 0, 0)
    aegis.setZ(bg, -10)
    local table_top = aegis.newRect(900, 360, 0.05, 0.16, 0.12)
    aegis.setPosition(table_top, 190, 210)
    aegis.setZ(table_top, -5)

    local title = label("VITORIA NO DUELO", 430, 118, 0.50, 1.00, 0.62)
    aegis.setScale(title, 1.30, 1.30)
    if aegis.tween then
        aegis.tween(title, { scaleX=1.42, scaleY=1.42 }, 0.55, "inout", nil, { loop=true, yoyo=true })
    end
    if aegis.burst then
        aegis.burst(640, 260, { count=34, speed=220, life=0.65, size=5, r=0.50, g=1.00, b=0.62 })
    end
    label("A IA ficou sem vida. Suas cartas dominaram a mesa.", 410, 220, 0.82, 0.90, 0.82)
    label("Score: " .. GAME.score, 410, 272, 0.95, 0.82, 0.40)
    label("Recorde: " .. hs, 410, 324, 0.60, 0.78, 1.00)
    label("Enter: novo duelo", 410, 410, 0.82, 0.90, 0.82)
    label("Esc: menu", 410, 462, 0.82, 0.90, 0.82)
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
