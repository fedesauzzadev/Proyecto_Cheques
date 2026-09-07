using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class B1_CuentasChequerasEcheqCbu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los echeqs viejos no tienen CBU emisor ni chequera (formato pre-B1):
            // se eliminan y el seed los regenera con cuenta + número reservado
            // (precedente: migración EcheqCmc7Idecheq11).
            migrationBuilder.Sql("DELETE FROM \"echeqs\";");

            migrationBuilder.AddColumn<string>(
                name: "CbuEmisor",
                table: "echeqs",
                type: "character varying(22)",
                maxLength: 22,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumeroCheque",
                table: "echeqs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumeroChequera",
                table: "echeqs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "chequeras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    ProximoNumero = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHabilitacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chequeras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cuentas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Cbu = table.Column<string>(type: "character varying(22)", maxLength: 22, nullable: false),
                    CuitTitular = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Moneda = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_echeqs_cbu_chequera_numero",
                table: "echeqs",
                columns: new[] { "CbuEmisor", "NumeroChequera", "NumeroCheque" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chequeras_cuenta_con_lugar",
                table: "chequeras",
                columns: new[] { "CuentaId", "Estado", "ProximoNumero" });

            migrationBuilder.CreateIndex(
                name: "IX_chequeras_CuentaId_Numero",
                table: "chequeras",
                columns: new[] { "CuentaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_Cbu",
                table: "cuentas",
                column: "Cbu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_cuit_titular",
                table: "cuentas",
                columns: new[] { "CuitTitular", "Activa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chequeras");

            migrationBuilder.DropTable(
                name: "cuentas");

            migrationBuilder.DropIndex(
                name: "ix_echeqs_cbu_chequera_numero",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "CbuEmisor",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "NumeroCheque",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "NumeroChequera",
                table: "echeqs");
        }
    }
}
