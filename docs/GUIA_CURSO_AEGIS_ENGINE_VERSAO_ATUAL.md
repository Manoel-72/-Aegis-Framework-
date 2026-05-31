# Guia Para IA Criar Curso - Aegis Engine 0.9.9

Este documento serve como referencia para uma IA, instrutor ou criador de
conteudo montar aulas, apostilas, roteiros, exercicios e projetos praticos da
Aegis Engine 0.9.9.

O foco desta versao e ensinar o caminho estavel do MVP: criar jogos 2D simples
com Lua, usando a CLI local, exemplos oficiais, API pequena, build Windows e
boas praticas de projeto.

## 1. Resumo Da Engine

Aegis Engine e uma engine/framework 2D feita em C#/.NET 8, com MonoGame
DesktopGL como backend grafico oficial e NLua para scripting Lua.

Objetivo da engine:

- permitir criar jogos 2D com Lua;
- oferecer uma API simples chamada `aegis.*`;
- manter o runtime, renderizacao, input, audio, fisica, cenas e build em C#;
- facilitar criacao de jogos por estudantes, desenvolvedores e IAs;
- permitir export Windows para testes e distribuicao inicial.

Backend oficial:

```text
MonoGame DesktopGL
```

Raylib:

```text
legacy/raylib-v0
```

O codigo Raylib antigo esta preservado apenas como referencia historica. Ele nao
faz parte do runtime MVP da versao 0.9.9.

## 2. Estado Real Da Versao 0.9.9

Esta versao ja esta boa para:

- curso pratico de jogos 2D simples;
- criacao de prototipos;
- testes internos e publicos controlados;
- criacao de conteudo didatico;
- uso por IA com documentacao orientada;
- export Windows `win-x64`.

Ainda nao deve ser vendida como engine final de mercado. Ela deve ser chamada de
preview, beta ou MVP publico inicial.

Ja esta implementado:

- API recomendada `aegis.create(...)`;
- `ComponentFactory` para criacao isolada de componentes;
- separacao Stable/MVP, Experimental e Legacy;
- fallback automatico de fonte;
- texto com tamanho real de fonte;
- camada HUD/UI separada do mundo;
- `displayMode` com `windowed` e `borderless`;
- build/export Windows mais confiavel;
- pacote limpo `Aegis-Framework-v0.9.9.zip`;
- exemplos oficiais em `examples/`;
- testes automatizados;
- scripts de verificacao e empacotamento;
- GitHub Actions para CI/release;
- checklist de release publico.

## 3. Estrutura Do Repositorio

Pastas principais:

```text
src/Aegis/                 nucleo da engine
src/Aegis.CLI/             ferramenta de linha de comando
AegisEditor/               editor desktop Avalonia
templates/                 templates de novos jogos
examples/                  jogos e demos oficiais
docs/                      documentacao
tests/Aegis.Tests/         testes automatizados
scripts/                   scripts de verificacao e release
legacy/raylib-v0/          codigo antigo Raylib, apenas historico
dist/                      builds e pacotes gerados, nao versionar
archive/                   artefatos historicos locais, nao publicar
```

Exemplos oficiais:

```text
examples/demo-platformer
examples/card-battle
examples/physics-lab
examples/hyper-casual
examples/top-down-shooter
examples/zone-recon
```

Documentos importantes:

```text
docs/MVP_API.md
docs/STABLE_LUA_API.md
docs/ENGINE_WORKING_RULES.md
docs/ENGINE_CLEANUP_AND_REFACTOR_PLAN.md
docs/RELEASE_CHECKLIST_0.9.9.md
INSTALL_0.9.9.md
INSTALL_SITE_DOWNLOAD.md
RELEASE_NOTES_0.9.9.md
```

## 4. Como Rodar A Engine

Use sempre a CLI local do pacote:

```powershell
.\aegis.cmd --help
```

Rodar demo:

