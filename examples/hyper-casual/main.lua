-- Fall Dodge — blocos caem, desvie. Hyper-casual, pouca física (hazards cinemáticos).
-- Setas / A D: mover   |   Space: reiniciar após game over

local P_W, P_H = 40, 36
-- Resolução 1280x720 (ver aegis.toml)
local SH = 720
local SW = 1280
local FLOOR_H = 40
local FLOOR_TOP = SH - FLOOR_H
local SPAWN_EVERY = 0.9
local BASE_FALL = 200

local player, player_rb, player_col
local score = 0
local spawn_t = 0
local playing = true
local speed_mul = 1.0
local blocks = {}
local label_title, label_hint

local function make_floor()
    local p = aegis.newRect(SW, FLOOR_H, 0.2, 0.25, 0.28)
    aegis.setPosition(p, 0, FLOOR_TOP)
    local c = aegis.addCollider(p, SW, FLOOR_H, 0, 0)
    aegis.setColliderLayer(c, "WORLD")
    aegis.setColliderMask(c, "PLAYER")
end

function aegis_init()
    aegis.setGlobalGravity(960)

    local logo = aegis.newSprite("aegis-logo.png")
    aegis.setPosition(logo, 1180, 16)
    aegis.setScale(logo, 0.14, 0.14)

    make_floor()

    player = aegis.newRect(P_W, P_H, 0.2, 0.65, 1.0)
    aegis.setPosition(player, SW * 0.5 - P_W * 0.5, FLOOR_TOP - P_H)
    player_rb = aegis.addRigidbody(player)
    aegis.setGravity(player_rb, 1.0)
    player_col = aegis.addCollider(player, P_W, P_H, 0, 0)
    aegis.setColliderLayer(player_col, "PLAYER")
    aegis.setColliderMask(player_col, "WORLD|ENEMY")
    aegis.setVelocity(player_rb, 0, 0)

    aegis.setCameraOff()

    label_title = aegis.newLabel("Dodge!  sobrevivência em segundos (timer)")
    aegis.setPosition(label_title, 16, 10)
    aegis.setColor(label_title, 0.9, 0.92, 0.95)

    label_hint = aegis.newLabel("Setas/A-D mover  |  cair=game over  |  Space restart")
    aegis.setPosition(label_hint, 16, 32)
    aegis.setColor(label_hint, 0.65, 0.7, 0.75)

    aegis.log("hyper-casual: escape dos blocos")
end

local function clear_blocks()
    for _, b in ipairs(blocks) do
        aegis.removeObject(b.obj)
    end
    blocks = {}
end

local function go_gameover()
    playing = false
    aegis.setText(label_title, "Game over — " .. string.format("score: %.1f s  —  Space p/ recomeçar", score))
end

local function restart()
    clear_blocks()
    score = 0
    spawn_t = 0
    speed_mul = 1.0
    playing = true
    aegis.setPosition(player, SW * 0.5 - P_W * 0.5, FLOOR_TOP - P_H)
    aegis.setVelocity(player_rb, 0, 0)
    aegis.setText(label_title, "Dodge!  sobrevivência (s)")
    aegis.log("reinício")
end

local function spawn_hazard()
    local w, h = 32 + aegis.randomInt(0, 20), 28
    local o = aegis.newRect(w, h, 0.9, 0.35, 0.2)
    local x = 24 + aegis.randomInt(0, SW - w - 48)
    aegis.setPosition(o, x, -h - 4)
    local c = aegis.addCollider(o, w, h, 0, 0)
    aegis.setColliderLayer(c, "ENEMY")
    aegis.setColliderMask(c, "PLAYER")
    aegis.setTrigger(c, true)
    aegis.onCollideEnter(c, function(a, b)
        if not playing then return end
        if b == player_col then go_gameover() end
    end)
    table.insert(blocks, { obj = o, col = c, w = w, h = h })
end

function aegis_update(dt)
    if aegis.keyPressed("Space") and not playing then
        restart()
        return
    end
    if not playing then
        return
    end

    local vx = 0
    local sp = 320
    if aegis.keyDown("Left")  or aegis.keyDown("A") then vx = -sp end
    if aegis.keyDown("Right") or aegis.keyDown("D") then vx =  sp end
    local vy = aegis.getVelocityY(player_rb)
    aegis.setVelocity(player_rb, vx, vy)

    local px = aegis.getX(player)
    if px < 0 then
        aegis.setPosition(player, 0, aegis.getY(player))
    elseif px > SW - P_W then
        aegis.setPosition(player, SW - P_W, aegis.getY(player))
    end

    score = score + dt
    speed_mul = 1.0 + math.min(score * 0.04, 2.5)
    aegis.setText(label_title, string.format("Dodge!  %.1f s  (v=%.0f%%)", score, speed_mul * 100))

    spawn_t = spawn_t + dt
    if spawn_t >= SPAWN_EVERY / speed_mul then
        spawn_t = 0
        spawn_hazard()
    end

    local fall = BASE_FALL * speed_mul
    for i = #blocks, 1, -1 do
        local b = blocks[i]
        local y = aegis.getY(b.obj) + fall * dt
        aegis.setPosition(b.obj, aegis.getX(b.obj), y)
        if y > SH + 80 then
            aegis.removeObject(b.obj)
            table.remove(blocks, i)
        end
    end
end

function aegis_draw() end
