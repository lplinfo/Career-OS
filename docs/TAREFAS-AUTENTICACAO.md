# Tarefas — Autenticação e autorização (divisão de trabalho)

> Documento operacional para execução paralela do [PLANO-AUTENTICACAO.md](PLANO-AUTENTICACAO.md).
> Leia o plano antes de implementar; este arquivo define quem faz o quê, em que ordem e o que NÃO fazer.

## Estado atual (base: `feat/PR-006-auth`)

- Backend `net9.0` em `backend/CareerOS.Api`. Já existe autenticação **legada**:
  - `Domain/User.cs` (`Id`, `Email`, `PasswordHash`, `CandidateProfileId`, `CreatedAt`) → tabela `users`
  - `Controllers/AuthController.cs` (`POST /api/auth/register`, `POST /api/auth/login`) usando `DbContext` direto
  - `Utils/PasswordHasher.cs` → SHA-256 com salt fixo (código legado a aposentar)
  - `Data/CareerDbContext.cs` herda de `DbContext`, tem `DbSet<User> Users`
  - Migrations já aplicadas: `20260828155934_InitialCreate.cs` e `20260828163932_AddUsers.cs` (**nunca editar**)
- Frontend Angular 21 em `frontend/`. `app.config.ts` já tem `provideHttpClient()`. `app.ts` envia `/api/auth/login|register` sem token, sessão em `localStorage` (`careeros_user_session`) sem `accessToken`. `app.html` tem abas Entrar/Criar Conta (e-mail/senha apenas).
- Testes: backend xUnit (`CareerOS.Api.Tests`: `PasswordHasherTests`, `ExportServiceTests`); frontend Karma+Jasmine.

## Visão das ondas

| Onda | Conteúdo | Execução |
|---|---|---|
| **1 — Fundação** | `ApplicationUser` IdentityCore sobre `users`, `CareerDbContext → IdentityUserContext`, `Program.cs` (Identity + JWT Bearer + UseAuthentication + Swagger Bearer), refatorar `AuthController` local (register/login/me) | 1 agente (Jules) |
| **2 — Paralela** | **Agente A:** JwtTokenService + ICurrentUser + `[Authorize]`/filtros de dono + migration `AddIdentityCoreToUsers` + lazy-migração legada + testes backend. **Agente B:** Google backend (`login-google`, `login-google-complete`, `exchange-google`, exchange service) + frontend (session service, interceptor, callback, botão, testes) | 2 agentes, sem tocar os mesmos arquivos |
| **3 — Integração** | Merge das branches, testes E2E, atualizar `docs/API.md` e `docs/ARQUITETURA.md` | 1 orquestrador |

---

## ONDA 1 — Fundação (agente único)

### Regras gerais (obrigatórias)

- **Não expandir escopo**: NÃO implementar Google, NÃO adicionar `[Authorize]`, NÃO mexer em autorização por dono, NÃO criar a migration `AddIdentityCoreToUsers` (é da Onda 2), NÃO alterar as migrations `InitialCreate`/`AddUsers`.
- **Não editar migrations existentes** (`InitialCreate`, `AddUsers`) e não deletar dados.
- Seguir o padrão do repo: commits `PR-007: <tipo>: <desc>` (ver `docs/PADRAO-PR.md`), nomes em inglês no código, estilo existente (primary constructors, `DateTimeOffset.UtcNow`).
- Rodar `dotnet build CareerOS.sln` / `dotnet test CareerOS.sln` ao final e corrigir o que quebrar. O frontend NÃO deve ser alterado nesta onda (exceto se exceção explícita).
- Ao remover/renomear `User`, atualizar **todas** as referências; a solução deve compilar e os testes devem passar ao final.

### F1 — Contrato de sessão (`AuthResponse`)

Criar `backend/CareerOS.Api/Contracts/AuthResponse.cs`:

```json
{
  "userId": "guid",
  "email": "ana@example.com",
  "candidateProfileId": "guid",
  "fullName": "Ana Souza",
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresAt": "2026-08-28T15:00:00+00:00"
}
```

- Nunca devolver `PasswordHash`, `SecurityStamp`, token Google ou detalhes de erro do Identity.
- Critério de aceite: tipo presente em `Contracts/` e usado na F4.

### F2 — `ApplicationUser` + `CareerDbContext` → Identity

- Criar `Domain/ApplicationUser.cs`:
  - `public sealed class ApplicationUser : IdentityUser<Guid>` com `CandidateProfileId` (Guid), `CreatedAt` (`DateTimeOffset`, default `UtcNow`) e `string? LegacyPasswordHash` (apenas janela de migração).
  - Não duplicar campos que `IdentityUser<Guid>` já fornece.
