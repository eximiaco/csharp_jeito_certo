# Fluxo 1 — Registrar uma nova decisão descrita pelo usuário

Use este roteiro quando o usuário respondeu `1` no menu inicial ou quando já chegou com "quero registrar uma ADR sobre X".

## Passo 1 — Colher a decisão do usuário

Faça ao usuário as perguntas abaixo **numa rodada só** (não fique devolvendo pergunta a pergunta). Pule as que já estiverem respondidas na mensagem dele.

1. **Qual é a decisão, em uma frase?** (será a base do título)
2. **O que motivou?** (problema observado, bug, pedido de cliente, regra externa)
3. **Em qual parte do repositório se aplica?** (ex.: "contexto `collect`", "qualquer serviço .NET que faça HTTP outbound", "projetos de teste de integração")
4. **Existe código hoje que já segue** — ou **viola** — essa decisão? Se sim, onde? (caminhos)
5. **Considerou alternativas?** Quais rejeitou e por quê?
6. **Há ADRs existentes que essa decisão refina, complementa ou substitui?**

Se o usuário não souber responder alguma (ex.: alternativas consideradas), tudo bem — siga em frente com o que houver.

## Passo 2 — Investigação no repositório

Com as respostas em mãos, faça sua própria arqueologia dirigida antes de escrever. O objetivo é **não aceitar cegamente** o que o usuário disse e enriquecer a ADR com evidência concreta.

- **Leia os ADRs relacionados** (os que o usuário citou + grep pelo tema na pasta de ADRs).
- **Busque exemplos reais** no código dos caminhos mencionados (`Grep` / `Glob`).
- **Confira se a decisão já está parcialmente codificada** em CLAUDE.md, `.cursor/rules/`, ou READMEs de contexto.
- **Descubra o próximo número de ADR:** `ls docs/architectural-decision-records/ | grep -E '^adr-[0-9]+' | sort -V | tail -1` → próximo = +1.

Se a investigação contradisser o que o usuário disse (ex.: ele afirmou que "ninguém ainda faz assim" mas você achou 3 lugares que já fazem), **aponte isso antes de escrever a ADR** — pode ser que a ADR vire uma formalização de algo que já é prática, e o tom muda.

## Passo 3 — Proposta (pausa obrigatória)

Apresente ao usuário, **antes de escrever qualquer arquivo**:

```
Proposta:

  Nº         : ADR-{XXX}
  Arquivo    : adr-{XXX}-{slug}.md
  Título     : {título proposto}
  Contexto   : {1 linha resumindo o problema + onde se aplica}
  Regras     : {lista curta do que a ADR vai obrigar/proibir}
  Impacto    : {ADRs que serão editadas/superseded, se alguma}

Confirma este escopo? (sim / ajustar)
```

**Pare aqui e aguarde.** Se o usuário pedir ajuste de título/escopo/numeração, aplique e reapresente.

## Passo 4 — Escrita

Depois de aprovado, escreva o arquivo usando [../TEMPLATE.md](../TEMPLATE.md). Diretrizes específicas deste fluxo:

- **Contexto** deve amarrar: (a) problema do usuário + (b) evidência do repo que você encontrou no Passo 2.
- **Regras Operacionais** precisam ser extraídas da decisão do usuário. Se ele falou "precisamos parar de usar X", vire isso em `NUNCA usar X. Padrão detectável: <regex/caminho>. Motivo: <frase do usuário>.`
- **Exemplos DO/DON'T**: use o código real que você encontrou (cite o arquivo). Se não achou, gere exemplos plausíveis na stack do projeto (.NET 9 / FastEndpoints / React 19 / Dapper / Flurl / WorkflowCore).
- **Status inicial:** normalmente `Ativa`. Use `Proposta` apenas se o usuário pediu para documentar uma decisão que ainda vai ser validada.

## Passo 5 — Efeitos colaterais

Depois de gravar o novo arquivo, execute **sempre**:

1. **Atualizar `docs/architectural-decision-records/README.md`** — adicione a linha da nova ADR em ordem numérica na seção "ADRs Existentes".
2. **Se a ADR substitui outra (`Superseded`)**: edite o rodapé da ADR substituída trocando o status para `Superseded by ADR-XXX` e adicione uma nota de 1 linha no topo da seção Decisão dela apontando para a nova.
3. **Se a ADR refina outra**: adicione a referência cruzada na seção *Relacionado* da outra ADR.

## Passo 6 — Entrega

Reporte ao usuário em uma mensagem curta:

- Caminho do arquivo criado.
- Resumo (3–5 linhas) do que a ADR decide.
- Lista dos arquivos que você também editou (README, ADRs relacionadas).
- Sugestão opcional de próximos passos — ex.: "vale atualizar `.cursor/rules/contexts/{contexto}-coding.mdc` para refletir a regra X desta ADR".

## Erros comuns a evitar neste fluxo

- **Escrever sem investigar o repo.** O usuário nem sempre tem o mapa completo — a investigação é o que torna a ADR útil.
- **Inventar alternativas que não foram consideradas** só para preencher a seção. Se não houve, omita.
- **Título genérico.** "Padrões de teste" é ruim; "xUnit + FluentAssertions + Moq + TestContainers" é bom.
- **Colocar Regras Operacionais vagas** ("seguir boas práticas"). Toda regra precisa de padrão detectável + motivo.
