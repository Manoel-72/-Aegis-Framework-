# Plano de limpeza e refatoracao da Aegis Engine

Este plano organiza as proximas melhorias da engine com foco em MVP tecnico,
qualidade de codigo e preparacao para lancamento publico.

## Estado atual

A engine ativa usa:

- C#/.NET 8;
- MonoGame DesktopGL;
- NLua;
- projetos ativos em `src/Aegis` e `src/Aegis.CLI`.

O build atual compila com sucesso.

## Achado importante: codigo Raylib legado

Existia codigo antigo em Raylib fora dos projetos ativos. Ele foi isolado em:

```text
legacy/raylib-v0/
```

Arquivos movidos para essa area:

- `legacy/raylib-v0/src/Core/Engine.cs`
- `legacy/raylib-v0/src/Scripting/LuaBridge.cs`
- `legacy/raylib-v0/src/Camera/Camera.cs`
- `legacy/raylib-v0/src/Resource/ResManager.cs`
- `legacy/raylib-v0/src/Scene/SpriteNode.cs`

Esses arquivos usam `Raylib_cs`, mas os projetos ativos encontrados sao:

- `src/Aegis/Aegis.csproj`
- `src/Aegis.CLI/Aegis.CLI.csproj`

Nenhum desses projetos referencia `Raylib_cs`. Portanto, o bloco Raylib parece
ser legado de uma versao anterior e nao deve ser misturado com o MVP atual.

### Decisao recomendada

Para MVP, manter MonoGame DesktopGL como backend principal.

Raylib pode ser mantido apenas como:

- referencia historica;
- experimento separado;
- possivel backend futuro, se houver motivo forte.

Nao recomendo trocar para Raylib agora, porque isso colocaria em risco:

- API Lua ja estabilizada;
- build/export atual;
- audio, fonte, UI, tilemap e pipeline MonoGame;
- documentacao MVP.

## Etapa 1 - Limpeza segura de repositorio

Objetivo: reduzir ruido sem alterar comportamento.

Tarefas:

- mover zips da raiz para `releases/` ou remover do repositorio;
- criar `examples/` ou `samples/`;
- mover jogos de exemplo para `examples/`;
- manter codigo Raylib legado isolado em `legacy/raylib-v0/`;
- atualizar README para apontar exemplos e backend oficial.

Regra: fazer uma mudanca por vez e compilar.

## Etapa 2 - Refatorar LuaRuntime sem mudar API

Objetivo: transformar `LuaRuntime.cs` em orquestrador.

Modulo alvo:

- `LuaCoreApi`
- `LuaDisplayApi`
- `LuaInputApi`
- `LuaPhysicsApi`
- `LuaAudioApi`
- `LuaSceneApi`
- `LuaConfigApi`
- `LuaEffectsApi`
- `LuaExperimentalApi`

Primeiro passo recomendado:

1. Extrair apenas registros de API para uma classe auxiliar.
2. Compilar.
3. Extrair um grupo pequeno, como config/display.
4. Compilar.
5. Repetir.

Evitar reescrever toda a runtime de uma vez.

## Etapa 3 - Endurecer build/export

Objetivo: build confiavel para usuarios.

Tarefas:

- teste automatizado para `aegis build`;
- validar `JOGAR.bat`;
- validar `aegis-build.json`;
- validar ausencia de logs antigos no pacote;
- testar jogo em maquina limpa Windows;
- documentar `displayMode: "windowed"` e `"borderless"`.

## Etapa 4 - Testes automatizados

Prioridade dos testes:

1. `ComponentFactory`
2. `ConfigManager`
3. `SceneManager`
4. fallback de fonte
5. build/export
6. transicao de cena
7. fisica basica

## Etapa 5 - Polimento para publico

Tarefas:

- remover comentarios internos como `Sprint`, `BUG #`, `Final`;
- trocar comentarios temporarios por comentarios tecnicos permanentes;
- padronizar logs;
- organizar docs;
- reduzir exemplos duplicados;
- criar tutorial oficial curto;
- criar template oficial usando apenas API Stable/MVP.

## Raylib vs MonoGame para o MVP

### Raylib

Vantagens:

- API simples;
- facil para prototipos;
- boa para exemplos pequenos;
- menor complexidade inicial.

Riscos para esta engine agora:

- exigiria reescrever o backend atual;
- quebraria parte do investimento em MonoGame;
- mudaria pipeline de fonte, audio, janela e render;
- aumentaria o risco antes do MVP.

### MonoGame

Vantagens:

- ja e o backend ativo;
- ja compila;
- ja exporta Windows;
- integra bem com C#/.NET;
- bom para engine 2D customizada;
- mais adequado para crescer com arquitetura propria.

Riscos:

- display/fullscreen exige cuidado;
- algumas coisas precisam ser feitas pela engine, como fonte, UI e pipeline.

### Decisao recomendada

Para o MVP atual: continuar com MonoGame DesktopGL.

Raylib nao deve ser removido sem avaliacao, mas tambem nao deve guiar a versao
principal. O codigo Raylib atual deve ser tratado como legado e isolado.

## Ordem recomendada das proximas tarefas

1. Isolar codigo Raylib legado.
2. Organizar zips e exemplos fora da raiz.
3. Criar primeiro projeto de testes.
4. Refatorar `LuaRuntime` por fatias pequenas.
5. Endurecer build/export com testes.
6. Validar display em teste longo.
7. Preparar README publico.

## Criterio para subir a nota da engine

Para MVP tecnico interno sair de 7/10 para 8.5/10:

- repo limpo;
- build verde;
- API MVP documentada;
- testes basicos;
- runtime menos concentrado.

Para lancamento publico sair de 5.5/10 para 7.5/10:

- export testado em maquina limpa;
- display validado;
- exemplos organizados;
- docs publicas;
- template oficial;
- sem codigo legado misturado.
