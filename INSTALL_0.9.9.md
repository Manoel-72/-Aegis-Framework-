# Instalacao - Aegis Framework 0.9.9

## Requisitos

- Windows 10/11
- .NET SDK 8 ou superior instalado

Verifique:

```bat
dotnet --version
```

## Como instalar para teste

1. Extraia `Aegis-Framework-v0.9.9.zip` em uma pasta local.
2. Abra um terminal na raiz extraida.
3. Rode:

```bat
aegis.cmd doctor
```

4. Teste o demo:

```bat
aegis.cmd run demo-platformer
```

## Criar build de jogo

```bat
aegis.cmd build demo-platformer --target win-x64
```

O pacote do jogo sera criado em:

```text
dist/
```

## Criar novo projeto

```bat
aegis.cmd new meu-jogo
```

Depois:

```bat
aegis.cmd run meu-jogo
```

## Observacao

Use `aegis.cmd` dentro desta pasta para garantir que voce esta usando a versao
0.9.9 do framework. O comando global `aegis` pode apontar para outra versao
instalada no Windows.
