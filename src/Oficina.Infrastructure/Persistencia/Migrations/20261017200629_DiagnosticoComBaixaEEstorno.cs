using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class DiagnosticoComBaixaEEstorno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoDaUltimaRecusa",
                table: "ordens_de_servico",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ordens_de_servico_itens",
                columns: table => new
                {
                    PecaInsumoId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_de_servico_itens", x => new { x.OrdemDeServicoId, x.PecaInsumoId });
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_itens_ordens_de_servico_OrdemDeServicoId",
                        column: x => x.OrdemDeServicoId,
                        principalTable: "ordens_de_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_itens_pecas_insumos_PecaInsumoId",
                        column: x => x.PecaInsumoId,
                        principalTable: "pecas_insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_itens_PecaInsumoId",
                table: "ordens_de_servico_itens",
                column: "PecaInsumoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordens_de_servico_itens");

            migrationBuilder.DropColumn(
                name: "MotivoDaUltimaRecusa",
                table: "ordens_de_servico");
        }
    }
}
