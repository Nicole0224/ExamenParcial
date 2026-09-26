using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIncidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Estacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Prioridad = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "Abierta"),
                    FechaReporte = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidencias", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidencias");
        }
    }
}
