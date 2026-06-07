# Aegis 2D World And Map Pipeline

Este guia define o fluxo oficial para criar fases 2D na Aegis Engine usando
mapas externos, principalmente Tiled JSON.

Status desta fase: em andamento.

## Objetivo

O mapa deve deixar de ser apenas um desenho de tiles e virar uma fonte de dados
do jogo:

- camadas visuais;
- colisao;
- spawns;
- zonas de trigger;
- pontos de camera;
- propriedades de fase;
- dados para IA e navegacao.

## Estrutura Recomendada

```text
meu-jogo/
  main.lua
  aegis.toml
  aegis.cfg
  res/
    sprites/
      tiles.png
      player.png
      enemy.png
    audio/
      stage1.wav
    tilemaps/
      fase1.json
```

## Tiled

Use mapas ortogonais exportados como JSON.

Decisao para v1.0:

```text
Tiled JSON e o formato oficial de mapa da Aegis v1.0.
.tmx/XML fica para uma versao posterior.
```

Motivo:

- JSON ja e suportado pelo runtime atual;
- JSON ja e validado pelo `aegis doctor`;
- JSON e mais simples de testar e documentar;
- reduz risco antes do lancamento publico;
- o Tiled exporta JSON de forma nativa.

Hoje o runtime suporta:

- tile layers com `data` em array ou CSV;
- tilesets embutidos com `image`;
- `visible` e `opacity` de layer;
- propriedade `solid=true` em tile para gerar colisao;
- object layers lidos como dados do mapa.
- tilemap procedural criado por grid Lua.

Ainda esta planejado:

- tilesets externos `.tsx`;
- `.tmx`/XML;
- spawn automatico por objeto;
- debug visual de object layers;
- preview visual no editor.

## Tile Layers

Tile layers sao usadas para desenhar o mapa.

```lua
local map = aegis.loadTilemap("tilemaps/fase1.json")
```

## Mapas Procedurais Em Lua

Para criar mapa sem arquivo JSON, monte uma tabela Lua com GIDs e passe para
`aegis.createTilemap`.

```lua
local grid = {
    { 1, 1, 1, 1, 1 },
    { 1, 0, 0, 0, 1 },
    { 1, 0, 2, 0, 1 },
    { 1, 1, 1, 1, 1 }
}

local map = aegis.createTilemap(grid, {
    tileset = "sprites/tiles.png",
    tileWidth = 16,
    tileHeight = 16
})
```

Tambem pode usar tabela plana:

```lua
local grid = {
    1, 1, 1,
    1, 0, 1,
    1, 1, 1
}

local map = aegis.createTilemap(grid, {
    tileset = "sprites/tiles.png",
    width = 3,
    height = 3,
    tileWidth = 16
})
```

Depois disso, a colisao usa o mesmo fluxo dos mapas do Tiled:

```lua
aegis.buildTilemapColliders(map, {
    solidGids = { 1 },
    merge = true
})
```

Para randomizacao repetivel:

```lua
aegis.setRandomSeed(12345)

local x = aegis.randomInt(1, 10)
local y = aegis.randomInt(1, 10)
```

Mesma seed, mesmo resultado.

Para colisao por GID:

```lua
aegis.buildTilemapColliders(map, {
    solidGids = { 1, 2, 3 },
    merge = true
})
```

Para colisao por propriedade do Tiled:

```lua
aegis.buildTilemapColliders(map, {
    solidGids = {},
    useTiledProperty = true,
    merge = true
})
```

## Object Layers

Object layers guardam dados de gameplay. No Tiled, crie uma camada do tipo
object layer chamada, por exemplo, `Objects`.

Exemplos de tipos:

```text
player_spawn
enemy
checkpoint
door
camera_zone
```

Cada objeto pode ter:

- `name`;
- `type`;
- `x`, `y`;
- `width`, `height`;
- propriedades customizadas.

## Lendo Objetos No Lua

Todos os objetos:

```lua
local objects = aegis.mapObjects(map)

for i, obj in ipairs(objects) do
    aegis.log(obj.type .. " em " .. obj.x .. "," .. obj.y)
end
```

Filtrando por tipo ou nome:

```lua
local spawns = aegis.mapObjectsByType(map, "player_spawn")

if #spawns > 0 then
    local spawn = spawns[1]
    player.x = spawn.x
    player.y = spawn.y
end
```

Lendo propriedades:

```lua
local enemies = aegis.mapObjectsByType(map, "enemy")

for i, obj in ipairs(enemies) do
    local enemyKind = obj.properties.kind or "basic"
    spawnEnemy(obj.x, obj.y, enemyKind)
end
```

## Criando Entidades A Partir Do Mapa

Use `aegis.spawnMapObjects` quando quiser transformar object layers em objetos
do jogo. A engine le cada objeto do mapa e chama o handler pelo `type`.

```lua
local created = aegis.spawnMapObjects(map, {
    player_spawn = function(obj)
        spawnPlayer(obj.x, obj.y)
    end,

    enemy = function(obj)
        local enemyKind = obj.properties.kind or "basic"
        spawnEnemy(obj.x, obj.y, enemyKind)
    end,

    default = function(obj)
        aegis.log("Objeto de mapa sem handler: " .. obj.name)
    end
})

aegis.log("Objetos criados: " .. created)
```

O handler pode ser encontrado por `type`, depois por `name`, e por ultimo pelo
handler `default`, se existir.

## Regra De Arquitetura

O carregamento visual continua em `TilemapNode`.

A leitura de dados do Tiled fica centralizada em `TiledMapDocument`, para que:

- testes possam validar mapa sem abrir janela grafica;
- o editor consiga ler mapas sem rodar o jogo;
- o doctor possa diagnosticar mapas antes do runtime;
- futuras APIs como `aegis.loadMap` usem a mesma base.

## API Atual

```lua
local map = aegis.loadTilemap("tilemaps/fase1.json")
local proc = aegis.createTilemap(grid, opts)
local objects = aegis.mapObjects(map)
local enemies = aegis.mapObjectsByType(map, "enemy")
aegis.spawnMapObjects(map, handlers)
```

## API Alvo

Esta fachada maior ainda sera implementada em etapa futura:

```lua
local map = aegis.loadMap("tilemaps/fase1.json", {
    collision = true,
    nav = true
})

aegis.spawnMapObjects(map, {
    player_spawn = function(obj)
        spawnPlayer(obj.x, obj.y)
    end,
    enemy = function(obj)
        spawnEnemy(obj.x, obj.y, obj.properties.kind)
    end
})
```

## Checklist Do Mapa

Antes de publicar uma fase:

```text
[ ] mapa esta em res/tilemaps/
[ ] tileset esta dentro de res/
[ ] tile layers aparecem corretamente
[ ] object layer tem spawns necessarios
[ ] objetos possuem type claro
[ ] propriedades customizadas estao escritas sem erro
[ ] colisao foi gerada por GID ou solid=true
[ ] doctor nao mostra erro
```
