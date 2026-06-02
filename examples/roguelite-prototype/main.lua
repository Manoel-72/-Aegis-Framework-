local TILE = 16
local MAP_W = 90
local MAP_H = 54
local WALL = 1
local FLOOR = 2

local map = nil
local player = nil
local portal = nil
local hud = {}
local enemies = {}
local loot = {}
local rooms = {}

local run_seed = 260601
local floor_index = 1
local hp = 10
local gold = 0
local kills = 0
local attack_cd = 0
local message = ""
local message_time = 0

local function clamp(v, a, b)
    if v < a then return a end
    if v > b then return b end
    return v
end

local function dist(ax, ay, bx, by)
    local dx = ax - bx
    local dy = ay - by
    return math.sqrt(dx * dx + dy * dy)
end

local function new_grid()
    local grid = {}
    for y = 1, MAP_H do
        grid[y] = {}
        for x = 1, MAP_W do
            grid[y][x] = WALL
        end
    end
    return grid
end

local function carve_room(grid, room)
    for y = room.y, room.y + room.h - 1 do
        for x = room.x, room.x + room.w - 1 do
            if x > 1 and y > 1 and x < MAP_W and y < MAP_H then
                grid[y][x] = FLOOR
            end
        end
    end
end

local function carve_h_corridor(grid, x1, x2, y)
    local a = math.min(x1, x2)
    local b = math.max(x1, x2)
    for x = a, b do
        if x > 1 and x < MAP_W and y > 1 and y < MAP_H then
            grid[y][x] = FLOOR
        end
    end
end

local function carve_v_corridor(grid, y1, y2, x)
    local a = math.min(y1, y2)
    local b = math.max(y1, y2)
    for y = a, b do
        if x > 1 and x < MAP_W and y > 1 and y < MAP_H then
            grid[y][x] = FLOOR
        end
    end
end

local function room_center(room)
    return math.floor(room.x + room.w / 2), math.floor(room.y + room.h / 2)
end

local function overlaps(a, b)
    return a.x - 1 < b.x + b.w and
        a.x + a.w + 1 > b.x and
        a.y - 1 < b.y + b.h and
        a.y + a.h + 1 > b.y
end

