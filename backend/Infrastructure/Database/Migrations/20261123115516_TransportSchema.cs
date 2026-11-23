using System;
using Fourway.Api.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Fourway.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class TransportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:agency_legal_form", "legal_person,natural_person")
                .Annotation("Npgsql:Enum:example_status", "active,archived")
                .Annotation("Npgsql:Enum:route_line_type", "V,A,D,M,Z,P,N")
                .Annotation("Npgsql:Enum:service_exception_type", "runs,does_not_run")
                .Annotation("Npgsql:Enum:stop_kind", "stops,passes_through")
                .Annotation("Npgsql:Enum:vehicle_mode", "A,T,E,V,L,P")
                .Annotation("Npgsql:Enum:week_parity", "all,odd,even")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,")
                .OldAnnotation("Npgsql:Enum:example_status", "active,archived");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION f_unaccent(text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                STRICT
                PARALLEL SAFE
                AS $$
                    SELECT public.unaccent('public.unaccent'::regdictionary, $1);
                $$;
                """);

            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ico = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    dic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_form = table.Column<AgencyLegalForm>(type: "agency_legal_form", nullable: false),
                    person_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ids_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ids_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    jdf_version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    exported_on = table.Column<DateOnly>(type: "date", nullable: true),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                    table.CheckConstraint("ck_services_rule_hash_not_empty", "rule_hash <> ''");
                });

            migrationBuilder.CreateTable(
                name: "stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    municipality = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    municipality_part = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    locality_detail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    district_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    location = table.Column<Point>(type: "geography (point, 4326)", nullable: true),
                    location_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    municipality_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "coalesce(f_unaccent(lower(btrim(municipality))), '')", stored: true),
                    municipality_part_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "coalesce(f_unaccent(lower(btrim(municipality_part))), '')", stored: true),
                    locality_detail_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "coalesce(f_unaccent(lower(btrim(locality_detail))), '')", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stops", x => x.id);
                    table.CheckConstraint("ck_stops_country_code_length", "char_length(country_code) = 2");
                    table.CheckConstraint("ck_stops_district_code_length", "char_length(district_code) = 2");
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    variant = table.Column<short>(type: "smallint", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    line_type = table.Column<RouteLineType>(type: "route_line_type", nullable: false),
                    vehicle_mode = table.Column<VehicleMode>(type: "vehicle_mode", nullable: false),
                    gtfs_route_type = table.Column<short>(type: "smallint", nullable: true),
                    one_way = table.Column<bool>(type: "boolean", nullable: false),
                    license_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    license_valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    license_valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    approving_authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                    table.CheckConstraint("ck_routes_gtfs_route_type_not_negative", "gtfs_route_type IS NULL OR gtfs_route_type >= 0");
                    table.CheckConstraint("ck_routes_validity", "valid_to IS NULL OR valid_to >= valid_from");
                    table.CheckConstraint("ck_routes_variant_not_negative", "variant >= 0");
                    table.ForeignKey(
                        name: "fk_routes_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_routes_import_batches_import_batch_id",
                        column: x => x.import_batch_id,
                        principalTable: "import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_exceptions",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    exception_type = table.Column<ServiceExceptionType>(type: "service_exception_type", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_exceptions", x => new { x.service_id, x.date });
                    table.ForeignKey(
                        name: "fk_service_exceptions_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monday = table.Column<bool>(type: "boolean", nullable: false),
                    tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    thursday = table.Column<bool>(type: "boolean", nullable: false),
                    friday = table.Column<bool>(type: "boolean", nullable: false),
                    saturday = table.Column<bool>(type: "boolean", nullable: false),
                    sunday = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: false),
                    week_parity = table.Column<WeekParity>(type: "week_parity", nullable: false),
                    parity_anchor = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_patterns", x => x.id);
                    table.CheckConstraint("ck_service_patterns_has_day", "monday OR tuesday OR wednesday OR thursday OR friday OR saturday OR sunday");
                    table.CheckConstraint("ck_service_patterns_validity", "valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_service_patterns_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stop_source_refs",
                columns: table => new
                {
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_stop_no = table.Column<int>(type: "integer", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_source_refs", x => new { x.import_batch_id, x.source_stop_no });
                    table.ForeignKey(
                        name: "fk_stop_source_refs_import_batches_import_batch_id",
                        column: x => x.import_batch_id,
                        principalTable: "import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stop_source_refs_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_ids_info",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ids_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fully_integrated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_ids_info", x => x.id);
                    table.ForeignKey(
                        name: "fk_route_ids_info_ids_systems_ids_system_id",
                        column: x => x.ids_system_id,
                        principalTable: "ids_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_route_ids_info_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_zones = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_route_stops", x => x.id);
                    table.CheckConstraint("ck_route_stops_sequence_positive", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_route_stops_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_route_stops_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_number = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wheelchair_accessible = table.Column<bool>(type: "boolean", nullable: false),
                    bikes_allowed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                    table.CheckConstraint("ck_trips_trip_number_positive", "trip_number > 0");
                    table.ForeignKey(
                        name: "fk_trips_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trips_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stop_times",
                columns: table => new
                {
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_sequence = table.Column<int>(type: "integer", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    arrival_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    departure_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                    arrival_time_max = table.Column<TimeSpan>(type: "interval", nullable: true),
                    departure_time_max = table.Column<TimeSpan>(type: "interval", nullable: true),
                    stop_kind = table.Column<StopKind>(type: "stop_kind", nullable: false),
                    request_stop = table.Column<bool>(type: "boolean", nullable: false),
                    board_front = table.Column<bool>(type: "boolean", nullable: false),
                    tariff_km = table.Column<int>(type: "integer", nullable: true),
                    actual_km = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_times", x => new { x.trip_id, x.stop_sequence });
                    table.CheckConstraint("ck_stop_times_actual_km_not_negative", "actual_km IS NULL OR actual_km >= 0");
                    table.CheckConstraint("ck_stop_times_arrival_max", "arrival_time IS NULL OR arrival_time_max IS NULL OR arrival_time_max >= arrival_time");
                    table.CheckConstraint("ck_stop_times_departure_max", "departure_time IS NULL OR departure_time_max IS NULL OR departure_time_max >= departure_time");
                    table.CheckConstraint("ck_stop_times_stop_sequence_positive", "stop_sequence > 0");
                    table.CheckConstraint("ck_stop_times_tariff_km_not_negative", "tariff_km IS NULL OR tariff_km >= 0");
                    table.ForeignKey(
                        name: "fk_stop_times_route_stops_route_stop_id",
                        column: x => x.route_stop_id,
                        principalTable: "route_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stop_times_stops_stop_id",
                        column: x => x.stop_id,
                        principalTable: "stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stop_times_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_operators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trip_operators", x => x.id);
                    table.CheckConstraint("ck_trip_operators_validity", "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_trip_operators_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trip_operators_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agencies_ico",
                table: "agencies",
                column: "ico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ids_systems_code",
                table: "ids_systems",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_import_batches_file_hash",
                table: "import_batches",
                column: "file_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_route_ids_info_ids_system_id",
                table: "route_ids_info",
                column: "ids_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_ids_info_route_id_ids_system_id",
                table: "route_ids_info",
                columns: new[] { "route_id", "ids_system_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_route_stops_route_id_sequence",
                table: "route_stops",
                columns: new[] { "route_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_route_stops_stop_id",
                table: "route_stops",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_agency_id",
                table: "routes",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_import_batch_id",
                table: "routes",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_license_number_variant_valid_from",
                table: "routes",
                columns: new[] { "license_number", "variant", "valid_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_patterns_service_id_valid_from_valid_to",
                table: "service_patterns",
                columns: new[] { "service_id", "valid_from", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "ix_services_rule_hash",
                table: "services",
                column: "rule_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stop_source_refs_stop_id",
                table: "stop_source_refs",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_stop_times_route_stop_id",
                table: "stop_times",
                column: "route_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_stop_times_stop_id",
                table: "stop_times",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_stops_location",
                table: "stops",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_stops_municipality_normalized_municipality_part_normalized_",
                table: "stops",
                columns: new[] { "municipality_normalized", "municipality_part_normalized", "locality_detail_normalized", "district_code", "country_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trip_operators_agency_id",
                table: "trip_operators",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_operators_trip_id_agency_id_valid_from",
                table: "trip_operators",
                columns: new[] { "trip_id", "agency_id", "valid_from" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_trips_route_id_trip_number",
                table: "trips",
                columns: new[] { "route_id", "trip_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_service_id",
                table: "trips",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "route_ids_info");

            migrationBuilder.DropTable(
                name: "service_exceptions");

            migrationBuilder.DropTable(
                name: "service_patterns");

            migrationBuilder.DropTable(
                name: "stop_source_refs");

            migrationBuilder.DropTable(
                name: "stop_times");

            migrationBuilder.DropTable(
                name: "trip_operators");

            migrationBuilder.DropTable(
                name: "ids_systems");

            migrationBuilder.DropTable(
                name: "route_stops");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "agencies");

            migrationBuilder.DropTable(
                name: "import_batches");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS f_unaccent(text);");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:example_status", "active,archived")
                .OldAnnotation("Npgsql:Enum:agency_legal_form", "legal_person,natural_person")
                .OldAnnotation("Npgsql:Enum:example_status", "active,archived")
                .OldAnnotation("Npgsql:Enum:route_line_type", "V,A,D,M,Z,P,N")
                .OldAnnotation("Npgsql:Enum:service_exception_type", "runs,does_not_run")
                .OldAnnotation("Npgsql:Enum:stop_kind", "stops,passes_through")
                .OldAnnotation("Npgsql:Enum:vehicle_mode", "A,T,E,V,L,P")
                .OldAnnotation("Npgsql:Enum:week_parity", "all,odd,even")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
