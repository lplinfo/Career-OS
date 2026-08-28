# Referência da API (CareerOS)

Base URL: `https://localhost:7276` (HTTP: `http://localhost:5062`).

Swagger interativo: `https://localhost:7276/swagger/index.html`

Todas as rotas usam o prefixo `/api`. As respostas são JSON, exceto as rotas de exportação.

---

## Autenticação

### `POST /api/auth/register`

Cria uma conta de usuário e um perfil de candidato inicial.

**Body** (`RegisterRequest`):

```json
{
  "email": "ana@example.com",
  "password": "segredo123",
  "fullName": "Ana Souza",
  "professionalTitle": "Desenvolvedora"
}
```

Validações: `email` (obrigatório, formato e-mail, máx. 320), `password` (mín. 6, máx. 100), `fullName` (obrigatório, máx. 200), `professionalTitle` (obrigatório, máx. 160).

**Respostas**

- `200 OK` — sucesso (mesmo `AuthResponse` do login, ver modelo no final):

```json
{
  "userId": "guid",
  "email": "ana@example.com",
  "candidateProfileId": "guid",
  "fullName": "Ana Souza",
  "accessToken": "eyJhbGciOiJIUzI1NiJ9...",
  "tokenType": "Bearer",
  "expiresAt": "2026-08-29T00:00:00+00:00"
}
```

- `400 Bad Request` — e-mail já cadastrado (`{ "message": "Este e-mail já está cadastrado." }`) ou `ModelState` inválido.

### `POST /api/auth/login`

Autentica e retorna a sessão.

**Body** (`LoginRequest`):

```json
{
  "email": "ana@example.com",
  "password": "segredo123"
}
```

**Respostas**

- `200 OK` — `AuthResponse` (mesmo formato do register, inclui `accessToken`/`expiresAt`).
- `401 Unauthorized` — `{ "message": "E-mail ou senha incorretos." }`.

Conta com *lockout* e, para contas legadas (hash SHA-256), faz *lazy migration* atômica para o hash do Identity ao validar a senha.

### `GET /api/auth/me`

Retorna a sessão do usuário autenticado.

**Requires**: `Authorization: Bearer <token>`.

**Respostas**

- `200 OK` — `AuthResponse` do usuário atual.
- `401 Unauthorized` — token ausente, inválido ou expirado.

### `POST /api/auth/login-google`

Inicia o fluxo de login social com Google. Redireciona o navegador para a URL de autorização do Google (0Auth) com `state` de proteção CSRF.

**Respostas**

- `302 Found` — `Location` apontando para o Google (requer `Authentication:Google:ClientId`/`ClientSecret` configurados e aplicação registrada no Google Cloud).

### `GET /api/auth/login-google-complete`

Callback do Google após o usuário consentir. Valida o `state` (uso único, expiração de 120s), troca o `code` por token e redireciona o navegador para `Authentication:FrontendBaseUrl/auth/callback?code=...`.

**Query**: `code` (obrigatório) · `state` (obrigatório).

**Respostas**

- `302 Found` — redirecionamento para o frontend com o `code` efêmero.
- `400 Bad Request` — `code` ausente ou `state` inválido/expirado.

### `POST /api/auth/exchange-google`

Troca o `code` efêmero (gerado pelo callback, uso único, 60s) por uma sessão final.

**Body** (`ExchangeGoogleRequest`):

```json
{ "code": "efemero_code" }
```

**Respostas**

- `200 OK` — `AuthResponse`.
- `400 Bad Request` — `code` inválido, expirado ou já utilizado.

---

## Candidate Profiles

> **Autorização**: desde a Onda 2, todas as rotas de perfis e currículos exigem
> `Authorization: Bearer <token>` (JWT obtido no `register`/`login`). O acesso é limitado ao
> **dono** do perfil (via `ICurrentUser`): sem token → `401 Unauthorized`; recurso de outro
> usuário → `404 Not Found`.

### `GET /api/candidate-profiles`

Lista todos os perfis, com `Experiences`, `Educations` e `Certifications` incluídas, ordenados por `FullName`.

**Resposta**: `200 OK` — array de `CandidateProfile`.

### `GET /api/candidate-profiles/{id}`

Obtém um perfil pelo `id` (GUID), com coleções incluídas.

**Respostas**: `200 OK` (perfil) · `404 Not Found`.

