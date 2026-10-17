using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oficina.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AberturaDeOrdemDeServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ordens_de_servico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AbertaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_de_servico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sequencias_anuais_de_os",
                columns: table => new
                {
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    Ultimo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sequencias_anuais_de_os", x => x.Ano);
                });

            migrationBuilder.CreateTable(
                name: "ordens_de_servico_historico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Origem = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Destino = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Momento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Autor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_de_servico_historico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_historico_ordens_de_servico_OrdemDeServic~",
                        column: x => x.OrdemDeServicoId,
                        principalTable: "ordens_de_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordens_de_servico_servicos",
                columns: table => new
                {
                    ServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_de_servico_servicos", x => new { x.OrdemDeServicoId, x.ServicoId });
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_servicos_ordens_de_servico_OrdemDeServico~",
                        column: x => x.OrdemDeServicoId,
                        principalTable: "ordens_de_servico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ordens_de_servico_servicos_servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_ClienteId",
                table: "ordens_de_servico",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_Numero",
                table: "ordens_de_servico",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ordens_de_servico_veiculo_em_andamento",
                table: "ordens_de_servico",
                column: "VeiculoId",
                unique: true,
                filter: "status NOT IN ('Entregue', 'Cancelada')");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_historico_OrdemDeServicoId",
                table: "ordens_de_servico_historico",
                column: "OrdemDeServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_de_servico_servicos_ServicoId",
                table: "ordens_de_servico_servicos",
                column: "ServicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ordens_de_servico_historico");

            migrationBuilder.DropTable(
                name: "ordens_de_servico_servicos");

            migrationBuilder.DropTable(
                name: "sequencias_anuais_de_os");

            migrationBuilder.DropTable(
                name: "ordens_de_servico");
        }
    }
}
