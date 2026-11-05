using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RevalW.Services.OrganizationAPI.Migrations
{
    /// <inheritdoc />
    public partial class m1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: false),
                    Zip = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    CoutryRegion = table.Column<string>(type: "text", nullable: false),
                    UserIds = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceGridColumns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Prefix = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceGridColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceGridColumns_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemGridColumns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Prefix = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGridColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemGridColumns_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewDeviceProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Readonly = table.Column<bool>(type: "boolean", nullable: false),
                    InputType = table.Column<string>(type: "text", nullable: true),
                    Min = table.Column<string>(type: "text", nullable: true),
                    Max = table.Column<string>(type: "text", nullable: true),
                    Step = table.Column<string>(type: "text", nullable: true),
                    ComboOptions = table.Column<string>(type: "text", nullable: true),
                    Default = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewDeviceProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewDeviceProperties_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewItemProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Readonly = table.Column<bool>(type: "boolean", nullable: false),
                    InputType = table.Column<string>(type: "text", nullable: true),
                    Min = table.Column<string>(type: "text", nullable: true),
                    Max = table.Column<string>(type: "text", nullable: true),
                    Step = table.Column<string>(type: "text", nullable: true),
                    ComboOptions = table.Column<string>(type: "text", nullable: true),
                    Default = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewItemProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewItemProperties_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewUserProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Readonly = table.Column<bool>(type: "boolean", nullable: false),
                    InputType = table.Column<string>(type: "text", nullable: true),
                    Min = table.Column<string>(type: "text", nullable: true),
                    Max = table.Column<string>(type: "text", nullable: true),
                    Step = table.Column<string>(type: "text", nullable: true),
                    ComboOptions = table.Column<string>(type: "text", nullable: true),
                    Default = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewUserProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewUserProperties_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Organization2Tag",
                columns: table => new
                {
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization2Tag", x => new { x.OrganizationId, x.TagId });
                    table.ForeignKey(
                        name: "FK_Organization2Tag_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Organization2Tag_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceGridColumns_OrganizationId",
                table: "DeviceGridColumns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGridColumns_OrganizationId",
                table: "ItemGridColumns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NewDeviceProperties_OrganizationId",
                table: "NewDeviceProperties",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NewItemProperties_OrganizationId",
                table: "NewItemProperties",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NewUserProperties_OrganizationId",
                table: "NewUserProperties",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization2Tag_TagId",
                table: "Organization2Tag",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceGridColumns");

            migrationBuilder.DropTable(
                name: "ItemGridColumns");

            migrationBuilder.DropTable(
                name: "NewDeviceProperties");

            migrationBuilder.DropTable(
                name: "NewItemProperties");

            migrationBuilder.DropTable(
                name: "NewUserProperties");

            migrationBuilder.DropTable(
                name: "Organization2Tag");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
