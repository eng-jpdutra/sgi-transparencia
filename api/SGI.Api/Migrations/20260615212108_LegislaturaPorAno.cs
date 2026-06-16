using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGI.Api.Migrations
{
    /// <inheritdoc />
    public partial class LegislaturaPorAno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Legislaturas_Nome",
                table: "Legislaturas");

            migrationBuilder.DropColumn(
                name: "DataFim",
                table: "Legislaturas");

            migrationBuilder.DropColumn(
                name: "DataInicio",
                table: "Legislaturas");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Legislaturas");

            migrationBuilder.AddColumn<int>(
                name: "AnoInicio",
                table: "Legislaturas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Numero",
                table: "Legislaturas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Legislaturas_AnoInicio",
                table: "Legislaturas",
                column: "AnoInicio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Legislaturas_Numero",
                table: "Legislaturas",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Legislaturas_AnoInicio",
                table: "Legislaturas");

            migrationBuilder.DropIndex(
                name: "IX_Legislaturas_Numero",
                table: "Legislaturas");

            migrationBuilder.DropColumn(
                name: "AnoInicio",
                table: "Legislaturas");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Legislaturas");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataFim",
                table: "Legislaturas",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataInicio",
                table: "Legislaturas",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Legislaturas",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Legislaturas_Nome",
                table: "Legislaturas",
                column: "Nome",
                unique: true);
        }
    }
}
