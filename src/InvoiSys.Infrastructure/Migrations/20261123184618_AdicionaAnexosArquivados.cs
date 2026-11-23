using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAnexosArquivados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anexos_arquivados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    HashConteudo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChaveObjeto = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TipoConteudo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeArquivo = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    Tamanho = table.Column<long>(type: "bigint", nullable: false),
                    ArquivadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexos_arquivados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexos_arquivados_HashConteudo",
                table: "anexos_arquivados",
                column: "HashConteudo");

            migrationBuilder.CreateIndex(
                name: "IX_anexos_arquivados_Url",
                table: "anexos_arquivados",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anexos_arquivados");
        }
    }
}
