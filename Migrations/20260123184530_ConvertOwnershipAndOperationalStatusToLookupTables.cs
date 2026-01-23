using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SMHFR_BE.Migrations
{
    /// <inheritdoc />
    public partial class ConvertOwnershipAndOperationalStatusToLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create lookup tables first
            migrationBuilder.CreateTable(
                name: "OperationalStatuses",
                columns: table => new
                {
                    OperationalStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatusName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalStatuses", x => x.OperationalStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Ownerships",
                columns: table => new
                {
                    OwnershipId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnershipType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ownerships", x => x.OwnershipId);
                });

            // Step 2: Create unique indexes first (needed for ON CONFLICT)
            migrationBuilder.CreateIndex(
                name: "IX_OperationalStatuses_StatusName",
                table: "OperationalStatuses",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ownerships_OwnershipType",
                table: "Ownerships",
                column: "OwnershipType",
                unique: true);

            // Step 3: Populate lookup tables with unique values from existing data
            migrationBuilder.Sql(@"
                INSERT INTO ""OperationalStatuses"" (""StatusName"", ""CreatedAt"")
                SELECT DISTINCT ""OperationalStatus"", NOW()
                FROM ""HealthFacilities""
                WHERE ""OperationalStatus"" IS NOT NULL AND ""OperationalStatus"" != ''
                ON CONFLICT (""StatusName"") DO NOTHING;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""Ownerships"" (""OwnershipType"", ""CreatedAt"")
                SELECT DISTINCT ""Ownership"", NOW()
                FROM ""HealthFacilities""
                WHERE ""Ownership"" IS NOT NULL AND ""Ownership"" != ''
                ON CONFLICT (""OwnershipType"") DO NOTHING;
            ");

            // Step 4: Add new foreign key columns (nullable initially)
            migrationBuilder.AddColumn<int>(
                name: "OperationalStatusId",
                table: "HealthFacilities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipId",
                table: "HealthFacilities",
                type: "integer",
                nullable: true);

            // Step 5: Populate foreign key columns with IDs from lookup tables
            migrationBuilder.Sql(@"
                UPDATE ""HealthFacilities"" hf
                SET ""OperationalStatusId"" = os.""OperationalStatusId""
                FROM ""OperationalStatuses"" os
                WHERE hf.""OperationalStatus"" = os.""StatusName"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""HealthFacilities"" hf
                SET ""OwnershipId"" = o.""OwnershipId""
                FROM ""Ownerships"" o
                WHERE hf.""Ownership"" = o.""OwnershipType"";
            ");

            // Step 6: Make foreign key columns required (set default for any nulls)
            migrationBuilder.Sql(@"
                UPDATE ""HealthFacilities""
                SET ""OperationalStatusId"" = (SELECT ""OperationalStatusId"" FROM ""OperationalStatuses"" ORDER BY ""OperationalStatusId"" LIMIT 1)
                WHERE ""OperationalStatusId"" IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""HealthFacilities""
                SET ""OwnershipId"" = (SELECT ""OwnershipId"" FROM ""Ownerships"" ORDER BY ""OwnershipId"" LIMIT 1)
                WHERE ""OwnershipId"" IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "OperationalStatusId",
                table: "HealthFacilities",
                type: "integer",
                nullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "OwnershipId",
                table: "HealthFacilities",
                type: "integer",
                nullable: false);

            // Step 7: Drop old indexes and columns
            migrationBuilder.DropIndex(
                name: "IX_HealthFacilities_OperationalStatus",
                table: "HealthFacilities");

            migrationBuilder.DropIndex(
                name: "IX_HealthFacilities_Ownership",
                table: "HealthFacilities");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                table: "HealthFacilities");

            migrationBuilder.DropColumn(
                name: "Ownership",
                table: "HealthFacilities");

            // Step 8: Create indexes for foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_OperationalStatusId",
                table: "HealthFacilities",
                column: "OperationalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_OwnershipId",
                table: "HealthFacilities",
                column: "OwnershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_HealthFacilities_OperationalStatuses_OperationalStatusId",
                table: "HealthFacilities",
                column: "OperationalStatusId",
                principalTable: "OperationalStatuses",
                principalColumn: "OperationalStatusId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HealthFacilities_Ownerships_OwnershipId",
                table: "HealthFacilities",
                column: "OwnershipId",
                principalTable: "Ownerships",
                principalColumn: "OwnershipId",
                onDelete: ReferentialAction.Cascade);

            // Step 9: Update StateCode length (unrelated but in migration)
            migrationBuilder.AlterColumn<string>(
                name: "StateCode",
                table: "States",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HealthFacilities_OperationalStatuses_OperationalStatusId",
                table: "HealthFacilities");

            migrationBuilder.DropForeignKey(
                name: "FK_HealthFacilities_Ownerships_OwnershipId",
                table: "HealthFacilities");

            migrationBuilder.DropTable(
                name: "OperationalStatuses");

            migrationBuilder.DropTable(
                name: "Ownerships");

            migrationBuilder.DropIndex(
                name: "IX_HealthFacilities_OperationalStatusId",
                table: "HealthFacilities");

            migrationBuilder.DropIndex(
                name: "IX_HealthFacilities_OwnershipId",
                table: "HealthFacilities");

            migrationBuilder.DropColumn(
                name: "OperationalStatusId",
                table: "HealthFacilities");

            migrationBuilder.DropColumn(
                name: "OwnershipId",
                table: "HealthFacilities");

            migrationBuilder.AlterColumn<string>(
                name: "StateCode",
                table: "States",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "OperationalStatus",
                table: "HealthFacilities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ownership",
                table: "HealthFacilities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_OperationalStatus",
                table: "HealthFacilities",
                column: "OperationalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_Ownership",
                table: "HealthFacilities",
                column: "Ownership");
        }
    }
}
