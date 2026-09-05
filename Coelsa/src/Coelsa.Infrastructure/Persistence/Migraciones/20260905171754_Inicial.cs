using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cheques_fisicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Cmc7 = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CuitLibrador = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CuitBeneficiario = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaDiferimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    MotivoRechazo = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cheques_fisicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "echeqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdEcheq = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    Cud = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodigoBanco = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NumeroCuenta = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CuitLibrador = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CuitBeneficiario = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaDiferimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    MotivoRechazo = table.Column<int>(type: "integer", nullable: true),
                    CantidadEndosos = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_echeqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BodyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cheques_cuit_beneficiario_activos",
                table: "cheques_fisicos",
                columns: new[] { "CuitBeneficiario", "FechaCreacion" },
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_cheques_cuit_librador_activos",
                table: "cheques_fisicos",
                columns: new[] { "CuitLibrador", "FechaCreacion" },
                filter: "\"Activo\" = true")
                .Annotation("Npgsql:IndexInclude", new[] { "Cmc7", "CuitBeneficiario", "Monto", "Moneda", "FechaEmision", "FechaDiferimiento", "Estado", "MotivoRechazo" });

            migrationBuilder.CreateIndex(
                name: "IX_cheques_fisicos_Cmc7",
                table: "cheques_fisicos",
                column: "Cmc7",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_echeqs_Cud",
                table: "echeqs",
                column: "Cud",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_echeqs_cuit_beneficiario_activos",
                table: "echeqs",
                columns: new[] { "CuitBeneficiario", "FechaCreacion" },
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_echeqs_cuit_librador_activos",
                table: "echeqs",
                columns: new[] { "CuitLibrador", "FechaCreacion" },
                filter: "\"Activo\" = true")
                .Annotation("Npgsql:IndexInclude", new[] { "IdEcheq", "CuitBeneficiario", "Monto", "Moneda", "FechaEmision", "FechaDiferimiento", "Estado", "MotivoRechazo" });

            migrationBuilder.CreateIndex(
                name: "IX_echeqs_IdEcheq",
                table: "echeqs",
                column: "IdEcheq",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cheques_fisicos");

            migrationBuilder.DropTable(
                name: "echeqs");

            migrationBuilder.DropTable(
                name: "idempotency_keys");
        }
    }
}
