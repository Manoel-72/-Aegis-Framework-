# Aegis Engine v0.9.9 - Instalacao Rapida

Este pacote contem a versao limpa da Aegis Engine 0.9.9, CLI, templates, demos e documentacao para testes de criacao de jogos.

## Requisitos

- Windows 10/11
- .NET 8 SDK

## 1) Extrair o zip

Extraia o arquivo `Aegis-Framework-v0.9.9.zip` em uma pasta local, por exemplo:

`D:\Aegis-Framework-v0.9.9`

## 2) Rodar pela CLI local (recomendado)

No PowerShell, entre na raiz do pacote e execute:

`.\aegis.cmd --help`

Exemplos:

- `.\aegis.cmd run examples/physics-lab`
- `.\aegis.cmd run examples/hyper-casual`
- `.\aegis.cmd run examples/demo-platformer`

## 3) Atalho opcional (Windows)

Tambem e possivel usar:

`.\aegis.cmd run examples/physics-lab`

## 4) Criar novo jogo

- `.\aegis.cmd new meu-jogo`
- `.\aegis.cmd new platformer meu-platformer`
- `.\aegis.cmd new topdown meu-topdown`
- `.\aegis.cmd new puzzle meu-puzzle`

## 5) Dica importante

Sempre execute comandos a partir da raiz do pacote, onde existem as pastas `src` e `templates`.
