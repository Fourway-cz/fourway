# Fourway.Import – JDF Synchronizační CLI Nástroj

CLI nástroj určený k periodické synchronizaci celostátního informačního systému o jízdních řádech (CIS JŘ) s PostgreSQL databází. Aplikace je navržena pro spouštění formou systémového Cron Jobu (např. v Dokploy), kde se spustí, stáhne nejnovější data, provede inkrementální změny a ukončí se.

---

## 1. Jak to funguje (Architektura a Pipeline)

Celý proces probíhá v následujících krocích:

```
[Cron Job] -> [1. Stažení JDF.zip]
                  |
             [2. Otevření vnějšího archivu (~13 000 ZIPů)]
                  |
             [3. Načtení známých FileHash z DB do paměti (HashSet)]
                  |
             [4. Iterace přes vnitřní ZIPy]
             /                          \
   (Hash v DB existuje)        (Hash v DB není - nový/změněný)
            |                                    |
   [Přeskočeno (0 ms)]          [Rozbalení do paměti (CP1250)]
                                                 |
                                [Parsování CSV: Linky, Zastávky, Spoje...]
                                                 |
                                [Deduplikace zastávek, dopravců a kalendářů]
                                                 |
                                [NpgsqlBinaryImporter: PostgreSQL COPY]
                                                 |
                                [Uložení hashe do import_batches]
```

---

## 2. Klíčové technické principy

### A. Rychlost a výkon: Proč Npgsql Binary `COPY` místo EF Core
JDF data pro celou ČR obsahují ~13 000 linek, stovky tisíc spojů a **3 až 8 milionů záznamů v `stop_times`**.
- **Problém EF Core:** EF Core Change Tracker by musel sledovat miliony objektů v paměti. To způsobuje vysokou spotřebu RAM (jednotky GB), zahlcení Garbage Collectoru a pomalé vkládání (parsování tisíců SQL `INSERT` příkazů trvá hodiny).
- **Řešení s `NpgsqlBinaryImporter`:** Využíváme nativní protokol PostgreSQL `COPY ... FROM STDIN (FORMAT BINARY)`. Data se streamují přímo ze socketu do tabulek rychlostí **150 000 – 500 000 řádků/s** bez nutnosti parsovat SQL text a bez alokací paměti.

Úryvek kódu pro streaming časů zastavení (`PostgresBulkImporter.cs`):
```csharp
await using var stWriter = await conn.BeginBinaryImportAsync(
    """
    COPY stop_times (
        trip_id, stop_sequence, route_stop_id, stop_id, platform_code,
        arrival_time, departure_time, arrival_time_max, departure_time_max,
        stop_kind, request_stop, board_front, tariff_km, actual_km
    ) FROM STDIN (FORMAT BINARY);
    """, ct);

foreach (var st in stopTimesToInsert)
{
    await stWriter.StartRowAsync(ct);
    await stWriter.WriteAsync(st.TripId, NpgsqlDbType.Uuid, ct);
    await stWriter.WriteAsync(st.StopSequence, NpgsqlDbType.Integer, ct);
    await stWriter.WriteAsync(st.RouteStopId, NpgsqlDbType.Uuid, ct);
    await stWriter.WriteAsync(st.StopId, NpgsqlDbType.Uuid, ct);
    await stWriter.WriteAsync(st.PlatformCode, NpgsqlDbType.Varchar, ct);
    await stWriter.WriteAsync(st.ArrivalTime, NpgsqlDbType.Interval, ct);
    await stWriter.WriteAsync(st.DepartureTime, NpgsqlDbType.Interval, ct);
    await stWriter.WriteAsync(st.ArrivalTimeMax, NpgsqlDbType.Interval, ct);
    await stWriter.WriteAsync(st.DepartureTimeMax, NpgsqlDbType.Interval, ct);
    await stWriter.WriteAsync(st.StopKind, ct);
    await stWriter.WriteAsync(st.RequestStop, NpgsqlDbType.Boolean, ct);
    await stWriter.WriteAsync(st.BoardFront, NpgsqlDbType.Boolean, ct);
    await stWriter.WriteAsync(st.TariffKm, NpgsqlDbType.Integer, ct);
    await stWriter.WriteAsync(st.ActualKm, NpgsqlDbType.Integer, ct);
}
await stWriter.CompleteAsync(ct);
```

