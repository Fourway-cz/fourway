using System.Data;
using Fourway.Core.Models;
using Fourway.Import.Parsing;
using Fourway.Import.Parsing.Models;
using Fourway.Import.Services;
using Npgsql;
using NpgsqlTypes;

namespace Fourway.Import.Storage;

public sealed class PostgresBulkImporter : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly CalendarService _calendarService = new();

    private readonly HashSet<string> _knownHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _knownAgencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _knownServices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _knownIdsSystems = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> _knownStops = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(Dictionary<string, Guid> Cache, string Key)> _uncommittedCacheEntries = [];

    public PostgresBulkImporter(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseNetTopologySuite();
        builder.MapEnum<AgencyLegalForm>("agency_legal_form");
        builder.MapEnum<RouteLineType>("route_line_type");
        builder.MapEnum<VehicleMode>("vehicle_mode");
        builder.MapEnum<StopKind>("stop_kind");
        builder.MapEnum<ServiceExceptionType>("service_exception_type");
        builder.MapEnum<WeekParity>("week_parity");

        _dataSource = builder.Build();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        await using (var cmd = new NpgsqlCommand("SELECT file_hash FROM import_batches;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                _knownHashes.Add(reader.GetString(0));
            }
        }

        await using (var cmd = new NpgsqlCommand("SELECT ico, id FROM agencies WHERE ico IS NOT NULL;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                _knownAgencies[reader.GetString(0)] = reader.GetGuid(1);
            }
        }

        await using (var cmd = new NpgsqlCommand("SELECT rule_hash, id FROM services;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                _knownServices[reader.GetString(0)] = reader.GetGuid(1);
            }
        }

        await using (var cmd = new NpgsqlCommand("SELECT code, id FROM ids_systems;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                _knownIdsSystems[reader.GetString(0)] = reader.GetGuid(1);
            }
        }

        await using (var cmd = new NpgsqlCommand(
            "SELECT municipality_normalized, municipality_part_normalized, locality_detail_normalized, district_code, country_code, id FROM stops;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var key = BuildStopKey(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4));
                _knownStops[key] = reader.GetGuid(5);
            }
        }
    }

    public bool IsHashAlreadyImported(string hash) => _knownHashes.Contains(hash);

    public async Task ResetDatabaseAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(
            """
            TRUNCATE TABLE
                agency_branches, agencies, examples, ids_systems, import_batches,
                route_ids_info, route_stops, routes, service_exceptions, service_notes,
                service_patterns, services, stop_source_refs, stop_times, stops,
                trip_operators, trips
            RESTART IDENTITY CASCADE;
            """, conn);
        await cmd.ExecuteNonQueryAsync(ct);

        _knownHashes.Clear();
        _knownAgencies.Clear();
        _knownServices.Clear();
        _knownIdsSystems.Clear();
        _knownStops.Clear();
        _uncommittedCacheEntries.Clear();
    }

    public async Task ImportPackageAsync(JdfPackage package, CancellationToken ct = default)
    {
        if (_knownHashes.Contains(package.FileHash))
        {
            return;
        }

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        _uncommittedCacheEntries.Clear();

        try
        {
            var jdfVersion = package.Verze?.CisloVerze ?? "1.10";
            var exportedOn = package.Verze?.DatumExportu;

            var batch = new ImportBatch(
                sourcePath: package.ArchiveName,
                fileHash: package.FileHash,
                jdfVersion: jdfVersion,
                exportedOn: exportedOn);

            await using (var cmd = new NpgsqlCommand(
                """
                INSERT INTO import_batches (id, source_path, file_hash, jdf_version, exported_on, imported_at)
                VALUES ($1, $2, $3, $4, $5, $6);
                """, conn, tx))
            {
                cmd.Parameters.AddWithValue(batch.Id);
                cmd.Parameters.AddWithValue(batch.SourcePath);
                cmd.Parameters.AddWithValue(batch.FileHash);
                cmd.Parameters.AddWithValue(batch.JdfVersion);
                cmd.Parameters.AddWithValue(batch.ExportedOn.HasValue ? batch.ExportedOn.Value : DBNull.Value);
                cmd.Parameters.AddWithValue(batch.ImportedAt);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            var agencyMap = new Dictionary<int, Guid>();
            foreach (var rawDopravce in package.Dopravci)
            {
                var agencyId = await ResolveAgencyAsync(rawDopravce, conn, tx, ct);
                agencyMap[rawDopravce.CisloDopravce] = agencyId;
            }

            var stopMap = new Dictionary<int, Guid>();
            var stopSourceRefs = new List<StopSourceRef>();
            foreach (var rawZastavka in package.Zastavky)
            {
                var stopId = await ResolveStopAsync(rawZastavka, conn, tx, ct);
                stopMap[rawZastavka.CisloZastavky] = stopId;
                stopSourceRefs.Add(new StopSourceRef(batch.Id, rawZastavka.CisloZastavky, stopId));
            }

            if (stopSourceRefs.Count > 0)
            {
                await using var writer = await conn.BeginBinaryImportAsync(
                    "COPY stop_source_refs (import_batch_id, source_stop_no, stop_id) FROM STDIN (FORMAT BINARY);", ct);
                foreach (var ssr in stopSourceRefs)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(ssr.ImportBatchId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(ssr.SourceStopNo, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(ssr.StopId, NpgsqlDbType.Uuid, ct);
                }
                await writer.CompleteAsync(ct);
            }

            var rawLinka = package.Linky.FirstOrDefault();
            if (rawLinka is not null)
            {
                if (!agencyMap.TryGetValue(rawLinka.CisloDopravce, out var agencyId))
                {
                    agencyId = agencyMap.Values.FirstOrDefault();
                    if (agencyId == Guid.Empty)
                    {
                        agencyId = await ResolveDefaultAgencyAsync(rawLinka, conn, tx, ct);
                    }
                }

                await using (var cmd = new NpgsqlCommand(
                    "DELETE FROM routes WHERE license_number = $1 AND variant = $2 AND valid_from = $3;", conn, tx))
                {
                    cmd.Parameters.AddWithValue(rawLinka.CisloLinky);
                    cmd.Parameters.AddWithValue(rawLinka.RozliseniLinky);
                    cmd.Parameters.AddWithValue(rawLinka.PlatnostOd);
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                var lineType = ParseRouteLineType(rawLinka.TypLinky);
                var vehicleMode = ParseVehicleMode(rawLinka.DopravniProstredek);

                var route = new Route(
                    licenseNumber: rawLinka.CisloLinky,
                    variant: rawLinka.RozliseniLinky,
                    validFrom: rawLinka.PlatnostOd,
                    validTo: rawLinka.PlatnostDo,
                    importBatchId: batch.Id,
                    agencyId: agencyId,
                    name: rawLinka.NazevLinky,
                    lineType: lineType,
                    vehicleMode: vehicleMode,
                    gtfsRouteType: null,
                    oneWay: rawLinka.JednosmernyProvoz,
                    licenseRef: rawLinka.CisloLicence,
                    licenseValidFrom: rawLinka.PlatnostLicenceOd,
                    licenseValidTo: rawLinka.PlatnostLicenceDo,
                    approvingAuthority: rawLinka.SchvalujiciOrgan);

                await using (var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO routes (
                        id, license_number, variant, valid_from, valid_to, import_batch_id,
                        agency_id, name, line_type, vehicle_mode, gtfs_route_type, one_way,
                        license_ref, license_valid_from, license_valid_to, approving_authority)
                    VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16);
                    """, conn, tx))
                {
                    cmd.Parameters.AddWithValue(route.Id);
                    cmd.Parameters.AddWithValue(route.LicenseNumber);
                    cmd.Parameters.AddWithValue(route.Variant);
                    cmd.Parameters.AddWithValue(route.ValidFrom);
                    cmd.Parameters.AddWithValue(route.ValidTo.HasValue ? route.ValidTo.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue(route.ImportBatchId);
                    cmd.Parameters.AddWithValue(route.AgencyId);
                    cmd.Parameters.AddWithValue(route.Name);
                    cmd.Parameters.AddWithValue(route.LineType);
                    cmd.Parameters.AddWithValue(route.VehicleMode);
                    cmd.Parameters.AddWithValue(route.GtfsRouteType.HasValue ? route.GtfsRouteType.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue(route.OneWay);
                    cmd.Parameters.AddWithValue((object?)route.LicenseRef ?? DBNull.Value);
                    cmd.Parameters.AddWithValue(route.LicenseValidFrom.HasValue ? route.LicenseValidFrom.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue(route.LicenseValidTo.HasValue ? route.LicenseValidTo.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue((object?)route.ApprovingAuthority ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                var routeStopMap = new Dictionary<int, RouteStop>();
                var routeStops = new List<RouteStop>();

                foreach (var rz in package.Zaslinky.OrderBy(x => x.TarifniCislo))
                {
                    if (!stopMap.TryGetValue(rz.CisloZastavky, out var stopId))
                    {
                        continue;
                    }

                    var zones = string.IsNullOrWhiteSpace(rz.TarifniPasmo)
                        ? []
                        : rz.TarifniPasmo.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    var isRequest = rz.PevneKody.Contains((int)FixedCode.RequestStop);

                    var routeStop = new RouteStop(
                        routeId: route.Id,
                        sequence: rz.TarifniCislo,
                        stopId: stopId,
                        tariffZones: zones,
                        requestStop: isRequest);

                    routeStopMap[rz.TarifniCislo] = routeStop;
                    routeStops.Add(routeStop);
                }

                if (routeStops.Count > 0)
                {
                    await using var rsWriter = await conn.BeginBinaryImportAsync(
                        "COPY route_stops (id, route_id, sequence, stop_id, tariff_zones) FROM STDIN (FORMAT BINARY);", ct);
                    foreach (var rs in routeStops)
                    {
                        await rsWriter.StartRowAsync(ct);
                        await rsWriter.WriteAsync(rs.Id, NpgsqlDbType.Uuid, ct);
                        await rsWriter.WriteAsync(rs.RouteId, NpgsqlDbType.Uuid, ct);
                        await rsWriter.WriteAsync(rs.Sequence, NpgsqlDbType.Integer, ct);
                        await rsWriter.WriteAsync(rs.StopId, NpgsqlDbType.Uuid, ct);
                        await rsWriter.WriteAsync(rs.TariffZones, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                    }
                    await rsWriter.CompleteAsync(ct);
                }

                var caskodyByTrip = package.Caskody
                    .GroupBy(x => x.CisloSpoje)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var tripsToInsert = new List<Trip>();
                var stopTimesToInsert = new List<StopTime>();

                foreach (var rawSpoj in package.Spoje)
                {
                    caskodyByTrip.TryGetValue(rawSpoj.CisloSpoje, out var tripCaskody);
                    tripCaskody ??= [];

                    var service = _calendarService.BuildService(
                        rawSpoj,
                        tripCaskody,
                        rawLinka.PlatnostOd,
                        rawLinka.PlatnostDo);

                    var serviceId = await ResolveServiceAsync(service, conn, tx, ct);

                    var wheelchair = rawSpoj.PevneKody.Contains((int)FixedCode.WheelchairAccessible);
                    var bikes = rawSpoj.PevneKody.Contains((int)FixedCode.BikesAllowed);
                    var localTransportExcluded = rawSpoj.PevneKody.Contains((int)FixedCode.LocalTransportExcluded);

                    var trip = new Trip(
                        routeId: route.Id,
                        tripNumber: rawSpoj.CisloSpoje,
                        serviceId: serviceId,
                        wheelchairAccessible: wheelchair,
                        bikesAllowed: bikes,
                        localTransportExcluded: localTransportExcluded);

                    tripsToInsert.Add(trip);

                    var zasspojeForTrip = package.Zasspoje
                        .Where(x => x.CisloSpoje == rawSpoj.CisloSpoje)
                        .OrderBy(x => x.TarifniCislo);

                    var stopSeq = 1;
                    foreach (var zs in zasspojeForTrip)
                    {
                        if (zs.CasPrijezduMinRaw == "<" && zs.CasOdjezduMinRaw == "<")
                        {
                            continue;
                        }

                        if (!routeStopMap.TryGetValue(zs.TarifniCislo, out var routeStop))
                        {
                            continue;
                        }

                        var isPassThrough = zs.CasPrijezduMinRaw == "|" || zs.CasOdjezduMinRaw == "|";
                        var stopKind = isPassThrough ? StopKind.PassesThrough : StopKind.Stops;

                        var arrival = ParseTime(zs.CasPrijezduMinRaw);
                        var departure = ParseTime(zs.CasOdjezduMinRaw);
                        var arrivalMax = ParseTime(zs.CasPrijezduMaxRaw);
                        var departureMax = ParseTime(zs.CasOdjezduMaxRaw);

                        var isRequest = zs.PevneKody.Contains((int)FixedCode.RequestStop);
                        var isBoardFront = zs.PevneKody.Contains((int)FixedCode.BoardFront);

                        var stopTime = new StopTime(
                            tripId: trip.Id,
                            stopSequence: stopSeq++,
                            routeStopId: routeStop.Id,
                            stopId: routeStop.StopId,
                            arrivalTime: arrival,
                            departureTime: departure,
                            arrivalTimeMax: arrivalMax,
                            departureTimeMax: departureMax,
                            stopKind: stopKind,
                            platformCode: zs.CisloStanoviste,
                            requestStop: isRequest,
                            boardFront: isBoardFront,
                            tariffKm: zs.TarifniKm,
                            actualKm: zs.SkutecneKm);

                        stopTimesToInsert.Add(stopTime);
                    }
                }

                if (tripsToInsert.Count > 0)
                {
                    await using var tripWriter = await conn.BeginBinaryImportAsync(
                        "COPY trips (id, route_id, trip_number, service_id, wheelchair_accessible, bikes_allowed) FROM STDIN (FORMAT BINARY);", ct);
                    foreach (var t in tripsToInsert)
                    {
                        await tripWriter.StartRowAsync(ct);
                        await tripWriter.WriteAsync(t.Id, NpgsqlDbType.Uuid, ct);
                        await tripWriter.WriteAsync(t.RouteId, NpgsqlDbType.Uuid, ct);
                        await tripWriter.WriteAsync(t.TripNumber, NpgsqlDbType.Integer, ct);
                        await tripWriter.WriteAsync(t.ServiceId, NpgsqlDbType.Uuid, ct);
                        await tripWriter.WriteAsync(t.WheelchairAccessible, NpgsqlDbType.Boolean, ct);
                        await tripWriter.WriteAsync(t.BikesAllowed, NpgsqlDbType.Boolean, ct);
                    }
                    await tripWriter.CompleteAsync(ct);
                }

                if (stopTimesToInsert.Count > 0)
                {
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
                }

                foreach (var linExt in package.LinExt)
                {
                    var idsId = await ResolveIdsSystemAsync(linExt.KodDopravy, conn, tx, ct);
                    var idsInfo = new RouteIdsInfo(
                        routeId: route.Id,
                        idsSystemId: idsId,
                        publicNumber: linExt.OznaceniLinky,
                        fullyIntegrated: linExt.PriznakIntegrace);

                    await using var cmd = new NpgsqlCommand(
                        """
                        INSERT INTO route_ids_info (id, route_id, ids_system_id, public_number, fully_integrated)
                        VALUES ($1, $2, $3, $4, $5)
                        ON CONFLICT (route_id, ids_system_id) DO NOTHING;
                        """, conn, tx);
                    cmd.Parameters.AddWithValue(idsInfo.Id);
                    cmd.Parameters.AddWithValue(idsInfo.RouteId);
                    cmd.Parameters.AddWithValue(idsInfo.IdsSystemId);
                    cmd.Parameters.AddWithValue(idsInfo.PublicNumber);
                    cmd.Parameters.AddWithValue(idsInfo.FullyIntegrated);
                    await cmd.ExecuteNonQueryAsync(ct);
                }
            }

            await tx.CommitAsync(ct);
            _uncommittedCacheEntries.Clear();
            _knownHashes.Add(package.FileHash);
        }
        catch
        {
            foreach (var (cache, key) in _uncommittedCacheEntries)
            {
                cache.Remove(key);
            }
            _uncommittedCacheEntries.Clear();
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Guid> ResolveAgencyAsync(
        RawDopravce raw,
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(raw.IcDopravce) &&
            _knownAgencies.TryGetValue(raw.IcDopravce, out var existingId))
        {
            return existingId;
        }

        var legalForm = raw.DruhDopravce == 2 ? AgencyLegalForm.NaturalPerson : AgencyLegalForm.LegalPerson;
        var personName = legalForm == AgencyLegalForm.NaturalPerson
            ? (raw.JmenoFyzickeOsoby ?? raw.NazevDopravce)
            : raw.JmenoFyzickeOsoby;

        var agency = new Agency(
            ico: raw.IcDopravce,
            dic: raw.DicDopravce,
            name: raw.NazevDopravce,
            legalForm: legalForm,
            personName: personName);

        if (!string.IsNullOrWhiteSpace(agency.Ico))
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO agencies (id, ico, dic, name, legal_form, person_name)
                VALUES ($1, $2, $3, $4, $5, $6)
                ON CONFLICT (ico) DO UPDATE SET name = EXCLUDED.name
                RETURNING id;
                """, conn, tx);

            cmd.Parameters.AddWithValue(agency.Id);
            cmd.Parameters.AddWithValue(agency.Ico);
            cmd.Parameters.AddWithValue((object?)agency.Dic ?? DBNull.Value);
            cmd.Parameters.AddWithValue(agency.Name);
            cmd.Parameters.AddWithValue(agency.LegalForm);
            cmd.Parameters.AddWithValue((object?)agency.PersonName ?? DBNull.Value);

            var resolvedId = (Guid)(await cmd.ExecuteScalarAsync(ct))!;
            TrackCacheEntry(_knownAgencies, agency.Ico, resolvedId);
            return resolvedId;
        }
        else
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO agencies (id, ico, dic, name, legal_form, person_name)
                VALUES ($1, $2, $3, $4, $5, $6)
                RETURNING id;
                """, conn, tx);

            cmd.Parameters.AddWithValue(agency.Id);
            cmd.Parameters.AddWithValue(DBNull.Value);
            cmd.Parameters.AddWithValue((object?)agency.Dic ?? DBNull.Value);
            cmd.Parameters.AddWithValue(agency.Name);
            cmd.Parameters.AddWithValue(agency.LegalForm);
            cmd.Parameters.AddWithValue((object?)agency.PersonName ?? DBNull.Value);

            return (Guid)(await cmd.ExecuteScalarAsync(ct))!;
        }
    }

    private async Task<Guid> ResolveDefaultAgencyAsync(
        RawLinka linka,
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct)
    {
        var agency = new RawDopravce(
            linka.IcDopravce,
            null,
            $"Dopravce linky {linka.CisloLinky}",
            1,
            null, null, null, null, null, null, null, linka.CisloDopravce);
        return await ResolveAgencyAsync(agency, conn, tx, ct);
    }

    private async Task<Guid> ResolveStopAsync(
        RawZastavka raw,
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct)
    {
        var district = (raw.Okres.Length > 2 ? raw.Okres[..2] : raw.Okres).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(district)) district = "--";

        var country = NormalizeCountryCode(raw.Stat);

        var hasPlatform = raw.PevneKody.Contains(15);
        var wheelchair = raw.PevneKody.Contains((int)FixedCode.WheelchairAccessible);
        var hasShelter = raw.PevneKody.Contains(16);
        var hasInfoBoard = raw.PevneKody.Contains(17);
        var hasLighting = raw.PevneKody.Contains(19);

        var stop = new Stop(
            municipality: raw.NazevObce,
            municipalityPart: raw.CastObce,
            localityDetail: raw.BlizsiUrceni,
            districtCode: district,
            countryCode: country,
            location: null,
            locationSource: null,
            hasPlatform: hasPlatform,
            wheelchairAccessible: wheelchair,
            hasShelter: hasShelter,
            hasInfoBoard: hasInfoBoard,
            hasLighting: hasLighting);

        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO stops (
                id, municipality, municipality_part, locality_detail, district_code, country_code,
                location, location_source, has_platform, wheelchair_accessible, has_shelter,
                has_info_board, has_lighting
            )
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)
            ON CONFLICT (municipality_normalized, municipality_part_normalized, locality_detail_normalized, district_code, country_code)
            DO UPDATE SET district_code = EXCLUDED.district_code
            RETURNING id, municipality_normalized, municipality_part_normalized, locality_detail_normalized, district_code, country_code;
            """, conn, tx);

        cmd.Parameters.AddWithValue(stop.Id);
        cmd.Parameters.AddWithValue(stop.Municipality);
        cmd.Parameters.AddWithValue((object?)stop.MunicipalityPart ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)stop.LocalityDetail ?? DBNull.Value);
        cmd.Parameters.AddWithValue(stop.DistrictCode);
        cmd.Parameters.AddWithValue(stop.CountryCode);
        cmd.Parameters.AddWithValue(DBNull.Value);
        cmd.Parameters.AddWithValue(DBNull.Value);
        cmd.Parameters.AddWithValue(stop.HasPlatform);
        cmd.Parameters.AddWithValue(stop.WheelchairAccessible);
        cmd.Parameters.AddWithValue(stop.HasShelter);
        cmd.Parameters.AddWithValue(stop.HasInfoBoard);
        cmd.Parameters.AddWithValue(stop.HasLighting);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var resId = reader.GetGuid(0);
            var key = BuildStopKey(
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5));
            TrackCacheEntry(_knownStops, key, resId);
            return resId;
        }

        return stop.Id;
    }

    private async Task<Guid> ResolveServiceAsync(
        Service service,
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct)
    {
        if (_knownServices.TryGetValue(service.RuleHash, out var existingId))
        {
            return existingId;
        }

        var serviceId = service.Id;

        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO services (id, rule_hash)
            VALUES ($1, $2)
            ON CONFLICT (rule_hash) DO NOTHING
            RETURNING id;
            """, conn, tx);
        cmd.Parameters.AddWithValue(serviceId);
        cmd.Parameters.AddWithValue(service.RuleHash);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is Guid returnedId)
        {
            serviceId = returnedId;

            if (service.Patterns.Count > 0)
            {
                await using var pWriter = await conn.BeginBinaryImportAsync(
                    """
                    COPY service_patterns (
                        id, service_id, monday, tuesday, wednesday, thursday, friday, saturday, sunday,
                        valid_from, valid_to, week_parity, parity_anchor
                    ) FROM STDIN (FORMAT BINARY);
                    """, ct);

                foreach (var p in service.Patterns)
                {
                    await pWriter.StartRowAsync(ct);
                    await pWriter.WriteAsync(p.Id, NpgsqlDbType.Uuid, ct);
                    await pWriter.WriteAsync(serviceId, NpgsqlDbType.Uuid, ct);
                    await pWriter.WriteAsync(p.Monday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.Tuesday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.Wednesday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.Thursday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.Friday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.Saturday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.Sunday, NpgsqlDbType.Boolean, ct);
                    await pWriter.WriteAsync(p.ValidFrom, NpgsqlDbType.Date, ct);
                    await pWriter.WriteAsync(p.ValidTo, NpgsqlDbType.Date, ct);
                    await pWriter.WriteAsync(p.WeekParity, ct);
                    await pWriter.WriteAsync(p.ParityAnchor, NpgsqlDbType.Date, ct);
                }
                await pWriter.CompleteAsync(ct);
            }

            if (service.Exceptions.Count > 0)
            {
                await using var eWriter = await conn.BeginBinaryImportAsync(
                    "COPY service_exceptions (service_id, date, exception_type) FROM STDIN (FORMAT BINARY);", ct);

                foreach (var ex in service.Exceptions)
                {
                    await eWriter.StartRowAsync(ct);
                    await eWriter.WriteAsync(serviceId, NpgsqlDbType.Uuid, ct);
                    await eWriter.WriteAsync(ex.Date, NpgsqlDbType.Date, ct);
                    await eWriter.WriteAsync(ex.ExceptionType, ct);
                }
                await eWriter.CompleteAsync(ct);
            }
        }
        else
        {
            await using var queryCmd = new NpgsqlCommand("SELECT id FROM services WHERE rule_hash = $1;", conn, tx);
            queryCmd.Parameters.AddWithValue(service.RuleHash);
            serviceId = (Guid)(await queryCmd.ExecuteScalarAsync(ct))!;
        }

        TrackCacheEntry(_knownServices, service.RuleHash, serviceId);
        return serviceId;
    }

    private async Task<Guid> ResolveIdsSystemAsync(
        string code,
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct)
    {
        if (_knownIdsSystems.TryGetValue(code, out var existingId))
        {
            return existingId;
        }

        var idsSystem = new IdsSystem(code, $"IDS {code}");
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO ids_systems (id, code, name)
            VALUES ($1, $2, $3)
            ON CONFLICT (code) DO NOTHING
            RETURNING id;
            """, conn, tx);
        cmd.Parameters.AddWithValue(idsSystem.Id);
        cmd.Parameters.AddWithValue(idsSystem.Code);
        cmd.Parameters.AddWithValue((object?)idsSystem.Name ?? DBNull.Value);

        var res = await cmd.ExecuteScalarAsync(ct);
        var id = idsSystem.Id;
        if (res is Guid returnedId)
        {
            id = returnedId;
        }
        else
        {
            await using var queryCmd = new NpgsqlCommand("SELECT id FROM ids_systems WHERE code = $1;", conn, tx);
            queryCmd.Parameters.AddWithValue(code);
            id = (Guid)(await queryCmd.ExecuteScalarAsync(ct))!;
        }

        TrackCacheEntry(_knownIdsSystems, code, id);
        return id;
    }

    private void TrackCacheEntry(Dictionary<string, Guid> cache, string key, Guid id)
    {
        cache[key] = id;
        _uncommittedCacheEntries.Add((cache, key));
    }

    private static string BuildStopKey(string m, string mp, string ld, string dist, string c) =>
        $"{m}|{mp}|{ld}|{dist}|{c}";

    private static string NormalizeCountryCode(string? stat)
    {
        var code = stat?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            return "CZ";
        }

        return code switch
        {
            "A" => "AT",
            "B" => "BE",
            "D" => "DE",
            "E" => "ES",
            "F" => "FR",
            "H" => "HU",
            "I" => "IT",
            "L" => "LU",
            "N" => "NO",
            "S" => "SE",
            "BIH" => "BA",
            "SLO" => "SI",
            "SRB" => "RS",
            _ when code.Length == 2 => code,
            _ => "XX"
        };
    }

    private static RouteLineType ParseRouteLineType(string? raw) => raw?.Trim().ToUpperInvariant() switch
    {
        "V" => RouteLineType.Regional,
        "A" => RouteLineType.Urban,
        "D" => RouteLineType.LongDistance,
        "M" => RouteLineType.International,
        "Z" => RouteLineType.InternationalSpecial,
        "P" => RouteLineType.Suburban,
        "N" => RouteLineType.Special,
        _ => RouteLineType.Regional
    };

    private static VehicleMode ParseVehicleMode(string? raw) => raw?.Trim().ToUpperInvariant() switch
    {
        "A" => VehicleMode.Bus,
        "T" => VehicleMode.Trolleybus,
        "E" => VehicleMode.Tram,
        "V" => VehicleMode.Train,
        "L" => VehicleMode.CableCar,
        "P" => VehicleMode.Ferry,
        _ => VehicleMode.Bus
    };

    private static TimeSpan? ParseTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "<" || raw == "|" || raw.Length < 4)
        {
            return null;
        }

        if (int.TryParse(raw.AsSpan(0, 2), out var hours) &&
            int.TryParse(raw.AsSpan(2, 2), out var minutes) &&
            hours >= 0 && hours <= 48 && minutes >= 0 && minutes <= 59)
        {
            return new TimeSpan(hours, minutes, 0);
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}
