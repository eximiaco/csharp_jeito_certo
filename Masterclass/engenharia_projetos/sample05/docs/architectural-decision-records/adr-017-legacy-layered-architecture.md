# ADR-017: Layered Architecture — Api / Application / Domain / Infrastructure

> **Escopo:** exclusivamente `legacy/` — **não se aplica** ao código em `modernized/`.

## Contexto

O projeto legado organiza o código em quatro projetos separados por camada horizontal. Aplica-se a toda adição de código em `legacy/`.

Evidência: `legacy/Gymerp.Api/`, `legacy/Gymerp.Application/`, `legacy/Gymerp.Domain/`, `legacy/Gymerp.Infrastructure/`. ProjectReferences em cascata: `Api → Application → Infrastructure → Domain`. `Gymerp.Common/` é uma camada transversal.

## Decisão

Adotamos Layered Architecture com quatro projetos distintos. Cada camada tem responsabilidade única: `Api` expõe endpoints HTTP; `Application` orquestra casos de uso via Services; `Domain` contém entidades e interfaces de repositório; `Infrastructure` implementa persistência, migrations e serviços externos. Dependências só fluem de cima para baixo (Api → Application → Domain; Infrastructure → Domain).

## Regras Operacionais

1. **NUNCA** referencie `Gymerp.Infrastructure` diretamente de `Gymerp.Api` sem passar por `Gymerp.Application`. *Padrão detectável:* `ProjectReference.*Infrastructure` em `Gymerp.Api.csproj`. *Motivo:* Api não deve conhecer detalhes de persistência.
2. **NUNCA** referencie `Gymerp.Application` ou `Gymerp.Api` de `Gymerp.Domain`. *Padrão detectável:* `ProjectReference.*Application\|*Api` em `Gymerp.Domain.csproj`. *Motivo:* Domain é a camada mais interna; não pode depender de camadas externas.
3. **SEMPRE** coloque interfaces de repositório em `Gymerp.Domain/Interfaces/` e implementações em `Gymerp.Infrastructure/Repositories/`. *Padrão detectável:* `I*Repository.cs` em `Domain/Interfaces/`; `*Repository.cs` em `Infrastructure/Repositories/`. *Motivo:* inversão de dependência — Domain define o contrato, Infrastructure implementa.
4. **SEMPRE** coloque interfaces de serviço em `Gymerp.Application/Interfaces/` e implementações em `Gymerp.Application/Services/`. *Padrão detectável:* `I*Service.cs` em `Application/Interfaces/`; `*Service.cs` em `Application/Services/`.

## Estrutura de Arquivos

```
legacy/
├── Gymerp.Api/
│   ├── Controllers/           ← endpoints HTTP
│   └── Program.cs             ← composição e DI
├── Gymerp.Application/
│   ├── DTOs/                  ← contratos de entrada/saída
│   ├── Interfaces/            ← contratos de serviço
│   └── Services/              ← lógica de orquestração
├── Gymerp.Domain/
│   ├── Entities/              ← entidades de domínio
│   └── Interfaces/            ← contratos de repositório
├── Gymerp.Infrastructure/
│   ├── Data/                  ← DbContext e migrations
│   ├── Repositories/          ← implementação dos repositórios
│   └── Services/              ← serviços de infra (notificação, etc.)
└── Gymerp.Common/             ← utilitários transversais
```

## Benefícios

- **Separação clara de responsabilidades** — cada projeto tem uma única razão de mudar.
- **Testabilidade** — Application pode ser testada sem Api; Infrastructure pode ser mockada.

## Relacionado

- [ADR-001](./adr-001-separacao-legacy-modernized.md): este projeto é o código legado em processo de migração.
- [ADR-002](./adr-002-dominios-por-contexto.md): o modernizado adota organização por contexto em vez de camadas.

---

**Data:** 25/05/2026
**Decisão Tomada por:** Time GymErp
**Status:** Ativa
