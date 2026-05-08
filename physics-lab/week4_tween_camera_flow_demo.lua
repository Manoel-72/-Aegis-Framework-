-- Week 4 demo: tween sequence, yoyo loop, camera deadzone/lookahead and FlowContainer.
local player
local hud
local seq

function aegis_init()
    player = aegis.newRect(32, 32, 0.2, 0.8, 1.0)
    aegis.setPosition(player, 80, 160)
    aegis.setZ(player, 10)

    aegis.setCameraTarget(player, 6)
    aegis.setCameraDeadzone(120, 80)
    aegis.setCameraLookahead(80, 4.0)

    local icon = aegis.newRect(18, 18, 1, 0.2, 0.2)
    local label = aegis.newLabel("HP 3  SCORE 000")
    hud = aegis.newFlow("horizontal", { gap = 8, padding = 12, align = "center" })
    aegis.flowAdd(hud, icon)
    aegis.flowAdd(hud, label)
    aegis.setPosition(hud, 10, 10)

    -- yoyo loop: efeito de pulsar no player
    aegis.tween(player, { scaleX = 1.3, scaleY = 1.3 }, 0.4, "inout", nil, { loop = true, yoyo = true })

    -- sequence: anda, espera, some; callback no fim
    seq = aegis.newSequence()
    aegis.seqAdd(seq, player, { x = 260 }, 0.35, "out")
    aegis.seqWait(seq, 0.15)
    aegis.seqAdd(seq, player, { alpha = 0.35 }, 0.4, "inout", function()
        aegis.log("sequence finished")
    end)
    aegis.seqPlay(seq)
end

function aegis_update(dt)
    local vx = 0
    if aegis.keyDown("Right") then vx = vx + 130 end
    if aegis.keyDown("Left") then vx = vx - 130 end
    if vx ~= 0 then aegis.move(player, vx * dt, 0) end
end
