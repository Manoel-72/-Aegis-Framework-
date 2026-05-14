# Aegis Engine v0.98 - Instalacao Rapida

Este pacote contem o framework original da Aegis Engine, CLI, templates e demos.

## Requisitos

- Windows 10/11
- .NET 8 SDK

## 1) Extrair o zip

Extraia o arquivo `Aegis-Framework-v0.98-site.zip` em uma pasta local, por exemplo:

`D:\Aegis-Framework-v0.98`

## 2) Rodar pela CLI local (recomendado)

No PowerShell, entre na raiz do pacote e execute:

`dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- --help`

Exemplos:

- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- run physics-lab`
- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- run hyper-casual`
- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- run demo-platformer`

## 3) Atalho opcional (Windows)

Tambem e possivel usar:

`aegis.cmd run physics-lab`

## 4) Criar novo jogo

- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- new meu-jogo`
- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- new platformer meu-platformer`
- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- new topdown meu-topdown`
- `dotnet run --project src/Aegis.CLI/Aegis.CLI.csproj -- new puzzle meu-puzzle`

## 5) Dica importante

Sempre execute comandos a partir da raiz do pacote, onde existem as pastas `src` e `templates`.
