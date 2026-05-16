local CARD_W, CARD_H = 178, 212
local player_hp, enemy_hp = 20, 20
local player_block, enemy_block = 0, 0
local energy, max_energy = 3, 3
local turn = 1
local phase = "player"
local message = ""
local player_deck, enemy_deck = {}, {}
local player_hand, enemy_hand = {}, {}
local player_grave, enemy_grave = {}, {}
local hand_slots = {}
local end_turn_btn, menu_btn
local ui_font = nil
local hover_card = 0
local last_hover_card = 0
local anim_time = 0

local function load_ui_font()
    if not (aegis.loadFont and aegis.setFont) then return end
    local candidates = {
        "Inter-Regular.ttf",
        "Roboto-Regular.ttf",
        "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arial.ttf"
    }
    for _, path in ipairs(candidates) do
        local ok, font = pcall(aegis.loadFont, path, 16)
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

local CARDS = {
    { name="Lamina Solar",     kind="Ataque",  cost=1, atk=3, block=0, heal=0, r=0.95,g=0.54,b=0.20, text="Causa 3 de dano direto." },
    { name="Escudo de Vidro",  kind="Defesa",  cost=1, atk=0, block=4, heal=0, r=0.28,g=0.65,b=0.95, text="Ganha 4 de defesa neste turno." },
    { name="Raio Curvo",       kind="Ataque",  cost=2, atk=5, block=0, heal=0, r=0.78,g=0.42,b=1.00, text="Corta a mesa com 5 de dano." },
    { name="Pacto Verde",      kind="Suporte", cost=1, atk=0, block=2, heal=2, r=0.20,g=0.85,b=0.42, text="Cura 2 e ganha 2 de defesa." },
    { name="Martelo Astral",   kind="Ataque",  cost=3, atk=8, block=0, heal=0, r=0.93,g=0.75,b=0.25, text="Golpe caro, mas enorme." },
    { name="Muralha Baixa",    kind="Defesa",  cost=2, atk=0, block=7, heal=0, r=0.35,g=0.56,b=0.72, text="Uma muralha simples e forte." },
    { name="Flecha Dupla",     kind="Mista",   cost=2, atk=4, block=1, heal=0, r=0.88,g=0.36,b=0.28, text="Ataca e ainda protege um pouco." },
    { name="Sopro Vital",      kind="Cura",    cost=2, atk=0, block=0, heal=5, r=0.36,g=0.92,b=0.62, text="Recupera 5 de vida." },
    { name="Cinzas Cortantes", kind="Mista",   cost=1, atk=2, block=2, heal=0, r=0.70,g=0.70,b=0.76, text="Uma jogada barata e flexivel." },
    { name="Olho do Oraculo",  kind="Mista",   cost=0, atk=1, block=1, heal=0, r=0.58,g=0.48,b=0.96, text="Custo zero para salvar turno." },
    { name="Carga Rubra",      kind="Ataque",  cost=2, atk=6, block=0, heal=0, r=0.95,g=0.24,b=0.24, text="Pressao ofensiva pura." },
    { name="Bencao Fria",      kind="Suporte", cost=1, atk=0, block=3, heal=1, r=0.45,g=0.92,b=0.92, text="Defende 3 e cura 1." },
    { name="Punho de Pedra",   kind="Mista",   cost=2, atk=3, block=4, heal=0, r=0.62,g=0.50,b=0.34, text="Bom para virar combate." },
    { name="Estrela Partida",  kind="Mista",   cost=3, atk=6, block=3, heal=0, r=0.85,g=0.76,b=0.45, text="Dano alto com seguranca." },
    { name="Chama Mansa",      kind="Mista",   cost=1, atk=2, block=0, heal=2, r=0.95,g=0.44,b=0.36, text="Fere e cura ao mesmo tempo." },
}

local function clamp(v, lo, hi)
    if v < lo then return lo end
    if v > hi then return hi end
    return v
end

