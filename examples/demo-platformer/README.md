# Aegis Demo Platformer

Demo vertical slice de 9 fases para provar os sistemas principais da Aegis Engine em uma sessao de teste mais longa.

## Sistemas usados

- Player com sprite antigo por atlas Aseprite JSON.
- Tilemap Tiled JSON com colisao automatica e merge de retangulos.
- Inimigos com pathfinding A* em `NavGrid`.
- Coletaveis usando circle collider como trigger.
- Objetivo por fase: coletar moedas suficientes para liberar a porta.
- Checkpoint por fase para reduzir repeticao depois de cair.
- Variacoes simples de layout com plataformas extras em fases avancadas.
- HUD desenhado por Lua.
- Particulas no pulo, coleta e morte.
- Audio de pulo, coleta, dano e som posicional dos inimigos.
- Menu -> jogo -> game over -> menu via SceneManager.
- Pause overlay com `aegis.pushScene` / `aegis.popScene`.
- Teclado e gamepad.

## Rodar

```bash
aegis run examples/demo-platformer
```

## Controles

- A/D ou setas: mover
- Espaco/Seta cima/A do controle: pular
- P/Esc/Start: pausar e voltar ao jogo
- M no pause: voltar ao menu
- Enter/Start: confirmar

## Observacao

Os assets sao placeholders gerados para o projeto. Troque por assets finais depois.
