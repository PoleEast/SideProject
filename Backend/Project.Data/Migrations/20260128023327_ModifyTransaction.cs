using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockPriceHistories_Exchange_Code_Date",
                table: "StockPriceHistories");

            migrationBuilder.RenameColumn(
                name: "Exchange",
                table: "Transactions",
                newName: "StockMarket");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "Transactions",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "ExchangeRateHistories",
                newName: "Currency");

            migrationBuilder.RenameIndex(
                name: "IX_ExchangeRateHistories_CurrencyCode_Date",
                table: "ExchangeRateHistories",
                newName: "IX_ExchangeRateHistories_Currency_Date");

            migrationBuilder.AlterColumn<string>(
                name: "Exchange",
                table: "StockPriceHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "StockMarket",
                table: "StockPriceHistories",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StockPriceHistories_StockMarket_Code_Date",
                table: "StockPriceHistories",
                columns: new[] { "StockMarket", "Code", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockPriceHistories_StockMarket_Code_Date",
                table: "StockPriceHistories");

            migrationBuilder.DropColumn(
                name: "StockMarket",
                table: "StockPriceHistories");

            migrationBuilder.RenameColumn(
                name: "StockMarket",
                table: "Transactions",
                newName: "Exchange");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "Transactions",
                newName: "CurrencyCode");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "ExchangeRateHistories",
                newName: "CurrencyCode");

            migrationBuilder.RenameIndex(
                name: "IX_ExchangeRateHistories_Currency_Date",
                table: "ExchangeRateHistories",
                newName: "IX_ExchangeRateHistories_CurrencyCode_Date");

            migrationBuilder.AlterColumn<string>(
                name: "Exchange",
                table: "StockPriceHistories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_StockPriceHistories_Exchange_Code_Date",
                table: "StockPriceHistories",
                columns: new[] { "Exchange", "Code", "Date" },
                unique: true);
        }
    }
}
