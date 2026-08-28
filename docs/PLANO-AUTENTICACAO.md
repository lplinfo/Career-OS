# Plano técnico — autenticação e autorização do CareerOS

> Escopo deste documento: plano de implementação. Nenhum trecho abaixo foi aplicado ao código. Os exemplos são deliberadamente parciais e servem para orientar a alteração futura.

## 0. Resumo e decisões

O CareerOS passará a autenticar usuários por senha local e por Google, ambos com **ASP.NET Core Identity persistido no PostgreSQL local**. Depois de qualquer autenticação válida, a API emitirá um JWT próprio do CareerOS. O navegador nunca usará um access token do Google para chamar a API.

Decisões propostas:

- Usar `IdentityCore<ApplicationUser>` com chave `Guid`, `UserManager` e `SignInManager`; não usar roles agora. A autorização inicial é por propriedade do recurso, não por papel.
- Evoluir a tabela existente `users`, mapeando-a como a tabela de usuários do Identity, e criar somente as tabelas auxiliares necessárias a claims, tokens e logins externos. Isso preserva `CandidateProfileId` e os usuários existentes.
- Criar `ApplicationUser : IdentityUser<Guid>` em `backend/CareerOS.Api/Domain/ApplicationUser.cs`; substituir a entidade `User` atual. O campo de vínculo `CandidateProfileId` continua no usuário.
- Assinar JWTs de curta duração com chave simétrica configurada fora do repositório em ambientes reais. Claims mínimas: `sub`/`ClaimTypes.NameIdentifier` (id do usuário), `email` e `candidate_profile_id`.
- Proteger integralmente `CandidateProfilesController` e `ResumesController` com `[Authorize]`, filtrando consultas e mutações pelo `candidate_profile_id` do JWT. Um GUID conhecido por outro usuário não será autorização.
- Para Google, usar o fluxo externo do Identity (`AddAuthentication().AddGoogle()`), finalizar o login no backend e redirecionar para um callback Angular com **código de troca único e curto**, não com JWT na query string. O callback troca o código pelo JWT da aplicação.
- Migrar hashes SHA-256 legados de forma controlada: verificar uma única vez e re-hash imediatamente com o hasher do Identity, ou exigir redefinição de senha. A opção preferida abaixo é migração preguiçosa de uma vez, seguida de remoção do código legado.

## 1. Estado atual e problemas

O backend é ASP.NET Core 9 (`net9.0`) em `backend/CareerOS.Api`. Em `Program.cs` há `AddControllers`, EF Core/Npgsql, Swagger, CORS para `http://localhost:4200`, `UseHttpsRedirection`, `UseCors` e `UseAuthorization`. Não há `AddAuthentication`, handler JWT ou `UseAuthentication`; portanto `UseAuthorization` não tem uma identidade autenticada para avaliar.

A conta atual, em `backend/CareerOS.Api/Domain/User.cs`, possui `Id`, `Email`, `PasswordHash`, `CandidateProfileId` e `CreatedAt`; é persistida como `users` em `Data/CareerDbContext.cs`. `AuthController` faz login e cadastro diretamente no `DbContext` e retorna apenas dados de sessão. `Utils/PasswordHasher.cs` usa SHA-256 com salt fixo no código. Isto não oferece work factor, salt individual, versionamento de hash, lockout nem os recursos de credencial do Identity; deve ser aposentado.

`CandidateProfilesController` e `ResumesController` não têm `[Authorize]`. Todos os `GET`, `PUT`, `DELETE`, exportações e o `GET by-candidate` aceitam qualquer GUID. Além de expor dados, `POST /api/resumes` e `PUT /api/resumes/{id}` aceitam `candidateProfileId` do corpo, permitindo reassociar currículo de outro usuário.

No frontend, `frontend/src/app/app.ts` usa um `UserSession` sem token, persiste-o em `localStorage` como `careeros_user_session` e faz todas as chamadas HTTP sem `Authorization`. `frontend/src/app/app.config.ts` ainda não provê `HttpClient`; a aplicação usa injeção de `HttpClient` no componente. A tela em `app.html` só oferece e-mail/senha.

