using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaContextoFonteEDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contexto",
                table: "fontes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Não classificado");

            migrationBuilder.AddColumn<string>(
                name: "Contexto",
                table: "documentos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Não classificado");

            migrationBuilder.Sql("ALTER TABLE fontes ALTER COLUMN \"Contexto\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE documentos ALTER COLUMN \"Contexto\" DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "IX_fontes_Contexto",
                table: "fontes",
                column: "Contexto");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_Contexto",
                table: "documentos",
                column: "Contexto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fontes_Contexto",
                table: "fontes");

            migrationBuilder.DropIndex(
                name: "IX_documentos_Contexto",
                table: "documentos");

            migrationBuilder.DropColumn(
                name: "Contexto",
                table: "fontes");

            migrationBuilder.DropColumn(
                name: "Contexto",
                table: "documentos");
        }
    }
}
