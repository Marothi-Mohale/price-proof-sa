using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceProof.Infrastructure.Persistence.Migrations
{
    public partial class AddReceiptOcrState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OcrConfidence",
                table: "receipt_records",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrLineItemsJson",
                table: "receipt_records",
                type: "character varying(16000)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrPayloadMetadataJson",
                table: "receipt_records",
                type: "character varying(32000)",
                maxLength: 32000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OcrProcessedUtc",
                table: "receipt_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrProviderName",
                table: "receipt_records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TransactionAtUtc",
                table: "receipt_records",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrConfidence",
                table: "receipt_records");

            migrationBuilder.DropColumn(
                name: "OcrLineItemsJson",
                table: "receipt_records");

            migrationBuilder.DropColumn(
                name: "OcrPayloadMetadataJson",
                table: "receipt_records");

            migrationBuilder.DropColumn(
                name: "OcrProcessedUtc",
                table: "receipt_records");

            migrationBuilder.DropColumn(
                name: "OcrProviderName",
                table: "receipt_records");

            migrationBuilder.DropColumn(
                name: "TransactionAtUtc",
                table: "receipt_records");
        }
    }
}
