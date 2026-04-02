using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceProof.Infrastructure.Persistence.Migrations
{
    public partial class AddMerchantRiskScoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentRiskScore",
                table: "merchants",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRiskLabel",
                table: "merchants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RiskUpdatedUtc",
                table: "merchants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentRiskScore",
                table: "branches",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRiskLabel",
                table: "branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RiskUpdatedUtc",
                table: "branches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "branch_risk_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalCases = table.Column<int>(type: "integer", nullable: false),
                    AnalyzedCases = table.Column<int>(type: "integer", nullable: false),
                    LikelyCardSurchargeCases = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceWeightedMismatchTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RecencyWeightedCaseCount = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    DismissedEquivalentRatio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    UnclearCaseRatio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Label = table.Column<int>(type: "integer", nullable: false),
                    CalculatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TriggeredByCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_risk_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branch_risk_snapshots_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "merchant_risk_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalCases = table.Column<int>(type: "integer", nullable: false),
                    AnalyzedCases = table.Column<int>(type: "integer", nullable: false),
                    LikelyCardSurchargeCases = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceWeightedMismatchTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RecencyWeightedCaseCount = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    DismissedEquivalentRatio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    UnclearCaseRatio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Label = table.Column<int>(type: "integer", nullable: false),
                    CalculatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TriggeredByCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_risk_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_merchant_risk_snapshots_merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "IsAdmin",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_risk_snapshots_BranchId_CalculatedUtc",
                table: "branch_risk_snapshots",
                columns: new[] { "BranchId", "CalculatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_merchant_risk_snapshots_MerchantId_CalculatedUtc",
                table: "merchant_risk_snapshots",
                columns: new[] { "MerchantId", "CalculatedUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_risk_snapshots");

            migrationBuilder.DropTable(
                name: "merchant_risk_snapshots");

            migrationBuilder.DropColumn(
                name: "CurrentRiskScore",
                table: "merchants");

            migrationBuilder.DropColumn(
                name: "CurrentRiskLabel",
                table: "merchants");

            migrationBuilder.DropColumn(
                name: "RiskUpdatedUtc",
                table: "merchants");

            migrationBuilder.DropColumn(
                name: "CurrentRiskScore",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "CurrentRiskLabel",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "RiskUpdatedUtc",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "users");
        }
    }
}
