using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIndiceUnicoDocumentoUrlHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM documentos
                        GROUP BY "UrlOriginal", "Hash"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Não foi possível criar o índice único IX_documentos_UrlOriginal_Hash: existem documentos duplicados por UrlOriginal e Hash.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_documentos_UrlOriginal",
                table: "documentos");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_UrlOriginal_Hash",
                table: "documentos",
                columns: new[] { "UrlOriginal", "Hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_documentos_UrlOriginal_Hash",
                table: "documentos");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_UrlOriginal",
                table: "documentos",
                column: "UrlOriginal");
        }
    }
}