local function build_dungeon()
    local grid = new_grid()
    rooms = {}

    for i = 1, 90 do
        local room = {
            w = aegis.randomInt(7, 14),
            h = aegis.randomInt(5, 10)
        }
        room.x = aegis.randomInt(2, MAP_W - room.w - 1)
        room.y = aegis.randomInt(2, MAP_H - room.h - 1)

        local ok = true
        for _, other in ipairs(rooms) do
            if overlaps(room, other) then
                ok = false
                break
            end
        end

        if ok then
            carve_room(grid, room)
            if #rooms > 0 then
                local ax, ay = room_center(rooms[#rooms])
                local bx, by = room_center(room)
                if aegis.randomInt(0, 1) == 0 then
                    carve_h_corridor(grid, ax, bx, ay)
                    carve_v_corridor(grid, ay, by, bx)
                else
                    carve_v_corridor(grid, ay, by, ax)
                    carve_h_corridor(grid, ax, bx, by)
                end
            end
            rooms[#rooms + 1] = room
            if #rooms >= 9 + math.min(floor_index, 4) then break end
        end
    end

    return grid
end

local function cell_at(px, py)
    local tx = math.floor(px / TILE) + 1
    local ty = math.floor(py / TILE) + 1
    return tx, ty
end

local dungeon_grid = nil

local function is_walkable(px, py)
    local points = {
        { px - 10, py - 10 },
        { px + 10, py - 10 },
        { px - 10, py + 10 },
        { px + 10, py + 10 }
    }

    for _, p in ipairs(points) do
        local tx, ty = cell_at(p[1], p[2])
        if tx < 1 or ty < 1 or tx > MAP_W or ty > MAP_H then return false end
        if dungeon_grid[ty][tx] == WALL then return false end
    end
    return true
end

local function place_in_room(room)
    local x = aegis.randomInt(room.x + 1, room.x + room.w - 2) * TILE - TILE / 2
    local y = aegis.randomInt(room.y + 1, room.y + room.h - 2) * TILE - TILE / 2
    return x, y
end

local function set_msg(text, seconds)
    message = text
    message_time = seconds or 2
end

local function clear_entities()
    for _, e in ipairs(enemies) do aegis.destroy(e.obj) end
    for _, l in ipairs(loot) do aegis.destroy(l.obj) end
    enemies = {}
    loot = {}
end

local function make_enemy(x, y, kind)
    local obj = aegis.newSprite("sprites/enemy.png")
    aegis.setPosition(obj, x - 12, y - 12)
    aegis.setScale(obj, 2, 2)
    local enemy = {
        obj = obj,
        x = x,
        y = y,
        hp = kind == "brute" and 3 or 2,
        speed = kind == "brute" and 55 or 75,
        hit = 0
    }
    enemies[#enemies + 1] = enemy
end

local function make_loot(x, y)
    local obj = aegis.newSprite("sprites/coin.png")
    aegis.setPosition(obj, x - 8, y - 8)
    aegis.setScale(obj, 1.5, 1.5)
    loot[#loot + 1] = { obj = obj, x = x, y = y }
end

local function create_hud()
    hud.title = aegis.newLabelSize("Aegis Roguelite Prototype", 24)
    aegis.setPosition(hud.title, 20, 18)

    hud.stats = aegis.newLabelSize("", 20)
    aegis.setPosition(hud.stats, 20, 52)

    hud.help = aegis.newLabelSize("WASD mover | Space atacar | R nova run | N proximo andar", 16)
    aegis.setPosition(hud.help, 20, 84)

    hud.msg = aegis.newLabelSize("", 18)
    aegis.setPosition(hud.msg, 20, 112)
end

local function update_hud()
    aegis.setText(hud.stats, "Seed " .. run_seed .. " | Andar " .. floor_index .. " | Vida " .. hp .. " | Ouro " .. gold .. " | Kills " .. kills)
    aegis.setText(hud.msg, message)
end

local function build_floor()
    aegis.clearAll()
    create_hud()
    clear_entities()

    aegis.setRandomSeed(run_seed + floor_index * 997)
    dungeon_grid = build_dungeon()

    map = aegis.createTilemap(dungeon_grid, {
        tileset = "sprites/tileset.png",
        tileWidth = TILE,
        tileHeight = TILE
    })
    aegis.buildTilemapColliders(map, { solidGids = { WALL }, merge = true })

    local start = rooms[1]
    local sx, sy = room_center(start)
    player = aegis.newRect(24, 24, 0.25, 0.75, 1.0)
    aegis.setPosition(player, sx * TILE - 12, sy * TILE - 12)
    aegis.addCollider(player, 24, 24)
    aegis.setCameraTarget(player)
    aegis.setCameraDeadzone(180, 110)
    aegis.setCameraLimits(0, 0, MAP_W * TILE, MAP_H * TILE)

    for i = 2, #rooms - 1 do
        local room = rooms[i]
        if i % 2 == 0 then
            local x, y = place_in_room(room)
            make_enemy(x, y, aegis.randomInt(1, 4) == 1 and "brute" or "basic")
        end
        if i % 3 == 0 then
            local x, y = place_in_room(room)
            make_loot(x, y)
        end
    end

    local last = rooms[#rooms]
    local px, py = place_in_room(last)
    portal = aegis.newRect(34, 34, 0.25, 1.0, 0.55)
    aegis.setPosition(portal, px - 17, py - 17)

    set_msg("Andar gerado em tempo real. Encontre o portal verde.", 2.5)
    update_hud()
end

function aegis_init()
    build_floor()
end

local function restart_run()
    run_seed = aegis.randomInt(1000, 999999)
    floor_index = 1
    hp = 10
    gold = 0
    kills = 0
    build_floor()
    set_msg("Nova run criada.", 2)
end

local function next_floor()
    floor_index = floor_index + 1
    hp = math.min(10, hp + 1)
    aegis.playSound("stairs.wav")
    build_floor()
    set_msg("Andar " .. floor_index .. ". Seed mantida, mapa novo.", 2)
end

local function attack()
    if attack_cd > 0 then return end
    attack_cd = 0.35
    aegis.playSound("click.wav")
    local px = aegis.getX(player) + 12
    local py = aegis.getY(player) + 12

    for i = #enemies, 1, -1 do
        local e = enemies[i]
        if dist(px, py, e.x, e.y) < 58 then
            e.hp = e.hp - 1
            e.hit = 0.12
            if e.hp <= 0 then
                kills = kills + 1
                if aegis.randomInt(1, 100) <= 45 then make_loot(e.x, e.y) end
                aegis.destroy(e.obj)
                table.remove(enemies, i)
            end
        end
    end
end

local function update_player(dt)
    local dx, dy = 0, 0
    if aegis.keyDown("A") or aegis.keyDown("Left") then dx = dx - 1 end
    if aegis.keyDown("D") or aegis.keyDown("Right") then dx = dx + 1 end
    if aegis.keyDown("W") or aegis.keyDown("Up") then dy = dy - 1 end
    if aegis.keyDown("S") or aegis.keyDown("Down") then dy = dy + 1 end

    if dx ~= 0 or dy ~= 0 then
        local len = math.sqrt(dx * dx + dy * dy)
        dx = dx / len
        dy = dy / len
        local speed = 210
        local nx = aegis.getX(player) + dx * speed * dt
        local ny = aegis.getY(player) + dy * speed * dt
        if is_walkable(nx + 12, aegis.getY(player) + 12) then
            aegis.setPosition(player, nx, aegis.getY(player))
        end
        if is_walkable(aegis.getX(player) + 12, ny + 12) then
            aegis.setPosition(player, aegis.getX(player), ny)
        end
    end

    if aegis.keyPressed("Space") then attack() end
end

local function update_enemies(dt)
    local px = aegis.getX(player) + 12
    local py = aegis.getY(player) + 12

    for _, e in ipairs(enemies) do
        local dx = px - e.x
        local dy = py - e.y
        local d = math.max(1, math.sqrt(dx * dx + dy * dy))

        if d < 360 then
            local nx = e.x + dx / d * e.speed * dt
            local ny = e.y + dy / d * e.speed * dt
            if is_walkable(nx, e.y) then e.x = nx end
            if is_walkable(e.x, ny) then e.y = ny end
            aegis.setPosition(e.obj, e.x - 12, e.y - 12)
        end

        if e.hit > 0 then
            e.hit = e.hit - dt
            aegis.setScale(e.obj, 2.4, 2.4)
        else
            aegis.setScale(e.obj, 2, 2)
        end

        if d < 24 then
            hp = hp - 1
            e.x = e.x - dx / d * 38
            e.y = e.y - dy / d * 38
            set_msg("Voce tomou dano.", 1)
            if hp <= 0 then
                restart_run()
                return
            end
        end
    end
end

local function update_loot()
    local px = aegis.getX(player) + 12
    local py = aegis.getY(player) + 12
    for i = #loot, 1, -1 do
        local l = loot[i]
        if dist(px, py, l.x, l.y) < 28 then
            gold = gold + 1
            aegis.playSound("loot.wav")
            aegis.destroy(l.obj)
            table.remove(loot, i)
        end
    end
end

local function update_portal()
    local px = aegis.getX(player) + 12
    local py = aegis.getY(player) + 12
    local ex = aegis.getX(portal) + 17
    local ey = aegis.getY(portal) + 17
    if dist(px, py, ex, ey) < 34 then
        next_floor()
    end
end

function aegis_update(dt)
    attack_cd = math.max(0, attack_cd - dt)
    if message_time > 0 then
        message_time = message_time - dt
        if message_time <= 0 then message = "" end
    end

    if aegis.keyPressed("R") then restart_run() end
    if aegis.keyPressed("N") then next_floor() end

    update_player(dt)
    update_enemies(dt)
    update_loot()
    update_portal()
    update_hud()
end
