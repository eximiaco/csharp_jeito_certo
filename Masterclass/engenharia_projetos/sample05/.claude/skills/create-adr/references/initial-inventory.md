# Fluxo 3 — Inventário inicial (repositório sem ADRs documentadas)

Use este roteiro quando o usuário respondeu `3` no menu inicial, ou quando a pasta `docs/architectural-decision-records/` está vazia / tem poucos arquivos e ele quer começar do zero.

> Observação para o FTC: este repositório **já tem 39+ ADRs**. Se o usuário escolher este fluxo mesmo assim, confirme antes: pergunte se ele quer (a) refazer o inventário a partir do zero substituindo as existentes — raríssimo; ou (b) **na verdade o fluxo 2** (validar e completar lacunas). Em 99% dos casos é (b); redirecione.

## Passo 1 — Mapear a topologia do repositório

Antes de ler código, entenda a forma do repo. Esta é uma arqueologia **top-down** inspirada no modelo C4 (Containers).

Rode em paralelo (via `Glob`, `Read`, `Bash ls`):

- **Raiz:** `ls` na raiz para ver pastas de primeiro nível.
- **Pastas candidatas a contexto/serviço/app:** `contexts/`, `apps/`, `services/`, `packages/`, `client/`, `server/`, `backend/`, `frontend/`.
- **Indicadores de monorepo:** `package.json` na raiz, `pnpm-workspace.yaml`, `*.sln` agregadora, `go.work`.
- **Orquestração/infra:** `docker-compose*.yml`, `Dockerfile`, `k8s/`, `terraform/`, `.github/workflows/`.
- **Documentação:** `docs/`, `README.md`, `CONTRIBUTING.md`, `CLAUDE.md`, `.cursor/rules/`.
- **Coexistência/migração:** `v1/`, `v2/`, `legacy/`, `modern/`.

Registre mentalmente: **topologia** (mono-repo? monolito modular? microserviços?) e **containers** (quais aplicações deployáveis existem).

## Passo 2 — Identificar stack e padrões

Agora olhe os arquivos que revelam stack:

- **Backend:** `*.csproj`, `go.mod`, `pom.xml`, `build.gradle`, `Cargo.toml`, `requirements.txt`, `pyproject.toml`.
- **Frontend:** `package.json` de cada app, `vite.config.*`, `next.config.*`, `tsconfig*.json`, `eslint.config.*`.
- **Dados:** `migrations/`, `schema.prisma`, `*.Migrations/`, scripts SQL.
- **Mensageria/workflow:** WorkflowCore, Temporal, Hangfire, Celery, BullMQ.
- **Frameworks HTTP:** FastEndpoints, ASP.NET Controllers, Express, Fastify, FastAPI, Spring.
- **Libs transversais:** clients HTTP (`Flurl`, `HttpClient`, `axios`), logging (`Serilog`, `winston`), telemetria (`OpenTelemetry`).
- **Testes:** projetos `*.tests`, `*.test.*`, pastas `tests/`, `__tests__/`.

## Passo 3 — Identificar decisões implícitas por grupo

Agrupe achados em três famílias. Para cada família, liste o que já está codificado como prática mas não tem ADR.

### Grupo A — Estrutura e organização (macro)

Decisões sobre **como o repo se divide**:
- Estratégia de mono-repo vs multi-repo.
- Contextos / bounded contexts / módulos.
- Separação front/back, BFF.
- Política de compartilhamento de código entre serviços (NuGet privado? `packages/`?).
- Convenção de nomenclatura de pastas/projetos.

### Grupo B — Engenharia e padrões de código

Decisões sobre **como se escreve cada pedaço**:
- Arquitetura interna (Clean / Hexagonal / Vertical Slice / MVC).
- Padrões de tratamento de erro (exceções, Result, Either).
- Framework HTTP e convenções de endpoint.
- Padrões de injeção de dependência.
- Logging, observabilidade, telemetria.
- Autenticação/autorização.
- Clients HTTP + políticas de retry/circuit-breaker.
- Padrões de acesso a dados (ORM? micro-ORM? SQL puro?).
- Workflows duráveis / sagas / jobs agendados.

### Grupo C — Qualidade e processo

Decisões sobre **testes, CI, releases, secrets**:
- Framework de teste + assertion library + mock.
- TestContainers / containers efêmeros.
- Convenção de nomenclatura de testes.
- Estrutura de pastas de teste.
- Gerenciamento de secrets (user-secrets, vaults).
- CI/CD, feature flags.

## Passo 4 — Compor proposta numerada

