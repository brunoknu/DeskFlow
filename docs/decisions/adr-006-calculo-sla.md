# ADR-006: Cálculo de SLA em Horas Corridas

**Data:** 2026-06-27

## Contexto

Chamados possuem prazos de primeira resposta e resolução. Feriados, dias úteis e horário comercial aumentam complexidade.

## Decisão

Calcular SLA em horas corridas (calendar time) nesta versão.
Utilizar `TimeProvider` em vez de `DateTime.UtcNow` direto, para permitir testes determinísticos.
SLA de resolução NÃO é pausado em `WaitingRequester` nesta versão (simplicidade).

## Alternativas Consideradas

- Calendário comercial com feriados: funcionalidade valiosa, mas requer tabela de feriados por empresa e lógica de "próximo dia útil". Adicionado ao roadmap.
- SLA pausado em WaitingRequester: válido operacionalmente; decidido não pausar nesta versão por simplicidade. Documentado como limitação.

## Consequências

**Positivas:**
- Implementação simples e previsível
- Testes determinísticos com `TimeProvider`

**Negativas:**
- SLA conta fim de semana e feriados (limitação conhecida)
- SLA não pausa quando aguardando resposta do solicitante (limitação conhecida)

## Limitações Documentadas

- Horário comercial: não implementado nesta versão
- Feriados por empresa: roadmap
- Pausa de SLA em WaitingRequester: roadmap
