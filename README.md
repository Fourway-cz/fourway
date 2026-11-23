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
Backend používá PostgreSQL s rozšířením PostGIS přes EF Core. Migrace se aplikují
automaticky při startu API.

Databáze vyžaduje:

- rozšíření `postgis` – geografické body zastávek (`geography(Point, 4326)`) a GiST index,
- rozšíření `unaccent` a pomocnou funkci `f_unaccent(text)` – normalizace názvů
  zastávek pro deduplikaci. Funkci vytváří migrace, protože `unaccent` není
  `IMMUTABLE` a nelze ho použít přímo v generovaných sloupcích.

V dev prostředí tyto požadavky splňuje image `postgis/postgis:18-3.7-alpine`.
Model PostGIS používá `NetTopologySuite`, souřadnice se ukládají jako
`Point(longitude, latitude)` v SRID 4326.

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