### B. Inkrementální import pomocí hashů
Abychom při každém spuštění cronu nemuseli rozbalovat a zpracovávat všech 13 000 souborů:
1. Při startu načteme existující hashe z tabulky `import_batches`:
   ```csharp
   await using var cmd = new NpgsqlCommand("SELECT file_hash FROM import_batches;", conn);
   // Uloženo do in-memory HashSet<string> (< 1 MB RAM)
   ```
2. Při procházení vnějšího archivu spočítáme SHA-256 komprimovaných bajtů vnitřního archivu. Pokud je hash v `HashSet` přítomen, archiv se **vůbec nerozbaluje ani nečte** a rovnou se přeskočí.
3. Denní cron tak namísto desítek minut zabere **jen 10–40 sekund** (zpracují se pouze linky, které se reálně změnily).

### C. Zpracování JDF syntaxe a kódování CP1250
JDF soubory používají specifický formát:
- Kódování **Windows-1250 (`CP1250`)**.
- Položky oddělené čárkou, každá hodnota v uvozovkách (`"..."`).
- Každý řádek je zakončený středníkem (`;`).
- Třída `JdfCsvReader` provádí rychlou tokenizaci streamu bez zbytečných mezilehlých alokací stringů.

### D. Deduplikace zastávek a kalendářů
- **Zastávky (`Stop`):** Databáze udržuje normalizované tvary (`municipality_normalized`, `locality_detail_normalized` atd.) spočítané funkcí `f_unaccent(lower(btrim(...)))` s unikátním indexem. Importér při prvním výskytu zastávku vloží pomocí `ON CONFLICT (...) DO UPDATE ... RETURNING id`, čímž získá kanonické `StopId` a uloží si ho do in-memory cache pro další výskyty.
- **Kalendáře (`Service`):** Třída `CalendarService` analyzuje časové kódy (`Caskody.txt`) a pevné kódy (`1` = pracovní dny, `2` = neděle), vytvoří vzory a výjimky a spočítá deterministický `RuleHash`. Pokud identický kalendář v DB již existuje, použije se jeho ID.

---

## 3. Použití z příkazové řádky (CLI)

```bash
# Spuštění s výchozím stahováním z CIS JŘ
Fourway.Import --connection-string "Host=localhost;Database=fourway;Username=fourway;Password=secret"

# Spuštění s lokálním souborem (přeskočí stahování ze sítě)
Fourway.Import --file /cesta/k/JDF.zip --connection-string "..."

# Testovací režim (dry-run): zkontroluje a zparsuje archiv bez zápisu do DB
Fourway.Import --file /cesta/k/JDF.zip --dry-run

# Nápověda
Fourway.Import --help
```

### Konfigurace přes proměnné prostředí
Místo parametru `--connection-string` lze nastavit standardní proměnnou prostředí:
```bash
export ConnectionStrings__DefaultConnection="Host=postgres;Database=fourway;Username=fourway;Password=secret"
```

---

## 4. Nasazení v Dokploy (Cron Job)

Dokploy umožňuje spouštět Docker kontejnery jako naplánované úlohy:

### 1. Vytvoření služby v Dokploy
1. V Dokploy vytvořte novou aplikaci nebo vyberte **Docker / Cron Job**.
2. Nastavte cestu k Dockerfile: `backend/Fourway.Import/Dockerfile`.
3. Nastavte Cron Schedule (např. každou noc ve 2:00 ráno):
   ```
   0 2 * * *
   ```

### 2. Nastavení Environment Variables v Dokploy
V záložce Environment přidejte:
```env
ConnectionStrings__DefaultConnection=Host=dokploy-postgres;Database=fourway;Username=postgres;Password=your_db_password
```

### 3. Průběh v kontejneru
- Dokploy ve 2:00 spustí kontejner.
- Kontejner stáhne `https://portal.cisjr.cz/pub/JDF/JDF.zip` do dočasného adresáře.
- Projde ~13 000 souborů, porovná hashe s tabulkou `import_batches`.
- Změněné a nové linky zapíše přímo přes PostgreSQL binary protocol do databáze.
- Smaže dočasný zip soubor.
- Kontejner korektně skončí s návratovým kódem `0`. Dokploy zaznamená úspěšný běh.
