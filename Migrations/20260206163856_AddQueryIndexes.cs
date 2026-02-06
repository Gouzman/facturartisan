using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacturArtisan.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Factures_CreatedAt",
                table: "Factures",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Numero",
                table: "Factures",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Statut",
                table: "Factures",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_CreatedAt",
                table: "Devis",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Factures_CreatedAt",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Factures_Numero",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Factures_Statut",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Devis_CreatedAt",
                table: "Devis");
        }
    }
}
