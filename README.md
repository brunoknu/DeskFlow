# DeskFlow

Sistema interno de Help Desk para registro e acompanhamento de solicitações de suporte.

## Tecnologias

- .NET 10 / ASP.NET Core
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (autenticação por cookie)
- Docker + Docker Compose
- Mailpit (e-mail local)

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Subindo o ambiente local

```bash
cp .env.example .env
docker compose up --build
```

Serviços disponíveis:

| Serviço    | URL                          |
|------------|------------------------------|
| API        | http://localhost:5000        |
| Swagger    | http://localhost:5000/scalar |
| Mailpit    | http://localhost:8025        |

## Rodando sem Docker

```bash
# 1. Suba apenas o SQL Server e o Mailpit
docker compose up db mailpit -d

# 2. Rode a API
dotnet run --project src/DeskFlow.Api
```

## Testes

```bash
dotnet test
```

## Usuários de desenvolvimento (seed automático)

| E-mail                    | Papel         | Senha            |
|---------------------------|---------------|------------------|
| admin@deskflow.local      | Administrator | Admin@123456!    |
| manager@deskflow.local    | Manager       | Manager@123456!  |
| agent1@deskflow.local     | Agent         | Agent@123456!    |
| agent2@deskflow.local     | Agent         | Agent@123456!    |
| requester@deskflow.local  | Requester     | Request@123456!  |

> Estes usuários existem apenas em `Development`. Nunca execute o seed em produção.

## Estrutura do projeto

```
src/
  DeskFlow.Api/           # Controllers, middlewares, configuração
  DeskFlow.Application/   # Casos de uso, handlers, DTOs
  DeskFlow.Domain/        # Entidades, regras de negócio, enums
  DeskFlow.Infrastructure/# EF Core, Identity, e-mail, arquivos

tests/
  DeskFlow.UnitTests/         # Testes de domínio
  DeskFlow.IntegrationTests/  # Testes de API com banco real
  DeskFlow.ArchitectureTests/ # Regras de dependência entre camadas
```

## Documentação

- [docs/architecture.md](docs/architecture.md) — visão geral da arquitetura
- [docs/security.md](docs/security.md) — controles de segurança
- [docs/decisions/](docs/decisions/) — ADRs (Architecture Decision Records)
