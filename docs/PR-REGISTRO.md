# Registro de números de Pull Requests

Este arquivo mantém o controle da **numeração manual** `PR-###` usada nos Pull Requests
do CareerOS. O padrão está documentado em [PADRAO-PR.md](PADRAO-PR.md).

## Regras

- Cada PR pega o **próximo número livre**.
- Atualize a linha ao abrir (situação `aberto`) e ao merge (`merged`) / cancelar (`cancelado`).
- **Nunca reutilize** um número já atribuído.

## Registro

| Número  | Título                          | Situação | Branch / Observação |
|---------|---------------------------------|----------|---------------------|
| PR-001  | feat: implementar telas de login/cadastro | merged | — |
| PR-002  | (frontend tests — aguardando Jules)       | aberto   | task/frontend-tests |
| PR-003  | docs: criar guia do padrão de PRs         | aberto   | task/pr-convention   |
| PR-004  | feat: adicionar autenticação JWT          | a definir| —                   |
| PR-005  | refactor: sanear domínio do backend (main quebrada: domínio duplicado, controller truncado, ExportService incompatível) + testes | aberto | task/backend-tests |

> Próximo número livre: ver última linha acima / número usado mais alto + 1.
