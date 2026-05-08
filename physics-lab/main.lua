-- Aegis Physics Lab — exercitar Rigidbody2D, AABB, gravidade e chão
--
-- A/D ou setas: mover
-- W / Space / seta cima: pular (só se grounded)
-- R: reposicionar jogador e caixas
-- 1/2/3/4: presets de gravidade global
-- C: cria caixa no ar acima do jogador
-- Atrito no chão (motor): aegis.setGroundFriction(rb, k), k=0 desliga, ~16 suave

local SPEED, JUMP = 240, -520
local WORLD_W, FLOOR_H = 2400, 40
-- 1280x720 (aegis.toml): chão alinhado à base da tela
local SH = 720
local FLOOR_TOP = SH - FLOOR_H
local P_W, P_H = 32, 36

local player, player_rb, player_col
local fire_emitter
local atlas_sprite, atlas_anim
local circle_sensor, circle_sensor_col, circle_sensor_hit
local crates = {}
local g_idx = 2
local G_LIST = { 500, 800, 1200, 2000 } -- 800 = default engine-ish

-- Chão / plataformas: somente Collider. Rigidbody é apenas para corpos dinâmicos.
local function platform(x, y, w, h, r, g, b)
    local p = aegis.newRect(w, h, r, g, b)
    aegis.setPosition(p, x, y)
    local c = aegis.addCollider(p, w, h, 0, 0)
    aegis.setColliderLayer(c, "WORLD")
    aegis.setColliderMask(c, "PLAYER|ENEMY")
    return p
end

local function add_crate(x, y)
    local o = aegis.newRect(28, 28, 0.55, 0.4, 0.15)
    aegis.setPosition(o, x, y)
    local rb = aegis.addRigidbody(o)
    aegis.setGravity(rb, 1.0)
    local c = aegis.addCollider(o, 28, 28, 0, 0)
    aegis.setColliderLayer(c, "ENEMY")
    aegis.setColliderMask(c, "WORLD|PLAYER|ENEMY")
    table.insert(crates, { obj = o, rb = rb, col = c })
end

local function reset_sandbox()
    aegis.setPosition(player, 120, FLOOR_TOP - P_H)
    aegis.setVelocity(player_rb, 0, 0)
    local y0 = 500
    for i, k in ipairs(crates) do
        aegis.setPosition(k.obj, 420 + (i - 1) * 32, y0)
        aegis.setVelocity(k.rb, 0, 0)
    end
end

local hud
function aegis_init()
    aegis.setGlobalGravity(G_LIST[g_idx])

    local logo = aegis.newSprite("aegis-logo.png")
    aegis.setPosition(logo, 20, 20)
    aegis.setScale(logo, 0.12, 0.12)

    platform(0, FLOOR_TOP, WORLD_W, FLOOR_H, 0.22, 0.22, 0.25)
    platform(280, 560, 140, 20, 0.35, 0.55, 0.3)
    platform(520, 480, 120, 20, 0.35, 0.55, 0.3)
    platform(200, 420, 200, 16, 0.3, 0.45, 0.35)

    add_crate(520, 320)
    add_crate(560, 280)
    add_crate(500, 240)

    player = aegis.newRect(P_W, P_H, 0.25, 0.5, 1.0)
    aegis.setPosition(player, 120, FLOOR_TOP - P_H)
    player_rb = aegis.addRigidbody(player)
    aegis.setGravity(player_rb, 1.0)
    -- Compat: versões antigas do CLI podem não expor setGroundFriction.
    if aegis.setGroundFriction then
        aegis.setGroundFriction(player_rb, 16)
    end
    player_col = aegis.addCollider(player, P_W, P_H, 0, 0)
    aegis.setColliderLayer(player_col, "PLAYER")
    aegis.setColliderMask(player_col, "WORLD|ENEMY|PICKUP")

    -- Emitter contínuo: fogo/fumaça seguindo o jogador.
    fire_emitter = aegis.newEmitter(P_W * 0.5, P_H, {
        rate = 25, duration = -1,
        speed = 60, life = 0.8, size = 5,
        r = 1.0, g = 0.4, b = 0.0,
        spread = 30, angle = 90,
        follow = player
    })

    -- Atlas Aseprite: sprite por nomes de frame, não por índice fixo.
    atlas_sprite = aegis.newSprite("sprites/player-sheet.png")
    aegis.setPosition(atlas_sprite, 820, FLOOR_TOP - 96)
    aegis.setScale(atlas_sprite, 2, 2)
    local atlas = aegis.loadAtlas("sprites/player-sheet.json")
    aegis.setAtlasFrame(atlas_sprite, atlas, "idle_00")
    atlas_anim = aegis.newAtlasAnimator(atlas_sprite, atlas)
    aegis.addAtlasClip(atlas_anim, "run", {"run_00", "run_01", "run_02", "run_03"}, 12)
    aegis.addAtlasClip(atlas_anim, "idle", {"idle_00", "idle_01"}, 6)
    aegis.play(atlas_anim, "run")

    circle_sensor = aegis.newRect(12, 12, 1.0, 0.85, 0.25)
    aegis.setPosition(circle_sensor, 720, FLOOR_TOP - 80)
    circle_sensor_col = aegis.addCircleCollider(circle_sensor, 42)
    aegis.setTrigger(circle_sensor_col, true)
    aegis.setColliderLayer(circle_sensor_col, "PICKUP")
    aegis.setColliderMask(circle_sensor_col, "PLAYER")
    aegis.onCollideEnter(circle_sensor_col, function(a, b)
        circle_sensor_hit = true
    end)
    aegis.onCollideExit(circle_sensor_col, function(a, b)
        circle_sensor_hit = false
    end)

    aegis.setCameraTarget(player, 6)
    aegis.setCameraOffset(0, -50)
    aegis.setCameraLimits(0, 0, WORLD_W, SH)
    aegis.setCameraZoom(1.35)

    hud = aegis.newLabel("")
    aegis.setPosition(hud, 8, 6)
    aegis.setColor(hud, 0.9, 0.95, 0.9)

    reset_sandbox()
    aegis.log("Physics Lab — chão/plataformas = Collider estático; 1-4G R C")
