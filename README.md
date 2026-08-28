# CareerOS

CareerOS é uma aplicação para organizar o histórico profissional de candidatos e gerar currículos multilíngues (português, inglês e italiano), em formatos adequados para processos seletivos e plataformas compatíveis com ATS.

## Funcionalidades

- Cadastro e login de usuários com JWT.
- Login social opcional com Google OAuth.
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
| Autenticação | JWT + Google OAuth opcional       |
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
- [Node.js](https://nodejs.org/) 20 ou superior, com `npm`
- [PostgreSQL](https://www.postgresql.org/) rodando localmente
- Tool global `dotnet-ef` instalada:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

- Angular CLI instalado globalmente é opcional; o projeto também funciona com `npx ng` e `npm`

## Executar localmente

### Backend

1. Compile a solução para validar o backend antes de subir a API:

   ```bash
   dotnet build CareerOS.sln
   ```

2. Crie o banco PostgreSQL `careeros` e garanta que o usuário informado na string de conexão tenha permissão de leitura e escrita:

   ```sql
   CREATE DATABASE careeros;
   ```

3. Configure `ConnectionStrings:CareerDatabase`.

   O valor padrão versionado em `backend/CareerOS.Api/appsettings.json` é:

   ```jsonc
   "ConnectionStrings": {
     "CareerDatabase": "Host=localhost;Port=5432;Database=careeros;Username=career_user;Password=careeruser"
   }
   ```

   Ajuste esse valor no ambiente local, se necessário, para apontar para o seu PostgreSQL.

4. Crie `backend/CareerOS.Api/appsettings.Development.json` com o bloco `JwtOptions`. Esse arquivo não deve ser versionado e é obrigatório para o backend subir:

   ```jsonc
   {
     "JwtOptions": {
       "SecretKey": "substitua-por-uma-chave-com-pelo-menos-32-bytes",
       "Issuer": "CareerOS.Api",
       "Audience": "CareerOS.Frontend"
     }
   }
   ```

   A `SecretKey` precisa ter no mínimo 32 bytes. Sem isso, `Program.cs` lança exceção e a aplicação não inicia.

5. Aplique as migrations do Entity Framework:

   ```bash
   dotnet ef database update --project backend/CareerOS.Api/CareerOS.Api.csproj --startup-project backend/CareerOS.Api/CareerOS.Api.csproj
   ```

   Esse comando usa o projeto da API como contexto e aplica o schema no banco `careeros`.

6. Inicie a API com o profile HTTPS:

   ```bash
   dotnet run --project backend/CareerOS.Api/CareerOS.Api.csproj --launch-profile https
   ```

   - API: `https://localhost:7276`
   - HTTP local: `http://localhost:5062`
   - Swagger: `https://localhost:7276/swagger/index.html`
   - Base consumida pelo frontend: `https://localhost:7276/api`

7. Login Google é opcional. Para habilitar o fluxo social localmente, preencha também os valores não versionados usados pelo backend:

   - `Authentication:Google:ClientId`
   - `Authentication:Google:ClientSecret`
   - `Authentication:FrontendBaseUrl` (padrão: `http://localhost:4200`)

   Sem credenciais reais do Google Cloud, o login social não funciona. O restante da aplicação continua operando normalmente com login por e-mail e senha.

### Frontend

Em um segundo terminal, dentro de `frontend/`:

```bash
npm install
npm run build
npm start
```

O frontend será disponibilizado em `http://localhost:4200`.

O frontend precisa da API no ar, porque consome `https://localhost:7276/api` e o CORS do backend libera apenas `http://localhost:4200`.

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

## Solução de problemas

- Erro de JWT ausente ou curto: crie ou corrija `backend/CareerOS.Api/appsettings.Development.json` com `JwtOptions:SecretKey` de pelo menos 32 bytes. Esse erro vem do `Program.cs` e impede a inicialização.
- Porta `7276` ocupada ou certificado HTTPS de desenvolvimento ausente: libere a porta, garanta que o profile `https` esteja disponível e, se necessário, confie no certificado local com `dotnet dev-certs https --trust`.
- Banco não encontrado ou sem permissão: confirme que o banco `careeros` existe, que o usuário do `ConnectionStrings:CareerDatabase` tem permissão no PostgreSQL e que o servidor está rodando.
- `npm test` falhando no Chrome headless: confirme que as dependências foram instaladas com `npm install` e que o ambiente possui o Chrome/Chromium exigido pelo Karma.

## Rotas da API (resumo)

| Método | Rota                                           | Descrição                          |
|--------|------------------------------------------------|------------------------------------|
| POST   | `/api/auth/register`                           | Cria conta e perfil inicial        |
| POST   | `/api/auth/login`                              | Autentica e retorna sessão         |
| GET    | `/api/auth/login-google`                       | Inicia o fluxo OAuth com Google    |
| GET    | `/api/auth/login-google-complete`              | Conclui o callback do Google       |
| POST   | `/api/auth/exchange-google`                    | Troca o código social por JWT      |
| GET    | `/api/auth/me`                                 | Retorna a sessão do usuário atual  |
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

A aplicação conta com API completa (auth, perfil e currículos), exportação multilíngue (PDF/DOCX/ATS), frontend Angular por etapas e testes unitários no backend e no frontend.
