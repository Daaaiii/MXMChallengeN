# MXMChallenge Backend

Backend ASP.NET Core para autenticacao, usuarios e modulo financeiro snapshot + sync.

## Requisitos

- .NET SDK `10.0.301`
- SQL Server ou Docker

## Comandos

```bash
dotnet restore
dotnet build
dotnet test
dotnet ef database update
dotnet run
```

Swagger fica disponivel no perfil local da API em `/swagger`.

## Docker

```bash
docker compose up --build
```

A API sobe em `http://localhost:8080` e o SQL Server em `localhost:1433`.

## Variaveis de ambiente

- `ConnectionStrings__DefaultConnection`
- `jwt__issuer`
- `jwt__audience`
- `jwt__secretKey`
- `Cors__AllowedOrigins__0`

## Endpoints financeiros

Todos os endpoints financeiros exigem `Authorization: Bearer <token>`.

- `GET /api/finance/state`
- `PUT /api/finance/state`
- `POST /api/finance/sync`
- `GET /api/health`
