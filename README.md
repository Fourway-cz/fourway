# Fourway
Fourway je moderní open-source aplikace pro vyhledávání spojů veřejné dopravy. Projekt vzniká v rámci projektových dnů na [Kyberně](https://kyberna.cz/) jako projekt 40 ve školním roce 2026/27.

## Struktura repozitáře
Takhle nějak bude vypadat struktura repozitáře
```
fourway/
├── frontend/
├── backend/
├── compose.yaml
├── compose.dev.yaml
```

## Spuštění Dev prostředí
```
docker compose -f compose.dev.yaml up
cd client
pnpm expo start
```

## Databáze
Backend používá PostgreSQL přes EF Core. Migrace se aplikují automaticky při startu API.

### Dev prostředí
Connection string je nastavený v `compose.dev.yaml` jako proměnná prostředí
`ConnectionStrings__DefaultConnection`. Při běhu mimo Docker je potřeba stejnou
proměnnou nastavit ručně, například:

```
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=fourway_db;Username=postgres;Password=SuperSecurePassword' dotnet run --project backend
```

### Migrace
Migrace se generují do `backend/Infrastructure/Database/Migrations`:

```
cd backend
dotnet tool restore
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=fourway_db;Username=postgres;Password=SuperSecurePassword' \
  dotnet ef migrations add NazevMigrace --output-dir Infrastructure/Database/Migrations
```

### Produkce
V produkci se connection string předává pouze přes proměnnou prostředí
`ConnectionStrings__DefaultConnection`, ideálně z tajného úložiště deploye.
V `appsettings.json` nejsou žádné databázové credentials.

## Licence
Ještě uvidíme