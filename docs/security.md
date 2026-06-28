# Segurança — DeskFlow

## Referência

OWASP ASVS Nível 2 como guia de implementação.
Este documento não representa certificação formal.

## Autenticação

- ASP.NET Core Identity com hash de senha (PBKDF2)
- Cookies HttpOnly, Secure (produção), SameSite=Strict
- Tempo de expiração configurável
- Account lockout após tentativas falhas
- Confirmação de e-mail obrigatória
- Recuperação de senha com token expirado em 1 hora
- Respostas neutras para evitar enumeração de contas
- Sessão invalidada quando usuário é bloqueado

## Autorização

- Deny by default: todos os endpoints exigem autenticação por padrão
- Políticas baseadas em papel (Role-based) + Resource-based
- Validação de propriedade do recurso em cada operação
- Proteção contra IDOR e BOLA
- Notas internas filtradas no banco, não no frontend

### Políticas

| Política                 | Papéis Autorizados              |
|--------------------------|----------------------------------|
| CanManageTickets         | Agent, Manager                   |
| CanAssignTickets         | Agent, Manager                   |
| CanManageUsers           | Administrator                    |
| CanManageDepartments     | Manager, Administrator           |
| CanManageSla             | Manager, Administrator           |
| CanViewReports           | Manager, Administrator           |
| CanViewInternalNotes     | Agent, Manager, Administrator*   |
| CanReopenTicket          | Requester (dentro do prazo)      |
| CanViewAuditLogs         | Manager, Administrator           |

*Administrator requer política explícita adicional

## Antiforgery

- Token gerado via `GET /api/auth/antiforgery`
- Enviado por header em todas as operações de escrita
- Validado pelo middleware do ASP.NET Core
- Incompatível com `AllowAnyOrigin`

## CORS

- Somente origens configuradas explicitamente
- Credenciais (`withCredentials`) somente com origem explícita
- Método OPTIONS tratado corretamente

## Rate Limiting

| Endpoint                       | Limite     |
|-------------------------------|------------|
| POST /api/auth/login           | 5/min      |
| POST /api/auth/forgot-password | 3/min      |
| POST /api/tickets              | 10/min     |
| POST /api/tickets/*/comments   | 20/min     |
| POST /api/tickets/*/attachments| 5/min      |
| GET  /api/*                    | 100/min    |
| Operações administrativas      | 30/min     |

## Headers de Segurança

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy` configurado por ambiente
- `Strict-Transport-Security` em produção

## Anexos

- Extensões permitidas em whitelist (não blacklist)
- Validação de assinatura de arquivo (magic bytes) para imagens e PDF
- Nome físico gerado pelo servidor (GUID)
- Armazenamento fora da pasta pública
- Hash SHA-256 para integridade
- Proteção contra path traversal
- `Content-Disposition: attachment` com nome sanitizado
- Verificação de autorização antes do download
- Tamanho máximo: 10 MB por arquivo, 5 arquivos por chamado

## Logging

Nunca registrar:
- Senhas ou hashes de senha
- Cookies de sessão
- Tokens de recuperação
- Authorization headers
- Connection strings
- Corpos de requisição de autenticação
- Conteúdo de comentários internos
- Bytes de arquivos

## Gerenciamento de Segredos

- `appsettings.json` nunca contém credenciais reais
- Segredos em variáveis de ambiente ou User Secrets (dev)
- `.env` fora do controle de versão
- `.env.example` com placeholders

## Mass Assignment

- DTOs específicos por operação (criar ≠ editar ≠ resposta)
- Campos protegidos (`RequesterId`, `AssignedAgentId`, `Status`) não aceitos via binding
- Operações específicas para cada mudança de campo sensível

## Limitações Conhecidas

- Antivírus externo não implementado nesta versão (roadmap)
- SLA não considera horário comercial nem feriados (horas corridas)
- Content-Security-Policy em modo permissivo durante desenvolvimento
