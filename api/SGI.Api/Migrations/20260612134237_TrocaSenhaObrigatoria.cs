using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGI.Api.Migrations
{
    /// <inheritdoc />
    public partial class TrocaSenhaObrigatoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeveTrocarSenha",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeveTrocarSenha",
                table: "Usuarios");
        }
    }
}
