# Status do Projeto — DeskFlow

Última atualização: 2026-06-30

## Funcionalidades Concluídas

- [x] Estrutura de solução .NET 10
- [x] Projetos: Domain, Application, Infrastructure, Api
- [x] Projetos de teste: UnitTests, IntegrationTests, ArchitectureTests
- [x] Entidades de domínio completas (Ticket, comentários, anexos, histórico, SLA, auditoria, outbox)
- [x] Máquina de estados com transições e invariantes
- [x] Value object `TicketNumber`, enums, exceções de domínio
- [x] Camada Application com handlers para todos os casos de uso de chamados
- [x] Interfaces de contrato (`IApplicationDbContext`, `IUserService`, `IAuditLogger`, `IFileStorage`, `IEmailSender`)
- [x] `ApplicationDbContext` com todas as configurações EF Core
- [x] Migration inicial (`InitialCreate`) gerada
- [x] `DatabaseSeeder` com dados fictícios (Nexus Tecnologia)
- [x] `OutboxWorker` para processamento assíncrono de notificações
- [x] `LocalFileStorage`, `SmtpEmailSender`, `AuditLogService`
- [x] `ApplicationUser` integrado ao Identity
- [x] Todos os controllers (Auth, Tickets, Categories, Departments, SlaPolicicies, Users, AuditLogs)
- [x] Autenticação por cookie (HttpOnly, SameSite, lockout)
- [x] Antiforgery configurado
- [x] Rate limiting por política (login, upload, etc.)
- [x] CORS restrito a origens configuradas
- [x] Security headers (HSTS, X-Frame-Options, etc.)
- [x] Problem Details + ExceptionMiddleware
- [x] Políticas de autorização (`CanManageTickets`, `CanAssignTickets`, etc.)
- [x] Proteção contra notas internas expostas para Requester (filtro na query)
- [x] Concorrência otimista via `RowVersion` (retorna HTTP 409)
- [x] Anexos com validação de extensão, tamanho e hash
- [x] Docker Compose (SQL Server 2022, Mailpit, API)
- [x] Dockerfile com multi-stage build e usuário não-root
- [x] 8 ADRs documentados
- [x] `README.md`, `.editorconfig`, `.env.example`, `.gitignore`, `.dockerignore`
- [x] 33 testes unitários de domínio (todos aprovados)

## Funcionalidades Pendentes (Parte 2)

- [ ] Frontend React/Vite (`DeskFlow.Web`)
- [ ] Testes de integração (Testcontainers)
- [ ] Testes de arquitetura (NetArchTest)
- [ ] Testes de segurança automatizados
- [ ] Dashboard e Kanban
- [ ] README definitivo com screenshots
- [ ] CI/CD (GitHub Actions)

## Limitações Conhecidas

- SLA calculado em horas corridas — sem calendário comercial, feriados ou horário útil
- Antivírus externo não implementado (registrado no roadmap)
- Frontend não implementado nesta parte
- Outbox processa e-mail apenas via Mailpit em Development; SMTP real requer configuração

## Comandos Executados

```bash
dotnet build                                          # 0 erros
dotnet ef migrations add InitialCreate \
  --project src/DeskFlow.Infrastructure \
  --startup-project src/DeskFlow.Api \
  --output-dir Persistence/Migrations
dotnet test tests/DeskFlow.UnitTests                  # 33/33 aprovados
```

## Resultado dos Builds

| Fase       | Status    | Observação              |
|------------|-----------|-------------------------|
| Completo   | Aprovado  | 0 erros, 24 avisos (NU) |

## Resultado dos Testes

| Suite              | Total | Passou | Falhou |
|--------------------|-------|--------|--------|
| UnitTests          | 33    | 33     | 0      |
| IntegrationTests   | —     | —      | —      |
| ArchitectureTests  | —     | —      | —      |
