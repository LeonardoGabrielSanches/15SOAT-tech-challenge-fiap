using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oficina.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ConclusaoDoDiagnosticoEOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ordens_de_servico_versoes",
                columns: table => new
                {
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MotivoDaRecusa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_de_servico_versoes", x => new { x.OrdemDeServicoId, x.Numero });
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_versoes_ordens_de_servico_OrdemDeServicoId",
                        column: x => x.OrdemDeServicoId,
                        principalTable: "ordens_de_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordens_de_servico_versoes_itens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersaoNumero = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_de_servico_versoes_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_versoes_itens_ordens_de_servico_versoes_O~",
                        columns: x => new { x.OrdemDeServicoId, x.VersaoNumero },
                        principalTable: "ordens_de_servico_versoes",
                        principalColumns: new[] { "OrdemDeServicoId", "Numero" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_versoes_Token",
                table: "ordens_de_servico_versoes",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_versoes_itens_OrdemDeServicoId_VersaoNume~",
                table: "ordens_de_servico_versoes_itens",
                columns: new[] { "OrdemDeServicoId", "VersaoNumero" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordens_de_servico_versoes_itens");

            migrationBuilder.DropTable(
                name: "ordens_de_servico_versoes");
        }
    }
}