## 2. Identity local vs. Azure

**ASP.NET Core Identity funciona totalmente de forma local; Azure não é requisito.** Identity é uma biblioteca de gerenciamento de contas e credenciais. Neste plano, seus registros ficam no PostgreSQL configurado por `ConnectionStrings:CareerDatabase` e são administrados por migrations do EF Core.

Google é apenas um provedor externo de identidade: a aplicação precisa de `ClientId` e `ClientSecret` criados no Google Cloud Console e de uma URL de callback HTTPS registrada ali. Isso não envolve Azure. Em produção, PostgreSQL, API e segredos podem estar em qualquer infraestrutura; Azure seria apenas uma possível opção de hospedagem/secret manager, não uma dependência do desenho.

## 3. Arquitetura proposta

### Fluxos

```text
[Angular] -- POST e-mail/senha --> [AuthController] --> [UserManager/ApplicationUser]
                                                     --> [PostgreSQL: users + user_logins]
                                                           |
                                                           v
                                                    [JwtTokenService]
                                                           |
              <-- AuthResponse { accessToken, expiresAt, dados da sessão } --+

[Angular] -- GET /api/auth/login-google --> [Google handler]
                                               |  (Google autentica o usuário)
                                               v
                           [/signin-google: callback técnico do handler]
                                               |
                                      [login-google-complete no backend]
                                               |
                               [SignInManager.GetExternalLoginInfoAsync]
                                               |
                       [cria/localiza ApplicationUser + CandidateProfile]
                                               |
                         [gera código único, 60 s, cache/armazenamento]
                                               v
[Angular /auth/callback?code=...] -- POST /api/auth/exchange-google --> [JwtTokenService]
                                               |
              <-- AuthResponse com JWT próprio do CareerOS ------------+

[Angular Http interceptor] -- Authorization: Bearer <JWT> --> [JWT bearer middleware]
                                                                  |
                                                                  v
                                    [Controller + CurrentUser accessor]
                                    somente CandidateProfileId da claim
```

### Contrato proposto de sessão

Atualizar o DTO de resposta (idealmente mover para `backend/CareerOS.Api/Contracts/AuthResponse.cs`) para:

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

Não devolver `PasswordHash`, `SecurityStamp`, token Google ou detalhes de erro do Identity. Os erros públicos devem ser genéricos no login para não enumerar e-mails.

## 4. Migração de dados do `User`

### Escolha: `IdentityCore` sobre a tabela `users` existente

Usar `AddIdentity` completo configuraria esquemas/cookies e roles que a API não precisa. A escolha é `AddIdentityCore<ApplicationUser>()` + `AddSignInManager()` + store EF. Para suportar senha e login Google sem tabelas de role, mudar `CareerDbContext` para herdar de `IdentityUserContext<ApplicationUser, Guid>` e usar `AddEntityFrameworkStores<CareerDbContext>()`.

Isso cria o suporte padrão de Identity para `users`, `user_claims`, `user_logins` e `user_tokens`; **não** cria `AspNetRoles`, `AspNetUserRoles` etc. Caso RBAC seja necessário no futuro, a evolução pode passar para `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` em migration separada.

Em `Data/CareerDbContext.cs`, mapear explicitamente as tabelas para nomes consistentes com o banco existente:

```csharp
// base.OnModelCreating(modelBuilder) primeiro.
modelBuilder.Entity<ApplicationUser>().ToTable("users");
modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

modelBuilder.Entity<ApplicationUser>().Property(x => x.Email).HasMaxLength(320);
modelBuilder.Entity<ApplicationUser>().Property(x => x.NormalizedEmail).HasMaxLength(320);
modelBuilder.Entity<ApplicationUser>().Property(x => x.UserName).HasMaxLength(320);
modelBuilder.Entity<ApplicationUser>().Property(x => x.NormalizedUserName).HasMaxLength(320);
modelBuilder.Entity<ApplicationUser>().HasIndex(x => x.NormalizedUserName).IsUnique();
```

