# ADR-003: Autenticação com Cookies

**Data:** 2026-06-27

## Contexto

O DeskFlow é um SPA com backend API. Necessidade de autenticação segura que funcione bem no navegador.

## Decisão

Utilizar cookies HttpOnly com ASP.NET Core Identity, sem JWT no localStorage.

## Alternativas Consideradas

- JWT no localStorage: vulnerável a XSS. Rejeitado.
- JWT em cookie HttpOnly: adiciona complexidade de refresh sem benefício real para SPA monolítica.
- Session server-side: menos escalável, mas viável.

## Consequências

**Positivas:**
- Cookie inacessível via JavaScript (XSS mitigation)
- Revogação de sessão simples (invalidar no servidor)
- ASP.NET Core Identity gerencia ciclo de vida

**Negativas:**
- Requer antiforgery para proteção contra CSRF
- CORS deve ser configurado cuidadosamente com `AllowCredentials`
- Não adequado para APIs consumidas por third-parties (fora do escopo)