- Remover `Domain/User.cs` e atualizar todas as referências.
- Alterar `Data/CareerDbContext.cs`:
  - Passar a herdar de `IdentityUserContext<ApplicationUser, Guid>`.
  - Chamar `base.OnModelCreating(modelBuilder);` **antes** dos mapeamentos atuais.
  - Preservar todos os `DbSet` de perfil/currículo e configurações atuais.
  - Mapear tabelas (substituindo o mapeamento de `users` atual): `ApplicationUser → "users"`, `IdentityUserClaim<Guid> → "user_claims"`, `IdentityUserLogin<Guid> → "user_logins"`, `IdentityUserToken<Guid> → "user_tokens"`. Propriedades `Email`/`UserName`/`NormalizedEmail`/`NormalizedUserName` com `MaxLength(320)`; índice único em `NormalizedUserName`. Não criar tables de roles.
- **Onde Colocar pacote**: adicionar `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (version 9.x consistente com o resto, preferir 9.0.16 se compatível no restore).
- Critério de aceite: build limpo, testes ainda passam, `db.DbSet<User>` não existe mais, `ApplicationUser` disponível.

### F3 — `Program.cs`: Identity + JWT Bearer + pipeline

- Configurar `ApplicationUser`/Identity e autenticação conforme seção 5.3 do plano (esboço no plano; não copiar variável implícita inexistente).
- `AddIdentityCore<ApplicationUser>` + `AddSignInManager()` + `AddEntityFrameworkStores<CareerDbContext>()` + `AddDefaultTokenProviders()`, com opções de `RequireUniqueEmail`, políticas de senha. **Não** incluir Google nem cookies na autenticação nesta onda.
- `AddAuthentication(...).AddJwtBearer(...)` com `TokenValidationParameters` (issuer/audience/signing key/lifetime, `NameClaimType = ClaimTypes.Email`).
- Configurar `JwtOptions` (Issuer `CareerOS.Api`, Audience `CareerOS.Frontend`, `AccessTokenMinutes` 15) e a chave via `appsettings.Development.json` **não versionada** OU `dotnet user-secrets`; **não** colocar chave real em `appsettings.json` versionado. Validar no startup (falhar cedo se ausente/fraca).
- Pipeline: manter CORS antes; inserir `app.UseAuthentication();` entre `UseCors` e `UseAuthorization`.
- Swagger: adicionar definição `Bearer` + requisito de segurança.
- Critério de aceite: aplicação sobe com auth configurada; `/api/auth/*` funciona via JWT; testes de token ficam para a Onda 2.

### F4 — `AuthController` local com Identity

Refatorar `Controllers/AuthController.cs` para `UserManager<ApplicationUser>`/`SignInManager<ApplicationUser>`/`CareerDbContext`:

- `POST /api/auth/register`: valida DTO; cria salvando `CandidateProfile`; cria `ApplicationUser` com `UserName`/`Email` e `UserManager.CreateAsync(user, password)`; em falha reverte/transaciona o perfil; sucesso → emite JWT e retorna `AuthResponse` com `201 Created`.
- `POST /api/auth/login`: `FindByEmailAsync` + `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`; falhas → `401` genérico (sem enumerar e-mails); sucesso → `AuthResponse` com JWT.
- `GET /api/auth/me` com `[Authorize]`: reidrata `AuthResponse` a partir das claims/banco sem novo token (ver nota 5.5.6 do plano).
- **Não** implementar login-google/complete/exchange nesta onda.
- Injetar também um serviço de emissão de token. Como `JwtTokenService` é de outra onda, criar neste momento uma **interface `IJwtTokenService`** (`Services/IJwtTokenService.cs`) e uma implementação mínima para a F3/F4 compilarem e emitirem JWT correto (claims `sub`/`email`/`candidate_profile_id`, issuer/audience, expiry): a Onda 2-a pode evoluí-la.
- Critério de aceite: fluxo completo register→login→`/me` funcionando com JWT; testes legados passam.

### Entregáveis da Onda 1

- Código das F1–F4 compilando (`dotnet build`), testes passando (`dotnet test`).
- Commits com mensagem `PR-007: ...`.
- NÃO criar branches adicionais; trabalho nesta branch única.
- Reportar resumo de decisões tomadas (versão do pacote Identity, posição da chave JWT).

---

## ONDA 2 — Instruções operacionais (execução em 2 sessões paralelas)

Partir da branch `main` atual (contém a Onda 1: `ApplicationUser`, `IdentityUserContext`, JWT, `AuthController` com register/login/me). Dois PRs independentes:
**PR-008 = Agente A (backend)**, **PR-009 = Agente B (Google + frontend)**.

### Divisão de arquivos (NÃO violar)

| Área | Agente A (PR-008) PODE TOCAR | Agente B (PR-009) PODE TOCAR |
|---|---|---|
| `Program.cs` | Sim | **Não** |
| `Services/IJwtTokenService.cs`, `Services/JwtTokenService.cs` | Sim | Não |
| `Services/ICurrentUser.cs`, `Services/ClaimsPrincipalExtensions.cs` (novos) | Sim | Não |
| `Controllers/CandidateProfilesController.cs`, `Controllers/ResumesController.cs` | Sim | Não |
| `Services/GoogleLoginExchangeService.cs` (novo) | Não | Sim |
| `Controllers/AuthController.cs` | **Somente** dentro do método `Login` (fallback lazy SHA-256) | Sim (adicionar endpoints google) |
| `Migrations/*` (gerar `AddIdentityCoreToUsers`) | Sim | **Não** (nunca rodar `dotnet ef`) |
| `Utils/PasswordHasher.cs` | Somente leitura/reuso | Não |
| `frontend/**` (app.ts, app.html, app.routes.ts, app.config.ts, auth/*, especs) | **Não** | Sim |
| `CareerOS.Api.Tests/*` (backend) | Sim | Não |
| `frontend/*.spec.ts` | Não | Sim |

Regras p/ os agentes:
- **Proibido** editar arquivos fora da sua coluna. Em `AuthController.cs`, A altera apenas o corpo do `Login`; B apenas adiciona novos métodos.
- **Proibido** alterar migrations existentes (`InitialCreate`, `AddUsers`) nem a `Program.cs` p/ B.
- Não inserir credenciais reais: Google ClientId/ClientSecret via config local não versionada (placeholders).
- Commits `PR-008: <tipo>: <desc>` (A) / `PR-009: <tipo>: <desc>` (B).

### Agente A — backend (PR-008)
1. `JwtTokenService` definitivo (claims stantardizados `sub`/`email`/`candidate_profile_id`, validação) + `ICurrentUser` (`Services/ICurrentUser.cs`) + `ClaimsPrincipalExtensions`.
2. `[Authorize]` e filtros de dono em `CandidateProfilesController` e `ResumesController` (usar `ICurrentUser`; mappings por `CandidateProfileId`); remover criação pública de perfil sem dono (`POST` exige pertencimento) e vetar `candidateProfileId` arbitrário no corpo do `POST /api/resumes`.
3. Gerar migration `AddIdentityCoreToUsers`: renomear `PasswordHash→LegacyPasswordHash`, adicionar colunas Identity em `users` (`SecurityStamp`,`EmailConfirmed`, `NormalizedUserName`/`NormalizedEmail` índices `UserNameIndex`/`EmailIndex`, `PhoneNumber*`, `Lockout*`, `AccessFailedCount`, `TwoFactorEnabled`, `ConcurrencyStamp`), criar `user_claims`/`user_logins`/`user_tokens`, FK `users.CandidateProfileId→candidate_profiles` (`Restrict`). Rodar `dotnet ef migrations add AddIdentityCoreToUsers` a partir de `backend/CareerOS.Api`.
4. Lazy-migração SHA-256: no `Login`, se `CheckPasswordSignInAsync` falhar e `LegacyPasswordHash` preenchido e `PasswordHasher.Verify` ok → migrar o hash (rehash com Identity), persistir e logar sem expirar.
5. Testes backend (JwtTokenService, ICurrentUser, autorização/filtros de dono, migration) e build `dotnet build/test CareerOS.sln` verde.

### Agente B — Google + frontend (PR-009)
1. Backend: `POST /api/auth/exchange-google` (troca `code` Google → `AuthResponse` JWT), `GET /api/auth/login-google` (URL de autorização), `GET /api/auth/login-google-complete` (callback, usa `GoogleLoginExchangeService` com código de uso único 60s). Sem criar usuário novo? Seguir seção Google do `docs/PLANO-AUTENTICACAO.md`; ClientId/Secret via config não versionada. Não rodar `dotnet ef`.
2. Frontend: `auth/auth-session.service.ts`, `auth/auth.interceptor.ts` (Bearer + 401/403), `auth/google-callback.component.ts`, rota `auth/callback` em `app.routes.ts`, botão "Entrar com Google" em `app.html`/`app.ts`, usar `accessToken` na sessão. Manter register/login local funcionando.
3. Testes: `npm test` no `frontend/` verde + testes dos componentes novos.

### Entrega da Onda 2
Cada sessão entrega diff/PR. Integração (fora do escopo dos agentes): aplicar em branches `feat/PR-008-*` e `feat/PR-009-*`, resolver possível conflito em `AuthController.cs`, rodar `dotnet build/test` + `npm test`, `codex review` de ambos, merge na `main`. Próximo número após merge: PR-010.

## ONDA 3 (resumo — NÃO executar agora)

Merge das branches, `dotnet test` + `npm test` ponta a ponta, atualizar `docs/API.md`/`docs/ARQUITETURA.md`.