`ApplicationUser` deve conservar os dados de domínio:

```csharp
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid CandidateProfileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Apenas durante a janela de migração; remover depois de todos migrarem.
    public string? LegacyPasswordHash { get; set; }
}
```

`IdentityUser<Guid>` já fornece `Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled` e `AccessFailedCount`. Não duplicar esses campos na entidade. `CandidateProfileId` e `CreatedAt` não existem no Identity e permanecem.

### Migration e dados existentes

1. Antes de gerar a migration, executar auditoria SQL somente leitura: e-mails nulos, GUIDs sem perfil correspondente, e e-mails duplicados quando normalizados (`lower(trim(email))`). Corrigir colisões antes de índices únicos normalizados.
2. Criar migration, por exemplo `AddIdentityCoreToUsers`, a partir da alteração de modelo; nunca editar as migrations já aplicadas `InitialAll` e `AddUsers`.
3. A migration deve renomear `users.PasswordHash` para `LegacyPasswordHash`, adicionar o novo `PasswordHash` nullable e os campos Identity citados. Preencher `UserName`, `NormalizedUserName`, `NormalizedEmail` com o e-mail normalizado e inicializar `ConcurrencyStamp`/`SecurityStamp` com GUIDs. Preservar `Id`, `CandidateProfileId` e `CreatedAt`.
4. Criar índices únicos em `NormalizedUserName` e, se a regra continuar sendo um e-mail por conta, em `NormalizedEmail`; remover/substituir com cuidado o índice antigo `IX_users_Email`. Criar `user_logins` com chave composta `(LoginProvider, ProviderKey)` e FK para `users`.
5. Adicionar FK de `users.CandidateProfileId` para `candidate_profiles.Id` somente após a auditoria. Definir `OnDelete(DeleteBehavior.Restrict)`: apagar perfil não pode deixar conta órfã.
6. Gerar SQL para revisão e testar em cópia do banco: `dotnet ef migrations add AddIdentityCoreToUsers`, `dotnet ef migrations script` e depois `dotnet ef database update` no ambiente de desenvolvimento.

### Transição da senha legada

Opção recomendada: durante uma janela limitada, no login local, quando `PasswordHash` estiver nulo e `LegacyPasswordHash` existir, verificar a senha com o hasher legado; se correta, atribuir a nova senha via `UserManager` (que usa PBKDF2 versionado por padrão), limpar `LegacyPasswordHash` e salvar tudo na mesma unidade de trabalho. Em qualquer erro, responder o mesmo `401` genérico. Esse caminho só aceita hash antigo para converter, jamais para cadastrar/alterar senha.

Após métricas indicarem que todas as contas ativas migraram (ou após prazo comunicado), forçar reset de senha das restantes, criar migration que remove `LegacyPasswordHash`, apagar `Utils/PasswordHasher.cs` e substituir `PasswordHasherTests.cs` por testes de Identity. Não manter SHA-256 como fallback permanente.

**Armadilhas importantes:** o usuário atual possui exatamente um perfil. Assim, após cadastrar/localizar a conta, o servidor deve sempre criar/vincular um `CandidateProfile` antes de emitir JWT. `POST /api/candidate-profiles` deixa de ser uma forma livre de criar perfis; retornará conflito/será removido, pois o perfil nasce no register/Google. `DELETE /api/candidate-profiles/{id}` conflita com a FK e com a semântica de conta: substituir por exclusão de conta deliberada ou desabilitá-lo (por exemplo, `409` orientando a operação de conta). Não permitir apagar apenas o perfil atual.

## 5. Passo a passo — backend

### 5.1 Pacotes e configurações

Em `backend/CareerOS.Api/CareerOS.Api.csproj`, manter a linha 9 do runtime consistente com o projeto atual e adicionar versões explícitas:

