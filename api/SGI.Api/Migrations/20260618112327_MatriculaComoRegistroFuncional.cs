using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGI.Api.Migrations
{
    /// <inheritdoc />
    public partial class MatriculaComoRegistroFuncional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pessoas_Matricula",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "Matricula",
                table: "Pessoas");

            migrationBuilder.AddColumn<int>(
                name: "MatriculaId",
                table: "Vinculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Pessoas",
                type: "TEXT",
                maxLength: 11,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MatriculaId",
                table: "Mandatos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Matriculas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matriculas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vinculos_MatriculaId",
                table: "Vinculos",
                column: "MatriculaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_Cpf",
                table: "Pessoas",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mandatos_MatriculaId",
                table: "Mandatos",
                column: "MatriculaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_Numero",
                table: "Matriculas",
                column: "Numero",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Mandatos_Matriculas_MatriculaId",
                table: "Mandatos",
                column: "MatriculaId",
                principalTable: "Matriculas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vinculos_Matriculas_MatriculaId",
                table: "Vinculos",
                column: "MatriculaId",
                principalTable: "Matriculas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mandatos_Matriculas_MatriculaId",
                table: "Mandatos");

            migrationBuilder.DropForeignKey(
                name: "FK_Vinculos_Matriculas_MatriculaId",
                table: "Vinculos");

            migrationBuilder.DropTable(
                name: "Matriculas");

            migrationBuilder.DropIndex(
                name: "IX_Vinculos_MatriculaId",
                table: "Vinculos");

            migrationBuilder.DropIndex(
                name: "IX_Pessoas_Cpf",
                table: "Pessoas");

            migrationBuilder.DropIndex(
                name: "IX_Mandatos_MatriculaId",
                table: "Mandatos");

            migrationBuilder.DropColumn(
                name: "MatriculaId",
                table: "Vinculos");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "MatriculaId",
                table: "Mandatos");

            migrationBuilder.AddColumn<string>(
                name: "Matricula",
                table: "Pessoas",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_Matricula",
                table: "Pessoas",
                column: "Matricula",
                unique: true);
        }
    }
}
