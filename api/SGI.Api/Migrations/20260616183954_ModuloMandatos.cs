using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGI.Api.Migrations
{
    /// <inheritdoc />
    public partial class ModuloMandatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mandatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VereadorId = table.Column<int>(type: "INTEGER", nullable: false),
                    LegislaturaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mandatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mandatos_Legislaturas_LegislaturaId",
                        column: x => x.LegislaturaId,
                        principalTable: "Legislaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mandatos_Vereadores_VereadorId",
                        column: x => x.VereadorId,
                        principalTable: "Vereadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mandatos_LegislaturaId",
                table: "Mandatos",
                column: "LegislaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mandatos_VereadorId",
                table: "Mandatos",
                column: "VereadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mandatos");
        }
    }
}
