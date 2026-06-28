# ADR-008: Outbox Pattern para Notificações

**Data:** 2026-06-27

## Contexto

Notificações por e-mail devem ser enviadas após operações no chamado. Envio direto dentro da transação principal pode falhar e comprometer a operação principal.

## Decisão

Utilizar Outbox Pattern: mensagens de notificação gravadas na mesma transação da operação principal, processadas por um worker assíncrono.

## Alternativas Consideradas

- Envio direto na transação: falha de SMTP reverte operação principal. Rejeitado.
- Fila externa (RabbitMQ, Service Bus): dependência adicional, fora do escopo do monólito nesta fase.
- Fire-and-forget com Task: sem garantia de entrega. Rejeitado.

## Consequências

**Positivas:**
- Operação principal não falha por problema de e-mail
- Garantia de at-least-once delivery
- Tentativas com espera progressiva
- Idempotência verificada por OutboxMessage.ProcessedAtUtc

**Negativas:**
- Latência eventual na entrega do e-mail
- Worker adicional para gerenciar
- Necessidade de limpeza periódica da tabela OutboxMessages

## Implementação

- `OutboxMessage` gravada na mesma transação do chamado
- `IHostedService` processa mensagens não processadas a cada 30 segundos
- Máximo de 5 tentativas por mensagem
- Espera progressiva: 1min, 5min, 15min, 30min, 1h
- Falha após limite: mensagem marcada como falha permanente, alerta gerado
