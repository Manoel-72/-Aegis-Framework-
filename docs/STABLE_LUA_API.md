# Aegis Stable Lua API

Nota: a lista congelada do MVP e a separacao oficial entre Stable, Legacy e Experimental
fica em `docs/MVP_API.md`.
O build Windows do MVP deve passar com `aegis build meu-jogo --target win-x64`.

Este documento define a API recomendada para novos jogos e templates da Aegis Engine.
As APIs antigas continuam funcionando por compatibilidade, mas novo código deve preferir
a superfície menor abaixo.

## Regra de Arquitetura

- `LuaRuntime` registra e orquestra a API Lua.
- Criação de componentes deve ficar em fábricas isoladas.
- O caminho oficial de criação é `aegis.create(tipo, opts)`.
- Remoção oficial é `aegis.destroy(obj)`.
- Métodos antigos como `newSprite`, `newLabel`, `newRect` e `newProgressBar` são aliases legados.

## Criação Recomendada

Labels usam uma fonte padrao automatica. O jogo pode fornecer uma fonte melhor em
`res/fonts/Inter-Regular.ttf`, `res/fonts/NotoSans-Regular.ttf` ou carregar uma fonte
manual com `aegis.loadFont`.
Para texto grande, prefira `aegis.create("label", { size = 30 })` ou
`aegis.newLabelSize(text, 30)` em vez de ampliar com `setScale`, porque escalar
texto rasterizado deixa a fonte pixelada.

```lua
local player = aegis.create("sprite", {
  path = "res/sprites/player.png",
  x = 100,
  y = 120,
  pivotX = 0.5,
  pivotY = 0.5
})

local title = aegis.create("label", {
  text = "Novo Jogo",
  x = 400,
  y = 80,
  r = 1,
  g = 1,
  b = 1,
  pivotX = 0.5
})

local hp = aegis.create("progressBar", {
  x = 24,
  y = 24,
  width = 220,
  height = 16,
  bg = { r = 0.12, g = 0.12, b = 0.14 },
  fill = { r = 0.2, g = 0.9, b = 0.35 }
})
```

## Tipos Suportados

- `group`: objeto vazio para agrupar filhos.
- `sprite`: sprite por textura. Requer `path`.
- `rect`: retangulo colorido. Usa `width`/`height` ou `w`/`h`.
- `label`: texto simples. Usa `text`.
- `richLabel`: texto com markup. Usa `text` ou `markup`.
- `panel`: painel 9-slice. Requer `path`; aceita `border`, `width`, `height`.
- `flow`: container horizontal/vertical. Aceita `direction`, `gap`, `padding`, `align`.
- `progressBar`: barra de progresso. Aceita `width`, `height`, `bg`, `fill`.
- `anim` ou `animatedSprite`: spritesheet animado. Requer `path`, `frameWidth`/`frameHeight`.

## Opções Comuns

Todos os tipos aceitam:

```lua
{
  x = 0,
  y = 0,
  z = 0,
  scale = 1,
  scaleX = 1,
  scaleY = 1,
  rotation = 0,
  alpha = 1,
  visible = true,
  pivotX = 0,
  pivotY = 0
}
```

Para cores, use `r`, `g`, `b`, `a` com valores entre `0` e `1`.

## Como Evoluir

Para adicionar um novo componente:

1. Crie ou ajuste a classe do componente em `src/Aegis/Display`, `src/Aegis/Physics` ou domínio correto.
2. Adicione a criação em `src/Aegis/Scripting/Components/ComponentFactory.cs`.
3. Exponha via `aegis.create("novoTipo", opts)`.
4. Só adicione alias em `LuaRuntime` se for necessário para compatibilidade.

O objetivo é manter uma API pequena para jogos, sem transformar `LuaRuntime` em um ponto único de toda regra de criação.
