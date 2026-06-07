# Roadmap De Lancamento - Aegis Engine 2D

Este documento e o plano vivo para levar a Aegis Engine de MVP tecnico para uma
versao publica de qualidade. Ele deve ser atualizado sempre que uma tarefa for
iniciada, concluida ou adiada.

Meta principal:

```text
Criar uma engine 2D em C#/.NET + MonoGame + Lua, com fluxo profissional para
criar jogos 2D, construir mapas, validar assets, exportar builds e ensinar por
curso/documentacao.
```

Escopo atual:

- 2D apenas.
- Sem 3D por enquanto.
- Backend oficial: MonoGame DesktopGL.
- Raylib apenas em `legacy/raylib-v0`.
- Foco em jogos pequenos e medios.
- Foco em curso, criacao por IA, prototipos e lancamento publico preview.

## Legenda Visual

Status:

| Simbolo | Significado |
| --- | --- |
| `[x]` | Feito |
| `[~]` | Parcial / em andamento |
| `[ ]` | Nao iniciado |
| `[!]` | Critico para lancamento |
| `[B]` | Bonus / depois do lancamento |

Prioridade:

| Prioridade | Significado |
| --- | --- |
| P0 | Bloqueia lancamento publico |
| P1 | Muito importante para qualidade |
| P2 | Importante, mas pode vir depois |
| P3 | Futuro / bonus |

## Painel Geral

| Area | Status | Prioridade | Objetivo |
| --- | --- | --- | --- |
| Build verde e testes | `[x]` | P0 | Garantir que a engine compila e testes passam |
| API Stable/MVP | `[x]` | P0 | Ter contrato pequeno e seguro para jogos |
| Release package 0.9.9 | `[x]` | P0 | Gerar ZIP limpo e verificavel |
| CI/release workflow | `[x]` | P1 | Automatizar build/test/package |
| Repo limpo | `[~]` | P0 | Reduzir ruido e artefatos locais |
| Exemplos sem warnings | `[x]` | P0 | Demos publicas sem erro/warn normal |
| Assets pipeline | `[~]` | P0 | Validar e organizar assets do jogo |
| Map pipeline 2D | `[~]` | P0 | Transformar mapas em fluxo oficial da engine |
| Object layers/spawn | `[~]` | P0 | Criar entidades a partir do mapa |
| Scene stack | `[x]` | P0 | `pushScene/popScene` para pause, modal e inventario |
| Tile batching | `[ ]` | P1 | Melhorar performance de mapas grandes |
| Debug visual | `[~]` | P1 | Ver colliders, grid, stats e mapa |
| Editor Hub | `[~]` | P1 | Tela inicial clara para iniciantes |
| Editor preview | `[ ]` | P2 | Visualizar projeto/mapa no editor |
| Docs de lancamento | `[~]` | P0 | Documentacao clara para usuario final |
| Curso oficial | `[~]` | P1 | Material para ensinar a engine |
| Cross-platform validation | `[ ]` | P2 | Validar Linux/macOS em CI quando possivel |

## Fase 0 - Fundacao Ja Concluida

Objetivo: registrar o que ja esta bom para nao refazer trabalho.

| Tarefa | Status | Arquivos/Notas |
| --- | --- | --- |
| Backend MonoGame DesktopGL ativo | `[x]` | `src/Aegis` |
| CLI local `aegis.cmd` | `[x]` | `aegis.cmd`, `src/Aegis.CLI` |
| API recomendada `aegis.create` | `[x]` | `ComponentFactory` |
| ComponentFactory isolada | `[x]` | `src/Aegis/Scripting/Components/ComponentFactory.cs` |
| Stable / Experimental / Legacy documentado | `[x]` | `docs/MVP_API.md` |
| LuaRuntime modularizado | `[x]` | `LuaRuntime.*Api.cs` |
| Fonte fallback automatica | `[x]` | `FontManager` |
| Display `windowed` e `borderless` | `[x]` | config/display |
| Flip de sprite via Lua | `[x]` | `aegis.setFlip(sprite, flipX, flipY?)` |
| Stack de cenas | `[x]` | `aegis.pushScene`, `aegis.popScene` |
| Exemplos em `examples/` | `[x]` | `examples/*` |
| Release ZIP 0.9.9 | `[x]` | `dist/Aegis-Framework-v0.9.9.zip` |
| Checklist de release | `[x]` | `docs/RELEASE_CHECKLIST_0.9.9.md` |
| CI/release workflow | `[x]` | `.github/workflows/ci-release.yml` |

