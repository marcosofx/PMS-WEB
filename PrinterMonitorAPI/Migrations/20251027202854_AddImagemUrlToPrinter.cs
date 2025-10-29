using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrinterMonitorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddImagemUrlToPrinter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Impressoras",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Impressoras");
        }
    }
}
