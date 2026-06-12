# SGI Transparência — Sistema de Gestão Integrada

Monorepo do SGI Transparência. Backend em .NET 8 (Minimal APIs) e frontend em React + Vite.

## Estrutura

```
sgi-transparencia/
├── docs/
│   └── adr/                  # Architecture Decision Records
├── api/
│   └── SGI.Api/              # API .NET 8
│       ├── Program.cs        # pipeline declarativo (visão geral da aplicação)
│       ├── Properties/       # launchSettings.json (porta e ambiente locais)
│       ├── appsettings.json  # configuração comum (sem segredos)
│       └── appsettings.Development.json  # configuração de DEV (SQLite)
└── web/                      # React + Vite (entra na Etapa 6)
```

As pastas de código do backend (Dominio/, Contratos/, Persistencia/, Rotas/)
serão criadas conforme as etapas avançarem — cada pasta nasce junto com o
primeiro arquivo que a justifica.

## Pré-requisitos

- .NET SDK 8.0 — https://dotnet.microsoft.com/download/dotnet/8.0

## Como executar (Etapa 1)

```bash
cd api/SGI.Api
dotnet run
```

A API sobe em `http://localhost:5180`. Teste o endpoint de saúde:

```bash
curl http://localhost:5180/saude
# Esperado: {"situacao":"saudavel","horarioUtc":"2026-..."}
```

## Roteiro de construção (Fundação + Autenticação)

| Etapa | Entrega | Situação |
|-------|---------|----------|
| 1 | Estrutura do monorepo + API mínima com /saude | ✅ |
| 2 | EntidadeBase + entidades de autenticação + ContextoDados + migration inicial | ✅ |
| 3 | Pipeline JWT + endpoint de login com BCrypt | ✅ |
| 4 | Refresh token com rotação + lockout de força bruta + troca de senha | ✅ |
| 5 | Rate limiting, CORS e tratamento global de erros | ⬜ |
| 6 | Scaffold React: cliente HTTP, TanStack Query, login e Route Guards | ⬜ |

As diretrizes de arquitetura do projeto (v2.1) e os ADRs vivem em `docs/`.
