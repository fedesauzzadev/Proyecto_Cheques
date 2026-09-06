using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class InstrumentosFechaVencimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_echeqs_cuit_librador_activos",
                table: "echeqs");

            migrationBuilder.DropIndex(
                name: "ix_cheques_cuit_librador_activos",
                table: "cheques_fisicos");

            // Las filas existentes se rellenan con max(emisión, diferimiento) + 30 días,
            // que cumple la regla de dominio (vencimiento posterior a ambas).
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaVencimiento",
                table: "echeqs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaVencimiento",
                table: "cheques_fisicos",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"echeqs\" SET \"FechaVencimiento\" = " +
                "(GREATEST(\"FechaEmision\", COALESCE(\"FechaDiferimiento\", \"FechaEmision\")) + INTERVAL '30 days')::date " +
                "WHERE \"FechaVencimiento\" IS NULL;");

            migrationBuilder.Sql(
                "UPDATE \"cheques_fisicos\" SET \"FechaVencimiento\" = " +
                "(GREATEST(\"FechaEmision\", COALESCE(\"FechaDiferimiento\", \"FechaEmision\")) + INTERVAL '30 days')::date " +
                "WHERE \"FechaVencimiento\" IS NULL;");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaVencimiento",
                table: "echeqs",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaVencimiento",
                table: "cheques_fisicos",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_echeqs_cuit_librador_activos",
                table: "echeqs",
                columns: new[] { "CuitLibrador", "FechaCreacion" },
                filter: "\"Activo\" = true")
                .Annotation("Npgsql:IndexInclude", new[] { "IdEcheq", "CuitBeneficiario", "Monto", "Moneda", "FechaEmision", "FechaDiferimiento", "FechaVencimiento", "Estado", "MotivoRechazo" });

            migrationBuilder.CreateIndex(
                name: "ix_cheques_cuit_librador_activos",
                table: "cheques_fisicos",
                columns: new[] { "CuitLibrador", "FechaCreacion" },
                filter: "\"Activo\" = true")
                .Annotation("Npgsql:IndexInclude", new[] { "Cmc7", "CuitBeneficiario", "Monto", "Moneda", "FechaEmision", "FechaDiferimiento", "FechaVencimiento", "Estado", "MotivoRechazo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_echeqs_cuit_librador_activos",
                table: "echeqs");

            migrationBuilder.DropIndex(
                name: "ix_cheques_cuit_librador_activos",
                table: "cheques_fisicos");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "cheques_fisicos");

            migrationBuilder.CreateIndex(
                name: "ix_echeqs_cuit_librador_activos",
                table: "echeqs",
                columns: new[] { "CuitLibrador", "FechaCreacion" },
                filter: "\"Activo\" = true")
                .Annotation("Npgsql:IndexInclude", new[] { "IdEcheq", "CuitBeneficiario", "Monto", "Moneda", "FechaEmision", "FechaDiferimiento", "Estado", "MotivoRechazo" });

            migrationBuilder.CreateIndex(
                name: "ix_cheques_cuit_librador_activos",
                table: "cheques_fisicos",
                columns: new[] { "CuitLibrador", "FechaCreacion" },
                filter: "\"Activo\" = true")
                .Annotation("Npgsql:IndexInclude", new[] { "Cmc7", "CuitBeneficiario", "Monto", "Moneda", "FechaEmision", "FechaDiferimiento", "Estado", "MotivoRechazo" });
        }
    }
}