local function rect(x, y, w, h, r, g, b, z)
    local o = aegis.newRect(w, h, r, g, b)
    aegis.setPosition(o, x, y)
    aegis.setZ(o, z or 0)
    return o
end

local function label(text, x, y, r, g, b, z)
    local l = aegis.newLabel(text)
    if ui_font then pcall(aegis.setFont, l, ui_font) end
    aegis.setPosition(l, x, y)
    aegis.setColor(l, r or 1, g or 1, b or 1)
    aegis.setZ(l, z or 10)
    return l
end

local function button(text, x, y, w, h)
    local b = { x=x, y=y, w=w, h=h }
    b.bg = rect(x, y, w, h, 0.13, 0.12, 0.20, 30)
    b.edge = rect(x, y, 5, h, 0.90, 0.66, 0.22, 31)
    b.label = label(text, x + 18, y + 12, 0.94, 0.88, 0.70, 32)
    aegis.setScale(b.label, 1.05, 1.05)
    return b
end

local function energy_panel()
    rect(430, 34, 420, 78, 0.055, 0.050, 0.085, 23)
    rect(430, 34, 420, 6, 0.90, 0.66, 0.22, 24)
    local title = label("ENERGIA", 452, 52, 0.98, 0.82, 0.34, 25)
    aegis.setScale(title, 1.28, 1.28)
    for i = 1, max_energy do
        local x = 590 + (i - 1) * 68
        local filled = i <= energy
        local orb = rect(x, 52, 44, 44,
            filled and 0.96 or 0.12,
            filled and 0.72 or 0.13,
            filled and 0.20 or 0.18,
            25)
        rect(x + 8, 60, 28, 28,
            filled and 1.00 or 0.055,
            filled and 0.88 or 0.060,
            filled and 0.48 or 0.085,
            26)
        if filled and aegis.tween then
            aegis.tween(orb, { scaleX = 1.08, scaleY = 1.08 }, 0.45, "inout", nil, { loop=true, yoyo=true })
        end
    end
    local value = label(energy .. " / " .. max_energy, 790, 55, 0.80, 0.92, 1.00, 26)
    aegis.setScale(value, 1.32, 1.32)
end

local function hit(b, mx, my)
    return b and mx >= b.x and mx <= b.x + b.w and my >= b.y and my <= b.y + b.h
end

local function make_card(index)
    return { def = CARDS[index], life = 3, id = index }
end

local function shuffle(list)
    for i = #list, 2, -1 do
        local j = aegis.randomInt(1, i)
        list[i], list[j] = list[j], list[i]
    end
end

local function new_deck()
    local deck = {}
    for i = 1, #CARDS do deck[#deck + 1] = i end
    shuffle(deck)
    return deck
end

