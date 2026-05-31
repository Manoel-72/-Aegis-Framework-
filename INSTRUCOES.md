# Aegis Engine — Patch v0.4.3

## O que está neste zip

Copie cada arquivo para o local indicado no seu projeto.

| Arquivo no zip                        | Destino no projeto              | Ação     |
|---------------------------------------|---------------------------------|----------|
| src/Scripting/LuaBridge.cs            | src/Scripting/LuaBridge.cs      | CRIAR    |
| src/Core/Engine.cs                    | src/Core/Engine.cs              | SUBSTITUIR |
| src/Camera/Camera.cs                  | src/Camera/Camera.cs            | SUBSTITUIR |

## Rodar (repo atual)

```bash
dotnet run --project src/Aegis.CLI -- run examples/physics-lab
```

## O que foi corrigido

### Bug principal: LuaBridge.cs faltava → projeto não compilava
O arquivo `src/Scripting/LuaBridge.cs` estava ausente. Era a causa raiz de tudo.

### Bug de pulo: setVel(body, vx, 0) matava VelocityY
A função `setVel` agora é segura:
- Se `vy == 0` E o body está em voo → VelocityY NÃO é cancelado
- O pulo funciona corretamente

### Bug de restart: Engine/Camera não limpavam estado
- `Engine.ClearNodes()` limpa world + HUD
- `Camera.Reset()` reseta todos os parâmetros
- `clearAll()` no Lua chama os dois + `World.Clear()`

### Filtro de eixo (World.cs — já estava correto)
- Passo X: só resolve se `overlapX < overlapY`
- Passo Y: só resolve se `overlapY <= overlapX`
- Evita o player ser empurrado lateralmente por um chão largo

## API de física correta (main.lua)

```lua
-- MOVER (não toca VelocityY):
setVelX(body, 280)

-- PULAR (impulso negativo = sobe):
if isGrounded(body) then
    jumpY(body, -560)
end

-- RETROCOMPAT (seguro agora):
setVel(body, vx, 0)  -- não cancela mais o pulo em voo
```