Resultado esperado desta fase:

```text
Base tecnica pronta para evoluir sem quebrar o MVP.
```

## Fase 1 - Fechar Lancamento Preview 0.9.9

Objetivo: deixar o pacote publico sem sinais obvios de prototipo.

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Corrigir warnings de audio nos exemplos | `[x]` | P0 | `demo-platformer` roda sem `[Audio|WARN]` |
| Melhorar `aegis doctor` na raiz da engine | `[x]` | P1 | Doctor entende quando esta na raiz do framework |
| Testar ZIP em pasta limpa | `[x]` | P0 | `doctor`, `run`, `build` passaram |
| Atualizar release notes com estado real | `[~]` | P0 | Comandos usam `examples/...` e explicam preview |
| Garantir pacote sem arquivos antigos | `[x]` | P0 | `verify.ps1` valida conteudo proibido |
| Regenerar ZIP final depois dos ajustes | `[~]` | P0 | `package-release.ps1` passa em Release |
| Testar exemplo principal por 10-15 min | `[ ]` | P1 | Sem tela preta/crash/log novo |
| Validar pause menu via `pushScene/popScene` | `[~]` | P1 | Teste automatizado passa; falta exemplo visual |

Entregavel:

```text
Aegis-Framework-v0.9.9.zip pronto para download publico preview.
```

## Fase 2 - Asset Pipeline MVP

Objetivo: transformar assets em parte confiavel da engine.

Problema atual:

```text
Assets carregam, mas a engine ainda nao tem um fluxo forte de validacao,
catalogo, diagnostico e erros amigaveis.
```

Tarefas:

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Criar `AssetManifest` interno | `[x]` | P0 | Lista sprites, audio, fontes, mapas |
| Criar `aegis doctor <jogo>` | `[x]` | P0 | Valida assets de uma pasta de jogo |
| Validar imagem ausente com erro claro | `[x]` | P0 | Erro mostra caminho esperado e pasta `res/` |
| Validar audio invalido | `[x]` | P0 | Doctor detecta `.wav` quebrado antes do runtime |
| Validar fonte fallback | `[~]` | P1 | Doctor mostra fonte escolhida |
| Padronizar pastas `res/` | `[~]` | P1 | Docs e templates usam mesmo padrao |
| Criar doc `ASSET_PIPELINE.md` | `[x]` | P1 | Explica sprites, audio, fontes e mapas |

API futura desejada:

```lua
local ok = aegis.validateAssets()
local tex = aegis.asset("sprites/player.png")
```

Regra de arquitetura:

```text
Nao espalhar validacao de asset pelo LuaRuntime. Criar modulo/fachada propria.
```

## Fase 3 - Map Pipeline 2D

Objetivo: mapa virar recurso oficial de primeira classe, nao apenas JSON que
carrega tiles.

