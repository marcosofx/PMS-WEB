using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrinterMonitorAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Impressoras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ip = table.Column<string>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    NomeCustomizado = table.Column<string>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Modelo = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroSerie = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    imagemUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ContadorTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Toners = table.Column<string>(type: "TEXT", nullable: false),
                    Bandejas = table.Column<string>(type: "TEXT", nullable: false),
                    Alertas = table.Column<string>(type: "TEXT", nullable: false),
                    EColorida = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impressoras", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Impressoras");
        }
    }
}
