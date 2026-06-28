# ADR-001: Monólito Modular

**Data:** 2026-06-27

## Contexto

O DeskFlow é um sistema de Help Desk para uso interno corporativo. A escala inicial é limitada a uma empresa com centenas de usuários simultâneos. A equipe de desenvolvimento é pequena.

## Decisão

Adotar monólito modular com Clean Architecture, em vez de microserviços.

## Alternativas Consideradas

- Microserviços: complexidade operacional desproporcional ao tamanho do time e volume de usuários.
- Monólito simples sem separação de camadas: dificulta testes e manutenção.

## Consequências

**Positivas:**
- Deploy e operação simplificados
- Transações ACID nativas
- Sem latência de rede entre serviços
- Refatoração mais segura com testes de arquitetura

**Negativas:**
- Escala horizontal limitada (aceitável para o contexto)
- Módulos devem ser mantidos separados por disciplina, não por compilador
