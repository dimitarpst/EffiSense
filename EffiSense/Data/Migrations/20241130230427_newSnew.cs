using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EffiSense.Data.Migrations
{
    /// <inheritdoc />
    public partial class newSnew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Appliances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Appliances",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