```powershell
.\aegis.cmd run examples/demo-platformer
```

Rodar outro exemplo:

```powershell
.\aegis.cmd run examples/card-battle
```

Criar projeto novo:

```powershell
.\aegis.cmd new meu-jogo
```

Rodar projeto novo:

```powershell
.\aegis.cmd run meu-jogo
```

Gerar build Windows:

```powershell
.\aegis.cmd build meu-jogo --target win-x64
```

Para curso, evite depender do comando global `aegis`, porque ele pode apontar
para outra versao instalada no Windows.

## 5. API Lua Recomendada

Para codigo novo, cursos e templates, ensine primeiro:

```lua
local player = aegis.create("sprite", {
    path = "sprites/player.png",
    x = 100,
    y = 200,
    z = 10
})

aegis.destroy(player)
```

APIs antigas como `aegis.newSprite`, `aegis.newLabel` e `aegis.newRect`
continuam funcionando por compatibilidade, mas devem ser tratadas como Legacy
em material novo.

Componentes principais via `aegis.create`:

```text
sprite
rect
label
richLabel
panel
flow
progressBar
anim
animatedSprite
```

Regra para IA:

```text
Use aegis.create em exemplos novos. Use APIs legacy apenas quando explicar
compatibilidade com projetos antigos.
```

## 6. Estabilidade Da API

A engine classifica APIs em:

- `Stable/MVP`: usar em cursos, templates e exemplos oficiais.
- `Experimental`: pode mudar; usar apenas em aulas bonus.
- `Legacy`: existe para compatibilidade; nao priorizar em codigo novo.

Fonte principal:

```text
docs/MVP_API.md
```

Para curso publico, a IA deve sempre basear aulas na API Stable/MVP.

## 7. ComponentFactory

A criacao de componentes foi isolada em:

```text
src/Aegis/Scripting/Components/ComponentFactory.cs
```

Explique no curso que a engine evita concentrar toda criacao dentro de
`LuaRuntime`. O runtime registra a API e orquestra Lua; a fabrica cria os
componentes.

Isso e importante para arquitetura limpa e crescimento da engine.

## 8. LuaRuntime Modular

O `LuaRuntime` foi quebrado em arquivos parciais por area:

```text
LuaRuntime.ApiRegistration.cs
LuaRuntime.AudioApi.cs
LuaRuntime.ConfigApi.cs
LuaRuntime.CoreApi.cs
LuaRuntime.DisplayApi.cs
LuaRuntime.EffectsApi.cs
LuaRuntime.ExperimentalApi.cs
LuaRuntime.GameplayApi.cs
LuaRuntime.InputApi.cs
LuaRuntime.PhysicsApi.cs
LuaRuntime.SceneApi.cs
```

Para curso de uso da engine, nao e necessario ensinar todos esses arquivos.
Para curso de arquitetura da engine, explique que essa divisao reduz risco,
facilita testes e evita que uma unica classe vire ponto central de tudo.

## 9. Texto, Fontes E HUD

A engine agora tenta carregar fonte padrao automaticamente.

Ordem geral:

- fontes dentro de `res/fonts`;
- fontes do sistema, como Segoe UI, Arial, DejaVu e Liberation.

Recomendado:

```lua
local title = aegis.create("label", {
    text = "Meu Jogo",
    size = 32,
    x = 40,
    y = 40,
    hud = true
})
```

Evite:

```lua
-- Evite ensinar isso como padrao para texto grande.
aegis.setScale(label, 4, 4)
```

Regra:

```text
Texto deve usar tamanho real de fonte. HUD deve usar hud = true.
```

UI imediata:

```lua
function aegis_draw_ui()
    aegis.drawText("Vida 3", 20, 20, 1, 1, 1, 1)
end
```

## 10. Display E Tela Cheia

Configuracao recomendada para janela:

```json
{
  "windowWidth": 1440,
  "windowHeight": 900,
  "displayMode": "windowed",
  "fullscreen": false
}
```