Tarefas:

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Criar `MapApi` separada | `[ ]` | P0 | Registro Lua isolado do runtime principal |
| Criar `aegis.loadMap` | `[ ]` | P0 | Fachada limpa para mapas novos |
| Suportar layers nomeadas | `[ ]` | P0 | Buscar layer por nome, nao so indice |
| Suportar opacidade/visibilidade de layer | `[x]` | P1 | Tiled visible/opacity respeitado |
| `setTile/getTile` por nome de layer | `[x]` | P1 | Aceita indice ou nome da layer |
| Suportar object layers do Tiled | `[x]` | P0 | Objetos do mapa acessiveis via Lua |
| Ler properties de map/layer/tile/object | `[~]` | P0 | Map/layer/object ja leem propriedades; tile props parciais |
| Suportar `.tmx` | `[B]` | P2 | Fora do escopo v1.0; formato oficial v1.0 e Tiled JSON |
| Suportar tilesets externos `.tsx` | `[ ]` | P1 | Tiled moderno funciona melhor |
| Suportar collision por propriedade | `[ ]` | P0 | `solid=true` gera collider |
| Suportar object spawn | `[x]` | P0 | Handlers Lua criam entidades a partir do mapa |
| Suportar tilemap procedural por grid Lua | `[x]` | P0 | `aegis.createTilemap(grid, opts)` cria mapa em memoria |
| Formato oficial `.scene.json` 2D | `[x]` | P0 | Base inicial para Editor visual abrir/salvar cenas |
| Suportar seed controlada | `[x]` | P1 | `aegis.setRandomSeed(seed)` repete sorteios |
| Criar testes de tilemap/mapa | `[~]` | P0 | Testes cobrem parser Tiled e object layers |
| Criar doc `2D_WORLD_AND_MAP_PIPELINE.md` | `[x]` | P0 | Guia oficial para construir mapa |

API alvo:

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
        spawnEnemy(obj.x, obj.y, obj.properties.type)
    end
})
```

Entregavel:

```text
Usuario consegue montar fase no Tiled e rodar na Aegis com colisao, objetos e
spawns sem posicionar tudo manualmente no Lua.
```

Decisao de v1.0:

```text
O formato oficial de mapas da Aegis v1.0 e Tiled JSON.
.tmx/XML fica planejado para v1.1+ para evitar risco extra no lancamento.
```

## Fase 4 - Tile Rendering E Performance

Objetivo: aproximar a engine de um fluxo tipo `TileGroup`, desenhando mapas
grandes com mais eficiencia.

Tarefas:

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Medir performance atual do tilemap | `[ ]` | P1 | FPS/tempo de draw documentado |
| Criar batches por layer/tileset | `[ ]` | P1 | Menos draw overhead em mapas grandes |
| Evitar alocacao por frame no tilemap | `[ ]` | P1 | Sem GC perceptivel em mapa grande |
| Rebuild parcial quando tile muda | `[ ]` | P2 | Atualiza apenas chunk afetado |
| Criar mapa benchmark | `[ ]` | P1 | Exemplo 200x200 ou maior |
| Teste de culling por camera | `[~]` | P1 | Culling ja existe, precisa teste |

API/arquitetura desejada:

```text
TilemapNode
  TileLayerBatch
  TileChunk
  TileDrawCache
