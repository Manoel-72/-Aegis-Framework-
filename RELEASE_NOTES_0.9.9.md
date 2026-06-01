# Aegis Framework 0.9.9

Versao limpa para testes de criacao de jogos e distribuicao inicial no site.

## Destaques

- API MVP mais clara com `aegis.create(...)` e `aegis.destroy(...)`.
- `ComponentFactory` para criacao isolada de componentes.
- Separacao de APIs `Stable`, `Experimental` e `Legacy`.
- Fonte padrao automatica e fallback para fontes do sistema.
- UI/HUD em camada propria, sem transformacao de camera.
- `displayMode` com suporte a `windowed` e `borderless`.
- `DisplayWakeLock` no Windows para reduzir risco de tela apagar durante jogo.
- Build/export Windows mais confiavel com validacao e `aegis-build.json`.
- `aegis doctor <jogo>` com validacao inicial de projeto e assets.
- `AssetValidator` inicial para detectar arquivos ausentes e formatos invalidos.
- Aegis Editor com Hub inicial para abrir projeto, exemplo, documentacao e recentes.
- `demo-platformer` validado sem `[Audio|WARN]` no teste principal.
- Codigo Raylib antigo isolado em `legacy/raylib-v0`.
- `.gitignore` configurado para ignorar `bin`, `obj`, `dist`, logs e zips.

## Backend oficial

O backend oficial desta versao e MonoGame DesktopGL.

O codigo Raylib antigo esta mantido apenas como referencia historica em
`legacy/raylib-v0` e nao faz parte do runtime MVP.

## Como rodar localmente

Na raiz do pacote:

```bat
aegis.cmd run examples/demo-platformer
```

Dentro de uma pasta de jogo:

```bat
..\aegis.cmd run
```

## Como gerar build Windows

```bat
aegis.cmd build examples/demo-platformer --target win-x64
```

O resultado sai em:

```text
dist/
```

## Status do preview

Esta versao e recomendada para testes, cursos, criacao de jogos pequenos e
download publico preview.

Ainda nao deve ser tratada como release final de mercado. O foco pos-0.9.9 e
fortalecer asset pipeline, map pipeline 2D, debug visual e editor.
