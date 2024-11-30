using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EffiSense.Data.Migrations
{
    /// <inheritdoc />
    public partial class newFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfAppliances",
                table: "Homes");

            migrationBuilder.RenameColumn(
                name: "EnergyConsumption",
                table: "Appliances",
                newName: "PowerRating");

            migrationBuilder.AddColumn<string>(
                name: "BuildingType",
                table: "Homes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "InsulationLevel",
                table: "Homes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Homes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingType",
                table: "Homes");

            migrationBuilder.DropColumn(
                name: "InsulationLevel",
                table: "Homes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Homes");

            migrationBuilder.RenameColumn(
                name: "PowerRating",
                table: "Appliances",
                newName: "EnergyConsumption");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfAppliances",
                table: "Homes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
