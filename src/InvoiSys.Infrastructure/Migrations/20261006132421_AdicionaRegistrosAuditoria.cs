using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaRegistrosAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registros_auditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Atividade = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Objeto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ObjetoId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Responsavel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EnderecoIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_auditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_registros_auditoria_OcorridoEm",
                table: "registros_auditoria",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_registros_auditoria_Tipo",
                table: "registros_auditoria",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_registros_auditoria_UsuarioId",
                table: "registros_auditoria",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registros_auditoria");
        }
    }
}
