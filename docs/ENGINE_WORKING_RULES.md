# Regras de trabalho da Aegis Engine

Este documento registra o modo de trabalho desejado para evoluir a Aegis Engine.

## Principio principal

Mudancas pequenas, seguras e locais.

Se houver duvida entre melhorar pouco com seguranca e refatorar muito, escolha
melhorar pouco com seguranca.

## Papel tecnico

Quem trabalhar nesta engine deve atuar como:

- engenheiro de software senior em C#;
- arquiteto de engine 2D;
- revisor cuidadoso de estabilidade;
- guardiao da qualidade para jogos publicaveis.

## Prioridades

1. Preservar build verde.
2. Preservar compatibilidade dos jogos existentes.
3. Melhorar arquitetura sem quebrar gameplay.
4. Preferir refatoracoes incrementais.
5. Separar claramente Stable, Experimental e Legacy.
6. Evitar adicionar recurso novo enquanto houver instabilidade essencial.
7. Manter exemplos, templates e documentacao alinhados com a API MVP.

## Regras para refatoracao

- Nao fazer grandes reescritas sem necessidade.
- Nao misturar limpeza de repo com mudanca de comportamento.
- Antes de remover codigo, verificar se ele e usado por projeto ativo.
- Codigo legado deve ser movido para area clara ou documentado antes de ser apagado.
- Cada etapa deve compilar.
- Quando possivel, adicionar teste antes ou junto da melhoria.

## Objetivo de produto

A engine deve ser boa para:

- criar jogos 2D pequenos e medios;
- ensinar desenvolvimento de jogos;
- gerar jogos por IA com API estavel;
- exportar builds confiaveis;
- eventualmente ser lancada publicamente.

## Backend grafico

O backend atual oficial e MonoGame DesktopGL.

Existe codigo antigo baseado em Raylib no repositorio, mas ele nao faz parte do
backend ativo do MVP. Qualquer decisao sobre Raylib deve ser tratada como
avaliacao arquitetural separada, nao como mudanca casual no runtime principal.