Configuracao recomendada para tela cheia estavel:

```json
{
  "windowWidth": 1440,
  "windowHeight": 900,
  "displayMode": "borderless",
  "fullscreen": true
}
```

Para MVP, ensine `borderless` como alternativa profissional ao fullscreen
exclusivo. Isso reduz risco de tela preta, problemas de driver e instabilidade
de apresentacao grafica.

## 11. Cenas

Exemplo de registro e transicao:

```lua
function aegis_init()
    aegis.registerScene("menu", "scenes/menu.lua")
    aegis.registerScene("level1", "scenes/level.lua")
    aegis.transitionTo("menu", "none")
end
```

Boas praticas:

- usar `aegis.clearAll()` ao iniciar cena;
- evitar chamar `transitionTo` varias vezes no mesmo frame;
- separar `menu.lua`, `level.lua`, `gameover.lua` em arquivos diferentes;
- manter `main.lua` como boot simples.

## 12. Input

Exemplo:

```lua
function aegis_update(dt)
    if aegis.keyDown("Right") then
        aegis.move(player, 160 * dt, 0)
    end
end
```

Ensinar:

- `keyDown`;
- `keyPressed`;
- mouse;
- gamepad, quando fizer sentido.

## 13. Fisica Basica

Exemplo:

```lua
local col = aegis.addCollider(player, 24, 32)
local rb = aegis.addRigidbody(player)
aegis.setVelocityX(rb, 120)
```

Ensinar:

- collider;
- rigidbody;
- gravidade;
- trigger;
- colisao enter/exit;
- plataformas simples.

Evitar em curso base:

- slope;
- one-way platform;
- sistemas ainda pouco testados.

## 14. Audio

Exemplo:

```lua
aegis.playSound("jump.wav")
aegis.playMusic("music.wav", true)
```

Ensinar:

- som curto;
- musica;
- volume;
- audio posicional simples apenas em aula bonus.

Observacao importante:

```text
Use arquivos de audio validos. Warnings de audio em exemplos devem ser corrigidos
antes de gravar aula publica.
```

## 15. Build E Export

Comando principal:

```powershell
.\aegis.cmd build examples/demo-platformer --target win-x64
```

O build gera:

```text
dist/aegis-demo-platformer-win-x64/
dist/aegis-demo-platformer-win-x64.zip
```

Explique no curso:

- a pasta `dist`;
- o arquivo `JOGAR.bat`;
- o arquivo `aegis-build.json`;
- como testar o jogo exportado em outra pasta;
- como evitar usar assets fora da pasta do jogo.

## 16. Verificacao, Testes E Release

Scripts oficiais:

```text
scripts/verify.ps1
scripts/package-release.ps1
```

Validar codigo:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify.ps1 -Configuration Release -SkipZip
```

Gerar pacote:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1 -Version 0.9.9 -Configuration Release
```

Checklist:

```text
docs/RELEASE_CHECKLIST_0.9.9.md
```

CI:

```text
.github/workflows/ci-release.yml
```

Testes atuais cobrem:

- `ConfigManager`;
- `ComponentFactory`;
- `FontManager`;
- `ProjectCreator`;
- `SceneManager`;
- build web/stub.

## 17. Recursos Bons Para Ensinar Agora

Aula 1 - Visao geral:

- o que e a Aegis;
- MonoGame + Lua;
- estrutura do pacote;
- rodando `demo-platformer`.

Aula 2 - Primeiro projeto:

- `aegis.cmd new meu-jogo`;
- `main.lua`;
- `aegis.cfg`;
- `res/`;
- `scenes/`.

Aula 3 - Sprites:

- `aegis.create("sprite")`;
- posicao;
- escala;
- rotacao;
- z-order.

Aula 4 - Texto e HUD:

- labels;
- fonte fallback;
- `hud = true`;
- `aegis_draw_ui`.

