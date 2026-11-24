using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaControleAgendamentoEFalhasFonte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AgendamentoPausado",
                table: "fontes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FalhasConsecutivas",
                table: "fontes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgendamentoPausado",
                table: "fontes");

            migrationBuilder.DropColumn(
                name: "FalhasConsecutivas",
                table: "fontes");
        }
    }
}