local function draw_to_three(deck, hand)
    while #hand < 3 and #deck > 0 do
        hand[#hand + 1] = make_card(table.remove(deck, 1))
    end
end

local function damage(target, amount)
    if target == "enemy" then
        local absorbed = math.min(enemy_block, amount)
        enemy_block = enemy_block - absorbed
        enemy_hp = clamp(enemy_hp - (amount - absorbed), 0, 20)
        if amount - absorbed > 0 and aegis.screenShake then aegis.screenShake(4, 0.12) end
    else
        local absorbed = math.min(player_block, amount)
        player_block = player_block - absorbed
        player_hp = clamp(player_hp - (amount - absorbed), 0, 20)
        if amount - absorbed > 0 and aegis.screenShake then aegis.screenShake(5, 0.14) end
    end
end

local function save_progress()
    aegis.save("card_save_valid", true)
    aegis.save("card_player_hp", player_hp)
    aegis.save("card_enemy_hp", enemy_hp)
    aegis.save("card_turn", turn)
end

local function finish_if_needed()
    if enemy_hp <= 0 then
        GAME.score = 1000 + player_hp * 25 + #player_deck * 10
        aegis.save("card_save_valid", false)
        sfx("win.wav", 0.55, 0)
        aegis.transitionTo("win", "fade", 0.35)
        return true
    end
    if player_hp <= 0 then
        GAME.score = math.max(0, 400 - enemy_hp * 10)
        aegis.save("card_save_valid", false)
        sfx("lose.wav", 0.55, 0)
        aegis.transitionTo("gameover", "fade", 0.35)
        return true
    end
    return false
end

local function apply_card(card, owner)
    local d = card.def
    if owner == "player" then
        if d.atk > 0 then damage("enemy", d.atk) end
        player_block = player_block + d.block
        player_hp = clamp(player_hp + d.heal, 0, 20)
    else
        if d.atk > 0 then damage("player", d.atk) end
        enemy_block = enemy_block + d.block
        enemy_hp = clamp(enemy_hp + d.heal, 0, 20)
    end
    card.life = card.life - (d.atk > 0 and 2 or 1)
end

local function keep_or_grave(hand, grave, index)
    local card = hand[index]
    if card.life <= 0 then
        grave[#grave + 1] = card
        table.remove(hand, index)
        return " A carta ficou sem vida e foi para o cemiterio."
    end
    return " Vida restante da carta: " .. card.life .. "."
end

local function draw_card(card, x, y, playable, slot)
    local d = card.def
    local hovered = hover_card == slot
    if hovered then
        y = y - 18
        rect(x - 9, y - 9, CARD_W + 18, CARD_H + 18, 0.92, 0.68, 0.22, 10)
    end
    rect(x + 7, y + 9, CARD_W, CARD_H, 0.02, 0.02, 0.035, 11)
    local body = rect(x, y, CARD_W, CARD_H, 0.10, 0.09, 0.14, 12)
    rect(x + 8, y + 8, CARD_W - 16, CARD_H - 16, d.r * 0.32, d.g * 0.32, d.b * 0.32, 13)
    rect(x + 12, y + 12, CARD_W - 24, 34, d.r, d.g, d.b, 14)
    label(d.name, x + 18, y + 19, 0.06, 0.04, 0.06, 16)
    label("Tipo: " .. d.kind, x + 18, y + 54, 0.95, 0.86, 0.62, 16)
    rect(x + 20, y + 80, CARD_W - 40, 42, d.r * 0.58, d.g * 0.58, d.b * 0.58, 14)
    label("Custo: " .. d.cost .. "     Vida da carta: " .. card.life .. "/3", x + 18, y + 132, 0.92, 0.88, 0.72, 16)
    label("Ataque " .. d.atk .. " | Defesa " .. d.block .. " | Cura " .. d.heal, x + 18, y + 156, 0.84, 0.90, 0.92, 16)
    label("Efeito: " .. d.text, x + 18, y + 182, 0.66, 0.72, 0.75, 16)
    if playable then
        rect(x, y, 6, CARD_H, 0.96, 0.72, 0.20, 17)
        label("CLIQUE PARA USAR", x + 30, y + 202, 0.98, 0.86, 0.40, 18)
    else
        rect(x, y, 6, CARD_H, 0.25, 0.25, 0.28, 17)
    end
    if hovered and aegis.tween then
        aegis.tween(body, { scaleX = 1.03, scaleY = 1.03 }, 0.18, "out")
    end
    hand_slots[#hand_slots + 1] = { x=x, y=y, w=CARD_W, h=CARD_H, index=slot }
end

local function card_back(x, y)
    rect(x + 6, y + 8, 112, 150, 0.02, 0.02, 0.035, 11)
    rect(x, y, 112, 150, 0.11, 0.08, 0.18, 12)
    rect(x + 10, y + 10, 92, 130, 0.24, 0.18, 0.34, 13)
    rect(x + 30, y + 42, 52, 64, 0.88, 0.65, 0.22, 14)
    label("IA", x + 44, y + 112, 0.90, 0.82, 0.62, 15)
end

local function hp_bar(x, y, name, hp, block, r, g, b)
    label(name, x, y, r, g, b, 20)
    rect(x, y + 30, 250, 24, 0.11, 0.10, 0.12, 20)
    rect(x + 3, y + 33, math.floor(244 * hp / 20), 18, r, g, b, 21)
    label("Vida " .. hp .. "/20   Defesa " .. block, x + 10, y + 62, 0.86, 0.88, 0.86, 22)
end

local function render()
    if aegis.clearAll then aegis.clearAll() end
    if aegis.setCameraOff then aegis.setCameraOff() end
    hand_slots = {}

    rect(0, 0, 1280, 720, 0.035, 0.045, 0.070, -20)
    rect(70, 100, 1140, 410, 0.045, 0.13, 0.105, -10)
    rect(70, 100, 1140, 8, 0.55, 0.36, 0.14, -8)
    rect(70, 502, 1140, 8, 0.55, 0.36, 0.14, -8)
    rect(0, 522, 1280, 198, 0.07, 0.065, 0.09, -7)

    hp_bar(90, 38, "PLAYER", player_hp, player_block, 0.30, 0.92, 0.58)
    hp_bar(940, 38, "IA RIVAL", enemy_hp, enemy_block, 0.95, 0.32, 0.32)
    local turn_lbl = label("Turno " .. turn .. " - " .. (phase == "player" and "sua vez" or "vez da IA"), 92, 126, 0.96, 0.78, 0.32, 20)
    aegis.setScale(turn_lbl, 1.18, 1.18)
    energy_panel()

    rect(166, 190, 92, 132, 0.20, 0.52, 0.36, 4)
    rect(186, 162, 52, 52, 0.70, 0.86, 0.66, 5)
    label("Voce", 176, 338, 0.72, 0.90, 0.78, 8)
    rect(1018, 190, 92, 132, 0.52, 0.16, 0.20, 4)
    rect(1038, 162, 52, 52, 0.86, 0.54, 0.48, 5)
    label("IA", 1052, 338, 0.95, 0.65, 0.60, 8)

    for i = 1, 3 do card_back(458 + (i - 1) * 126, 136) end

    rect(260, 374, 760, 68, 0.055, 0.050, 0.075, 18)
    label(message, 286, 386, 0.86, 0.88, 0.78, 20)
    label("Regras: ataque gasta 2 de vida da carta; defesa/cura gasta 1; vida 0 vai ao cemiterio.", 262, 420, 0.58, 0.68, 0.70, 20)

    for i, card in ipairs(player_hand) do
        local x = 318 + (i - 1) * 214
        draw_card(card, x, 482, phase == "player" and energy >= card.def.cost, i)
    end

    label("Seu deck " .. #player_deck .. " | Seu cemiterio " .. #player_grave, 84, 684, 0.56, 0.66, 0.70, 20)
    label("Deck IA " .. #enemy_deck .. " | Cemiterio IA " .. #enemy_grave, 880, 684, 0.56, 0.66, 0.70, 20)
    end_turn_btn = button("Encerrar turno", 1030, 560, 176, 48)
    menu_btn = button("Menu", 1030, 620, 176, 48)
end

local function play_player_card(index)
    if phase ~= "player" then return end
    local card = player_hand[index]
    if not card then return end
    if energy < card.def.cost then
        sfx("deny.wav", 0.40, 0)
        if aegis.flashScreen then aegis.flashScreen({ r=0.8, g=0.15, b=0.15 }, 0.09) end
        message = "Energia insuficiente para usar " .. card.def.name .. "."
        render()
        return
    end
    energy = energy - card.def.cost
    sfx("card.wav", 0.46, card.def.atk > 0 and 0.10 or -0.05)
    if aegis.burst then
        aegis.burst(640, 420, { count=18, speed=150, life=0.35, size=4, r=card.def.r, g=card.def.g, b=card.def.b })
    end
    if aegis.flashScreen and card.def.atk > 0 then aegis.flashScreen({ r=1.0, g=0.78, b=0.28 }, 0.06) end
    apply_card(card, "player")
    message = "Voce usou " .. card.def.name .. "." .. keep_or_grave(player_hand, player_grave, index)
    if not finish_if_needed() then
        save_progress()
        render()
    end
end

local function enemy_choice()
    local best_i, best_score = nil, -999
    for i, card in ipairs(enemy_hand) do
        local d = card.def
        if d.cost <= energy then
            local score = d.atk * 3 + d.block + d.heal
            if enemy_hp <= 8 then score = score + d.heal * 3 + d.block end
            if player_hp <= d.atk then score = score + 100 end
            if score > best_score then
                best_i, best_score = i, score
            end
        end
    end
    return best_i
end

local function start_player_turn()
    phase = "player"
    turn = turn + 1
    energy = max_energy
    sfx("turn.wav", 0.32, 0.08)
    player_block = 0
    draw_to_three(player_deck, player_hand)
    message = "Sua vez. Escolha uma carta com texto e custo suficiente."
    save_progress()
    render()
end

local function enemy_turn()
    phase = "enemy"
    energy = max_energy
    sfx("turn.wav", 0.25, -0.12)
    enemy_block = 0
    draw_to_three(enemy_deck, enemy_hand)
    message = "A IA esta escolhendo cartas..."
    render()

    local played = 0
    while played < 2 do
        local i = enemy_choice()
        if not i then break end
        local card = enemy_hand[i]
        energy = energy - card.def.cost
        sfx("card.wav", 0.28, -0.18)
        apply_card(card, "enemy")
        message = "IA usou " .. card.def.name .. ": " .. card.def.text
        keep_or_grave(enemy_hand, enemy_grave, i)
        played = played + 1
        if finish_if_needed() then return end
    end
    start_player_turn()
end

local function end_player_turn()
    if phase ~= "player" then return end
    save_progress()
    enemy_turn()
end

local function load_or_new()
    local valid = GAME.cardContinue and aegis.load("card_save_valid") == true
    player_hp = valid and (tonumber(aegis.load("card_player_hp")) or 20) or 20
    enemy_hp = valid and (tonumber(aegis.load("card_enemy_hp")) or 20) or 20
    turn = valid and (tonumber(aegis.load("card_turn")) or 1) or 1
    player_block, enemy_block = 0, 0
    energy, max_energy = 3, 3
    phase = "player"
    player_deck, enemy_deck = new_deck(), new_deck()
    player_hand, enemy_hand = {}, {}
    player_grave, enemy_grave = {}, {}
    draw_to_three(player_deck, player_hand)
    draw_to_three(enemy_deck, enemy_hand)
    message = valid and "Duelo salvo carregado. O baralho foi reembaralhado para este prototipo." or "Novo duelo iniciado. Reduza a IA a zero."
end

function aegis_init()
    if aegis.clearAll then aegis.clearAll() end
    load_ui_font()
    load_or_new()
    hover_card = 0
    last_hover_card = 0
    save_progress()
    render()
end

function aegis_update(dt)
    anim_time = anim_time + dt
    local mx, my = aegis.mouseX(), aegis.mouseY()
    local hovered = 0
    for _, slot in ipairs(hand_slots) do
        if hit(slot, mx, my) then hovered = slot.index end
    end
    if hovered ~= hover_card then
        hover_card = hovered
        if hover_card ~= 0 then sfx("click.wav", 0.10, -0.25) end
        render()
        if not aegis.mouseLeftJust() then return end
    end
    if aegis.keyPressed("Escape") then
        save_progress()
        aegis.transitionTo("menu", "fade", 0.25)
        return
    end
    if aegis.keyPressed("Enter") then
        end_player_turn()
        return
    end
    if aegis.mouseLeftJust() then
        if hit(menu_btn, mx, my) then
            save_progress()
            aegis.transitionTo("menu", "fade", 0.25)
            return
        end
        if hit(end_turn_btn, mx, my) then
            sfx("click.wav", 0.32, 0)
            end_player_turn()
            return
        end
        for _, slot in ipairs(hand_slots) do
            if hit(slot, mx, my) then
                play_player_card(slot.index)
                return
            end
        end
    end
end

function aegis_draw() end