### `POST /api/candidate-profiles`

Cria um perfil. Valida a consistência de datas de experiências/formações (início anterior ao término, exceto se `isCurrent`).

**Body** (`CandidateProfileRequest`) — exemplo:

```json
{
  "fullName": "Ana Souza",
  "professionalTitle": "Desenvolvedora",
  "professionalSummary": "Resumo profissional.",
  "email": "ana@example.com",
  "openToRemoteWork": true,
  "experiences": [
    {
      "companyName": "ACME",
      "jobTitle": "Dev",
      "startDate": "2020-01-01",
      "endDate": "2022-12-31",
      "isCurrent": false,
      "order": 0
    }
  ],
  "educations": [],
  "certifications": []
}
```

**Respostas**: `201 Created` (com `Location` para o novo perfil) · `400 Bad Request` (`{ "message": "..." }` em inconsistência de datas).

### `PUT /api/candidate-profiles/{id}`

Atualiza o perfil, substituindo as coleções de experiências/formações/certificações pelas enviadas.

**Respostas**: `200 OK` · `400 Bad Request` · `404 Not Found`.

### `DELETE /api/candidate-profiles/{id}`

Exclui o perfil.

**Respostas**: `204 No Content` · `404 Not Found`.

---

## Resumes (currículos)

### `GET /api/resumes`

Lista todos os currículos, ordenados por `UpdatedAt` decrescente.

### `GET /api/resumes/{id}`

Obtém um currículo por `id`.

**Respostas**: `200 OK` · `404 Not Found`.

### `GET /api/resumes/by-candidate/{candidateProfileId}`

Lista os currículos de um candidato.

### `POST /api/resumes`

Cria um currículo associado a um perfil.

**Body** (`ResumeRequest`) — exemplo:

```json
{
  "candidateProfileId": "guid",
  "language": "en",
  "targetCountry": "US",
  "showPhone": true,
  "showEmail": true,
  "showLocation": true,
  "customizedTitle": "Software Engineer",
  "customizedSummary": "Career summary.",
  "skills": "C#, Angular, Azure",
  "customizedExperiencesJson": null,
  "customizedEducationsJson": null,
  "customizedCertificationsJson": null
}
```

Campos: `language` default `"pt"`; `targetCountry` default `"BR"`; os JSONs customizados, quando preenchidos, sobrescrevem as coleções do perfil na exportação.

**Resposta**: `201 Created` (com `Location`).

### `PUT /api/resumes/{id}`

Atualiza um currículo.

**Respostas**: `200 OK` · `404 Not Found`.

### `DELETE /api/resumes/{id}`

Exclui um currículo.

**Respostas**: `204 No Content` · `404 Not Found`.

---

## Exportação

Os três formatos carregam o perfil associado (com coleções) antes de gerar o artefato.

### `GET /api/resumes/{id}/export/pdf`

- **Resposta**: `200 OK` — `application/pdf`, arquivo `resume_{language}.pdf`.
- `404 Not Found` se currículo ou perfil não existirem.

### `GET /api/resumes/{id}/export/docx`

- **Resposta**: `200 OK` — `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, arquivo `resume_{language}.docx`.
- `404 Not Found` se currículo ou perfil não existirem.

### `GET /api/resumes/{id}/export/ats`

- **Resposta**: `200 OK` — `text/plain; charset=utf-8` com o texto do currículo no formato ATS.
- `404 Not Found` se currículo ou perfil não existirem.

> Idiomas suportados: `pt`, `en`, `it`. Qualquer outro valor é tratado como `pt`.

---

## Modelos (resumo)

**AuthResponse**

```json
{
  "userId": "guid",
  "email": "string",
  "candidateProfileId": "guid",
  "fullName": "string",
  "accessToken": "JWT (Bearer)",
  "tokenType": "Bearer",
  "expiresAt": "ISO-8601"
}
```

**Resume**

```json
{
  "id": "guid",
  "candidateProfileId": "guid",
  "language": "pt",
  "targetCountry": "BR",
  "showPhone": true,
  "showEmail": true,
  "showLocation": true,
  "customizedTitle": "string",
  "customizedSummary": "string",
  "skills": "string",
  "customizedExperiencesJson": "string|null",
  "customizedEducationsJson": "string|null",
  "customizedCertificationsJson": "string|null",
  "updatedAt": "ISO-8601"
}
```
