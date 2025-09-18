using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MottuVision.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patio",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("patio_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_grupo",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("status_grupo_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    usuario = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    senha = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("usuario_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "zona",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    letra = table.Column<string>(type: "NVARCHAR2(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("zona_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    nome = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    status_grupo_id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("status_pk", x => x.id);
                    table.ForeignKey(
                        name: "status_fk",
                        column: x => x.status_grupo_id,
                        principalTable: "status_grupo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moto",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    placa = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    chassi = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    qr_code = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: true),
                    data_entrada = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    previsao_entrega = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    fotos = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: true),
                    zona_id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    patio_id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    status_id = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    observacoes = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("moto_pk", x => x.id);
                    table.ForeignKey(
                        name: "moto_patio_fk",
                        column: x => x.patio_id,
                        principalTable: "patio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "moto_status_fk",
                        column: x => x.status_id,
                        principalTable: "status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "moto_zona_fk",
                        column: x => x.zona_id,
                        principalTable: "zona",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moto_patio_id",
                table: "moto",
                column: "patio_id");

            migrationBuilder.CreateIndex(
                name: "IX_moto_status_id",
                table: "moto",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_moto_zona_id",
                table: "moto",
                column: "zona_id");

            migrationBuilder.CreateIndex(
                name: "moto_chassi_uk",
                table: "moto",
                column: "chassi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "moto_placa_uk",
                table: "moto",
                column: "placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_status_grupo_id",
                table: "status",
                column: "status_grupo_id");

            migrationBuilder.CreateIndex(
                name: "usuario_usuario_uk",
                table: "usuario",
                column: "usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moto");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "patio");

            migrationBuilder.DropTable(
                name: "status");

            migrationBuilder.DropTable(
                name: "zona");

            migrationBuilder.DropTable(
                name: "status_grupo");
        }
    }
}
