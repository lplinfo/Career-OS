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
| PR-006  | feat: adicionar autenticação (registro e login de usuários) | merged | feat/PR-006-auth |
| PR-007  | feat: migrar usuários para IdentityCore e adicionar JWT (Onda 1) | merged | feat/auth-onda1 |
| PR-008  | feat: auth evolution, filtros de dono, migration e rehash legado (Onda 2-A) | merged | feat-PR-008-backend-auth-owner-filters (PR GitHub #7) |
| PR-009  | feat: autenticação Google (backend callback + frontend) (Onda 2-B) | merged | feat/auth-onda2-final (PR GitHub #9) |
| PR-010  | docs: guia de build e execução local + atualização docs API/ARQUITETURA/registro | aberto | docs/PR-010-build-run-guide |

> Próximo número livre: `PR-011`.