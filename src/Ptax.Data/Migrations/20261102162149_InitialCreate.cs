using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ptax.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_rates",
                columns: table => new
                {
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    buy_rate = table.Column<decimal>(type: "numeric(18,5)", precision: 18, scale: 5, nullable: false),
                    sell_rate = table.Column<decimal>(type: "numeric(18,5)", precision: 18, scale: 5, nullable: false),
                    buy_parity = table.Column<decimal>(type: "numeric(18,5)", precision: 18, scale: 5, nullable: false),
                    sell_parity = table.Column<decimal>(type: "numeric(18,5)", precision: 18, scale: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_rates", x => new { x.currency, x.date });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_rates");
        }
    }
}