end

function aegis_update(dt)
    if aegis.keyPressed("1") then g_idx = 1; aegis.setGlobalGravity(G_LIST[1]) end
    if aegis.keyPressed("2") then g_idx = 2; aegis.setGlobalGravity(G_LIST[2]) end
    if aegis.keyPressed("3") then g_idx = 3; aegis.setGlobalGravity(G_LIST[3]) end
    if aegis.keyPressed("4") then g_idx = 4; aegis.setGlobalGravity(G_LIST[4]) end
    if aegis.keyPressed("R") then reset_sandbox() end
    if aegis.keyPressed("C") then
        add_crate(aegis.getX(player) - 4, aegis.getY(player) - 200)
    end
    if aegis.keyPressed("F") and fire_emitter then
        aegis.stopEmitter(fire_emitter)
        fire_emitter = nil
    end

    local vx = 0
    if aegis.keyDown("Left")  or aegis.keyDown("A") then vx = -SPEED end
    if aegis.keyDown("Right") or aegis.keyDown("D") then vx =  SPEED end

    local vy = aegis.getVelocityY(player_rb)
    if (aegis.keyPressed("Space") or aegis.keyPressed("W") or aegis.keyPressed("Up"))
        and aegis.isGrounded(player_rb) then
        vy = JUMP
    end
    aegis.setVelocity(player_rb, vx, vy)

    -- Evita cair do mundo
    local px = aegis.getX(player)
    if px < 0 then
        aegis.setPosition(player, 0, aegis.getY(player))
        aegis.setVelocityX(player_rb, 0)
    elseif px > WORLD_W - P_W then
        aegis.setPosition(player, WORLD_W - P_W, aegis.getY(player))
        aegis.setVelocityX(player_rb, 0)
    end

    local g = G_LIST[g_idx]
    aegis.setText(hud, string.format(
        "G=%.0f [%d/4]  |  vy=%.0f  |  grounded=%s  |  circleTrigger=%s  |  A/D  jump  1-4G  R  C  F=stop emitter",
        g, g_idx, aegis.getVelocityY(player_rb), tostring(aegis.isGrounded(player_rb)), tostring(circle_sensor_hit)
    ))
end

function aegis_draw()
    if circle_sensor then
        aegis.drawCircle(aegis.getX(circle_sensor) + 42, aegis.getY(circle_sensor) + 42, 42, 1.0, 0.85, 0.25)
    end
end

--[[
Week 3 API examples:

-- Tilemap collision merge:
-- local mapa = aegis.loadTilemap("maps/fase1.json")
-- local count = aegis.buildTilemapColliders(mapa, {
--   solidGids = {1,2,3},
--   merge = true,
--   layer = "WORLD"
-- })
-- aegis.log("tilemap colliders: " .. count)

-- A* navigation:
-- local nav = aegis.navFromTilemap(mapa, { solidGids = {1,2,3}, diagonal = false })
-- local path = aegis.navFindPath(nav, 32, 32, 400, 160)

-- Object shaders:
-- aegis.setShader(player, "outline", { r=0, g=0, b=0, width=2 })
-- aegis.setShader(player, "flash", { r=1, g=0.2, b=0.2 })
-- aegis.clearShader(player)

-- Screen shader:
-- aegis.setScreenShader("vignette", { intensity = 0.5 })
-- aegis.clearScreenShader()

-- Audio/gamepad:
-- aegis.playSoundAt("explosion.wav", aegis.getX(player), aegis.getY(player), { maxDist=600 })
-- if aegis.padConnected(0) and aegis.padPressed(0, "A") then aegis.padVibrate(0, 0.5, 0.5, 0.3) end
]]
