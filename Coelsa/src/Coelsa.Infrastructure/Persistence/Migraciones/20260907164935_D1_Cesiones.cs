using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coelsa.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class D1_Cesiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cesiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EcheqId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    CuitCedente = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CuitCesionario = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    DomicilioCesionario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cesiones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cesiones_echeq_numero_unicos",
                table: "cesiones",
                columns: new[] { "EcheqId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cesiones");
        }
    }
}
