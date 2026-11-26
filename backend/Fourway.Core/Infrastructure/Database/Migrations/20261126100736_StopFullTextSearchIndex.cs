using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fourway.Core.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class StopFullTextSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GIN index pro fulltextové vyhledávání v GetStops. Výraz tsvectoru
            // musí přesně odpovídat dotazu v Features/Stops/GetStops.cs.
            migrationBuilder.Sql(
                """
                CREATE INDEX ix_stops_search_vector
                    ON stops USING gin (
                        to_tsvector(
                            'simple',
                            municipality_normalized || ' ' ||
                            municipality_part_normalized || ' ' ||
                            locality_detail_normalized));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_stops_search_vector;");
        }
    }
}
