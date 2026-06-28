# ADR-005: Concorrência Otimista com RowVersion

**Data:** 2026-06-27

## Contexto

Múltiplos agentes podem atualizar o mesmo chamado simultaneamente. Necessidade de evitar sobrescritas silenciosas.

## Decisão

Utilizar `rowversion` (tipo `timestamp` do SQL Server) como token de concorrência no EF Core.
Conflito resulta em `DbUpdateConcurrencyException`, traduzido para HTTP 409 com Problem Details.

## Alternativas Consideradas

- Locks pessimistas: reduz throughput, complexidade adicional.
- Sem controle: dados inconsistentes em produção.
- Versão inteira manual: `rowversion` nativo é mais eficiente.

## Consequências

**Positivas:**
- Sem locks no banco durante a operação
- Conflito detectado no commit
- EF Core suporte nativo com `[Timestamp]`

**Negativas:**
- Cliente deve recarregar dados após 409 e reenviar com novo `rowVersion`
- Requer campo `RowVersion` no DTO de atualização
