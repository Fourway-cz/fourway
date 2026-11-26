using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fourway.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ServiceNotesAndAgencyBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "local_transport_excluded",
                table: "trips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_info_board",
                table: "stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_lighting",
                table: "stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_platform",
                table: "stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_shelter",
                table: "stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "wheelchair_accessible",
                table: "stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "request_stop",
                table: "route_stops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "agency_branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_number = table.Column<int>(type: "integer", nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    phone_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone1 = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    phone2 = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    fax = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    web = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agency_branches", x => x.id);
                    table.CheckConstraint("ck_agency_branches_branch_number_not_negative", "branch_number >= 0");
                    table.ForeignKey(
                        name: "fk_agency_branches_agencies_agency_id",
                        column: x => x.agency_id,
                        principalTable: "agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_notes", x => x.id);
                    table.CheckConstraint("ck_service_notes_sequence_not_negative", "sequence >= 0");
                    table.ForeignKey(
                        name: "fk_service_notes_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agency_branches_agency_id_branch_number",
                table: "agency_branches",
                columns: new[] { "agency_id", "branch_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_notes_service_id",
                table: "service_notes",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agency_branches");

            migrationBuilder.DropTable(
                name: "service_notes");

            migrationBuilder.DropColumn(
                name: "local_transport_excluded",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "has_info_board",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "has_lighting",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "has_platform",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "has_shelter",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "wheelchair_accessible",
                table: "stops");

            migrationBuilder.DropColumn(
                name: "request_stop",
                table: "route_stops");
        }
    }
}
