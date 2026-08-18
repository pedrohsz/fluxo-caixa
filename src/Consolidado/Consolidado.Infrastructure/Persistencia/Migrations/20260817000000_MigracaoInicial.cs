using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consolidado.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class MigracaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saldos_diarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalCreditos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDebitos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantidadeCreditos = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeDebitos = table.Column<int>(type: "integer", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saldos_diarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saldos_diarios_ContaId_Data",
                table: "saldos_diarios",
                columns: new[] { "ContaId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saldos_diarios");
        }
    }
}
