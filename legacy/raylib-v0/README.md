# Aegis Raylib legacy prototype

Este diretorio guarda o prototipo antigo da Aegis baseado em Raylib.

Ele foi isolado fora de `src/` para deixar claro que nao faz parte do backend
ativo do MVP atual.

## Status

- Backend: Raylib
- Estado: legado / referencia historica
- Projeto ativo: nao
- Backend oficial atual: MonoGame DesktopGL em `src/Aegis`

## Motivo da quarentena

O codigo Raylib estava misturado no diretorio `src/`, mas os projetos ativos da
engine sao:

- `src/Aegis/Aegis.csproj`
- `src/Aegis.CLI/Aegis.CLI.csproj`

Esses projetos nao referenciam `Raylib_cs`. Para reduzir ruido e evitar
confusao arquitetural, o prototipo antigo foi movido para ca.

## Regra

Nao usar este codigo em templates, docs MVP ou jogos novos.

Se um dia Raylib voltar a ser avaliado, deve ser tratado como backend separado,
com projeto proprio e decisao arquitetural explicita.
