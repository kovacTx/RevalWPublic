using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevalW.Services.OrganizationAPI.Migrations
{
    /// <inheritdoc />
    public partial class m2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewDeviceProperties_Organizations_OrganizationId",
                table: "NewDeviceProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_NewItemProperties_Organizations_OrganizationId",
                table: "NewItemProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_NewUserProperties_Organizations_OrganizationId",
                table: "NewUserProperties");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "NewUserProperties",
                newName: "ConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_NewUserProperties_OrganizationId",
                table: "NewUserProperties",
                newName: "IX_NewUserProperties_ConfigId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "NewItemProperties",
                newName: "ConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_NewItemProperties_OrganizationId",
                table: "NewItemProperties",
                newName: "IX_NewItemProperties_ConfigId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "NewDeviceProperties",
                newName: "ConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_NewDeviceProperties_OrganizationId",
                table: "NewDeviceProperties",
                newName: "IX_NewDeviceProperties_ConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewDeviceProperties_Organizations_ConfigId",
                table: "NewDeviceProperties",
                column: "ConfigId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewItemProperties_Organizations_ConfigId",
                table: "NewItemProperties",
                column: "ConfigId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewUserProperties_Organizations_ConfigId",
                table: "NewUserProperties",
                column: "ConfigId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewDeviceProperties_Organizations_ConfigId",
                table: "NewDeviceProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_NewItemProperties_Organizations_ConfigId",
                table: "NewItemProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_NewUserProperties_Organizations_ConfigId",
                table: "NewUserProperties");

            migrationBuilder.RenameColumn(
                name: "ConfigId",
                table: "NewUserProperties",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_NewUserProperties_ConfigId",
                table: "NewUserProperties",
                newName: "IX_NewUserProperties_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "ConfigId",
                table: "NewItemProperties",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_NewItemProperties_ConfigId",
                table: "NewItemProperties",
                newName: "IX_NewItemProperties_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "ConfigId",
                table: "NewDeviceProperties",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_NewDeviceProperties_ConfigId",
                table: "NewDeviceProperties",
                newName: "IX_NewDeviceProperties_OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewDeviceProperties_Organizations_OrganizationId",
                table: "NewDeviceProperties",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewItemProperties_Organizations_OrganizationId",
                table: "NewItemProperties",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewUserProperties_Organizations_OrganizationId",
                table: "NewUserProperties",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
