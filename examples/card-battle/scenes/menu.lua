local buttons = {}
local selected = 1
local can_continue = false
local pulse = 0
local extra_open = false
local extra_nodes = {}
local selector = nil
local fonts = {}
local hover_index = 0
local title_node = nil

local function load_ui_font()
    fonts = {}
    if aegis.loadDefaultFont then
        for _, size in ipairs({16, 18, 20, 24, 30, 34}) do
            local ok, font = pcall(aegis.loadDefaultFont, size)
            if ok and font then fonts[size] = font end
        end
    elseif aegis.loadFont then
        for _, size in ipairs({16, 18, 20, 24, 30, 34}) do
            local ok, font = pcall(aegis.loadFont, "Inter-Regular.ttf", size)
            if ok and font then fonts[size] = font end
        end
    end
end

local function pick_font(size)
    return fonts[size] or fonts[20] or fonts[18] or fonts[16]
end

local function make_label(text, size)
    if aegis.newLabelSize then
        local ok, l = pcall(aegis.newLabelSize, text, size or 20)
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

local function rect(x, y, w, h, r, g, b, z)
    local o = aegis.newRect(w, h, r, g, b)
    aegis.setPosition(o, x, y)
    aegis.setZ(o, z or 0)
    return o
end

local function label(text, x, y, r, g, b, z, size)
    local l = make_label(text, size or 20)
    local font = pick_font(size or 20)
    if font then pcall(aegis.setFont, l, font) end
    aegis.setPosition(l, x, y)
    aegis.setColor(l, r or 1, g or 1, b or 1)
    aegis.setZ(l, z or 10)
    return l
end