```

Entregavel:

```text
Mapas grandes rodam estaveis, com culling e batching previsiveis.
```

## Fase 5 - Debug Visual E Diagnostico

Objetivo: facilitar desenvolvimento, curso e correcao de bugs.

Tarefas:

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Debug draw de colliders | `[~]` | P1 | Toggle visivel e documentado |
| Debug draw de grid/tilemap | `[ ]` | P1 | Mostra grid e bounds do mapa |
| Debug draw de object layers | `[ ]` | P1 | Mostra nomes/retangulos dos objetos |
| Debug stats oficial | `[~]` | P1 | FPS, objetos, colliders, tiles |
| Doctor com diagnostico de jogo | `[ ]` | P0 | `doctor <jogo>` claro para usuario |
| Logs padronizados | `[~]` | P1 | Warnings acionaveis, sem ruido normal |
| Crash log mais amigavel | `[~]` | P1 | Indica cena/arquivo Lua quando possivel |

API alvo:

```lua
aegis.setDebugDraw(true)
aegis.setDebugLayer("colliders", true)
aegis.setDebugLayer("tilegrid", true)
aegis.setDebugLayer("mapObjects", true)
```

Entregavel:

```text
Aluno/dev consegue ver o que esta acontecendo no mapa e na fisica.
```

## Fase 6 - Editor Hub E Preview

Objetivo: transformar o AegisEditor em uma porta de entrada clara para
iniciantes, sem tentar fazer um editor completo cedo demais.

### Fase 6A - Editor Hub

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Tela inicial limpa | `[x]` | P1 | Editor abre no Hub, nao no painel tecnico |
| Botao Novo Projeto | `[~]` | P1 | Botao existe; wizard visual fica para 6B |
| Botao Abrir Projeto | `[x]` | P1 | Abre seletor de pasta e valida `main.lua` |
| Botao Abrir Exemplo | `[x]` | P1 | Abre `examples/demo-platformer` quando encontrado |
| Botao Documentacao | `[x]` | P1 | Abre o guia principal de curso/docs |
| Lista de projetos recentes | `[x]` | P1 | Persiste projetos em AppData |
| Voltar do workspace para Hub | `[x]` | P1 | Botao `Hub` volta para tela inicial |

### Fase 6B - Run/Build/Doctor Visual

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Botao Run amigavel | `[~]` | P1 | Run + Connect existe, precisa simplificar texto |
| Botao Stop | `[x]` | P1 | Para o runtime pelo pipe/processo sem abrir copias extras |
| Botao Restart | `[x]` | P1 | Para e inicia o runtime novamente pelo editor |
| Botao Doctor | `[ ]` | P1 | Executa `aegis doctor <projeto>` e mostra problemas |
| Botao Build Windows | `[ ]` | P1 | Executa `aegis build <projeto> --target win-x64` |
| Painel de problemas | `[ ]` | P1 | Mostra erros/warnings do AssetValidator |
| Console/logs claro | `[~]` | P1 | Console existe, precisa linguagem mais amigavel |

### Fase 6C - Project Explorer

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Listar cenas | `[ ]` | P1 | Mostra `main.lua` e `scenes/*.lua` |
| Listar assets | `[ ]` | P1 | Mostra sprites, audio, fonts e tilemaps |
| Abrir arquivo selecionado | `[ ]` | P2 | Abre no editor Lua ou explorador |
| Mostrar configuracoes do projeto | `[ ]` | P2 | Exibe titulo, resolucao, displayMode |

### Fase 6D - Preview

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Preview de tilemap | `[ ]` | P2 | Renderiza mapa sem rodar jogo |
| Preview de colliders | `[ ]` | P2 | Mostra colisao gerada |
| Preview de object layers | `[ ]` | P2 | Mostra nomes/retangulos dos objetos |

### Fase 6E - Scene View Profissional

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Zoom no Scene View | `[x]` | P1 | Mouse wheel aproxima/afasta com limite seguro |
| Pan no Scene View | `[x]` | P1 | Botao direito ou meio arrasta a camera do editor |
| Snap grid | `[x]` | P1 | Botao Snap liga/desliga alinhamento em grid 32px |
| Reset de viewport | `[x]` | P1 | Botao Reset volta zoom/pan ao padrao |
| Selecionar entidade no canvas | `[x]` | P1 | Clique seleciona objeto real da cena |
| Arrastar entidade | `[x]` | P1 | Movimento respeita snap quando ativo |
| Deletar entidade | `[x]` | P1 | Delete/Backspace remove da cena, hierarchy e inspector |
| Gizmo de mover | `[x]` | P1 | Entidade selecionada mostra eixos X/Y no canvas |
| Evitar runtime duplicado | `[x]` | P0 | Play nao abre varias janelas se uma ja estiver viva |

Nao fazer ainda:

```text
Editor completo de tiles.
```

Primeiro objetivo:

```text
Hub, diagnostico e preview. Pintura de mapa pode vir depois.
```

## Fase 7 - Documentacao E Curso

Objetivo: tornar a engine ensinavel e usavel por IA.

Tarefas:

| Tarefa | Status | Prioridade | Criterio De Pronto |
| --- | --- | --- | --- |
| Guia para IA criar curso | `[x]` | P1 | `GUIA_CURSO...` atualizado |
| MVP API documentada | `[x]` | P0 | `docs/MVP_API.md` |
| Stable Lua API | `[x]` | P1 | `docs/STABLE_LUA_API.md` |
| Guia de assets | `[x]` | P1 | `ASSET_PIPELINE.md` |
| Guia de mapas 2D | `[ ]` | P0 | `2D_WORLD_AND_MAP_PIPELINE.md` |
| Tutorial primeiro jogo | `[ ]` | P1 | Do zero ao build |
| Tutorial mapa com Tiled | `[ ]` | P0 | Criar mapa, colisao e spawn |
| Checklist de release | `[x]` | P0 | `RELEASE_CHECKLIST_0.9.9.md` |

Entregavel:

```text
Usuario novo consegue instalar, criar jogo, criar mapa e exportar sem ajuda.
```

## Fase 8 - Lancamento Publico

Objetivo: publicar com qualidade controlada.

Checklist minimo:

| Item | Status |
| --- | --- |
| Build Release passa | `[x]` |
| Testes passam | `[x]` |
| ZIP limpo gerado | `[x]` |
| ZIP testado em pasta limpa | `[x]` |
| Exemplo principal sem warnings | `[x]` |
| Exemplo roguelite procedural | `[x]` |
| Release notes atualizadas | `[~]` |
| Install guide atualizado | `[x]` |
| Site com comandos corretos | `[~]` |
| CI passando no GitHub | `[ ]` |
| Versao/tag criada | `[ ]` |

Comando oficial de release:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1 -Version 0.9.9 -Configuration Release
```

## Fase 9 - Pos-Lancamento

Objetivo: evoluir sem quebrar usuarios.

Tarefas:

| Tarefa | Status | Prioridade |
| --- | --- | --- |
| Suporte Linux build testado | `[ ]` | P2 |
| Suporte macOS build testado em CI | `[ ]` | P2 |
| Templates extras | `[ ]` | P2 |
| LDtk import | `[ ]` | P2 |
| Hot reload de assets | `[ ]` | P2 |
| Editor de mapa proprio | `[B]` | P3 |
| Marketplace/plugin system | `[B]` | P3 |

## Ordem Recomendada De Implementacao Agora

Esta e a ordem mais segura para melhorar muito sem refatorar demais:

1. Corrigir warnings de audio dos exemplos.
2. Melhorar `aegis doctor <jogo>` para validar assets.
3. Criar `docs/2D_WORLD_AND_MAP_PIPELINE.md`.
4. Implementar leitura de object layers do Tiled.
5. Implementar `aegis.spawnMapObjects`. `[x]`
6. Adicionar testes de tilemap/mapa.
7. Criar debug visual de grid/object layers.
8. Melhorar tilesets externos `.tsx`.
9. Criar batching/chunks de tilemap.
10. Criar preview de mapa no editor.
11. Avaliar suporte `.tmx` depois do v1.0.

## Criterios Para Dizer Que Esta Pronto Para Lancar

A engine pode ser publicada como preview quando:

- usuario baixa ZIP e roda demo sem crash;
- demo principal nao gera warning normal no log;
- `aegis.cmd doctor` explica problemas de forma clara;
- `aegis.cmd new` cria projeto funcional;
- `aegis.cmd build` gera ZIP de jogo funcional;
- `examples/roguelite-prototype` prova mapa procedural, seed e build;
- docs explicam instalacao e primeiro jogo;
- exemplos usam API Stable/MVP;
- CI roda build/test/package;
- pacote nao contem lixo local, saves, logs ou artefatos antigos.

A engine pode ser chamada de engine 2D madura quando:

- mapa e asset pipeline forem oficiais;
- Tiled JSON import estiver robusto;
- object layers virarem entidades;
- debug visual ajudar producao;
- tile batching aguentar mapas grandes;
- editor tiver preview util;
- testes cobrirem core, assets, mapas, build e cenas.

## Como Atualizar Este Roadmap

Ao iniciar uma tarefa:

```text
[ ] -> [~]
```

Ao concluir:

```text
[~] -> [x]
```

Se a tarefa bloquear lancamento:

```text
[!]
```

Sempre registre no final do commit ou anotacao:

```text
Fase:
Tarefa:
Status:
Validacao:
Proximo passo:
```
