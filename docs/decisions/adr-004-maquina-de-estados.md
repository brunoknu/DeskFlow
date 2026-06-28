# ADR-004: Máquina de Estados para Status do Chamado

**Data:** 2026-06-27

## Contexto

Chamados passam por múltiplos status com transições restritas. Mudanças arbitrárias comprometem integridade e SLA.

## Decisão

Implementar máquina de estados explícita no domínio (`Ticket.Transition()`), com validação de transição antes de qualquer persistência.

## Alternativas Consideradas

- Status como campo livre no DTO: rejeitado por ausência de controle de invariantes.
- State pattern completo com classes por estado: sobrecomplexidade para os 8 status atuais.

## Consequências

**Positivas:**
- Transições inválidas rejeitadas com exceção de domínio
- Histórico registrado a cada transição
- Regras centralizadas e testáveis
- Campos derivados (ResolvedAtUtc, ClosedAtUtc) atualizados na transição

**Negativas:**
- Cada novo status requer atualização da tabela de transições permitidas
