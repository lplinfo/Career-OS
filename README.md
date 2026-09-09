# CareerOS

CareerOS é uma aplicação para organizar o histórico profissional de candidatos e gerar currículos multilíngues (português, inglês e italiano), em formatos adequados para processos seletivos e plataformas compatíveis com ATS.

## Funcionalidades

- Cadastro e login de usuários com JWT.
- Login social opcional com Google OAuth.
- Perfil profissional com histórico de experiências, formações e certificações.
- Geração de currículos **multilíngues** (pt / en / it) e direcionados a um país (`TargetCountry`).
- Exportação de currículo em três formatos: **PDF**, **DOCX** e **texto ATS**.
- Rascunho automático do perfil no `localStorage` do navegador.
- Gestão centralizada de segredos com **OpenBao** (Vault-compatible) e suporte a AppRole authentication.
- Orquestração local com **.NET Aspire** (Dashboard, OpenTelemetry tracing/metrics e geração de manifest cloud).

## Tecnologias

| Camada     | Tecnologia                          |
|------------|-------------------------------------|
| Orquestração| .NET Aspire 9                      |
| API        | ASP.NET Core 9 / C#                 |
| Frontend   | Angular 21 (standalone)             |
| Dados      | PostgreSQL + Entity Framework Core  |
| Segredos   | OpenBao (Vault-compatible AppRole)  |
| Autenticação | JWT + Google OAuth opcional       |
| Testes API | xUnit (backend)                     |
| Docs       | OpenAPI / Swagger                   |

## Estrutura

```
Career-OS/
├── CareerOS.AppHost/          # .NET Aspire App Host (orquestrador da aplicação)
├── CareerOS.ServiceDefaults/  # Padrões de serviço (OpenTelemetry, Health Checks, Resiliência)
├── backend/
│   ├── CareerOS.Api/          # API REST ASP.NET Core (Controllers, Domain, Migrations)
│   └── CareerOS.Api.Tests/    # Testes unitários xUnit
├── scripts/
│   └── openbao-bootstrap.sh   # Script de bootstrap e seeding do OpenBao
├── frontend/                  # Aplicação Angular (formulário por etapas)
├── deploy/                    # Manifest de publicação gerado pelo Aspire
├── docker-compose.yml         # Containers OpenBao e PostgreSQL (fallback local)
├── CareerOS.sln               # Solução .NET
└── docs/                      # Documentação (arquitetura e API)
```

Mais detalhes: [docs/ARQUITETURA.md](docs/ARQUITETURA.md) e [docs/API.md](docs/API.md).

