using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRateSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    RateTimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Sku);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    SnapshotId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuoteCurrency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => new { x.SnapshotId, x.QuoteCurrency });
                    table.ForeignKey(
                        name: "FK_ExchangeRates_ExchangeRateSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "ExchangeRateSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LatestExchangeRates",
                columns: table => new
                {
                    BaseCurrency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    RateTimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestExchangeRates", x => new { x.BaseCurrency, x.QuoteCurrency });
                    table.ForeignKey(
                        name: "FK_LatestExchangeRates_ExchangeRateSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "ExchangeRateSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateSnapshots_Provider_BaseCurrency_RateTimestampUtc",
                table: "ExchangeRateSnapshots",
                columns: new[] { "Provider", "BaseCurrency", "RateTimestampUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LatestExchangeRates_SnapshotId",
                table: "LatestExchangeRates",
                column: "SnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "LatestExchangeRates");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ExchangeRateSnapshots");
        }
    }
}
