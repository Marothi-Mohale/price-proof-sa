using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceProof.Infrastructure.Persistence.Migrations
{
    public partial class AddCaseAnalysisState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalysisClassification",
                table: "cases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnalysisConfidence",
                table: "cases",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisExplanation",
                table: "cases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnalysisUpdatedUtc",
                table: "cases",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisClassification",
                table: "cases");

            migrationBuilder.DropColumn(
                name: "AnalysisConfidence",
                table: "cases");

            migrationBuilder.DropColumn(
                name: "AnalysisExplanation",
                table: "cases");

            migrationBuilder.DropColumn(
                name: "AnalysisUpdatedUtc",
                table: "cases");
        }
    }
}
