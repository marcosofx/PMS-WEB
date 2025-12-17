using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrinterMonitorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMarcaToPrinter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Impressoras",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Impressoras");
        }
    }
}