| Pacote | Versão proposta | Finalidade |
| --- | --- | --- |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `9.0.16` | Store EF Core de Identity com PostgreSQL |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `9.0.16` | Validação do JWT da API |
| `Microsoft.AspNetCore.Authentication.Google` | `9.0.16` | Handler OAuth/OpenID Connect do Google |
| `System.IdentityModel.Tokens.Jwt` | `8.16.0` | Criação explícita do JWT por `JwtSecurityTokenHandler` |
| `Microsoft.EntityFrameworkCore.InMemory` | `9.0.16` (somente testes) | Testes unitários de store/serviços; preferir PostgreSQL real para integração |
| `Microsoft.AspNetCore.Mvc.Testing` | `9.0.16` (somente testes) | `WebApplicationFactory` para testes HTTP |

Antes de fixar as referências, validar a disponibilidade/compatibilidade no restore da solução e alinhar quaisquer atualizações de patch do conjunto `Microsoft.AspNetCore.*` e EF Core; não misturar major 8 com runtime 9.

Adicionar em `backend/CareerOS.Api/appsettings.Development.json` (arquivo novo local, preferencialmente ignorado se contiver segredo) apenas valores não sensíveis como issuer/audience. Para segredos, configurar:

```bash
dotnet user-secrets init --project backend/CareerOS.Api
dotnet user-secrets set "Authentication:Jwt:Key" "<base64-de-pelo-menos-32-bytes>" --project backend/CareerOS.Api
dotnet user-secrets set "Authentication:Google:ClientId" "..." --project backend/CareerOS.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "..." --project backend/CareerOS.Api
```

Esquema de configuração (sem chave real no Git):

```json
"Authentication": {
  "Jwt": { "Issuer": "CareerOS.Api", "Audience": "CareerOS.Frontend", "AccessTokenMinutes": 15 },
  "Google": { "ClientId": "", "ClientSecret": "" },
  "FrontendBaseUrl": "http://localhost:4200"
}
```

Em produção, sobrescrever por variáveis `Authentication__Jwt__Key`, `Authentication__Google__ClientId` e `Authentication__Google__ClientSecret`/cofre de segredos. Registrar no Google a redirect URI técnica exata do handler, por exemplo `https://api.exemplo.com/signin-google` (e a URI HTTPS local de desenvolvimento usada pelo perfil). Ela não é a rota de conclusão da API.

### 5.2 Identidade, banco e serviços

1. Criar `Domain/ApplicationUser.cs` como descrito na seção 4; remover `Domain/User.cs` somente na implementação que também atualizar todos os usos.
2. Alterar `Data/CareerDbContext.cs` para `IdentityUserContext<ApplicationUser, Guid>` e preservar todos os `DbSet` de perfil/currículo. Chamar `base.OnModelCreating` antes dos mapeamentos atuais e configurar as tabelas/limites/índices Identity.
3. Criar `Services/JwtTokenService.cs` e uma interface `Services/IJwtTokenService.cs`. O serviço recebe `IOptions<JwtOptions>`, valida configuração no startup e contém a única implementação de geração de token.
4. Criar `Services/CurrentUser.cs`/`ICurrentUser.cs` ou uma extensão `Extensions/ClaimsPrincipalExtensions.cs` que lê e valida `candidate_profile_id` como GUID. Centralizar esse parsing evita ações que esquecem validar a claim.
5. Criar `Services/GoogleLoginExchangeService.cs` para armazenar e consumir uma vez o código de troca de 60 segundos. Em desenvolvimento uma implementação com `IMemoryCache` é suficiente; em produção usar `IDistributedCache` compartilhado (Redis ou tabela), pois o callback e o POST de troca podem atingir instâncias distintas. Não armazenar token Google nesse código.

### 5.3 `Program.cs`: DI, Identity, JWT e pipeline

Em `backend/CareerOS.Api/Program.cs`, após o `AddDbContext`, registrar Identity e autenticação. Esboço:

```csharp
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Authentication:Jwt"));
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddSignInManager()
.AddEntityFrameworkStores<CareerDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.Email
    };
})
.AddCookie(IdentityConstants.ExternalScheme)
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
    options.SignInScheme = IdentityConstants.ExternalScheme;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = false;
});
```