Aula 5 - Input:

- teclado;
- mouse;
- movimento basico.

Aula 6 - Cenas:

- menu;
- gameplay;
- game over;
- `registerScene`;
- `transitionTo`.

Aula 7 - Fisica:

- collider;
- rigidbody;
- gravidade;
- triggers.

Aula 8 - Audio:

- som curto;
- musica;
- volume.

Aula 9 - Save/config:

- salvar progresso;
- configuracao de janela;
- boas praticas.

Aula 10 - Build Windows:

- `aegis build`;
- testar `dist`;
- preparar zip do jogo.

Aula 11 - Projeto final:

- mini platformer;
- top-down simples;
- card battle simples.

Aula 12 - Bonus:

- tween;
- particulas;
- camera;
- recursos experimentais explicados com cuidado.

## 18. Recursos Que Devem Ser Bonus Ou Experimentais

Nao ensinar como base principal:

- drag/drop;
- hand layout;
- upgrade system;
- autozoom;
- audio 3D;
- editor pipe;
- slope/one-way platform;
- fullscreen exclusivo.

Esses recursos podem aparecer em aulas bonus, com aviso claro:

```text
Esta API ainda pode mudar. Use em prototipos, nao como base de projeto longo.
```

## 19. Regras Para IA Criar Aulas

Quando uma IA criar conteudo de curso sobre Aegis Engine 0.9.9, ela deve:

- usar `aegis.cmd`, nao depender de comando global;
- usar caminhos `examples/...`;
- ensinar `aegis.create` primeiro;
- consultar `docs/MVP_API.md`;
- tratar APIs Legacy como compatibilidade;
- tratar APIs Experimental como bonus;
- usar `hud = true` para HUD;
- usar fonte com `size`, nao escala artificial;
- evitar fullscreen exclusivo;
- recomendar `windowed` ou `borderless`;
- criar assets dentro de `res/`;
- criar cenas dentro de `scenes/`;
- incluir exercicios praticos;
- incluir checkpoints de teste;
- incluir comandos que o aluno possa copiar sem copiar cercas Markdown;
- validar final com `aegis build <jogo> --target win-x64`.

## 20. Prompt Pronto Para Pedir Curso A Uma IA

Use este prompt quando quiser pedir para outra IA montar o curso:

```text
Voce e um instrutor senior de desenvolvimento de jogos 2D. Crie um curso
pratico da Aegis Engine 0.9.9 usando este documento como fonte principal.

Contexto:
- Aegis Engine usa C#/.NET 8, MonoGame DesktopGL e Lua via NLua.
- O backend oficial e MonoGame. Raylib esta apenas em legacy/raylib-v0.
- O aluno deve usar a CLI local .\aegis.cmd.
- Os exemplos oficiais ficam em examples/.
- A API recomendada e aegis.create(...).
- O contrato principal da API e docs/MVP_API.md.
- Recursos experimentais devem ficar apenas em aulas bonus.

Monte:
1. ementa do curso;
2. objetivos de aprendizagem;
3. pre-requisitos;
4. roteiro por aula;
5. explicacao curta de cada topico;
6. exercicio pratico por aula;
7. codigo Lua exemplo;
8. comandos PowerShell corretos;
9. projeto final;
10. criterios de avaliacao.

Evite inventar APIs que nao existem. Prefira exemplos simples, testaveis e
compativeis com a versao 0.9.9.
```

## 21. Resumo Final

A Aegis Engine 0.9.9 esta pronta para servir como base de curso pratico de jogos
2D simples. O curso deve ensinar o caminho Stable/MVP, usar MonoGame como backend
oficial, evitar APIs experimentais no nucleo do aprendizado e finalizar com build
Windows testavel.

Para conteudo publico, trate a versao como preview profissional: boa para
ensinar, prototipar e criar jogos pequenos, mas ainda em evolucao para uma
release ampla de mercado.
