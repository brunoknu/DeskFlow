# ADR-002: SQL Server

**Data:** 2026-06-27

## Contexto

Necessidade de banco relacional com suporte a transações ACID, concorrência otimista nativa (`rowversion`/`timestamp`), Identity e EF Core.

## Decisão

Utilizar SQL Server (imagem `mcr.microsoft.com/mssql/server:2022-latest`) em Docker para desenvolvimento.

## Alternativas Consideradas

- PostgreSQL: excelente opção, mas `rowversion` é nativo do SQL Server sem necessidade de extensões.
- SQLite: inadequado para produção e sem suporte completo a features avançadas.

## Consequências

**Positivas:**
- `rowversion` nativo para concorrência otimista
- Integração nativa com ASP.NET Core Identity
- EF Core com suporte completo

**Negativas:**
- Licença comercial em produção (dev/test gratuito via Developer Edition)
- Consumo de memória maior que PostgreSQL em containers