Na implementação real, obter `jwt`/`signingKey` de opções validadas no startup (falhar cedo se ausentes ou fracas), e não de uma variável implícita inexistente no esboço. Os serviços customizados (`IJwtTokenService`, `ICurrentUser`, store de exchange) também entram no DI.

Manter CORS antes dos endpoints; `AllowAnyHeader()` já permite `Authorization`. Entre `UseCors("frontend")` e `UseAuthorization()`, inserir obrigatoriamente:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

No Swagger, adicionar definição `Bearer` e requisito de segurança, para permitir testar endpoints protegidos com `Authorization: Bearer <JWT>`.

### 5.4 Serviço e claims JWT

`JwtTokenService` deve aceitar `ApplicationUser` e o nome de exibição já obtido do perfil. Não carregar dados confiando no cliente. Esboço:

```csharp
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email!),
    new Claim("candidate_profile_id", user.CandidateProfileId.ToString())
};
var token = new JwtSecurityToken(
    issuer: options.Issuer, audience: options.Audience, claims: claims,
    notBefore: now.UtcDateTime, expires: now.AddMinutes(options.AccessTokenMinutes).UtcDateTime,
    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
return new JwtSecurityTokenHandler().WriteToken(token);
```

Usar `DateTimeOffset.UtcNow`, uma única origem de clock injetável para teste e `jti` se for implementada lista de revogação. Nunca colocar senha, hash, segredo, dados extensos de perfil ou token Google nas claims.

### 5.5 `AuthController` e endpoints

Refatorar `Controllers/AuthController.cs` para receber `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `CareerDbContext`, `IJwtTokenService`, opções de URL frontend e o serviço de exchange. Separar DTOs internos para `Contracts/Auth` se o arquivo continuar crescendo.

1. `POST /api/auth/register`: validar DTO; criar e salvar o `CandidateProfile`; criar `ApplicationUser` com `UserName` e `Email`; chamar `UserManager.CreateAsync(user, request.Password)`. Em falha, desfazer/transactionar o perfil. Sucesso gera JWT e devolve `AuthResponse` com `201 Created` (documentar a mudança do `200` atual).
2. `POST /api/auth/login`: localizar com `FindByEmailAsync`; chamar `CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`; mapear falhas a `401` genérico e bloqueio a `423`/mensagem apropriada conforme política. Ao êxito, fazer a migração preguiçosa legada quando aplicável, carregar o perfil e emitir JWT.
3. `GET /api/auth/login-google`: construir `AuthenticationProperties` com `RedirectUri = Url.Action(nameof(LoginGoogleComplete))`; retornar `Challenge(properties, GoogleDefaults.AuthenticationScheme)`. Validar/guardar internamente o retorno permitido; nunca aceitar URL arbitrária do cliente como redirect. O handler redirecionará o navegador ao Google.
4. O `CallbackPath` `/signin-google` é manipulado pelo middleware Google e grava a identidade externa no cookie temporário `IdentityConstants.ExternalScheme`; ele não deve coincidir com controller. Em seguida o handler redireciona para `GET /api/auth/login-google-complete`, que chama `GetExternalLoginInfoAsync`; procura `FindByLoginAsync(info.LoginProvider, info.ProviderKey)`; se não existir, obtém e-mail confirmado na claim Google, procura por e-mail (política de link), cria perfil mínimo e `ApplicationUser` sem senha, e chama `AddLoginAsync`. O nome/título ausente deve levar o usuário a completar o perfil, não ser inventado como dado confiável. Gerar código de troca, limpar o cookie externo e redirecionar apenas para `${FrontendBaseUrl}/auth/callback?code=...`.
5. `POST /api/auth/exchange-google`: receber o código, consumi-lo atomicamente e, se válido/não expirado, carregar o usuário e emitir o JWT. Retornar o mesmo `AuthResponse`. Reuso, expiração ou usuário inexistente retornam `401` sem detalhes.
6. Opcional, mas recomendado para UX: `GET /api/auth/me` com `[Authorize]`, que monta `AuthResponse` sem senha/token novo a partir das claims/banco. Útil para reidratar sessão sem confiar cegamente no `localStorage`.

Definir previamente política de e-mail Google: só auto-linkar se `email_verified` for verdadeiro e o e-mail coincidir; em contas locais existentes, pode exigir login local e fluxo explícito de “vincular Google”, em vez de vinculação automática. Isso evita takeover se o provedor/claim for mal configurado.

### 5.6 Autorização por propriedade

Adicionar `[Authorize]` no nível de classe em:

- `backend/CareerOS.Api/Controllers/CandidateProfilesController.cs`;
- `backend/CareerOS.Api/Controllers/ResumesController.cs`.

O helper deve produzir `Unauthorized` para JWT ausente/invalidado (feito pelo middleware) e `Forbid` (`403`) para token válido cujo perfil não é dono do recurso. Esboço de leitura segura:

```csharp
private Guid CurrentCandidateProfileId() =>
    User.GetCandidateProfileId(); // lança/retorna falha controlada se a claim for inválida
