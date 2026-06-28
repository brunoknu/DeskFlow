# ADR-007: Armazenamento de Anexos no Sistema de Arquivos

**Data:** 2026-06-27

## Contexto

Chamados podem ter arquivos anexados. Necessidade de armazenamento seguro, acessível por autorização.

## Decisão

Armazenar arquivos em diretório configurável fora da pasta pública (`data/attachments/`).
Nome físico: GUID gerado pelo servidor.
Metadados (nome original, content-type, hash, tamanho) no banco.
Download apenas por endpoint autorizado.

## Alternativas Consideradas

- Blob storage (Azure, S3): ideal para produção, mas adiciona dependência externa e custo desnecessário para MVP.
- Banco de dados (varbinary): performance ruim para arquivos grandes.
- Pasta pública: inseguro, acesso sem autenticação.

## Consequências

**Positivas:**
- Sem dependência de cloud nesta versão
- Acesso controlado por autorização
- Simples de implementar e testar

**Negativas:**
- Não escala horizontalmente sem storage compartilhado (NFS, etc.)
- Backup manual necessário para o diretório de arquivos

## Roadmap

- Suporte a Azure Blob Storage ou S3 como alternativa configurável
- Antivírus externo (ClamAV ou similar)
