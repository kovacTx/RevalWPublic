using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevalW.Services.TemplatesAPI.Migrations
{
    /// <inheritdoc />
    public partial class m2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewItemProperties_Configs_OrganizationId",
                table: "NewItemProperties");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "NewItemProperties",
                newName: "ConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_NewItemProperties_OrganizationId",
                table: "NewItemProperties",
                newName: "IX_NewItemProperties_ConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewItemProperties_Configs_ConfigId",
                table: "NewItemProperties",
                column: "ConfigId",
                principalTable: "Configs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewItemProperties_Configs_ConfigId",
                table: "NewItemProperties");

            migrationBuilder.RenameColumn(
                name: "ConfigId",
                table: "NewItemProperties",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_NewItemProperties_ConfigId",
                table: "NewItemProperties",
                newName: "IX_NewItemProperties_OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewItemProperties_Configs_OrganizationId",
                table: "NewItemProperties",
                column: "OrganizationId",
                principalTable: "Configs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
