# Asset Pipeline - Aegis Engine 0.9.9 Preview

Este documento define o fluxo oficial inicial de assets da Aegis Engine.

Objetivo:

```text
Dar um caminho simples e confiavel para organizar, validar e usar assets em
jogos 2D feitos com Aegis.
```

## 1. Estrutura Recomendada

Todo jogo deve manter assets dentro de `res/`:

```text
meu-jogo/
  main.lua
  aegis.toml
  aegis.cfg
  res/
    sprites/
    audio/
    fonts/
    tilemaps/
```

Pastas recomendadas:

| Pasta | Uso |
| --- | --- |
| `res/sprites/` | PNG/JPG/JPEG de sprites, tilesets e UI |
| `res/audio/` | WAV/OGG/MP3 |
| `res/fonts/` | TTF/OTF |
| `res/tilemaps/` | JSON/TMJ de mapas |

## 2. Como Referenciar Assets Em Lua

Sprite:

```lua
local player = aegis.create("sprite", {
    path = "sprites/player.png",
    x = 100,
    y = 100
})
```

Audio:

```lua
aegis.playSound("jump.wav")
aegis.playMusic("music.wav", true)
```

Tilemap:

```lua
local map = aegis.loadTilemap("tilemaps/level1.json")
```

Label com fonte fallback:

```lua
local title = aegis.create("label", {
    text = "Meu Jogo",
    size = 32,
    hud = true
})
```

## 3. Regras De Caminho

Use caminhos relativos ao `res/`.

Correto:

```lua
path = "sprites/player.png"
aegis.playSound("jump.wav")
aegis.loadTilemap("tilemaps/level1.json")
```

Evite:

```lua
path = "C:/Users/meu-pc/player.png"
path = "../fora-do-jogo/player.png"
```

Assets devem viajar junto com o jogo no build.

## 4. AssetManifest

A engine possui um manifesto interno:

```text
src/Aegis/Resource/AssetManifest.cs
```

Ele lista arquivos dentro de `res/` e categoriza:

- sprites;
- audio;
- fonts;
- tilemaps;
- data;
- unknown.

O manifesto e base para:

- `aegis doctor <jogo>`;
- editor visual;
- validacao antes do build;
- futuro `aegis.asset(...)`.

## 5. AssetValidator

A engine possui um validador inicial:

```text
src/Aegis/Resource/AssetValidator.cs
```

Ele valida:

- `main.lua`;
- `aegis.toml`;
- `aegis.cfg`;
- pasta `res/`;
- PNG por assinatura;
- WAV por cabecalho `RIFF/WAVE`, chunk `fmt` e `data`;
- JSON;
- TTF/OTF;
- referencias simples em Lua.

Comando:

```powershell
.\aegis.cmd doctor examples/demo-platformer
```

## 6. Saida Esperada Do Doctor

Exemplo:

```text
Aegis Doctor
Project: ...
Assets: 12 total, 5 sprite(s), 7 audio, 0 font(s), 3 tilemap(s)
INFO main.lua.present: main.lua found.
Summary: 0 error(s), 0 warning(s)
```

O doctor deve retornar erro quando:

- `main.lua` nao existe;
- `aegis.toml` nao existe;
- asset literal referenciado em Lua nao existe;
- PNG tem cabecalho invalido;
- WAV nao tem estrutura valida;
- JSON esta invalido.

Warnings sao usados para:

- `aegis.cfg` ausente, porque pode ser criado ao rodar;
- fonte local ausente, porque existe fallback;
- tileset externo ainda nao suportado totalmente.

## 7. Estado Atual

Implementado:

- `AssetManifest`;
- `AssetValidator`;
- `aegis doctor <jogo>`;
- resumo de assets por tipo no doctor;
- testes automatizados para manifesto e validador;
- validacao inicial de PNG/WAV/JSON/fontes.

Ainda falta:

- manifest persistido em arquivo;
- validacao de OGG/MP3;
- validacao profunda de tilemap;
- validacao de tilesets externos `.tsx`;
- integracao visual no Aegis Editor;
- futuro `aegis.asset(...)`.

## 8. API Futura Possivel

No futuro, a engine pode expor:

```lua
local ok = aegis.validateAssets()
local tex = aegis.asset("sprites/player.png")
```

Isso nao deve substituir `aegis.create`.

Para iniciantes, o caminho principal continua:

```lua
aegis.create("sprite", {
    path = "sprites/player.png"
})
```

## 9. Regras Para Cursos

Ao ensinar assets:

1. Explique a pasta `res/`.
2. Mostre sprites primeiro.
3. Mostre audio depois.
4. Mostre fontes.
5. Mostre tilemaps.
6. Rode `aegis doctor <jogo>`.
7. Corrija um erro proposital de asset ausente.
8. Faca build e confirme que assets foram empacotados.

## 10. Checklist De Projeto

Antes de publicar um jogo:

```text
[ ] main.lua existe
[ ] aegis.toml existe
[ ] aegis.cfg existe ou sera criado
[ ] assets estao dentro de res/
[ ] sprites carregam
[ ] audio nao gera warning
[ ] tilemaps carregam
[ ] doctor <jogo> nao tem errors
[ ] build win-x64 gera dist/
```
