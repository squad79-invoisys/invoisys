using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFonteColetaLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fontes_coleta_lease",
                columns: table => new
                {
                    fonte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proprietario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    geracao = table.Column<long>(type: "bigint", nullable: false),
                    valido_ate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fontes_coleta_lease", x => x.fonte_id);
                    table.ForeignKey(
                        name: "FK_fontes_coleta_lease_fontes_fonte_id",
                        column: x => x.fonte_id,
                        principalTable: "fontes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fontes_coleta_lease");
        }
    }
}
