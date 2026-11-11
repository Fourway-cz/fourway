using System;
using Fourway.Api.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fourway.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:example_status", "active,archived");

            migrationBuilder.CreateTable(
                name: "examples",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<ExampleStatus>(type: "example_status", nullable: false, defaultValue: ExampleStatus.Active),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_examples", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_examples_name",
                table: "examples",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examples_status",
                table: "examples",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "examples");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:example_status", "active,archived");
        }
    }
}
