using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SMHFR_BE.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacilityTypes",
                columns: table => new
                {
                    FacilityTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityTypes", x => x.FacilityTypeId);
                });

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

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    StateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.StateId);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    RegionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    RegionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.RegionId);
                    table.ForeignKey(
                        name: "FK_Regions_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    DistrictId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    DistrictName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.DistrictId);
                    table.ForeignKey(
                        name: "FK_Districts_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "RegionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthFacilities",
                columns: table => new
                {
                    HealthFacilityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacilityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HealthFacilityName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    FacilityTypeId = table.Column<int>(type: "integer", nullable: false),
                    OwnershipId = table.Column<int>(type: "integer", nullable: false),
                    OperationalStatusId = table.Column<int>(type: "integer", nullable: false),
                    HCPartners = table.Column<string>(type: "text", nullable: true),
                    HCProjectEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NutritionClusterPartners = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DamalCaafimaadPartner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DamalCaafimaadProjectEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BetterLifeProjectPartner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BetterLifeProjectEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CaafimaadPlusPartner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CaafimaadPlusProjectEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FacilityInChargeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FacilityInChargeNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthFacilities", x => x.HealthFacilityId);
                    table.ForeignKey(
                        name: "FK_HealthFacilities_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "DistrictId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthFacilities_FacilityTypes_FacilityTypeId",
                        column: x => x.FacilityTypeId,
                        principalTable: "FacilityTypes",
                        principalColumn: "FacilityTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthFacilities_OperationalStatuses_OperationalStatusId",
                        column: x => x.OperationalStatusId,
                        principalTable: "OperationalStatuses",
                        principalColumn: "OperationalStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthFacilities_Ownerships_OwnershipId",
                        column: x => x.OwnershipId,
                        principalTable: "Ownerships",
                        principalColumn: "OwnershipId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Districts_RegionId_DistrictName",
                table: "Districts",
                columns: new[] { "RegionId", "DistrictName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityTypes_TypeName",
                table: "FacilityTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_DistrictId",
                table: "HealthFacilities",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_FacilityId",
                table: "HealthFacilities",
                column: "FacilityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_FacilityTypeId",
                table: "HealthFacilities",
                column: "FacilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_Latitude_Longitude",
                table: "HealthFacilities",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_OperationalStatusId",
                table: "HealthFacilities",
                column: "OperationalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacilities_OwnershipId",
                table: "HealthFacilities",
                column: "OwnershipId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Regions_StateId_RegionName",
                table: "Regions",
                columns: new[] { "StateId", "RegionName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_StateCode",
                table: "States",
                column: "StateCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthFacilities");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "FacilityTypes");

            migrationBuilder.DropTable(
                name: "OperationalStatuses");

            migrationBuilder.DropTable(
                name: "Ownerships");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "States");
        }
    }
}