Liste **todos** os ADRs sugeridos, numerados sequencialmente a partir do 001 (se o repo está zerado) ou a partir do próximo disponível. Agrupe por letra (A/B/C) para o usuário escolher facilmente.

```
Com base no inventário, proponho os seguintes ADRs:

GRUPO A — Estrutura e organização
  ADR-001 — Estratégia de mono-repo
             Evidência: ftc.all.contexts.sln + contexts/*/{context}.sln
  ADR-002 — Bounded contexts por pasta em contexts/
             Evidência: contexts/collect, contexts/core, contexts/tenant, ...
  ADR-003 — Comunicação inter-contextos só via HTTP
             Evidência: nenhum ProjectReference cruzando contextos; PolicyFactory em ftc.shared

GRUPO B — Engenharia e padrões
  ADR-004 — Framework HTTP: FastEndpoints
             Evidência: PackageReference em todos os httpapi; Endpoint.cs em Features/**
  ADR-005 — Vertical Slice Feature/Operação
             Evidência: Features/{Feature}/{Operation}/{Endpoint,Handler,Models}.cs
  ADR-006 — Padrão Result (CSharpFunctionalExtensions) para erros de negócio
             Evidência: Handler.cs retornando Result<T> em dezenas de lugares
  ...

GRUPO C — Qualidade e processo
  ADR-NNN — xUnit + FluentAssertions + Moq + TestContainers
             Evidência: *.tests.csproj + Usings.cs
  ...

Total: N ADRs propostas.

Quer que eu crie todas? Um grupo específico? Só as TOP-10 mais críticas?
```

**Pare e aguarde.** O usuário provavelmente vai querer começar por um subset — respeite.

## Passo 5 — Escrita em lote

Para cada ADR aprovada, escreva usando [../TEMPLATE.md](../TEMPLATE.md). Particularidades deste fluxo:

- **Contexto** começa com a **evidência** ("Todos os projetos `ftc.*.httpapi` referenciam `FastEndpoints`..."). Isso mostra que a ADR está formalizando prática existente, não propondo algo novo.
- **Decisão** usa tempo presente ("Adotamos FastEndpoints como..."), confirmando retroativamente.
- **Regras Operacionais** vêm da observação do código: o que é feito, o que não é, o que deveria ser.
- **Data:** use a data atual (hoje) — ADRs de inventário não tentam reconstruir a data original da decisão.
- **Status:** `Ativa`.

Se o número de ADRs a escrever for alto (>5), considere produzir em lotes e pedir confirmação intermediária ao usuário (ex.: "Acabei de criar ADRs 001–005 do Grupo A. Posso seguir para o Grupo B?").

## Passo 6 — Consistência cruzada (antes de terminar)

ADRs do Grupo A (estrutura) e Grupo B (padrões) precisam se sustentar mutuamente. Antes de entregar, verifique:

- A ADR de arquitetura interna (ex.: Vertical Slice) **combina** com a estrutura de pastas documentada na ADR de organização?
- A ADR de framework HTTP **combina** com o padrão de endpoint documentado?
- As ADRs de testes refletem a estrutura que você documentou na ADR de organização de projetos?

Se encontrar conflito, ajuste antes de entregar.

## Passo 7 — Atualizar o README

Ao final, escreva/atualize `docs/architectural-decision-records/README.md` com:
- Uma breve introdução (o que são ADRs aqui).
- Lista completa em ordem numérica.
- Link para o `TEMPLATE.md` da pasta (opcionalmente, copie o `TEMPLATE.md` da skill para a pasta de ADRs para facilitar humanos que queiram criar manualmente).

## Passo 8 — Relatório final

Reporte ao usuário:
- Quantas ADRs foram criadas.
- Caminhos dos arquivos.
- Recomendações de **próximos passos** — ex.: "vale alinhar `.cursor/rules/` e `CLAUDE.md` com estas ADRs para que as regras sejam aplicadas por IAs".

## Erros comuns neste fluxo

- **Criar ADR de tudo.** Nem toda escolha é uma decisão arquitetural. Prefira menos ADRs e mais concretas.
- **Pular a pausa de aprovação.** Um inventário pode gerar 20+ ADRs; o usuário precisa filtrar antes de escrever tudo.
- **Inventar motivações.** Se você não sabe por que foi escolhido X, escreva "Adotamos X; a evidência no repo é A/B/C" — não invente argumentos históricos.
- **Não agrupar por relevância.** Estrutura vem antes de engenharia, engenharia antes de processo — essa ordem ajuda o usuário a priorizar.
