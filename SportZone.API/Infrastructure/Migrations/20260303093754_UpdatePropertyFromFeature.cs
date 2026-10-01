using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePropertyFromFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "Features",
                newName: "ImgUrl");

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "Features",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Desc",
                table: "Features");

            migrationBuilder.RenameColumn(
                name: "ImgUrl",
                table: "Features",
                newName: "Value");
        }
    }
}