## Pré-requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) 20 ou superior, com `npm`
- [Docker](https://www.docker.com/) ou Podman para o runtime de containers do .NET Aspire
- Tool global `dotnet-ef` instalada:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

- Angular CLI instalado globalmente é opcional; o projeto também funciona com `npx ng` e `npm`

## Desenvolvimento com .NET Aspire

O CareerOS utiliza o **.NET Aspire** para orquestrar a stack completa de desenvolvimento local (PostgreSQL, OpenBao, API e Frontend Angular) com telemetria integrada, métricas, logs centralizados e dashboard unificado.

### 1. Iniciar a Aplicação via Aspire AppHost

Para subir toda a aplicação e suas dependências de infraestrutura com um único comando:

```bash
dotnet run --project CareerOS.AppHost/CareerOS.AppHost.csproj
```

O .NET Aspire iniciará:
- **Dashboard Aspire**: Acessível na URL indicada no terminal (geralmente `https://localhost:18888` ou similar) com gráficos de OpenTelemetry, traces e logs.
- **PostgreSQL**: Container PostgreSQL 16 Alpine persistente.
- **OpenBao**: Container OpenBao Vault-compatible escutando na porta `8200`.
- **CareerOS.Api**: API REST escutando em `https://localhost:7276` e `http://localhost:5062`.
- **CareerOS.Frontend**: Aplicação Angular escutando em `http://localhost:4200`.

> **Nota sobre Runtime de Containers**: O Aspire requer Docker Desktop, Docker Engine ou Podman rootless (ex: `DOCKER_HOST=unix:///run/user/1000/podman/podman.sock`). Se preferir rodar sem o AppHost do Aspire, o arquivo `docker-compose.yml` continua disponível como fallback.

### 2. Gerar Manifest de Publicação (Cloud / Azure Container Apps - ACA)

O Aspire permite exportar um manifest de publicação JSON em formato padrão (`aspire-8.0.json`) para provisionamento em plataformas de nuvem, como Azure Container Apps (ACA) ou instâncias Kubernetes via `azd` ou ferramentas IAC:

```bash
dotnet run --project CareerOS.AppHost/CareerOS.AppHost.csproj -- --publisher manifest --output-path deploy/manifest.json
```

O arquivo gerado em `deploy/manifest.json` descreve os recursos da aplicação, imagens, parâmetros de banco e segredos. Para migração inicial em nuvem (ACA), os containers de banco (Postgres) e cofre (OpenBao) são definidos como recursos de container, podendo posteriormente ser substituídos por serviços gerenciados (Azure Database for PostgreSQL / Azure Key Vault).

## Gestão de Segredos com OpenBao

O CareerOS utiliza o **OpenBao** (fork comunitário do HashiCorp Vault com API compatível) para gerenciar segredos em runtime (connection string do PostgreSQL, chave secreta JWT e credenciais do Google OAuth).

### 1. Subir a Infraestrutura (Docker Compose - Fallback)

Caso prefira subir apenas a infraestrutura sem o Aspire AppHost:

```bash
docker-compose up -d
```

> **Dev vs. Prod**: No ambiente de desenvolvimento local (`docker-compose.yml`), o OpenBao roda em modo dev (`-dev`), no qual o cofre inicia desmembrado (*unsealed*) e pronto para uso com token root de desenvolvimento (`root`). Em ambiente de produção, o OpenBao deve rodar em cluster selado (*sealed*) com backend de armazenamento persistente criptografado (e.g. Raft) e processo de unseal via Shamir secret sharing ou Auto-Unseal (KMS), prevenindo acesso desautorizado aos dados persistidos.

### 2. Executar o Bootstrap de Segredos

O script de bootstrap habilita a engine KV v2 em `secret/`, grava os segredos dos caminhos `careeros/database`, `careeros/jwt` e `careeros/auth-google`, cria a política `careeros-read`, habilita a autenticação AppRole e configura a role `careeros`:

```bash
./scripts/openbao-bootstrap.sh
```

O script é idempotente (pode ser executado múltiplas vezes) e imprimirá as credenciais AppRole (`ROLE_ID` e `SECRET_ID`) ao finalizar.

### 3. Executar a Aplicação com OpenBao Habilitado

Defina as variáveis de ambiente com as credenciais obtidas no bootstrap:

```bash
export OpenBao__Enabled=true
export BAO_ADDR=http://localhost:8200
export BAO_ROLE_ID=<ROLE_ID_GERADO>
export BAO_SECRET_ID=<SECRET_ID_GERADO>

dotnet run --project backend/CareerOS.Api/CareerOS.Api.csproj --launch-profile https
```

### 4. Executar sem OpenBao (Modo Fallback / CI)

Caso o OpenBao esteja desativado (`OpenBao__Enabled=false` ou não definido) ou indisponível durante a inicialização, o custom `ConfigurationProvider` no .NET captura o evento e mantém as configurações padrão locais. Isso garante que o pipeline de CI/CD e testes unitários passem de forma resiliente sem depender de instâncias externas de cofre.

## Executar localmente (Modo Tradicional)

### Backend

1. Compile a solução para validar o backend antes de subir a API:

   ```bash
   dotnet build CareerOS.sln
   ```

2. Suba o banco de dados PostgreSQL e o OpenBao via Docker Compose:

   ```bash
   docker-compose up -d
   ```

3. Execute o bootstrap do OpenBao se desejar usar segredos centralizados:

   ```bash
   ./scripts/openbao-bootstrap.sh
   ```

4. Aplique as migrations do Entity Framework:

   ```bash
   dotnet ef database update --project backend/CareerOS.Api/CareerOS.Api.csproj --startup-project backend/CareerOS.Api/CareerOS.Api.csproj
   ```

5. Inicie a API com o profile HTTPS:

   ```bash
   dotnet run --project backend/CareerOS.Api/CareerOS.Api.csproj --launch-profile https
   ```

   - API: `https://localhost:7276`
   - HTTP local: `http://localhost:5062`
   - Swagger: `https://localhost:7276/swagger/index.html`
   - Base consumida pelo frontend: `https://localhost:7276/api`

### Frontend

Em um segundo terminal, dentro de `frontend/`:

```bash
npm install
npm run build
npm start
```

O frontend será disponibilizado em `http://localhost:4200`.

## Executar os testes

### Backend (xUnit)

```bash
dotnet test CareerOS.sln
```

Cobre, entre outros:
- `PasswordHasher` (hash determinístico e verificação)
- `ExportService` (texto ATS localizado para pt/en/it, fallback de coleções com JSON customizado, e formatos DOCX/PDF)
- `OpenBaoConfigurationProvider` (resiliência de fallback e carregamento de segredos em runtime)

### Frontend (Karma + Jasmine)

```bash
cd frontend
npm test
```

> Requer um navegador (padrão: Chrome headless) conforme configurado no `karma.conf.js`.

## Solução de problemas

- OpenBao não responde: Verifique se o container está ativo com `docker-compose ps` e se a porta 8200 está acessível.
- Porta `7276` ocupada ou certificado HTTPS de desenvolvimento ausente: libere a porta e confie no certificado local com `dotnet dev-certs https --trust`.
- Banco não encontrado ou sem permissão: confirme que o container do PostgreSQL está rodando via `docker-compose up -d`.

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

A aplicação conta com API completa, exportação multilíngue (PDF/DOCX/ATS), frontend Angular por etapas, orquestração local com .NET Aspire, testes unitários abrangentes e gestão de segredos centralizada via OpenBao com fallback transparente para CI/CD.
