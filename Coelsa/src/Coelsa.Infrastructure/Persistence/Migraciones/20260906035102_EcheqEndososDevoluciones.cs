using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class EcheqEndososDevoluciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "devoluciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EcheqId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    CuitSolicitante = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devoluciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "endosos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EcheqId = table.Column<Guid>(type: "uuid", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    CuitEndosante = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CuitEndosatario = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endosos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_devoluciones_echeq_numero_unicos",
                table: "devoluciones",
                columns: new[] { "EcheqId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_endosos_echeq_orden_unicos",
                table: "endosos",
                columns: new[] { "EcheqId", "Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devoluciones");

            migrationBuilder.DropTable(
                name: "endosos");
        }
    }
}
