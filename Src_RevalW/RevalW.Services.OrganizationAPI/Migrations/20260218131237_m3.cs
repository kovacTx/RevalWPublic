using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevalW.Services.OrganizationAPI.Migrations
{
    /// <inheritdoc />
    public partial class m3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CoutryRegion",
                table: "Organizations",
                newName: "CountryRegion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CountryRegion",
                table: "Organizations",
                newName: "CoutryRegion");
        }
    }
}
