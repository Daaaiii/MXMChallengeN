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

## Smoke test financeiro

Com a API rodando em `http://localhost:8080`, valide cadastro, login, snapshot e sync:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-finance-e2e.ps1
```

Valide tambem um merge com conflitos:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-finance-conflict.ps1
```

Por padrao, o usuario temporario criado pelo script e removido ao final. Use `-KeepUser` para preservar os dados.

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
