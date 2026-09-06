using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class EcheqSinBancoCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoBanco",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "NumeroCuenta",
                table: "echeqs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoBanco",
                table: "echeqs",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroCuenta",
                table: "echeqs",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");
        }
    }
}
