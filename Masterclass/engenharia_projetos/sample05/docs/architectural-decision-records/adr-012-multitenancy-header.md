# ADR-012: Multi-tenancy via Header HTTP com Resolução em Middleware

> **Escopo:** exclusivamente `modernized/GymErp/` — **não se aplica** ao código em `legacy/`.

## Contexto

O GymErp serve múltiplos tenants (academias), cada um com seu próprio schema PostgreSQL. A resolução do tenant precisa acontecer no início de cada request HTTP. Aplica-se a todo o pipeline HTTP em `modernized/GymErp/`.

Evidência: `Tenant/TenantAccessorRegisterMiddleware.cs` registra o tenant no início do request. `Tenant/HttpTenantLocatorStrategy.cs` lê o tenant do header HTTP. `Tenant/PostgresTenantLocatorStrategy.cs` consulta PostgreSQL para resolver a connection string. `Program.cs` chama `app.UseTenant()`. Interfaces `ITenantLocator`, `ITenantHttpAccessor`, `IDbConnectionFactory` em `Common/`.

## Decisão

Adotamos resolução de tenant via header HTTP `X-Tenant-Id` (ou equivalente) processado em middleware antes de qualquer lógica de negócio. O tenant resolvido é armazenado no `TenantAccessor` (scoped por request) e usado por `TenantDbConnectionFactory` para injetar a connection string correta no DbContext e Dapper. Duas estratégias de localização estão disponíveis: HTTP (para forwarding) e PostgreSQL (para lookup direto).

## Regras Operacionais

1. **SEMPRE** registre `app.UseTenant()` no pipeline antes de `UseFastEndpoints()`. *Padrão detectável:* `UseTenant()` antes de `UseFastEndpoints(` em `Program.cs`. *Motivo:* o DbContext depende do tenant resolvido; sem isso, a connection string fica nula.
2. **NUNCA** resolva o tenant dentro de um Handler ou Endpoint diretamente. *Padrão detectável:* `ITenantLocator` injetado em `Handler.cs` ou `Endpoint.cs`. *Motivo:* a resolução é responsabilidade do middleware, não do domínio.
3. **SEMPRE** use `IDbConnectionFactory` (que internamente usa o tenant) ao invés de connection string hardcoded. *Padrão detectável:* `IDbConnectionFactory` em repositórios e handlers de query. *Motivo:* isola a lógica de tenant do código de acesso a dados.
4. **PREFIRA** `HttpTenantLocatorStrategy` em contextos onde o tenant já foi resolvido upstream (ex.: API Gateway propagando o header). *Motivo:* evita round-trip adicional ao PostgreSQL.

## Estrutura de Arquivos

```
modernized/GymErp/Tenant/
├── TenantAccessor.cs                    ← armazena tenant do request atual (scoped)
├── TenantAccessorRegisterMiddleware.cs  ← middleware que resolve e registra o tenant
├── TenantMiddlewareExtensions.cs        ← app.UseTenant()
├── HttpTenantLocatorStrategy.cs         ← resolve tenant via header HTTP
├── PostgresTenantLocatorStrategy.cs     ← resolve tenant via query PostgreSQL
├── TenantDbConnectionFactory.cs         ← cria connection com schema do tenant
└── PostgresTenantStringConnection.cs    ← monta connection string por tenant
```

## Benefícios

- **Transparente para o domínio** — Handlers não sabem que existe multi-tenancy.
- **Flexível** — duas estratégias de resolução intercambiáveis.
- **Seguro** — tenant resolvido no middleware impede acesso a dados de outro tenant.

## Relacionado

- [ADR-011](./adr-011-efcore-dapper.md): DbContext usa connection string resolvida pelo tenant.
- [ADR-007](./adr-007-autofac-modulos.md): TenantAccessor registrado como scoped no CommonModule.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
