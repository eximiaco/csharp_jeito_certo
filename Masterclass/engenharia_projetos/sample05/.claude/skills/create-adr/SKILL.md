---
name: create-adr
description: Cria/revisa/revoga ADRs do FTC. Use para: registrar decisão técnica, documentar padrão, formalizar escolha arquitetural, "criar ADR", "registrar decisão", "arqueologia de arquitetura", inventário de ADRs. Dispare mesmo sem mencionar "ADR" — se o pedido é capturar/formalizar uma decisão técnica, use esta skill.
---

# create-adr — Gestão de ADRs do FTC

Esta skill cuida do ciclo de vida dos **Architecture Decision Records** (ADRs) do repositório. Os ADRs produzidos aqui são **fundação para regras de desenvolvimento** seguidas por humanos e por IAs (Cursor, Claude Code, Copilot). Por isso cada ADR deve:

- **Ser objetivo** — contexto curto, decisão clara, sem rodeios.
- **Apontar evidência no repo** — caminhos concretos (`contexts/core/...`), arquivos, comandos.
- **Trazer exemplos de código** quando a decisão impacta código (DO vs DON'T).
- **Expor regras executáveis** — uma seção de regras operacionais que uma IA possa aplicar sem interpretar narrativa.

## Parâmetros fixos

| Parâmetro | Valor |
|-----------|-------|
| Pasta destino | `docs/architectural-decision-records/` (raiz do repo) |
| Template base | `.claude/skills/create-adr/TEMPLATE.md` (usar **sempre** este; ignorar `TEMPLATE.md` da pasta de ADRs quando houver conflito) |
| Idioma | Português (PT-BR) |
| Nome do arquivo | `adr-XXX-slug-em-kebab-case.md` (XXX = 3 dígitos com zeros à esquerda; sem acentos no slug) |
| Índice | `docs/architectural-decision-records/README.md` — atualizar a lista de ADRs ao final de qualquer criação/revogação |

Antes de qualquer escrita: garanta que a pasta existe (`ls docs/architectural-decision-records/`). Se não existir, crie-a.

## Passo 0 — Pergunta inicial (obrigatório)

**Sempre** comece perguntando ao usuário qual dos três fluxos ele quer, a menos que o pedido já indique claramente um deles (ex.: "crie um ADR registrando que decidimos X" → fluxo 1; "faça o inventário inicial de ADRs" → fluxo 3).

Apresente exatamente esta pergunta:

```
Qual ação você quer executar?

  1) Registrar uma nova decisão que você vai me descrever agora.
  2) Validar o repositório: procurar decisões novas, atualizações
     em ADRs existentes ou ADRs a revogar.
  3) Inventário inicial: levantar do zero os ADRs que deveriam
     existir para este repositório.
```

Aguarde a resposta (`1`, `2` ou `3`) antes de seguir. Cada fluxo tem um roteiro dedicado:

- **Fluxo 1 — Registrar nova decisão** → siga [references/register-new.md](references/register-new.md)
- **Fluxo 2 — Validar / atualizar / revogar** → siga [references/validate-repo.md](references/validate-repo.md)
- **Fluxo 3 — Inventário inicial** → siga [references/initial-inventory.md](references/initial-inventory.md)

Em qualquer um dos três fluxos, **NÃO escreva arquivos sem aprovação explícita do usuário** sobre os títulos/numeração propostos.

## Princípios transversais

Estes princípios valem para **qualquer** ADR que você escrever, independente do fluxo.

### 1. ADRs servem de regra para IAs

Pense em cada ADR como um "rulebook" que o Cursor/Claude vai ler antes de editar código do contexto. Isso muda três coisas em relação a um ADR tradicional:

- **Títulos precisam ser específicos.** Em vez de "Organização de código", escreva "Vertical Slice por Feature/Operação". Em vez de "Padrões de teste", escreva "xUnit + FluentAssertions + Moq + TestContainers".
- **A seção de Regras Operacionais é a mais importante** (ver template). Use verbos imperativos (`SEMPRE`, `NUNCA`, `PREFIRA`) com um **padrão detectável** ao lado (um regex, um caminho de pasta, um nome de arquivo, um símbolo).
- **Todo DON'T tem um DO ao lado.** Se você proíbe algo, mostre a alternativa certa.

### 2. Contexto curto, decisão direta

O **Contexto** justifica a decisão em 1–2 parágrafos. Se estiver escrevendo três parágrafos, pare e remova o que não é causa direta da decisão. Alternativas rejeitadas cabem em lista de 2–4 itens (não seção de meia página).

### 3. Exemplos que compilam (ou quase)

Exemplos de código devem ser plausíveis na stack do projeto (.NET 9 / FastEndpoints / React 19 / Dapper / Flurl / WorkflowCore). Prefira copiar um trecho real do repo a inventar pseudo-código. Se o exemplo for longo (>40 linhas), sumarize e aponte o arquivo real (`Ver contexts/collect/backend/.../MainCollectAzureBoardWorkflow.cs`).

### 4. Escopo e evidência

Toda ADR precisa responder duas perguntas no **Contexto**:

- **Onde no repo a decisão se aplica?** (ex.: "`contexts/core/backend/ftc.core`", "todos os contextos que fazem HTTP outbound", "apenas projetos de teste de integração")
- **Onde está a evidência hoje?** (arquivo/pasta concretos que já seguem — ou violam — o padrão)

### 5. Relacionamento com outras ADRs

Se a decisão refina, substitui ou conflita com outra ADR, **declare** na seção *Relacionado* e, se for substituição, marque a ADR antiga como *Superseded by ADR-XXX* atualizando o status dela.

## Numeração e nomenclatura

- Descubra o próximo número com: `ls docs/architectural-decision-records/ | grep -E '^adr-[0-9]+' | sort -V | tail -1` e some 1.
- **Zeros à esquerda** mantêm ordenação lexicográfica: `adr-001`, `adr-012`, `adr-105`.
- Slug em kebab-case, minúsculo, sem acentos, sem stopwords redundantes (ex.: `adr-040-politica-cache-http` em vez de `adr-040-a-politica-para-o-cache-http`).
- Depois de criar o arquivo, **atualize o `README.md`** da pasta de ADRs adicionando a linha correspondente em ordem numérica.

## Revisão de qualidade (antes de entregar)

Antes de declarar uma ADR como pronta, rode mentalmente este checklist:

- [ ] O título diz concretamente **qual padrão/tecnologia/regra** a ADR estabelece.
- [ ] O Contexto responde "onde no repo?" e "qual a evidência?".
- [ ] A Decisão cabe em 1–3 parágrafos e está no indicativo ("Adotamos...", "Definimos que...").
- [ ] Existe pelo menos **uma regra operacional** com verbo imperativo + padrão detectável.
- [ ] Existe pelo menos **um exemplo DO e um DON'T** (se a ADR impacta código).
- [ ] O rodapé tem data no formato `DD/MM/AAAA` e status (`Ativa` / `Proposta` / `Revogada` / `Superseded by ADR-XXX`).
- [ ] O `README.md` de ADRs foi atualizado.
- [ ] Se substitui outra ADR, a antiga foi marcada como *Superseded*.

## Sugestão de melhoria em ADRs existentes

Quando o fluxo 2 identificar uma ADR **desatualizada mas ainda válida** (ex.: o padrão continua em uso mas o exemplo aponta para código que já foi movido), a ação correta é **editar a ADR existente**, não criar uma nova. Registre a edição no rodapé com `Atualizada em DD/MM/AAAA — motivo`. Só crie nova ADR quando:

- A decisão muda de direção (aí a antiga é *Superseded*).
- A decisão antiga permanece válida mas surge um **refinamento complementar** (como ADR-039 complementa ADR-026).

Para melhorias puramente redacionais (clareza, exemplo melhor, regra que faltava) em uma ADR `Ativa`, edite in-place e descreva as mudanças ao usuário antes de salvar.

## Resumo do fluxo

1. Pergunte qual ação (1/2/3).
2. Siga o roteiro em `references/` correspondente.
3. Proponha títulos e numeração → **pause para aprovação**.
4. Escreva os arquivos usando [TEMPLATE.md](TEMPLATE.md).
5. Atualize `README.md` da pasta de ADRs.
6. Rode o checklist de revisão e reporte ao usuário o que foi feito.
