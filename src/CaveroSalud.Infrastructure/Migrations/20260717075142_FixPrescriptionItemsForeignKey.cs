using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaveroSalud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPrescriptionItemsForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionId1",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_PrescriptionId1",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "PrescriptionId1",
                table: "PrescriptionItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionId1",
                table: "PrescriptionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId1",
                table: "PrescriptionItems",
                column: "PrescriptionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionId1",
                table: "PrescriptionItems",
                column: "PrescriptionId1",
                principalTable: "Prescriptions",
                principalColumn: "Id");
        }
    }
}
