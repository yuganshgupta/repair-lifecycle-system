using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessActivityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "BusinessActivityEvents",
                schema: "dbo",
                columns: table => new
                {
                    BusinessActivityEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    TargetUserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ComplaintId = table.Column<int>(type: "int", nullable: true),
                    TicketNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessActivityEvents", x => x.BusinessActivityEventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessActivityEvents_ActorUserId_OccurredAtUtc",
                schema: "dbo",
                table: "BusinessActivityEvents",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessActivityEvents_ComplaintId_OccurredAtUtc",
                schema: "dbo",
                table: "BusinessActivityEvents",
                columns: new[] { "ComplaintId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessActivityEvents_CustomerId_OccurredAtUtc",
                schema: "dbo",
                table: "BusinessActivityEvents",
                columns: new[] { "CustomerId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessActivityEvents_EventType_OccurredAtUtc",
                schema: "dbo",
                table: "BusinessActivityEvents",
                columns: new[] { "EventType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessActivityEvents_OccurredAtUtc",
                schema: "dbo",
                table: "BusinessActivityEvents",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessActivityEvents",
                schema: "dbo");
        }
    }
}
