# CareerOS

CareerOS é uma aplicação para organizar o histórico profissional de candidatos e gerar currículos multilíngues, em formatos adequados para processos seletivos e plataformas compatíveis com ATS.

## Tecnologias

- API: ASP.NET Core 9 / C#
- Frontend: Angular
- Dados: PostgreSQL e Entity Framework Core

## Estrutura

- `backend/CareerOS.Api`: API REST, migrations e integração com PostgreSQL.
- `frontend`: interface Angular para o preenchimento do perfil profissional.

## Executar localmente

### API

1. Configure a string de conexão `CareerDatabase` em `backend/CareerOS.Api/appsettings.json`.
2. Crie o banco PostgreSQL `careeros`.
3. Aplique as migrations:

   ```bash
   cd backend/CareerOS.Api
   dotnet ef database update
   ```

4. Inicie a API:

   ```bash
   dotnet run --launch-profile https
   ```

O Swagger estará disponível em `https://localhost:7276/swagger/index.html`.

### Frontend

```bash
cd frontend
npm start
```

O frontend será disponibilizado em `http://localhost:4200`.

## Estado atual

A base da solução, a API do perfil do candidato e sua migration inicial já foram criadas. O formulário Angular por etapas e a exportação de currículos serão implementados nas próximas fases.
