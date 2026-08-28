# Arquitetura do CareerOS

Este documento descreve a organização, o fluxo de dados e as decisões de design da aplicação CareerOS.

## Visão geral

CareerOS é uma aplicação **front-end + back-end**:

- **Backend**: API REST em ASP.NET Core 9 (`backend/CareerOS.Api`), usando Entity Framework Core com PostgreSQL.
- **Frontend**: aplicação Angular 21 standalone (`frontend/`), um componente monolítico `App` que concentra toda a lógica de UI, formulários e chamadas HTTP.

O frontend conversa com a API via `HttpClient` em `http://localhost:5062/api`, habilitado pelo CORS configurado em `Program.cs` para `http://localhost:4200`.

## Estrutura do backend

```
backend/CareerOS.Api/
├── Controllers/          # AuthController, CandidateProfilesController, ResumesController
├── Contracts/            # DTOs de entrada (CandidateProfileRequest, ResumeRequest)
├── Data/
│   └── CareerDbContext.cs# DbContext do EF Core
├── Domain/               # Entidades (CandidateProfile, Resume, WorkExperience, ...)
├── Migrations/           # Migrations do EF Core
├── Services/
│   └── ExportService.cs  # Geração de PDF, DOCX e texto ATS
└── Utils/
    └── PasswordHasher.cs # Hash/verificação de senha (SHA-256 + salt)
```

### Camadas

- **Controller**: recebe e valida as requisições, orquestra o acesso ao banco via `CareerDbContext` e retorna `ActionResult`.
- **Domain**: entidades puras (sem lógica de persistência).
- **Contracts**: DTOs de entrada desacoplados das entidades, permitindo controlar o que a API aceita.
- **Services / Utils**: lógica reutilizável (exportação de currículos; hash de senha).

## Modelo de dados

Entidades principais (em `Domain/`):

- **User**: conta de usuário (`Email`, `PasswordHash`, `CandidateProfileId`).
- **CandidateProfile**: dados profissionais (`FullName`, `ProfessionalTitle`, `ProfessionalSummary`, contato, localização, `OpenToRemoteWork`, `OpenToRelocation`).
- **WorkExperience**, **Education**, **Certification**: coleções do perfil (com `Order` para ordenação).
- **Resume**: um currículo, sempre associado a um `CandidateProfileId`, com `Language` (pt/en/it), `TargetCountry`, campos de exibição (`ShowPhone`, `ShowEmail`, `ShowLocation`) e JSONs customizados de experiências/formacões/certificações.

### Relacionamentos

```
User ──┐
       ├──< CandidateProfile ──< WorkExperience
       │        └──< Education
       │        └──< Certification
       │        └──< Resume (Language, TargetCountry, export flags)
```

## Fluxo de dados (exportação de currículo)

1. O frontend salva o **perfil** (`POST/PUT /api/candidate-profiles`) e cria um **resume** (`POST /api/resumes`).
2. Ao exportar (`GET /api/resumes/{id}/export/{pdf|docx|ats}`), o `ResumesController` carrega o `CandidateProfile` **com** `Experiences`, `Educations` e `Certifications`.
3. O `ExportService` resolve as coleções:
   - Se o resume possui `Customized*Json`, **deserializa** essas coleções;
   - Caso contrário (ou em caso de JSON malformado), **cai para as coleções do perfil**.
4. Gera o artefato conforme o formato e retorna como `File`/`Content` no HTTP response.

### Localização

O `ExportService` possui um `GetLocalization` que retorna cabeçalhos de seção conforme o idioma do resume: `en` (Professional Summary / Core Skills / ...), `it` (Riepilogo Professionale / ...) e o **padrão `pt`** (Resumo Profissional / ...).

## Autenticação

- O `AuthController` `register` normaliza o e-mail, cria um `CandidateProfile` inicial e um `User` com `PasswordHasher.HashPassword`.
- O `login` valida credenciais via `PasswordHasher.VerifyPassword` e retorna um `AuthResponse` com `UserId`, `CandidateProfileId` e `FullName`.
- O frontend guarda a sessão em `localStorage` (`careeros_user_session`) — **não há uso de JWT/tokens** no momento; o estado de autenticação é controlado apenas pela sessão armazenada no navegador.

## Frontend

- O componente `App` (standalone) em `frontend/src/app/app.ts` concentra: forms de login/cadastro, o formulário de perfil por etapas (stepper), CRUD de currículos e exportação.
- Usa `ReactiveFormsModule`/`FormsModule`, `FormBuilder`, `FormArray` (experiências, formações, certificações) e validators customizados:
  - `dateRangeValidator` (data de término anterior à de início → inválido, ignorado se `IsCurrent`);
  - `passwordMatchValidator` (confirmação de senha).
- Persistência de rascunho do perfil em `localStorage` (`careeros_profile_draft`) com salvamento automático no `valueChanges`.
- Consome a API diretamente via `HttpClient` (sem serviços intermediários).

## Testes

### Backend (`backend/CareerOS.Api.Tests`)

Projeto xUnit que referencia a API e testa:

- **PasswordHasherTests**: determinismo do hash, diferenciação entre senhas, `VerifyPassword` para senha correta/incorreta.
- **ExportServiceTests**: texto ATS localizado (en/it/pt), os dois ramos de resolução de coleções (JSON customizado e fallback para o perfil, incluindo JSON malformado), e magic bytes de DOCX (`PK`) e PDF (`%PDF`).

### Frontend

Suíte Karma + Jasmine (em implementação) cobrindo validators e a lógica testável do componente `App` com mocks de `HttpClient` (`provideHttpClientTesting`) e `localStorage` fake.
