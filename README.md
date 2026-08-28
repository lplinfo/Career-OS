# CareerOS

CareerOS é uma aplicação para organizar o histórico profissional de candidatos e gerar currículos multilíngues (português, inglês e italiano), em formatos adequados para processos seletivos e plataformas compatíveis com ATS.

## Funcionalidades

- Cadastro e login de usuários (com hash de senha SHA-256 + salt).
- Perfil profissional com histórico de experiências, formações e certificações.
- Geração de currículos **multilíngues** (pt / en / it) e direcionados a um país (`TargetCountry`).
- Exportação de currículo em três formatos: **PDF**, **DOCX** e **texto ATS**.
- Rascunho automático do perfil no `localStorage` do navegador.

## Tecnologias

| Camada     | Tecnologia                          |
|------------|-------------------------------------|
| API        | ASP.NET Core 9 / C#                 |
| Frontend   | Angular 21 (standalone)             |
| Dados      | PostgreSQL + Entity Framework Core  |
| Testes API | xUnit (backend)                     |
| Docs       | OpenAPI / Swagger                   |

## Estrutura

```
Career-OS/
├── backend/
│   └── CareerOS.Api/          # API REST ASP.NET Core (Controllers, Domain, Migrations)
│   └── CareerOS.Api.Tests/    # Testes unitários xUnit
├── frontend/                  # Aplicação Angular (formulário por etapas)
├── CareerOS.sln               # Solução .NET (API + testes)
└── docs/                      # Documentação (arquitetura e API)
```

Mais detalhes: [docs/ARQUITETURA.md](docs/ARQUITETURA.md) e [docs/API.md](docs/API.md).

## Pré-requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (v20 ou superior) e npm
- [PostgreSQL](https://www.postgresql.org/) rodando localmente
- Angular CLI (opcional; o projeto usa `npx ng` / `npm`)

## Executar localmente

### 1. Backend (API)

1. Configure a string de conexão `CareerDatabase` em `backend/CareerOS.Api/appsettings.json`:

   ```jsonc
   "ConnectionStrings": {
     "CareerDatabase": "Host=localhost;Port=5432;Database=careeros;Username=postgres;Password=postgres"
   }
   ```

2. Crie o banco PostgreSQL `careeros` (se ainda não existir).

3. Aplique as migrations:

   ```bash
   cd backend/CareerOS.Api
   dotnet ef database update
   ```

4. Inicie a API:

   ```bash
   dotnet run --launch-profile https
   ```

   - API: `https://localhost:7276` / `http://localhost:5062`
   - Swagger: `https://localhost:7276/swagger/index.html`

### 2. Frontend (Angular)

Em um segundo terminal:

```bash
cd frontend
npm install
npm start
```

O frontend será disponibilizado em `http://localhost:4200`.

> O backend precisa estar rodando, pois o frontend consome a API em `http://localhost:5062/api` e o perfil em `http://localhost:4200` é permitido pelo CORS.

## Executar os testes

### Backend (xUnit)

```bash
dotnet test CareerOS.sln
```

Cobre, entre outros, o `PasswordHasher` (hash determinístico e verificação) e o `ExportService` (texto ATS localizado para pt/en/it, fallback de coleções com JSON customizado, e formatos DOCX/PDF).

### Frontend (Karma + Jasmine)

```bash
cd frontend
npm test
```

> Requer um navegador (padrão: Chrome headless) conforme configurado no `karma.conf.js`.

## Rotas da API (resumo)

| Método | Rota                                           | Descrição                          |
|--------|------------------------------------------------|------------------------------------|
| POST   | `/api/auth/register`                           | Cria conta e perfil inicial        |
| POST   | `/api/auth/login`                              | Autentica e retorna sessão         |
| GET    | `/api/candidate-profiles`                      | Lista perfis                       |
| GET    | `/api/candidate-profiles/{id}`                 | Obtém um perfil                    |
| POST   | `/api/candidate-profiles`                      | Cria um perfil                     |
| PUT    | `/api/candidate-profiles/{id}`                 | Atualiza um perfil                 |
| DELETE | `/api/candidate-profiles/{id}`                 | Exclui um perfil                   |
| GET    | `/api/resumes`                                 | Lista currículos                   |
| GET    | `/api/resumes/by-candidate/{candidateId}`      | Currículos de um candidato         |
| POST   | `/api/resumes`                                 | Cria um currículo                  |
| PUT    | `/api/resumes/{id}`                            | Atualiza um currículo              |
| DELETE | `/api/resumes/{id}`                            | Exclui um currículo                |
| GET    | `/api/resumes/{id}/export/pdf`                 | Exporta como PDF                   |
| GET    | `/api/resumes/{id}/export/docx`                | Exporta como DOCX                  |
| GET    | `/api/resumes/{id}/export/ats`                 | Exporta como texto ATS             |

## Estado atual

A aplicação conta com API completa (auth, perfil e currículos), exportação multilíngue (PDF/DOCX/ATS), frontend Angular por etapas e testes unitários no backend. A suíte de testes do frontend e a documentação estão em andamento.