```

Aplicação por rota:

| Controller/ação atual | Mudança obrigatória |
| --- | --- |
| `CandidateProfiles.GetAll` | Não listar todos: consultar `Where(x => x.Id == currentProfileId)` ou remover a rota/listagem administrativa. |
| `CandidateProfiles.Get/Put/Delete(id)` | Comparar `id` com a claim antes de carregar/modificar. Para `Delete`, substituir pela operação de excluir conta ou bloquear conforme seção 4. |
| `CandidateProfiles.Post` | Remover/retirar do contrato público: perfil é criado pelo cadastro. Se mantido, ignorar qualquer identidade do corpo e garantir um único perfil do usuário. |
| `Resumes.GetAll` | Filtrar `Where(x => x.CandidateProfileId == currentProfileId)`. |
| `Resumes.Get(id)`, `Put`, `Delete`, `ExportPdf/Docx/Ats` | Carregar somente com `FirstOrDefaultAsync(x => x.Id == id && x.CandidateProfileId == currentProfileId)`. Para reduzir enumeração, `404` é aceitável para recurso alheio; `403` também é válido se já foi carregado. Escolher uma regra e testá-la. |
| `Resumes.GetByCandidate(candidateProfileId)` | Rejeitar quando o parâmetro diferir da claim, ou substituir por `GET /api/resumes/mine` sem GUID. |
| `Resumes.Post` | Não confiar em `ResumeRequest.CandidateProfileId`: removê-lo do contrato de criação ou sobrescrevê-lo no servidor com a claim. |

Para esta aplicação pequena, o helper de claim + filtros EF é mais claro que um handler complexo. Se crescer para compartilhamento de currículos, criar `AuthorizationHandler<OperationAuthorizationRequirement, Resume>`/`CandidateProfile` e chamar `IAuthorizationService.AuthorizeAsync`; centraliza políticas sem espalhar regras.

## 6. Passo a passo — frontend Angular

1. Em `frontend/src/app/app.config.ts`, adicionar `provideHttpClient(withInterceptors([authInterceptor]))` e criar `frontend/src/app/auth/auth.interceptor.ts`. O interceptor deve anexar `Authorization: Bearer ${session.accessToken}` apenas para URLs sob `http://localhost:5062/api`, excluindo `/api/auth/login`, `/register`, `/login-google` e `/exchange-google` quando não houver token.

```ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = authStorage.get()?.accessToken;
  const isApi = req.url.startsWith('http://localhost:5062/api');
  return next(isApi && token && !isExpired(token)
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req);
};
```

O código real não deve instanciar `authStorage` globalmente: criar `frontend/src/app/auth/auth-session.service.ts` injetável, responsável por persistir, limpar, verificar `expiresAt` e expor a sessão. Centralizar a chave `careeros_user_session` nele.

