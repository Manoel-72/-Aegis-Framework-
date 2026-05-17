# Arcanos da Mesa

Prototipo de jogo de cartas feito para a Aegis Engine.

## Rodar

```bash
aegis run card-battle
```

## Regras

- Jogador e IA comecam com 20 de vida.
- O baralho tem 15 cartas diferentes.
- A mao sempre tenta ficar com 3 cartas.
- Cada carta tem 3 de vida.
- Usar uma carta de ataque consome 2 de vida dela.
- Usar uma carta sem ataque consome 1 de vida dela.
- Carta com vida 0 vai para o cemiterio.
- Reduza a IA a 0 para vencer.

## Texto e fontes

A Aegis so desenha `newLabel` quando alguma fonte TTF foi carregada.
Este prototipo agora inclui uma fonte local e tenta carregar:

- `card-battle/res/fonts/Inter-Regular.ttf`
- `card-battle/res/fonts/Roboto-Regular.ttf`
- `C:/Windows/Fonts/segoeui.ttf`
- `C:/Windows/Fonts/arial.ttf`

Para distribuir o jogo em outros computadores, coloque uma fonte `.ttf` em
`card-battle/res/fonts/Inter-Regular.ttf`.

## Polimento atual

- Menu com botoes animados, descricoes e som de hover/clique.
- Cartas com texto completo e destaque ao passar o mouse.
- Painel grande de energia com 3 orbes visiveis.
- Sons curtos em `res/audio/` para carta, turno, erro, vitoria e derrota.
- Pequenos efeitos com tween, screen shake, flash e particulas quando a engine suporta.
