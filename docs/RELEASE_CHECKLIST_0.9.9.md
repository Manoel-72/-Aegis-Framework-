# Checklist de Release Publico - Aegis Framework 0.9.9

Este checklist define o processo minimo para publicar a Aegis Framework 0.9.9
com seguranca no site oficial.

## 1. Conferir versao

- `VERSION` deve conter `0.9.9`.
- `RELEASE_NOTES_0.9.9.md` deve estar atualizado.
- `INSTALL_0.9.9.md` deve explicar instalacao com `aegis.cmd`.
- `INSTALL_SITE_DOWNLOAD.md` deve ter comandos curtos para usuario final.

## 2. Validar codigo fonte

Execute na raiz do repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify.ps1 -Configuration Release -SkipZip
```

Resultado esperado:

- build da solution sem erros;
- testes automatizados passando;
- nenhum erro no teste de build web.

## 3. Gerar pacote limpo

Execute:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1 -Version 0.9.9 -Configuration Release
```

Resultado esperado:

```text
dist\Aegis-Framework-v0.9.9.zip
```

O script tambem valida o conteudo do ZIP.

## 4. Testar o ZIP em pasta limpa

Extraia o ZIP em uma pasta fora do repositorio, por exemplo:

```text
C:\tmp\Aegis-Framework-v0.9.9-test
```

Dentro da pasta extraida, rode:

```powershell
.\aegis.cmd doctor
.\aegis.cmd run examples/demo-platformer
.\aegis.cmd build examples/demo-platformer --target win-x64
```

Resultado esperado:

- `doctor` sem erro critico;
- demo abre usando a CLI local;
- build Windows gera pacote em `dist\`;
- nenhum `crash.log` novo aparece na raiz do pacote.

## 5. Conferir conteudo do pacote

O ZIP deve conter:

- `aegis.cmd`;
- `VERSION`;
- `INSTALL_0.9.9.md`;
- `RELEASE_NOTES_0.9.9.md`;
- `src/Aegis/`;
- `src/Aegis.CLI/`;
- `templates/`;
- `examples/demo-platformer/`;
- `docs/MVP_API.md`;
- `scripts/verify.ps1`;
- `scripts/package-release.ps1`;
- `tests/Aegis.Tests/`.

O ZIP nao deve conter:

- `.git/`;
- `bin/`;
- `obj/`;
- `dist/`;
- `archive/`;
- `saves/`;
- arquivos `.zip`, `.log`, `.tmp`, `.bak`, `.old`, `.orig`;
- PDF/DOCX antigos gerados de documentacao.

## 6. Teste rapido de criacao de jogo

Na pasta extraida:

```powershell
.\aegis.cmd new meu-jogo-teste
.\aegis.cmd run meu-jogo-teste
.\aegis.cmd build meu-jogo-teste --target win-x64
```

Resultado esperado:

- projeto novo criado;
- jogo abre;
- build Windows gerado.

Depois do teste, remova a pasta `meu-jogo-teste` antes de publicar o ZIP.

## 7. Texto recomendado para o site

Titulo:

```text
Aegis Framework 0.9.9 - Preview para criacao de jogos 2D
```

Descricao curta:

```text
Versao preview da Aegis Engine com runtime MonoGame, scripting Lua,
templates, demos, build Windows, API MVP documentada e pacote limpo para testes.
```

Comando principal:

```powershell
.\aegis.cmd run examples/demo-platformer
```

Aviso recomendado:

```text
Esta e uma versao preview para testes, cursos e criacao de jogos pequenos.
Use a CLI local aegis.cmd para garantir que voce esta usando a versao 0.9.9.
```

## 8. Criterio de publicacao

Publicar apenas se todos os itens abaixo estiverem OK:

- build Release passou;
- testes passaram;
- pacote foi gerado pelo script oficial;
- ZIP passou na verificacao automatica;
- ZIP foi testado em pasta limpa;
- comandos do site foram conferidos;
- changelog e instalacao estao atualizados.

Se algum item falhar, nao publique o ZIP. Corrija, gere novo pacote e repita o
checklist desde a etapa 2.
