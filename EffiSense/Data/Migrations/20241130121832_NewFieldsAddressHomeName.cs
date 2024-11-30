using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EffiSense.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewFieldsAddressHomeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Homes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseName",
                table: "Homes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Homes");

            migrationBuilder.DropColumn(
                name: "HouseName",
                table: "Homes");
        }
    }
}
