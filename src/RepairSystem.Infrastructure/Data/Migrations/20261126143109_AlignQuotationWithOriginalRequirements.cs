using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignQuotationWithOriginalRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintDiagnoses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Quotes_EstimatedTotalCost",
                table: "Quotes");

            migrationBuilder.AddColumn<string>(
                name: "DiagnosticFindings",
                table: "Quotes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quotes_EstimatedTotalCost",
                table: "Quotes",
                sql: "[EstimatedTotalCost] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quotes_WarrantyCharges",
                table: "Quotes",
                sql: "([IsWarrantyValid] = 1 AND [InspectionFee] = 0 AND [EstimatedTotalCost] = 0) OR ([IsWarrantyValid] = 0 AND [InspectionFee] > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Quotes_EstimatedTotalCost",
                table: "Quotes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Quotes_WarrantyCharges",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "DiagnosticFindings",
                table: "Quotes");

            migrationBuilder.CreateTable(
                name: "ComplaintDiagnoses",
                columns: table => new
                {
                    ComplaintDiagnosisId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedRepairDays = table.Column<int>(type: "int", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IdentifiedFault = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RecommendedRepair = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintDiagnoses", x => x.ComplaintDiagnosisId);
                    table.CheckConstraint("CK_ComplaintDiagnoses_EstimatedRepairDays", "[EstimatedRepairDays] >= 0 AND [EstimatedRepairDays] <= 365");
                    table.ForeignKey(
                        name: "FK_ComplaintDiagnoses_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "ComplaintId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplaintDiagnoses_Users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quotes_EstimatedTotalCost",
                table: "Quotes",
                sql: "[EstimatedTotalCost] >= [InspectionFee]");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintDiagnoses_ComplaintId",
                table: "ComplaintDiagnoses",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintDiagnoses_TechnicianId",
                table: "ComplaintDiagnoses",
                column: "TechnicianId");
        }
    }
}
