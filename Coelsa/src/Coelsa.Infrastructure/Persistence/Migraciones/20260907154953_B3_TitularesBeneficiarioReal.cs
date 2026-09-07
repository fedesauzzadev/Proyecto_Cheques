using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class B3_TitularesBeneficiarioReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreBeneficiario",
                table: "echeqs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreLibrador",
                table: "echeqs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoDocBeneficiario",
                table: "echeqs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "NombreTitular",
                table: "cuentas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            // Backfill de filas pre-B3: tipo CUIT y nombres marcados (el seed
            // solo siembra cuando las tablas están vacías, así que no pisa nada).
            migrationBuilder.Sql("UPDATE \"echeqs\" SET \"TipoDocBeneficiario\" = 1 WHERE \"TipoDocBeneficiario\" = 0;");
            migrationBuilder.Sql("UPDATE \"echeqs\" SET \"NombreLibrador\" = '(sin dato)' WHERE \"NombreLibrador\" = '';");
            migrationBuilder.Sql("UPDATE \"echeqs\" SET \"NombreBeneficiario\" = '(sin dato)' WHERE \"NombreBeneficiario\" = '';");
            migrationBuilder.Sql("UPDATE \"cuentas\" SET \"NombreTitular\" = '(sin dato)' WHERE \"NombreTitular\" = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreBeneficiario",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "NombreLibrador",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "TipoDocBeneficiario",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "NombreTitular",
                table: "cuentas");
        }
    }
}
