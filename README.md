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

## Licence
Ještě uvidíme