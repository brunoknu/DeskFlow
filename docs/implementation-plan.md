# Plano de Implementação — DeskFlow

## Visão Geral

Sistema corporativo de Help Desk para a empresa fictícia Nexus Tecnologia.
Arquitetura: Clean Architecture como monólito modular.
Stack: .NET 10, ASP.NET Core, EF Core, SQL Server, React/TypeScript.

## Estrutura da Solução

```
DeskFlow/
  src/
    DeskFlow.Domain/         — Entidades, enums, regras, invariantes
    DeskFlow.Application/    — Casos de uso, commands, queries, validações
    DeskFlow.Infrastructure/ — EF Core, Identity, migrações, e-mail, outbox
    DeskFlow.Api/            — Controllers, middlewares, configuração
    DeskFlow.Web/            — React + TypeScript (Parte 2)
  tests/
    DeskFlow.UnitTests/
    DeskFlow.IntegrationTests/
    DeskFlow.ArchitectureTests/
  docs/
    decisions/               — ADRs
    diagrams/
```

## Fases de Implementação

### Fase 1 — Fundação
- Solução .NET 10
- Projetos e referências
- Docker Compose (SQL Server + Mailpit)
- Configuração de logging (Serilog)
- Health checks
- Problem Details
- OpenAPI / Scalar
- .editorconfig, .gitignore, .env.example

### Fase 2 — Identity
- ApplicationUser (estendendo IdentityUser)
- Departamentos
- Papéis: Requester, Agent, Manager, Administrator
- Políticas de autorização
- Endpoints de autenticação (register, login, logout, me, forgot-password, reset-password, confirm-email, antiforgery)
- Cookie seguro (HttpOnly, Secure, SameSite)
- Lockout, confirmação de e-mail
- Seed de desenvolvimento

### Fase 3 — Chamados
- Entidades: Ticket, TicketCategory, TicketStatusHistory, TicketAssignmentHistory
- Migrações
- Número amigável (HD-AAAA-NNNNNN)
- Criação de chamado (Requester)
- Consulta por ID
- Listagem com paginação e filtros
- Acesso restrito do Requester

### Fase 4 — Atendimento
- Atribuição de chamado a agente
- Prioridades (Low, Medium, High, Critical)
- Máquina de estados (New → ... → Closed)
- Concorrência otimista com RowVersion
- Resolução com resumo obrigatório
- Fechamento
- Reabertura com prazo
- Históricos de status e atribuição

### Fase 5 — Interações e SLA
- Comentários públicos
- Notas internas (filtradas no backend)
- Cálculo de SLA (horas corridas)
- Primeira resposta (comentário público de Agent)
- Políticas de SLA por prioridade
- Auditoria de operações críticas
- Testes de segurança (IDOR, notas internas, etc.)

### Fase 6 — Anexos e Notificações
- Upload seguro (validação de extensão, content-type, hash)
- Download autorizado
- Armazenamento fora da pasta pública
- Outbox persistida no banco
- Worker de processamento de e-mails
- Integração com Mailpit (dev)
- Testes de upload inválido, path traversal

## Sequência Sugerida de Commits

```
chore: initialize solution and local environment
feat: add identity and authorization policies
feat: implement ticket creation and requester access
feat: add assignment and ticket workflow
feat: add public comments and internal notes
feat: implement sla policies
feat: add secure attachments
feat: process ticket notifications through outbox
test: cover authorization and ticket workflow
docs: add backend architecture and setup
```