2. Atualizar `UserSession` em `frontend/src/app/app.ts` (ou movê-lo para `auth/auth.models.ts`) com `accessToken`, `tokenType` e `expiresAt`. `setupUserSession` só persiste uma resposta que contenha JWT válido e ainda não expirado. Ao restaurar, descartar sessão expirada e não carregar perfil/currículos.
3. Alterar `login()` e `register()` para tipar a resposta como o novo contrato e continuar chamando `setupUserSession`; nenhum header manual é necessário nas APIs protegidas após o interceptor.
4. Em `frontend/src/app/app.html`, abaixo de “Entrar na Conta”, acrescentar botão “Entrar com Google”. O método em `app.ts` redireciona a página inteira, não faz XHR:

```ts
loginWithGoogle() {
  window.location.assign(`${this.apiUrl}/auth/login-google`);
}
```

5. Criar `frontend/src/app/auth/google-callback.component.ts` e declarar a rota `auth/callback` em `frontend/src/app/app.routes.ts`. O componente lê `code` de `ActivatedRoute`, faz `POST /api/auth/exchange-google` uma única vez, entrega a resposta ao `AuthSessionService` e navega para `/`. Remover `code` da URL com navegação/replace assim que lido e mostrar erro seguro para código expirado. Não guardar `id_token` do Google nem esperar que um token Google seja JWT da aplicação.
6. No interceptor, tratar resposta `401` globalmente: limpar sessão/draft de forma consistente, evitar loop em endpoints de auth e redirecionar/mostrar a tela de login com “Sua sessão expirou”. `403` deve informar que o recurso não pertence à conta sem fazer logout. Os handlers locais de tela continuam tratando `400` de validação.
7. Ajustar as chamadas do componente: usar `GET /resumes/mine` (ou o endpoint mantido com o próprio ID), não construir IDs de outro usuário; no create de currículo, não enviar `candidateProfileId` como autoridade — o servidor o decide. Exportações via `HttpClient` receberão o header pelo interceptor.
8. Como `accessToken` em `localStorage` é vulnerável a XSS, implementar CSP, sanitização e evitar scripts de terceiros. Em evolução posterior, preferir access token apenas em memória + refresh token em cookie `HttpOnly; Secure; SameSite` (se o produto aceitar esse desenho). Não prometer proteção contra XSS apenas por usar JWT.

## 7. Segurança e hardening

- Gerar chave JWT criptograficamente aleatória com pelo menos 256 bits; nunca usar senha humana, chave em `appsettings.json` versionado ou o salt legado. Rotacionar a chave com estratégia de `kid`/chaves anteriores durante a transição.
- Validar assinatura, issuer, audience, expiração e `not before`; usar HTTPS em produção e HSTS. Não desabilitar validação de certificado para “fazer Google funcionar”.
- Access token: 10–20 minutos (15 no exemplo). Refresh token é opcional nesta primeira entrega; sem ele, o frontend pede login novamente ao expirar. Se for incluído depois, armazenar somente hash do refresh token no banco, rotação a cada uso, revogação/logout e cookie HttpOnly; não implementar refresh token superficialmente.
- Configurar senha com comprimento/complexidade, lockout, rate limiting de `/login`, mensagens neutras e logs auditáveis sem tokens/senhas. Avaliar confirmação de e-mail e recuperação de senha antes de abrir ao público.
- Limitar CORS às origens exatas de desenvolvimento/produção; não usar `AllowAnyOrigin` junto de credenciais. Para JWT no header, cookies de credencial não são necessários na API normal.
- O endpoint de callback Google deve validar correlação/state padrão do handler e o código de troca deve ser aleatório, de uso único, com TTL curto e armazenamento atômico. Nunca redirecionar JWT como query parameter; ele vaza por histórico, logs e `Referer`.
- Tratar `SecurityStamp` em revogação de senha/conta. JWTs já emitidos não são revogados automaticamente: manter expiração curta e, se necessário, introduzir `jti`/deny-list ou versão de sessão do usuário.
- Revisar autorização em toda nova rota que exponha perfil, currículo, download ou exportação. `[Authorize]` sem filtro por dono não satisfaz a proteção exigida.

