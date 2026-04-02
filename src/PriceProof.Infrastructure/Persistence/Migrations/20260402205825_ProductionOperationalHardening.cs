using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PriceProof.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductionOperationalHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerificationSentUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerificationTokenExpiresUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationTokenHash",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerifiedUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedSignInCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastFailedSignInUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPasswordChangedUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEndsUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordResetSentUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordResetTokenExpiresUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Xml = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_protection_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stored_binary_objects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Bucket = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stored_binary_objects", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "EmailVerificationSentUtc", "EmailVerificationTokenExpiresUtc", "EmailVerificationTokenHash", "EmailVerifiedUtc", "FailedSignInCount", "IsEmailVerified", "LastFailedSignInUtc", "LastPasswordChangedUtc", "LockoutEndsUtc", "PasswordResetSentUtc", "PasswordResetTokenExpiresUtc", "PasswordResetTokenHash" },
                values: new object[] { null, null, null, new DateTimeOffset(new DateTime(2025, 1, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, true, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "EmailVerificationSentUtc", "EmailVerificationTokenExpiresUtc", "EmailVerificationTokenHash", "EmailVerifiedUtc", "FailedSignInCount", "IsEmailVerified", "LastFailedSignInUtc", "LastPasswordChangedUtc", "LockoutEndsUtc", "PasswordResetSentUtc", "PasswordResetTokenExpiresUtc", "PasswordResetTokenHash" },
                values: new object[] { null, null, null, new DateTimeOffset(new DateTime(2025, 1, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, true, null, null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_data_protection_keys_FriendlyName",
                table: "data_protection_keys",
                column: "FriendlyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stored_binary_objects_Bucket_CaseId_CreatedUtc",
                table: "stored_binary_objects",
                columns: new[] { "Bucket", "CaseId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stored_binary_objects_StorageKey",
                table: "stored_binary_objects",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "stored_binary_objects");

            migrationBuilder.DropColumn(
                name: "EmailVerificationSentUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiresUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FailedSignInCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastFailedSignInUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockoutEndsUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetSentUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "users");
        }
    }
}
