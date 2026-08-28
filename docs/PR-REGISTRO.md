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
| PR-001  | Candidate Profile Collections and Multilingual Resume Exports | merged | feat/careeros-multilingual-resume-exports |
| PR-002  | Set up Karma+Jasmine test infrastructure and unit tests | merged | feat-frontend-karma-jasmine-unit-tests |
| PR-005  | refactor: sanear domínio do backend e regenerar migrations | merged | task/backend-tests |
| PR-006  | feat: adicionar autenticação (registro e login de usuários) | aberto  | feat/PR-006-auth |

> Próximo número livre: número usado mais alto + 1 → `PR-007`.