## 8. Testes sugeridos

### Backend — `backend/CareerOS.Api.Tests`

Adicionar `Microsoft.AspNetCore.Mvc.Testing` e um fixture PostgreSQL efêmero/isolado (Testcontainers ou banco de teste); `InMemory` não valida peculiaridades de índices, FKs e PostgreSQL. Cobrir:

- `JwtTokenServiceTests`: claims `sub`, e-mail e `candidate_profile_id`; issuer/audience; expiração; falha com chave/configuração inválida.
- `AuthIntegrationTests`: cadastro cria perfil+usuário Identity e retorna JWT; login correto; senha errada; e-mail duplicado normalizado; lockout; token expirado; usuário legado que migra uma vez; novo hash não é SHA-256 e `LegacyPasswordHash` é limpo.
- Testes de migration: dados `users` pré-existentes são preservados, índices normalizados detectam colisões e login externo é persistido em `user_logins`.
- `GoogleLoginFlowTests`: mockar handler/info externo, testar criação, associação de login, conta já existente, e-mail não verificado, código de troca único/expirado e nunca retornar token Google.
- `AuthorizationIntegrationTests`: sem header recebe `401`; usuário A acessa seus perfil/currículos/exportações; A não lê, altera, apaga, exporta nem cria currículo para o perfil B; listagens retornam somente A. Testar `candidateProfileId` adulterado no body.
- Remover/reescrever `PasswordHasherTests.cs` quando o compatibilizador legado for removido; os testes não devem perpetuar SHA-256.

### Frontend — `frontend/src/app`

Com `provideHttpClientTesting`, cobrir:

- interceptor acrescenta `Bearer` somente a chamadas da API com token válido e nunca a origem externa/Google;
- sessão é persistida, restaurada se válida e apagada se expirada/401;
- login/registro consomem `accessToken` e carregam apenas o perfil da sessão;
- botão Google altera `window.location` para o endpoint correto;
- callback troca código, persiste sessão, remove código da URL e lida com expiração/erro;
- `401` limpa estado e mostra login; `403` não apaga uma sessão válida.

## 9. Roadmap e ordem de implementação

1. **Preparação:** auditar banco e backups; decidir janela de migração legada; criar credenciais OAuth Google e URLs de desenvolvimento/produção; definir valores de issuer/audience e política de senha.
2. **Modelo e migration:** introduzir `ApplicationUser`/`IdentityUserContext`, mapeamentos e migration revisada em cópia de dados. Validar rollback e FK de perfil antes de produção.
3. **Infraestrutura de autenticação:** adicionar pacotes, opções seguras, Identity, JWT bearer, `UseAuthentication`, `JwtTokenService`, Swagger Bearer e testes de token.
4. **Login local:** refatorar register/login para `UserManager`/`SignInManager`, emitir `AuthResponse` com JWT e executar a estratégia de conversão SHA-256.
5. **Autorização:** adicionar `[Authorize]`, helper de claim e filtros de dono em todos os endpoints de perfil/currículo/exportação; retirar as rotas/contratos que permitem criar/reassociar recursos de terceiros.
6. **Google:** implementar challenge/callback, criação/link seguro de conta e troca de código de uso único; testar com credenciais locais HTTPS e ambiente de homologação.
7. **Angular:** criar serviço de sessão/interceptor/callback/rota/botão; adaptar contratos e tratamento 401/403; validar navegação completa nos dois provedores.
8. **Qualidade e lançamento:** executar testes de integração ponta a ponta, atualizar `docs/API.md` e `docs/ARQUITETURA.md`, observar falhas de login/migração, remover definitivamente o fallback legado ao fim da janela e considerar refresh token/recuperação de senha como próximo incremento.

Os riscos que exigem maior cuidado são: aplicar migration sem auditar duplicidade de e-mail, deixar `CandidateProfileId` controlável pelo cliente, expor JWT no callback Google, manter o SHA-256 indefinidamente, e alterar `users` sem testar os dados existentes. Cada um deve ser critério explícito de aceite antes do deploy.
