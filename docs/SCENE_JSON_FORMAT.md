# Aegis Scene JSON Format

`*.scene.json` e o formato oficial inicial para cenas visuais 2D da Aegis Editor.

Este formato nao substitui Lua no v0.9.9. Ele e a base para o editor abrir, exibir, salvar e instanciar cenas visuais 2D no runtime.

## Exemplo

```json
{
  "format": "aegis.scene",
  "version": 2,
  "name": "Main",
  "kind": "2d",
  "width": 1280,
  "height": 720,
  "entities": [
    {
      "id": "player",
      "name": "Player",
      "type": "Sprite",
      "components": {
        "Transform": {
          "position": [160, 320],
          "rotation": 0,
          "scale": [1, 1]
        },
        "SpriteRenderer": {
          "sprite": "sprites/player.png",
          "color": [1, 1, 1, 1],
          "layer": 0,
          "flip_x": false
        },
        "Collider2D": {
          "shape": "box",
          "size": [32, 48],
          "offset": [0, 0],
          "is_trigger": false
        },
        "Rigidbody2D": {
          "type": "dynamic",
          "gravity_scale": 1,
          "linear_drag": 0
        }
      }
    }
  ],
  "tilemaps": []
}
```

## Campos

- `format`: sempre `aegis.scene`.
- `version`: versao do formato. A versao atual e `2`; cenas v1 sao migradas em memoria.
- `name`: nome amigavel da cena.
- `kind`: `2d`. A engine nao assume 3D neste ciclo.
- `width` / `height`: tamanho de referencia da cena.
- `entities`: objetos principais da cena.
- `tilemaps`: referencias a mapas Tiled JSON usados pela cena.

## Entidades

Campos principais:

- `id`: identificador estavel dentro da cena.
- `name`: nome exibido na Hierarchy.
- `type`: tipo visual inicial, como `Camera`, `Sprite`, `Group` ou `Tilemap`.
- `components`: formato novo orientado a componentes. A v2 ja aceita `Transform` e `SpriteRenderer`.
- `x`, `y`: posicao 2D.
- `scaleX`, `scaleY`: escala.
- `rotation`: rotacao em radianos.
- `texturePath`: sprite relativo a `res/`, quando aplicavel.
- `scriptPath`: script relativo ao projeto, quando aplicavel.

Os campos diretos `x`, `y`, `scaleX`, `scaleY`, `rotation` e `texturePath` continuam aceitos por compatibilidade. Para cenas novas, prefira `components`.

## Componentes Suportados No Runtime

### Transform

```json
"Transform": {
  "position": [100, 200],
  "rotation": 0,
  "scale": [1, 1]
}
```

Aplica posicao, rotacao e escala ao objeto criado.

### SpriteRenderer

```json
"SpriteRenderer": {
  "sprite": "sprites/player.png",
  "color": [1, 1, 1, 1],
  "flip_x": false,
  "flip_y": false
}
```

Carrega o sprite relativo a `res/`. Nesta etapa, `color` aplica apenas alpha (`color[3]`) e `flip_x`/`flip_y` aplicam espelhamento quando o objeto renderizavel for um sprite.

### Collider2D

```json
"Collider2D": {
  "shape": "box",
  "size": [32, 48],
  "offset": [0, 0],
  "is_trigger": false
}
```

Cria e registra um collider no runtime. `shape` aceita `box` e `circle`.

### Rigidbody2D

```json
"Rigidbody2D": {
  "type": "dynamic",
  "gravity_scale": 1,
  "linear_drag": 0
}
```

Cria e registra um corpo fisico. `type` aceita `dynamic`, `kinematic` e `static`.

## Estado Atual

Implementado:

- serializer com metadados `format`, `version`, `kind`, `width`, `height`;
- save atomico no editor usando arquivo temporario antes de substituir a cena;
- criacao automatica de `scenes/main.scene.json` ao abrir projeto no editor;
- load/save local no editor;
- preview simples via Hierarchy, Inspector e Scene View;
- runtime carrega `.scene.json` com `aegis.loadScene("scenes/main.scene.json")`;
- Play do editor salva `scenes/active.scene.json` e inicia o runtime com `--scene=scenes/active.scene.json`;
- `SCENE_LOAD` do editor aceita `.scene.json` e instancia a cena no jogo;
- entidades `Sprite`, `Group`, `Empty` e `Camera` preservam transform basico;
- `Sprite.texturePath` carrega textura relativa a `res/`.
- formato `components` v2 aceito no runtime para `Transform`, `SpriteRenderer`, `Collider2D` e `Rigidbody2D`.
- AssetValidator verifica referencias de sprite e script dentro de `.scene.json`.

## Uso no Lua

```lua
function aegis_init()
    aegis.loadScene("scenes/main.scene.json")
end
```

Por padrao, `aegis.loadScene(path)` limpa o mundo antes de instanciar a cena. Para carregar sem limpar:

```lua
aegis.loadScene("scenes/decoracao.scene.json", false)
```

Ainda nao implementado:

- componentes runtime avancados declarados no JSON;
- scripts por entidade;
- `.prefab.json`;
- ECS puro com IDs numericos;
- tilemaps instanciados automaticamente pela lista `tilemaps`;
- drag and drop visual completo.
