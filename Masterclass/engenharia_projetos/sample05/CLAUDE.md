# GymErp — Harness raiz

Este arquivo é a **única fonte de verdade operacional** para Claude Code neste repositório. `.cursor/rules/*.mdc` é legado e será removido — não consulte.

## Roteamento por escopo

| Você está editando… | Carregue regras de… |
|---|---|
| `modernized/**` | [`modernized/CLAUDE.md`](./modernized/CLAUDE.md) |
| `legacy/**` | [`legacy/CLAUDE.md`](./legacy/CLAUDE.md) |
| `docs/architectural-decision-records/**` | Template em [`docs/architectural-decision-records/TEMPLATE.md`](./docs/architectural-decision-records/TEMPLATE.md) ou skill `/create-adr` |

ADRs ficam em [`docs/architectural-decision-records/`](./docs/architectural-decision-records/). Cada ADR declara seu escopo (`modernized` / `legacy` / repo). 23 ADRs ativas — leia o [`README.md`](./docs/architectural-decision-records/README.md) antes de criar nova.

## Regras universais (valem nos 2 escopos)

### R1 — Cite a ADR ao decidir
Toda decisão arquitetural deve referenciar a ADR que a sustenta. Se a ADR não existe e o padrão é recorrente, **pare e escale** (regra R3). Não invente convenção nova sem ADR.

### R2 — Nunca misture padrões entre escopos
- **Não** introduza `Result<T>`, FastEndpoints, `IUnitOfWork`, Vertical Slice, ou Aggregates com factory method em `legacy/`.
- **Não** introduza MVC Controllers, `InvalidOperationException` para regra de negócio, interfaces obrigatórias por serviço, ou `SaveChangesAsync` por operação em `modernized/`.
- ADR-001 estabelece a separação como estratégia de migração — manter as duas árvores coerentes consigo mesmas é o ponto.
- **Integração entre escopos** (rota legacy↔modernized, dados que atravessam a fronteira) acontece **exclusivamente** via `modernized/GymErp/Domain/Orchestration/`. Nenhuma outra pasta de `modernized/` deve fazer chamada HTTP para legacy. Ver `modernized/CLAUDE.md` §13.

### R3 — Padrão sem ADR = bloqueador
Se você encontrar padrão de código recorrente que **não** está em ADR e não está em `modernized/CLAUDE.md` ou `legacy/CLAUDE.md`:
1. **Não** copie o padrão.
2. **Não** invente um novo.
3. Pergunte ao usuário; se confirmado como padrão oficial, escale para virar ADR via skill `/create-adr`.

### R4 — Bugs conhecidos não são padrão
Itens listados em "Cleanup pendente" nos CLAUDE.md por escopo são **bugs** — não copie o código existente, siga o padrão prescrito.

## Operação

- **Builds/testes**: rode no escopo certo. `dotnet build modernized/GymErp.sln` vs `legacy/gymerp.sln`. Solução raiz `GymErp.sln` agrega ambos.
- **Linguagem**: PT-BR em mensagens de negócio (erros, logs de domínio), EN em código (nomes, comentários técnicos quando absolutamente necessários).
- **Não crie arquivos `.md` novos** sem pedido explícito (ADRs e CLAUDE.md são exceção autorizada).

## Skills relevantes

- `/create-adr` — cria/revisa/revoga ADR (já existe em `.claude/skills/create-adr/`)
- `/code-review`, `/security-review`, `/verify` — qualidade antes de commit
