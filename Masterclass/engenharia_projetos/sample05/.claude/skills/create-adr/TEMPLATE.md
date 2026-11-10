# ADR-XXX: [Título específico — o quê + quando/onde]

> Exemplos de bons títulos: "Padrão Result para Tratamento de Erros", "xUnit + FluentAssertions + Moq + TestContainers", "Boundaries de Features no Mesmo Contexto". Evite títulos genéricos tipo "Padrões de teste" ou "Organização do código".

## Contexto

[1–2 parágrafos. Responda obrigatoriamente:
  1. **Onde** no repo a decisão se aplica? (ex.: "contexto `core`", "qualquer serviço .NET que faça HTTP outbound", "apenas projetos de teste de integração")
  2. **Qual a evidência** hoje? (caminho concreto de pasta/arquivo que já segue — ou viola — o padrão)
  3. **O que motivou** a decisão? (problema observado, bug recorrente, regra imposta externamente)]

**Alternativas consideradas** (quando aplicável, lista de 2–4 itens):
1. [Alternativa A] — rejeitada porque...
2. [Alternativa B] — rejeitada porque...
3. [Alternativa C] — **escolhida**.

## Decisão

[1–3 parágrafos no indicativo. Comece com "Adotamos...", "Definimos que...", "Estabelecemos...". Nada de "propomos" ou "sugerimos" — ADR `Ativa` é afirmativa.]

## Regras Operacionais

> Esta seção é a fundação para regras de desenvolvimento usadas por IAs (Cursor, Claude Code, Copilot). Cada regra deve ter **verbo imperativo** + **padrão detectável** (caminho, símbolo, regex) + o **porquê** em uma linha.

1. **SEMPRE** [ação]. *Padrão detectável:* [`Features/*/{Feature}/{Operation}/Endpoint.cs`]. *Motivo:* [por que].
2. **NUNCA** [ação proibida]. *Padrão detectável:* [`using *.Domain.Features.{OtherFeature}.` dentro de outra feature]. *Motivo:* [por que].
3. **PREFIRA** [opção A] a [opção B] quando [condição]. *Motivo:* [por que].

[Adicione tantas regras quanto necessário. Se tiver mais de 8 regras, provavelmente a ADR está querendo cobrir dois assuntos — considere dividir.]

## Exemplos

### ✅ DO

```csharp
// Arquivo: contexts/{contexto}/backend/.../{exemplo-real-ou-plausível}.cs
public sealed class ListUsersHandler(
    IStorageService storage,
    TenantAccessor tenant)
{
    public async Task<Result<ListUsersResponse>> HandleAsync(
        ListUsersRequest req, CancellationToken ct)
    {
        // ...
    }
}
```

### ❌ DON'T

```csharp
// Por que está errado: [1 linha explicando o problema]
public class ListUsersHandler
{
    private readonly IStorageService _storage;
    public ListUsersHandler(IStorageService storage) { _storage = storage; }
    // ...
}
```

[Use a linguagem/stack correta do contexto impactado (C# para backend .NET, TypeScript/TSX para frontend, YAML para compose, etc.). Se a ADR não impacta código diretamente (ex.: decisão puramente organizacional), pode-se omitir esta seção.]

## Padrões Estabelecidos (Opcional)

[Use quando a decisão introduz convenções nomeadas que merecem uma subseção própria — ex.: nomenclatura de testes, estrutura de pastas de workflow, nomes de interfaces.]

### 1. [Nome do padrão]

- **Formato:** [descrição]
- **Exemplo:** `GetAgingChart_WithDoneItem_ShouldNotAppearInCurrentDate`

## Estrutura de Arquivos (Opcional)

[Quando a decisão define layout de pastas/arquivos, mostre a árvore. Use caminhos reais do repo.]

```
contexts/{contexto}/backend/{projeto}/
├── Domain/
│   └── Features/
│       └── {Feature}/
│           └── {Operation}/
│               ├── Endpoint.cs
│               ├── Handler.cs
│               └── Models.cs
└── Program.cs
```

## Comparação de Abordagens (Opcional)

[Só use se você realmente avaliou múltiplas alternativas e uma tabela ajuda a entender o porquê da escolha.]

| Critério | Abordagem A (escolhida) | Abordagem B | Abordagem C |
|----------|-------------------------|-------------|-------------|
| [Critério 1] | ✅ | ❌ | ⚠️ |
| [Critério 2] | ✅ | ✅ | ❌ |

## Benefícios

- **[Benefício 1]** — [frase curta sobre como este benefício se manifesta no projeto]
- **[Benefício 2]** — ...
- **[Benefício 3]** — ...

## Riscos e Limitações (Opcional)

- **[Risco 1]:** [descrição]. *Mitigação:* [como reduzimos].
- **[Risco 2]:** [descrição]. *Mitigação:* [como reduzimos].

## Como Verificar Conformidade (Opcional mas recomendado)

> Dicas para humanos e IAs checarem se o código segue a ADR.

- **Grep de anti-padrão:** `rg "using.*Domain\.Features\." --type cs` dentro de outra feature.
- **Checklist manual:** [itens concretos, ex.: "todo `Endpoint.cs` tem um `Models.cs` ao lado"].
- **Lint/build automatizado:** [se existir uma regra no `.editorconfig`, `eslint`, Roslyn analyzer, mencione].

## Próximos Passos (Opcional)

1. [Ação específica — ex.: "Aplicar em novas features"]
2. [Ação específica — ex.: "Refatorar `X` no próximo ciclo"]
3. [Ação específica — ex.: "Atualizar `.cursor/rules/contexts/{contexto}.mdc` para refletir esta ADR"]

## Relacionado (Opcional)

- [ADR-XXX](./adr-XXX-slug.md): [Como se relaciona — complementa? refina? depende?]
- [Link externo](https://...): [o que é]

---

**Data:** DD/MM/AAAA
**Decisão Tomada por:** [Pessoa ou equipe]
**Status:** Ativa <!-- Opções: Proposta | Ativa | Revogada | Superseded by ADR-XXX -->
