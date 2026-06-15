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
| 5 | Rate limiting, CORS e tratamento global de erros | ✅ |
| 6 | Scaffold React: cliente HTTP, TanStack Query, login e Route Guards | ✅ |

### Etapa 6 — sub-passos

| Sub-passo | Entrega | Situação |
|-----------|---------|----------|
| 6.1 | Scaffold Vite + React + TS, MUI, tema, TanStack Query | ✅ |
| 6.2 | Cliente HTTP central + refresh via cookie HttpOnly (Etapa 4.1) | ✅ |
| 6.3 | Tela de login + contexto de sessão + troca de senha | ✅ |
| 6.4 | React Router + Route Guards (auth e perfil) + layout autenticado | ✅ |

> **Nota de deploy (BrowserRouter):** em produção, o servidor que serve
> o frontend deve redirecionar todas as rotas para o index.html
> (fallback SPA), senão recarregar uma URL interna (ex.: /admin) dá 404.
> O Vite já trata isso em desenvolvimento.

## Como executar o frontend (a partir da Etapa 6.1)

```bash
cd web
npm install      # baixa as dependências (só na 1ª vez)
npm run dev      # sobe o Vite em http://localhost:5173
```

O backend (`api/SGI.Api`) e o frontend (`web`) rodam ao mesmo tempo,
em terminais separados. O CORS do backend já autoriza a origem 5173.