local function button(text, x, y, action, enabled)
    local b = { text = text, x = x, y = y, w = 286, h = 58, action = action, enabled = enabled ~= false }
    b.shadow = rect(x + 8, y + 8, b.w, b.h, 0.015, 0.015, 0.025, 3)
    b.bg = rect(x, y, b.w, b.h, 0.13, 0.11, 0.20, 5)
    b.line = rect(x, y, 7, b.h, 0.88, 0.64, 0.22, 6)
    b.label = label(text, x + 32, y + 15, 0.92, 0.88, 0.76, 8, 22)
    buttons[#buttons + 1] = b
    return b
end

local function inside(b, mx, my)
    return mx >= b.x and mx <= b.x + b.w and my >= b.y and my <= b.y + b.h
end

local function first_enabled()
    for i, b in ipairs(buttons) do
        if b.enabled then return i end
    end
    return 1
end

local function move_selection(dir)
    local i = selected
    for _ = 1, #buttons do
        i = i + dir
        if i < 1 then i = #buttons end
        if i > #buttons then i = 1 end
        if buttons[i].enabled then
            selected = i
            return
        end
    end
end

local function new_game()
    sfx("click.wav", 0.38, 0.10)
    GAME.cardContinue = false
    aegis.save("card_save_valid", false)
    aegis.transitionTo("game", "fade", 0.30)
end

local function continue_game()
    if not can_continue then return end
    sfx("click.wav", 0.38, 0.04)
    GAME.cardContinue = true
    aegis.transitionTo("game", "fade", 0.30)
end

local function clear_extra()
    for _, o in ipairs(extra_nodes) do
        if aegis.removeObject then aegis.removeObject(o) end
    end
    extra_nodes = {}
end

local function toggle_extra()
    sfx("click.wav", 0.35, -0.05)
    extra_open = not extra_open
    clear_extra()
    if extra_open then
        extra_nodes[#extra_nodes + 1] = rect(452, 424, 660, 142, 0.075, 0.065, 0.115, 30)
        extra_nodes[#extra_nodes + 1] = rect(452, 424, 660, 6, 0.90, 0.66, 0.22, 31)
        extra_nodes[#extra_nodes + 1] = label("Extra", 482, 450, 0.96, 0.78, 0.30, 32, 24)
        extra_nodes[#extra_nodes + 1] = label("Jogo bom demais: cartas com texto, custo, vida e uma IA sem vergonha.", 482, 492, 0.78, 0.86, 0.86, 32, 18)
        extra_nodes[#extra_nodes + 1] = label("Este prototipo e separado do Zone Recon.", 482, 524, 0.60, 0.68, 0.72, 32, 18)
    end
end

local function exit_game()
    sfx("click.wav", 0.35, -0.20)
    if os and os.exit then os.exit() end
end

function aegis_init()
    if aegis.clearAll then aegis.clearAll() end
    if aegis.setCameraOff then aegis.setCameraOff() end
    load_ui_font()

    buttons = {}
    selected = 1
    pulse = 0
    extra_open = false
    extra_nodes = {}
    can_continue = aegis.load("card_save_valid") == true
    hover_index = 0

    rect(0, 0, 1280, 720, 0.035, 0.043, 0.070, -20)
    rect(0, 500, 1280, 220, 0.045, 0.12, 0.10, -12)
    rect(0, 512, 1280, 10, 0.55, 0.36, 0.14, -10)
    rect(708, 66, 384, 246, 0.06, 0.055, 0.095, -5)
    rect(720, 78, 360, 222, 0.09, 0.07, 0.13, -4)

    for i = 1, 6 do
        local x = 700 + i * 70
        local y = 112 + ((i % 2) * 28)
        local card = rect(x, y, 58, 92, 0.13 + i * 0.015, 0.10, 0.20 + i * 0.015, -3)
        aegis.setRotation(card, -0.25 + i * 0.09)
        if aegis.tween then
            aegis.tween(card, { y = y - 10 }, 1.0 + i * 0.08, "inout", nil, { loop=true, yoyo=true })
        end
        rect(x + 8, y + 10, 42, 12, 0.86, 0.66, 0.24, -2)
        rect(x + 12, y + 35, 34, 34, 0.20, 0.30, 0.36, -2)
    end

    title_node = label("ARCANOS DA MESA", 80, 78, 0.96, 0.80, 0.38, 10, 30)
    if aegis.tween then
        aegis.tween(title_node, { alpha = 0.82 }, 0.9, "inout", nil, { loop=true, yoyo=true })
    end
    label("Duelo tatico de cartas: leia, escolha, ataque, defenda e sobreviva.", 84, 146, 0.75, 0.84, 0.86, 10, 18)
    label("Cada carta tem nome, custo, vida, ataque, defesa, cura e efeito escrito.", 84, 178, 0.63, 0.73, 0.78, 10, 18)

    button("Jogar", 98, 270, new_game, true)
    button("Continuar", 98, 340, continue_game, can_continue)
    button("Extra", 98, 410, toggle_extra, true)
    button("Exit", 98, 480, exit_game, true)
    selected = first_enabled()
    selector = rect(90, buttons[selected].y - 6, 304, 70, 0.22, 0.16, 0.32, 4)

    label("Novo jogo: comeca um duelo do zero.", 420, 278, 0.74, 0.82, 0.84, 10, 18)
    label("Continuar: volta ao HP salvo do prototipo.", 420, 348, 0.74, 0.82, 0.84, 10, 18)
    label("Extra: mostra uma nota divertida.", 420, 418, 0.74, 0.82, 0.84, 10, 18)
    label("Exit: fecha o jogo.", 420, 488, 0.74, 0.82, 0.84, 10, 18)
    label("Mouse: clique nos botoes. Teclado: W/S + Enter.", 98, 588, 0.58, 0.68, 0.72, 10, 18)
end

function aegis_update(dt)
    pulse = pulse + dt
    local mx, my = aegis.mouseX(), aegis.mouseY()

    for i, b in ipairs(buttons) do
        if b.enabled and inside(b, mx, my) then selected = i end
    end
    if aegis.keyPressed("Down") or aegis.keyPressed("S") then move_selection(1) end
    if aegis.keyPressed("Up") or aegis.keyPressed("W") then move_selection(-1) end
    if aegis.keyPressed("Enter") or aegis.keyPressed("Space") then
        local b = buttons[selected]
        if b and b.enabled then b.action() end
    end
    if aegis.mouseLeftJust() then
        for i, b in ipairs(buttons) do
            if b.enabled and inside(b, mx, my) then
                selected = i
                b.action()
                break
            end
        end
    end

    if selected ~= hover_index then
        hover_index = selected
        sfx("click.wav", 0.12, -0.35)
    end

    for i, b in ipairs(buttons) do
        if not b.enabled then
            aegis.setColor(b.label, 0.34, 0.36, 0.38)
        elseif i == selected then
            aegis.setColor(b.label, 1.00, 0.94, 0.70)
        else
            aegis.setColor(b.label, 0.82, 0.82, 0.78)
        end
    end

    if selector and buttons[selected] then
        aegis.setPosition(selector, 90 + math.sin(pulse * 6) * 4, buttons[selected].y - 6)
    end
end

function aegis_draw() end
