# Aegis Roguelite Prototype

Exemplo oficial para testar mapa procedural na Aegis Engine.

Ele demonstra:

- `aegis.setRandomSeed(seed)`;
- `aegis.createTilemap(grid, opts)`;
- `aegis.buildTilemapColliders(map, opts)`;
- movimento top-down com colisao por grid;
- salas geradas por seed;
- spawn de inimigos, loot e portal;
- troca de andar sem carregar Tiled JSON.

## Rodar

Da raiz do framework:

```powershell
.\aegis.cmd run examples\roguelite-prototype
```

## Controles

```text
WASD ou setas - mover
Space          - ataque curto
R              - nova run
N              - proximo andar rapido
```

## Ideia

Este exemplo nao tenta ser um jogo completo. Ele e uma prova tecnica para saber
se a engine consegue sustentar um roguelite 2D com mapa em memoria.
