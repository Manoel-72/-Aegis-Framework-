# Aegis Framework

> Framework 2D code-first para criar jogos com código.

Aegis é uma engine/framework 2D leve e minimalista, feita para jogos indie. O núcleo é escrito em C# com MonoGame; a lógica do jogo vive em Lua, mantendo a iteração rápida e o código limpo.

**Em desenvolvimento ativo — API pode mudar entre versões.**

---

## Início Rápido

```bash
dotnet tool install -g aegis

aegis new meu-jogo
cd meu-jogo
aegis run
```

Sem configuração, sem editor pesado. Crie, rode, itere.

---

## Arquitetura

```
Núcleo C# (Engine)
├── Game Loop          — timestep fixo, delta time
├── Renderer           — renderização 2D de sprites e tilemaps
├── Física             — detecção e resolução de colisões
├── Input              — abstração de teclado e mouse
├── Áudio              — efeitos sonoros e música
├── Sistema de Cenas   — gerenciamento de entidades e ciclo de vida
└── Bridge Lua         — API completa exposta para Lua

Camada Lua (Lógica do Jogo)
├── Scripts de comportamento de entidades
├── Definições de cenas
└── Sistemas específicos do jogo
```

A engine inicializa em C#, sobe todos os subsistemas, e passa o controle para o Lua. Tudo que um jogo precisa — mover entidades, tocar sons, ler input, trocar cenas — é acessível direto do Lua.

---

## API Lua

### Entidade

```lua
local jogador = Entity.new("jogador")
jogador:setPosition(100, 200)
jogador:setSprite("assets/jogador.png")
jogador:setTag("jogador")

function jogador:update(dt)
    local vx, vy = self:getVelocity()
    self:setVelocity(vx, vy - 9.8 * dt)
end
```

### Input

```lua
if Input.isKeyDown("direita") then
    self:setVelocity(150, self:getVelocity())
end

if Input.isKeyPressed("espaco") then
    self:pular()
end
```

### Física

```lua
function onCollision(self, outro)
    if outro:getTag() == "inimigo" then
        self:receberDano(10)
    end
end
```

### Áudio

```lua
Audio.play("assets/sons/pulo.wav")
Audio.playMusic("assets/musica/tema.ogg", true)
Audio.setVolume(0.8)
```

### Cena

```lua
Scene.load("cenas/fase_01.lua")
Scene.addEntity(jogador)
```

---

## Download

Baixe a versão atual direto pelo site oficial — não é necessário clonar o repositório.

**[⬇️ aegis-engine.netlify.app](https://aegis-engine.netlify.app/#download)**

---

## Documentação

A documentação completa — guias de instalação, API Lua, arquitetura, exemplos e roadmap — está disponível em:

**[📖 sites.google.com/view/docsaegis/docs](https://sites.google.com/view/docsaegis/docs)**

---

## Status do Projeto

**v0.9.9 — pré-lançamento**

| Sistema | Status |
|---|---|
| Game Loop | ✅ Pronto |
| Renderer 2D | ✅ Pronto |
| Física | ✅ Pronto |
| Input | ✅ Pronto |
| Áudio | ✅ Pronto |
| Sistema de Cenas / Entidades | ✅ Pronto |
| Integração Lua | ✅ Pronto |
| Editor Visual | ✅ Pronto |
| Loader de Tilemap (.tmx) | 🔧 Planejado |
| Sistema de Animações | 🔧 Planejado |
| API Lua estável e documentada | 🔧 Em andamento |

---

## Licença

Projeto pessoal — todos os direitos reservados.
