using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class fixDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e9b846ed-6e4b-4895-a805-d6e0c3cb436b", "AQAAAAIAAYagAAAAEKHuMlEcEes96yDcglMSYFJNkcK9Sl4beTY+HErhVdfUu3LyWcBsvKEWNb8dou4MvA==", "56299696-6088-43da-827d-092592078696" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-id",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f0a0c8c4-d97f-400c-8e2a-c62073a430c7", "AQAAAAIAAYagAAAAEI/S/WiFN4vIH7tAschccmmrRf66qjowQOY3PgCtPN0nbNr9jkOg2S7KEHnvnd+wrA==", "f7ed35ae-78d7-4b6c-8103-4a0ce032552f" });
        }
    }
}
