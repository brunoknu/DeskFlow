# Arquitetura — DeskFlow

## Estilo Arquitetural

Monólito modular seguindo Clean Architecture de forma pragmática.
Dependências fluem de fora para dentro: API → Application → Domain.
Infrastructure implementa as interfaces definidas em Application.

## Camadas

### Domain (`DeskFlow.Domain`)

Núcleo do sistema. Não depende de nenhum framework externo.

Responsabilidades:
- Entidades com invariantes encapsuladas
- Value objects (TicketNumber, etc.)
- Enums (TicketStatus, TicketPriority, UserRole)
- Regras de transição de status
- Regras de SLA
- Regras de atribuição e resolução

### Application (`DeskFlow.Application`)

Orquestração dos casos de uso. Depende somente do Domain.

Organização por feature (Vertical Slice dentro da camada):
```
Features/
  Authentication/
  Tickets/
    CreateTicket/
    GetTicketById/
    SearchTickets/
    AssignTicket/
    ChangeTicketStatus/
    AddPublicComment/
    AddInternalNote/
    ResolveTicket/
    ReopenTicket/
    AddAttachment/
    GetAttachment/
    RateTicket/
  Categories/
  Departments/
  SlaPolicies/
  Users/
  AuditLogs/
```

Cada feature contém:
- `Command.cs` / `Query.cs`
- `Handler.cs`
- `Validator.cs`
- `Response.cs` / `Dto.cs`

### Infrastructure (`DeskFlow.Infrastructure`)

Implementações concretas das interfaces do Application.

Responsabilidades:
- `ApplicationDbContext` (EF Core + Identity)
- Repositories via DbContext direto
- Migrações
- Envio de e-mail (MailKit + SMTP)
- Outbox message processing
- Armazenamento de anexos
- Serviço de auditoria

### Api (`DeskFlow.Api`)

Camada de entrada HTTP.

Responsabilidades:
- Roteamento e controllers
- Middleware de erros (Problem Details)
- Autenticação (cookies)
- Antiforgery
- Rate limiting
- CORS restrito
- OpenAPI / Scalar
- Health checks
- Injeção de dependências

## Fluxo de Requisição

```
HTTP Request
  → Middleware (auth, antiforgery, rate-limit)
  → Controller
    → Validator (FluentValidation)
    → Handler (Application)
      → Domain (regras/invariantes)
      → Infrastructure (persistência, e-mail)
    → Response DTO
  → Problem Details (em caso de erro)
HTTP Response
```

## Regras de Dependência

- Domain: sem dependências externas (exceto abstrações básicas)
- Application: somente Domain
- Infrastructure: Domain + Application + EF Core + Identity + MailKit
- Api: Application + Infrastructure + ASP.NET Core

## Concorrência

Concorrência otimista com `RowVersion` (timestamp do SQL Server).
Conflito retorna HTTP 409 com Problem Details sem expor detalhes do banco.

## Segurança

Referência: OWASP ASVS Nível 2.
Detalhes em `docs/security.md`.
