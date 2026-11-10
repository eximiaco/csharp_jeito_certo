# Fluxo 2 — Validar o repositório (atualizar, criar ou revogar ADRs)

Use este roteiro quando o usuário respondeu `2` no menu inicial. O objetivo é uma **varredura cruzada** entre o que o repositório faz hoje e o que os ADRs afirmam, produzindo três saídas:

- **ADRs novas** para decisões implícitas (o código faz, mas ninguém documentou).
- **Edições** em ADRs desatualizadas (padrão continua válido, mas exemplos/caminhos mudaram).
- **Revogações / Supersedes** quando o código claramente abandonou a decisão.

## Passo 1 — Inventário do que existe

Antes de olhar código, leia **todas** as ADRs atuais. Faça isso com um `ls` + leituras dirigidas:

```
ls docs/architectural-decision-records/
```

Para cada ADR, capture mentalmente:
- Qual é o escopo (contexto/contextos)?
- Qual é a Decisão central (1 linha)?
- Qual é o status?
- Que *Regras Operacionais* ou *Padrões Estabelecidos* ela impõe?

Não precisa transcrever tudo — basta ter o suficiente para cruzar contra o código depois. Priorize ADRs `Ativa`.

## Passo 2 — Investigação dirigida por ADR

Para cada ADR `Ativa` cujas regras impactam código, rode uma verificação. O padrão é: tomar o **padrão detectável** da ADR (ou inferi-lo se não estiver explícito) e buscar conformidade/violação.

Exemplos do tipo de verificação:

- **ADR-024 (Vertical Slice):** `Glob` por pastas `Commands/`, `Queries/`, `Controllers/`, `Repositories/` — qualquer ocorrência dentro de `contexts/*/backend/` é um desvio.
- **ADR-038 (Boundaries de features):** `Grep` por `using .*\.Domain\.Features\.` dentro de outra feature.
- **ADR-022 (Interfaces só quando justificadas):** `Grep` por interfaces `I{Nome}Handler` ou `I{Nome}HttpClient` — normalmente são ruído.
- **ADR-023 (Construtor primário):** classes de handler/endpoint/serviço usando construtor tradicional para DI simples.
- **ADR-033 (Logging Serilog via ftc.shared):** services HTTP novos sem `AddLogs()` + `AddTelemetry()` no `Program.cs`.
- **ADR-034 (Dapper + PostgreSQL):** strings de filtro sem `DbType.AnsiString` em queries indexadas.

Faça uma lista interna (não precisa mostrar ao usuário nesta fase) de:
- **Violações encontradas** → candidato a reforçar a ADR com exemplo novo.
- **Caminhos citados na ADR que não existem mais** → candidato a edição da ADR.
- **Padrões codificados que a ADR não cobre** → gap, candidato a nova ADR.

## Passo 3 — Arqueologia em busca de decisões novas

Além de validar as ADRs existentes, procure **decisões implícitas** que ainda não estão documentadas. Pontos a olhar:

- **Commits recentes** (`git log --oneline -30`) — mudanças grandes de arquitetura/infra costumam virar ADR candidata.
- **Novas pastas/projetos** aparecendo em `contexts/` — ex.: um contexto novo, um serviço novo, uma árvore de Workflows/Steps.
- **`CLAUDE.md` e `.cursor/rules/`** — regras ali descritas mas sem ADR correspondente são um forte indicativo de ADR implícita.
- **Stack e libs novas** — um `PackageReference` novo em vários projetos costuma ser decisão arquitetural.
- **READMEs de contexto e docs/how-to/** — instruções operacionais que se repetem em vários lugares.
- **Padrões de teste** — mudanças na forma de escrever testes que não estão em ADRs de teste.

## Passo 4 — Classificação das descobertas

Classifique cada achado em uma das 4 categorias:

| Categoria | Quando usar | Ação |
|-----------|-------------|------|
| **Nova ADR** | Decisão codificada em vários lugares mas não documentada. | Propor nova ADR com numeração sequencial. |
| **Editar ADR existente** | Decisão continua válida; só exemplo/caminho/regra ficou desatualizado. | Propor edição in-place + rodapé "Atualizada em DD/MM/AAAA — motivo". |
| **Superseded** | Código abandonou a decisão e adotou outra. | Propor nova ADR + marcar a antiga como `Superseded by ADR-XXX`. |
| **Revogada** | Decisão foi abandonada sem substituta clara (o contexto sumiu). | Propor status `Revogada` + nota curta explicando. |

## Passo 5 — Proposta (pausa obrigatória)

Apresente ao usuário um **resumo agrupado**, não um relatório inchado. Formato sugerido:

```
Validação concluída. Encontrei:

➕ NOVAS ADRs propostas (N):
  ADR-{XXX} — {título} — {evidência: "presente em contexts/X/... e contexts/Y/..."}
  ADR-{XXY} — {título} — {evidência}

✏️  EDIÇÕES em ADRs existentes (N):
  ADR-{YYY} — motivo: caminhos citados foram movidos; 2 regras desatualizadas.
  ADR-{ZZZ} — motivo: faltam exemplos DO/DON'T.

🔁 SUPERSEDES (N):
  ADR-{AAA} → nova ADR-{XXX} — {motivo}

❌ REVOGAÇÕES (N):
  ADR-{BBB} — {motivo}

Quer que eu prossiga com tudo, um subconjunto, ou só revise uma lista específica?
```

**Pare e aguarde.** O usuário pode escolher só um subconjunto ou pedir mais evidência em algum item antes de aprovar.

## Passo 6 — Execução

Para cada item aprovado, execute a ação correspondente:

- **Nova ADR:** escreva usando [../TEMPLATE.md](../TEMPLATE.md), seguindo os mesmos princípios do [fluxo 1](register-new.md) (Passo 4 em diante).
- **Edição:** aplique `Edit` direto na ADR, mantendo numeração, e adicione no rodapé uma linha `Atualizada em DD/MM/AAAA — {resumo curto do que mudou}`.
- **Superseded:** crie a nova ADR + edite a antiga mudando o status para `Superseded by ADR-XXX` e adicione nota curta na seção Decisão da antiga.
- **Revogada:** edite a ADR mudando status para `Revogada` e adicione nota de 1–2 linhas explicando por que não se aplica mais.

**Efeitos colaterais obrigatórios depois de qualquer alteração:**

1. Atualizar `docs/architectural-decision-records/README.md` (adicionar novas, atualizar status das que mudaram).
2. Se houve Superseded, garantir que as duas pontas (nova e antiga) se referenciam mutuamente.
3. Rodar o checklist de revisão do [SKILL.md](../SKILL.md) para cada ADR criada/editada.

## Passo 7 — Relatório final

Reporte ao usuário:
- Quantas ADRs foram criadas / editadas / revogadas / supersedidas.
- Lista dos arquivos tocados.
- Quaisquer pendências que você **não** executou (ex.: "deixei fora a ADR sobre caching porque a evidência era fraca — sugiro investigarmos depois").

## Erros comuns neste fluxo

- **Entregar um relatório gigante sem agrupar.** O usuário quer tomar decisão rápida sobre N itens, não ler 10 páginas.
- **Propor "nova ADR" para algo que é só edição.** Se o padrão continua válido, edite.
- **Não ler o CLAUDE.md do projeto.** Ele frequentemente já sumariza regras que merecem ADR própria.
- **Supersedar sem investigação forte.** Antes de marcar algo como substituído, tenha evidência de código clara.
