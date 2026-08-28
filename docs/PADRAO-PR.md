# Guia de Pull Requests — CareerOS

Padrão de nomenclatura e numeração para os Pull Requests do repositório CareerOS.

## Visão geral

O CareerOS usa uma **numeração própria e manual** (`PR-###`, ex.: `PR-001`, `PR-002`)
para identificar cada Pull Request de forma consistente. Esse número **não é** o número
automático do GitHub — é um identificador de gestão que controlamos manualmente e que é
referenciado no **título do PR**, no **nome da branch** e nas **mensagens de commit**.

## Formato do título do PR

```
PR-###: <tipo>: <descricao>
```

Onde:

- `PR-###` — número sequencial próprio (ex.: `PR-001`).
- `<tipo>` — um dos tipos válidos (ver abaixo).
- `<descricao>` — descrição curta e objetiva da mudança.

Exemplos:

- `PR-001: feat: adicionar autenticação JWT com Identity local`
- `PR-002: test: adicionar testes unitários do ExportService`
- `PR-003: docs: criar guia do padrão de PRs`

## Tipos válidos

| Tipo     | Uso                                    |
|----------|----------------------------------------|
| `feat`   | Nova funcionalidade ou melhoria        |
| `fix`    | Correção de bug                        |
| `test`   | Adição/ajuste de testes                |
| `docs`   | Documentação                           |
| `refactor` | Refatoração sem mudança de comportamento |
| `chore`  | Tarefas de manutenção/build/ferramentas |
| `perf`   | Melhoria de performance                |

## Nomenclatura de branch

Cada PR trabalha em uma **branch própria** cujo nome carrega o número e o tipo.

```
<tipo>/PR-###-descricao-curta
```

Exemplos:

- `feat/PR-004-jwt-auth`
- `test/PR-002-backend-tests`
- `docs/PR-003-pr-guide`

> Use hífens (`-`) para separar as palavras da descrição curta. Não use dois-pontos no nome da branch.

## Mensagens de commit

As mensagens de commit seguem o mesmo padrão, referenciando o número do PR:

```
PR-###: <tipo>: <descricao>
```

Exemplo:

```
PR-001: feat: adicionar autenticação JWT com Identity local
```

## Registro de números

- Cada PR pega o **próximo número livre**.
- O número usado deve ser **registrado** em [`docs/PR-REGISTRO.md`](PR-REGISTRO.md),
  que lista os números já utilizados e a situação (aberto/merged/cancelado).
- Ao associar terminar (merge/drop), atualize a linha no registro.
- **Nunca reutilize um número** já atribuído.

## Checklist antes de abrir o PR

- [ ] Título no formato `PR-###: <tipo>: <descricao>`.
- [ ] Número `PR-###` é o **próximo livre** e foi **registrado** em `docs/PR-REGISTRO.md`.
- [ ] Branch segue `<tipo>/PR-###-descricao-curta`.
- [ ] Commits seguem `PR-###: <tipo>: <descricao>`.
- [ ] Testes passam (`dotnet test CareerOS.sln` no backend; `npm test` no frontend, quando aplicável).

## Exemplos

| Item        | Exemplo                                    |
|-------------|--------------------------------------------|
| Número      | `PR-004`                                   |
| Título      | `PR-004: feat: adicionar autenticação JWT` |
| Branch      | `feat/PR-004-jwt-auth`                     |
| Commit      | `PR-004: feat: adicionar autenticação JWT` |
