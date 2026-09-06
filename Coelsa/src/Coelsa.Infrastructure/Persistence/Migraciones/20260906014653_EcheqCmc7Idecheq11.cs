using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class EcheqCmc7Idecheq11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los echeqs demo con formato viejo (IDECHEQ de 18 + CUD, sin CMC7) se
            // eliminan: el seed los regenera con IDECHEQ de 11 letras + CMC7
            // (InicializacionBaseDeDatos siembra cada tipo por separado).
            migrationBuilder.Sql("DELETE FROM \"echeqs\";");

            migrationBuilder.DropIndex(
                name: "IX_echeqs_Cud",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "Cud",
                table: "echeqs");

            migrationBuilder.AlterColumn<string>(
                name: "IdEcheq",
                table: "echeqs",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(18)",
                oldMaxLength: 18);

            migrationBuilder.AddColumn<string>(
                name: "Cmc7",
                table: "echeqs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_echeqs_Cmc7",
                table: "echeqs",
                column: "Cmc7",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_echeqs_Cmc7",
                table: "echeqs");

            migrationBuilder.DropColumn(
                name: "Cmc7",
                table: "echeqs");

            migrationBuilder.AlterColumn<string>(
                name: "IdEcheq",
                table: "echeqs",
                type: "character varying(18)",
                maxLength: 18,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<string>(
                name: "Cud",
                table: "echeqs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_echeqs_Cud",
                table: "echeqs",
                column: "Cud",
                unique: true);
        }
    }
}
