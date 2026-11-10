# ADR-001: Separação legacy/modernized como Estratégia de Migração Incremental

## Contexto

O repositório contém dois sistemas completos lado a lado: `legacy/` (ASP.NET MVC Controllers, Layered Architecture, sem DDD) e `modernized/` (FastEndpoints, Vertical Slice, DDD). Ambos têm suas próprias solutions (`.sln`) e suítes de teste independentes. A decisão afeta a estrutura raiz do repo e a estratégia de evolução do produto.

Evidência: `legacy/gymerp.sln` e `GymErp.sln` (raiz, incluindo `modernized/`). O `.cursor/rules/` declara explicitamente: "NUNCA aplique essas regras para o projeto legado, dentro da pasta `legacy`".

**Alternativas consideradas:**
1. Big Bang — reescrever tudo de uma vez e descartar o legado — rejeitada pelo risco de regressão e indisponibilidade.
2. Branch de feature de longa duração — rejeitada por divergência crescente com main.
3. Coexistência na mesma base de código com separação de pastas — **escolhida**.

## Decisão

Adotamos a coexistência de `legacy/` e `modernized/` na raiz do mesmo repositório durante o período de migração. O código legado é mantido operacional e sem modificações de padrão; o código modernizado cresce com as novas regras. Ao final da migração, `legacy/` será removido.

Cada pasta possui sua própria `*.sln`, projetos, testes e configuração. Não há `ProjectReference` cruzando as duas pastas.

## Regras Operacionais

1. **NUNCA** aplique padrões do projeto modernizado (FastEndpoints, Vertical Slice, Autofac, Result) ao código em `legacy/`. *Padrão detectável:* qualquer `using GymErp.*` dentro de `legacy/`. *Motivo:* o legado tem ciclo de vida separado e não recebe refatoração estrutural.
2. **SEMPRE** adicione novas features no projeto `modernized/`. *Padrão detectável:* novos arquivos `.cs` devem residir em `modernized/GymErp/`. *Motivo:* o legado está em modo manutenção.
3. **NUNCA** adicione `ProjectReference` de `modernized/` para `legacy/` ou vice-versa. *Padrão detectável:* grep `ProjectReference.*legacy` ou `ProjectReference.*modernized` em `.csproj` do outro lado. *Motivo:* os dois ciclos de vida são independentes.

## Estrutura de Arquivos

```
sample05/
├── legacy/
│   ├── gymerp.sln
│   ├── Gymerp.Api/
│   ├── Gymerp.Application/
│   ├── Gymerp.Domain/
│   ├── Gymerp.Infrastructure/
│   └── Gymerp.IntegrationTests/
├── modernized/
│   ├── GymErp/
│   ├── GymErp.UnitTests/
│   └── GymErp.IntegrationTests/
└── GymErp.sln          ← solution agregadora (aponta para modernized/)
```

## Benefícios

- **Rollback seguro** — o legado continua funcional se a migração travar.
- **Comparação direta** — desenvolvedores podem ver side-by-side o antes e o depois.
- **Escopo isolado** — regras de AI/linters aplicam-se apenas ao modernizado.

## Relacionado

- [ADR-002](./adr-002-dominios-por-contexto.md): organização interna do projeto modernizado.
- [ADR-003](./adr-003-fastendpoints.md): decisão de framework no modernizado.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
